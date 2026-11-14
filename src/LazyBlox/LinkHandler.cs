using LazyBlox.Localization;
namespace LazyBlox;

public static class ExitCodes
{
    public const int Ok = 0;
    public const int Usage = 1;
    public const int Rejected = 2;
    public const int BrowserFailed = 3;
    public const int LoopDetected = 4;
    public const int Error = 5;
}

/// <summary>
/// The hot path: URL in -> Roblox deep link or browser forward out.
/// Roblox path: parse -> registry probe for roblox: -> ShellExecuteEx. No config file, no network.
/// </summary>
public sealed class LinkHandler
{
    private readonly ISystemShell _shell;
    private readonly IRegistryView _registry;
    private readonly Lazy<AppConfig> _config;
    private readonly string? _selfExe;

    public LinkHandler(ISystemShell shell, IRegistryView registry, Func<AppConfig> configLoader, string? selfExe)
    {
        _shell = shell;
        _registry = registry;
        _config = new Lazy<AppConfig>(configLoader, LazyThreadSafetyMode.None);
        _selfExe = selfExe;
    }

    /// <summary>Set once the launch request has been handed to Windows; for timing.</summary>
    public RouteDecision LastDecision { get; private set; }
    public bool ConfigWasLoaded => _config.IsValueCreated;

    public int Handle(string rawUrl)
    {
        RouteDecision decision = UrlRouter.Route(rawUrl, () => _config.Value.LegacyPrivateServerLinks);
        LastDecision = decision;

        switch (decision.Kind)
        {
            case RouteKind.Roblox:
                if (_shell.IsProtocolRegistered("roblox"))
                {
                    LaunchResult r = _shell.OpenUri(decision.Target);
                    if (r.Success) return ExitCodes.Ok;
                }
                // Roblox missing or deep link failed: let the website handle it (it offers install / join).
                return Forward(NormalizedOrRaw(rawUrl));

            case RouteKind.Browser:
                return Forward(decision.Target);

            default:
                _shell.ShowError(L.F(L.S.ErrCannotOpen, decision.Reason) + "\n\n" + Truncate(rawUrl));
                return ExitCodes.Rejected;
        }
    }

    private static string NormalizedOrRaw(string raw) =>
        UrlRouter.TryNormalize(raw, out Uri? uri, out _) ? uri.AbsoluteUri : raw;

    /// <summary>
    /// Forwards a validated http(s) URL to a real browser executable (never via the https association).
    /// Order: configured command -> configured ProgID's current command -> other registered browsers (optional).
    /// </summary>
    public int Forward(string url)
    {
        if (ForwardGuard.IsReentry(url))
        {
            _shell.ShowError(L.S.ErrLoop + "\n\n" + Truncate(url));
            return ExitCodes.LoopDetected;
        }

        AppConfig cfg = _config.Value;
        var errors = new List<string>();
        var tried = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string template in CandidateTemplates(cfg))
        {
            if (!tried.Add(template)) continue;
            if (!BrowserCommand.TryBuild(template, url, _selfExe, out ProcessInvocation? inv, out string? err))
            {
                errors.Add(err);
                continue;
            }
            LaunchResult r = _shell.StartProcess(inv, ForwardGuard.CreateEnvironment(url));
            if (r.Success) return ExitCodes.Ok;
            errors.Add(r.Error ?? "unknown error");
        }

        string detail = errors.Count == 0 ? L.S.ErrNoBrowser : string.Join("\n", errors.Distinct());
        _shell.ShowError(L.S.ErrBrowserFailed + "\n" + detail +
                         "\n\n" + L.S.ErrBrowserHint + "\n\nURL: " + Truncate(url));
        return ExitCodes.BrowserFailed;
    }

    private IEnumerable<string> CandidateTemplates(AppConfig cfg)
    {
        if (!string.IsNullOrWhiteSpace(cfg.BrowserCommand)) yield return cfg.BrowserCommand;
        if (!string.IsNullOrWhiteSpace(cfg.BrowserProgId) && cfg.BrowserProgId != AppInfo.ProgId)
        {
            string? cmd = BrowserLocator.GetProgIdCommand(_registry, cfg.BrowserProgId);
            if (cmd is not null) yield return cmd;
        }
        if (cfg.AutoDetectFallback)
        {
            List<BrowserCandidate> all = BrowserLocator.Enumerate(_registry);
            BrowserCandidate? preferred = BrowserLocator.DetectDefault(_registry, all);
            if (preferred is not null) yield return preferred.Command;
            foreach (BrowserCandidate c in all) yield return c.Command;
        }
    }

    private static string Truncate(string s) => s.Length <= 300 ? s : s[..300] + "…";
}

/// <summary>
/// Defense in depth against forward loops: the browser process receives an env marker
/// "&lt;unix-ms&gt;:&lt;hash(url)&gt;". If we are started again for the same URL within a few seconds while
/// carrying that marker, something re-dispatched the URL to us, so we stop instead of looping.
/// (Primary protection is that we only CreateProcess absolute browser executables, never ShellExecute https.)
/// </summary>
public static class ForwardGuard
{
    public static readonly TimeSpan Window = TimeSpan.FromSeconds(10);

    public static IReadOnlyDictionary<string, string> CreateEnvironment(string url, DateTimeOffset? now = null) =>
        new Dictionary<string, string> { [AppInfo.ForwardGuardEnvVar] = Format(url, now ?? DateTimeOffset.UtcNow) };

    public static string Format(string url, DateTimeOffset now) =>
        $"{now.ToUnixTimeMilliseconds()}:{Hash(url):x8}";

    /// <summary>Captured once, before this process sets the variable for its own children.</summary>
    private static readonly string? InheritedMarker = Environment.GetEnvironmentVariable(AppInfo.ForwardGuardEnvVar);

    public static bool IsReentry(string url, string? marker = null, DateTimeOffset? now = null)
    {
        marker ??= InheritedMarker;
        if (string.IsNullOrEmpty(marker)) return false;
        int colon = marker.IndexOf(':');
        if (colon <= 0 || !long.TryParse(marker.AsSpan(0, colon), out long ms)) return false;
        if (!uint.TryParse(marker.AsSpan(colon + 1), System.Globalization.NumberStyles.HexNumber, null, out uint hash)) return false;
        TimeSpan age = (now ?? DateTimeOffset.UtcNow) - DateTimeOffset.FromUnixTimeMilliseconds(ms);
        return hash == Hash(url) && age >= TimeSpan.Zero - TimeSpan.FromSeconds(1) && age <= Window;
    }

    /// <summary>FNV-1a; stable across processes (unlike string.GetHashCode).</summary>
    public static uint Hash(string s)
    {
        uint h = 2166136261;
        foreach (char c in s) { h ^= c; h *= 16777619; }
        return h;
    }
}
