using LazyBlox.Localization;
using System.Diagnostics;
using System.Text;

namespace LazyBlox;

public sealed record InstallOptions(
    /// <summary>Register the exe where it is instead of copying to %LOCALAPPDATA%\Programs\LazyBlox.</summary>
    bool InPlace = false,
    /// <summary>Optional browser override: ProgID, StartMenuInternet name, or absolute exe path.</summary>
    string? Browser = null);

/// <summary>Install / uninstall / status orchestration. Registry writes happen only from here, and only on explicit request.</summary>
public sealed class Installer(WindowsRegistry registry, Action<string> log)
{
    public string ResolveTargetExe(InstallOptions o) =>
        o.InPlace ? (AppInfo.CurrentExePath ?? throw new InvalidOperationException("Cannot determine the executable path")) : AppInfo.InstalledExePath;

    /// <summary>Preview text: what install would change. Nothing is written.</summary>
    public string DescribeInstall(InstallOptions o)
    {
        string exe = ResolveTargetExe(o);
        AppConfig cfg = PrepareConfig(AppConfig.Load(), o, out string browserNote);
        var sb = new StringBuilder();
        sb.AppendLine(L.S.PlanInstallHeader);
        sb.AppendLine();
        if (!o.InPlace && !BrowserCommand.PathsEqual(AppInfo.CurrentExePath, exe))
            sb.AppendLine(L.F(L.S.PlanCopy, AppInfo.CurrentExePath, exe)).AppendLine();
        sb.AppendLine(L.S.PlanRegistry);
        sb.Append(RegistrationPlan.Describe(RegistrationPlan.BuildInstall(exe)));
        sb.AppendLine();
        sb.AppendLine(L.F(L.S.PlanConfig, AppInfo.ConfigPath));
        sb.AppendLine(L.F(L.S.PlanBrowser, cfg.BrowserName, browserNote));
        sb.AppendLine(L.F(L.S.PlanCommand, cfg.BrowserCommand));
        sb.AppendLine();
        sb.AppendLine(L.S.PlanUnchanged);
        sb.AppendLine(L.S.PlanSwitchYourself);
        return sb.ToString();
    }

    public string DescribeUninstall()
    {
        var sb = new StringBuilder();
        sb.AppendLine(L.S.PlanUninstallHeader);
        sb.AppendLine();
        sb.AppendLine(L.S.PlanUninstallRegistry);
        sb.Append(RegistrationPlan.Describe(RegistrationPlan.BuildUninstall()));
        sb.AppendLine();
        sb.AppendLine(L.F(L.S.PlanDeleteConfigDir, AppInfo.DataDirectory));
        sb.AppendLine(L.F(L.S.PlanDeleteInstallDir, AppInfo.InstallDirectory));
        sb.AppendLine();
        sb.AppendLine(L.S.PlanUninstallUnchanged);
        return sb.ToString();
    }

    public bool Install(InstallOptions o)
    {
        string exe = ResolveTargetExe(o);
        AppConfig cfg = PrepareConfig(AppConfig.Load(), o, out string note);
        if (string.IsNullOrWhiteSpace(cfg.BrowserCommand))
        {
            log(L.S.LogNoBrowser);
            return false;
        }

        if (!o.InPlace) CopySelf(exe);

        RegistrationPlan.Apply(registry, RegistrationPlan.BuildInstall(exe));
        if (registry.CurrentUserPrefix.Length == 0)
            Native.SHChangeNotify(Native.SHCNE_ASSOCCHANGED, Native.SHCNF_IDLIST, 0, 0);
        cfg.Save();

        log(L.F(L.S.LogBrowser, cfg.BrowserName, note));
        List<string> problems = RegistrationPlan.VerifyInstalled(registry, exe);
        if (problems.Count > 0)
        {
            log(L.S.LogVerifyProblems);
            foreach (string p in problems) log("  " + p);
            return false;
        }
        log(L.S.LogVerifyOk);
        log(L.S.LogNextStep);
        return true;
    }

    public bool Uninstall(bool keepConfig)
    {
        AppConfig cfg = AppConfig.Load();
        string? httpsChoice = BrowserLocator.GetUserChoiceProgId(registry, "https");

        RegistrationPlan.Apply(registry, RegistrationPlan.BuildUninstall());
        if (registry.CurrentUserPrefix.Length == 0)
            Native.SHChangeNotify(Native.SHCNE_ASSOCCHANGED, Native.SHCNF_IDLIST, 0, 0);

        List<string> left = RegistrationPlan.VerifyRemoved(registry);
        foreach (string l in left) log("  " + l);
        log(left.Count == 0 ? L.S.LogRemovedClean : L.S.LogRemovedPartial);

        if (!keepConfig) TryDeleteDirectory(AppInfo.DataDirectory);
        if (registry.CurrentUserPrefix.Length == 0) RemoveInstalledFiles(); // never touch real files from a sandboxed (test) registry

        if (httpsChoice == AppInfo.ProgId)
        {
            string original = string.IsNullOrEmpty(cfg.BrowserName) ? cfg.OriginalHttpsProgId : cfg.BrowserName;
            log(L.F(L.S.LogStillDefault, original));
        }
        return left.Count == 0;
    }

    public InstallStatus GetStatus()
    {
        string? registeredCmd = registry.GetString(RegHive.CurrentUser, RegistrationPlan.ProgIdKey + @"\shell\open\command", null);
        string? exe = null;
        List<string> problems = [];
        if (registeredCmd is not null)
        {
            List<string> t = BrowserCommand.Tokenize(registeredCmd);
            exe = t.Count > 0 ? t[0] : null;
            problems = exe is null ? [L.S.LogBadCommand] : RegistrationPlan.VerifyInstalled(registry, exe);
        }
        AppConfig cfg = AppConfig.Load();
        return new InstallStatus(
            registeredCmd is not null, problems, exe,
            BrowserLocator.GetUserChoiceProgId(registry, "https"),
            BrowserLocator.GetUserChoiceProgId(registry, "http"),
            cfg.BrowserName, cfg.BrowserCommand,
            WindowsShell.Instance.IsProtocolRegistered("roblox"),
            cfg.LegacyPrivateServerLinks);
    }

    public string Status()
    {
        InstallStatus s = GetStatus();
        var sb = new StringBuilder();
        sb.AppendLine(L.F(L.S.StRegistered, s.Registered ? L.S.StYes : L.S.StNo));
        if (s.Registered)
        {
            sb.AppendLine(L.F(L.S.StVerify, s.Problems.Count == 0 ? "OK" : string.Join(" / ", s.Problems)));
            sb.AppendLine(L.F(L.S.StExe, s.RegisteredExe));
        }
        sb.AppendLine(L.F(L.S.StHttps, s.HttpsProgId ?? L.S.NotSet) + (s.HttpsIsLazyBlox ? L.S.StInUse : ""));
        sb.AppendLine(L.F(L.S.StHttp, s.HttpProgId ?? L.S.NotSet) + (s.HttpIsLazyBlox ? L.S.StInUse : ""));
        sb.AppendLine(L.F(L.S.StBrowser, string.IsNullOrEmpty(s.BrowserName) ? L.S.NotSet : s.BrowserName));
        sb.AppendLine(L.F(L.S.StCommand, s.BrowserCommand));
        sb.AppendLine(L.F(L.S.StRoblox, s.RobloxRegistered ? L.S.StRobloxFound : L.S.StRobloxMissing));
        return sb.ToString();
    }

    /// <summary>Fills browser fields: explicit override &gt; existing valid config &gt; detected default.</summary>
    public AppConfig PrepareConfig(AppConfig cfg, InstallOptions o, out string note)
    {
        string? https = BrowserLocator.GetUserChoiceProgId(registry, "https");
        string? http = BrowserLocator.GetUserChoiceProgId(registry, "http");
        if (https is not null && https != AppInfo.ProgId) cfg.OriginalHttpsProgId = https;
        if (http is not null && http != AppInfo.ProgId) cfg.OriginalHttpProgId = http;

        List<BrowserCandidate> all = BrowserLocator.Enumerate(registry);
        if (!string.IsNullOrWhiteSpace(o.Browser))
        {
            BrowserCandidate? pick = FindBrowser(all, o.Browser!);
            if (pick is null) { note = L.F(L.S.NoteSpecifiedNotFound, o.Browser); return cfg; }
            Assign(cfg, pick);
            note = L.S.NoteSpecified;
            return cfg;
        }

        if (!string.IsNullOrWhiteSpace(cfg.BrowserCommand)
            && BrowserCommand.TryBuild(cfg.BrowserCommand, "https://example.com/", AppInfo.CurrentExePath, out ProcessInvocation? inv, out _)
            && File.Exists(inv.FileName))
        {
            note = L.S.NoteKeepExisting;
            return cfg;
        }

        BrowserCandidate? detected = BrowserLocator.DetectDefault(registry, all);
        if (detected is null) { note = L.S.NoteNotDetected; return cfg; }
        Assign(cfg, detected);
        note = L.S.NoteDetected;
        return cfg;
    }

    /// <summary>Matches by ProgID, display name, StartMenuInternet name or absolute exe path.</summary>
    public static BrowserCandidate? FindBrowser(IReadOnlyList<BrowserCandidate> all, string spec)
    {
        BrowserCandidate? hit = all.FirstOrDefault(c =>
            string.Equals(c.ProgId, spec, StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.Name, spec, StringComparison.OrdinalIgnoreCase)
            || string.Equals(c.RegisteredAppName, spec, StringComparison.OrdinalIgnoreCase));
        if (hit is not null) return hit;
        if (Path.IsPathFullyQualified(spec) && File.Exists(spec)
            && !BrowserCommand.IsForbidden(spec, AppInfo.CurrentExePath, out _))
            return new BrowserCandidate(Path.GetFileNameWithoutExtension(spec), null, $"\"{spec}\" \"%1\"", null);
        return null;
    }

    public static void Assign(AppConfig cfg, BrowserCandidate c)
    {
        cfg.BrowserName = c.Name;
        cfg.BrowserProgId = c.ProgId ?? "";
        cfg.BrowserCommand = c.Command;
    }

    private void CopySelf(string target)
    {
        string? self = AppInfo.CurrentExePath;
        if (self is null || BrowserCommand.PathsEqual(self, target)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(self, target, overwrite: true);
        log(L.F(L.S.LogCopied, target));
    }

    private void RemoveInstalledFiles()
    {
        string exe = AppInfo.InstalledExePath;
        if (!File.Exists(exe)) return;
        if (!BrowserCommand.PathsEqual(AppInfo.CurrentExePath, exe))
        {
            try { File.Delete(exe); TryDeleteDirectory(AppInfo.InstallDirectory); log(L.F(L.S.LogDeleted, AppInfo.InstallDirectory)); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { log(L.F(L.S.LogDeleteFailed, exe, e.Message)); }
            return;
        }
        // We are the installed exe and cannot delete ourselves while running: delete after exit.
        string dir = AppInfo.InstallDirectory;
        if (exe.AsSpan().IndexOfAny("\"%^!") >= 0)
        {
            log(L.F(L.S.LogCannotAutoDelete, dir));
            return;
        }
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"))
        {
            Arguments = $"/d /c ping -n 3 127.0.0.1 >nul & del /f /q \"{exe}\" & rmdir \"{dir}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        try { using Process? _ = Process.Start(psi); log(L.F(L.S.LogDeleteAfterExit, dir)); }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { log(L.F(L.S.LogDeleteManually, dir)); }
    }

    private void TryDeleteDirectory(string dir)
    {
        try { if (Directory.Exists(dir)) { Directory.Delete(dir, recursive: true); log(L.F(L.S.LogDeleted, dir)); } }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { log(L.F(L.S.LogDeleteFailed, dir, e.Message)); }
    }

    /// <summary>Opens Settings &gt; Default apps on LazyBlox's own page (Windows 11), falling back to the generic page.</summary>
    public static LaunchResult OpenDefaultAppsSettings(ISystemShell shell, string? registeredAppUser = AppInfo.AppName)
    {
        if (registeredAppUser is not null)
        {
            LaunchResult r = shell.OpenUri("ms-settings:defaultapps?registeredAppUser=" + Uri.EscapeDataString(registeredAppUser));
            if (r.Success) return r;
        }
        return shell.OpenUri("ms-settings:defaultapps");
    }
}

public sealed record InstallStatus(
    bool Registered, IReadOnlyList<string> Problems, string? RegisteredExe,
    string? HttpsProgId, string? HttpProgId,
    string BrowserName, string BrowserCommand,
    bool RobloxRegistered, bool LegacyEnabled)
{
    public bool HttpsIsLazyBlox => HttpsProgId == AppInfo.ProgId;
    public bool HttpIsLazyBlox => HttpProgId == AppInfo.ProgId;
    public bool Healthy => Registered && Problems.Count == 0;
}
