using LazyBlox.Localization;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using LazyBlox.Ui;

namespace LazyBlox;

internal static class Program
{
    private static bool _hasConsole;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            // Hot path first: URLs (what Windows passes via "%1"). No console, no config unless needed.
            if (args.Length > 0 && args[0].Contains("://", StringComparison.Ordinal))
                return HandleUrls(args);

            if (args.Length == 0)
                return Gui.RunSettings();

            EnsureConsole();
            return RunCommand(args);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            Report(L.F(L.S.ErrorPrefix, e.Message));
            return ExitCodes.Error;
        }
    }

    private static int HandleUrls(string[] urls)
    {
        var handler = new LinkHandler(WindowsShell.Instance, WindowsRegistry.Instance, () => AppConfig.Load(), AppInfo.CurrentExePath);
        int exit = ExitCodes.Ok;
        // Normally exactly one URL. Several are processed in order (bounded), each independently.
        const int maxUrls = 16;
        for (int i = 0; i < urls.Length && i < maxUrls; i++)
        {
            int code = handler.Handle(urls[i]);
            if (code != ExitCodes.Ok) exit = code;
            if (i == 0) WriteTimingIfEnabled(handler.LastDecision);
        }
        if (urls.Length == 1 && handler.LastRobloxLaunch is { } launch) OfferRestartIfStuck(launch.DeepLink, launch.LaunchedUtc);
        return exit;
    }

    /// <summary>
    /// After the hand-off (so the launch itself is never delayed): if an already-running Roblox ignored the link,
    /// ask the user whether to restart Roblox into that server. Best-effort; its own watching never shows errors.
    /// </summary>
    private static void OfferRestartIfStuck(string deepLink, DateTime launchedUtc)
    {
        if (AppInfo.Silent) return;
        try
        {
            if (!AppConfig.Load().RestartIfStuck) return;
            StuckFallback.Run(deepLink, launchedUtc, WindowsRobloxHost.Instance, WindowsShell.Instance,
                () => Gui.Confirm(L.S.StuckTitle, L.S.StuckBody, null, L.S.BtnRestartRoblox, danger: true));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception
                                       or InvalidOperationException) { }
    }

    /// <summary>Timing log is checked AFTER the launch request, so it never delays it.</summary>
    private static void WriteTimingIfEnabled(RouteDecision d)
    {
        double ms = Native.MillisecondsSinceProcessCreation();
        try
        {
            if (!File.Exists(AppInfo.TimingFlagPath)) return;
            File.AppendAllText(AppInfo.TimingLogPath,
                string.Create(CultureInfo.InvariantCulture, $"{DateTime.Now:O}\t{d.Kind}\t{ms:F2}ms\n"));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { /* timing is best-effort */ }
    }

    // ------------------------------------------------------------------ CLI

    private static int RunCommand(string[] args)
    {
        string cmd = args[0].TrimStart('-', '/').ToLowerInvariant();
        var flags = new HashSet<string>(args.Skip(1).Where(a => a.StartsWith("--", StringComparison.Ordinal)), StringComparer.OrdinalIgnoreCase);
        List<string> positional = [.. args.Skip(1).Where(a => !a.StartsWith("--", StringComparison.Ordinal))];
        string? Option(string name)
        {
            for (int i = 1; i < args.Length - 1; i++)
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        var installer = new Installer(WindowsRegistry.Instance, Report);
        switch (cmd)
        {
            case "help" or "h" or "?":
                Report(L.S.Help);
                return ExitCodes.Ok;

            case "status":
                Report(installer.Status());
                return ExitCodes.Ok;

            case "plan":
                Report(positional.FirstOrDefault()?.Equals("uninstall", StringComparison.OrdinalIgnoreCase) == true
                    ? installer.DescribeUninstall()
                    : installer.DescribeInstall(new InstallOptions(flags.Contains("--in-place"), Option("--browser"))));
                return ExitCodes.Ok;

            case "install":
            {
                var o = new InstallOptions(flags.Contains("--in-place"), Option("--browser"));
                if (!Confirmed(flags, uninstall: false, installer.DescribeInstall(o))) return ExitCodes.Usage;
                bool ok = installer.Install(o);
                if (ok && !flags.Contains("--no-settings")) Installer.OpenDefaultAppsSettings(WindowsShell.Instance);
                return ok ? ExitCodes.Ok : ExitCodes.Error;
            }

            case "uninstall":
            {
                if (!Confirmed(flags, uninstall: true, installer.DescribeUninstall())) return ExitCodes.Usage;
                bool ok = installer.Uninstall(flags.Contains("--keep-config"));
                if (!_hasConsole && !flags.Contains("--quiet"))
                    Gui.Message(ok ? L.S.UninstalledTitle : L.S.UninstallPartialTitle,
                        L.S.AfterUninstallHint,
                        ok ? MessageKind.Success : MessageKind.Error);
                return ok ? ExitCodes.Ok : ExitCodes.Error;
            }

            case "verify":
            {
                string? cmdLine = WindowsRegistry.Instance.GetString(RegHive.CurrentUser, RegistrationPlan.ProgIdKey + @"\shell\open\command", null);
                if (cmdLine is null)
                {
                    List<string> left = RegistrationPlan.VerifyRemoved(WindowsRegistry.Instance);
                    Report(left.Count == 0 ? L.S.CliNotInstalledClean : string.Join("\n", left));
                    return left.Count == 0 ? ExitCodes.Ok : ExitCodes.Error;
                }
                string exe = BrowserCommand.Tokenize(cmdLine)[0];
                List<string> problems = RegistrationPlan.VerifyInstalled(WindowsRegistry.Instance, exe);
                Report(problems.Count == 0 ? L.F(L.S.CliInstalledOk, exe) : string.Join("\n", problems));
                return problems.Count == 0 ? ExitCodes.Ok : ExitCodes.Error;
            }

            case "browsers":
            {
                AppConfig cfg = AppConfig.Load();
                foreach (BrowserCandidate b in BrowserLocator.Enumerate(WindowsRegistry.Instance))
                    Report($"{(b.Command == cfg.BrowserCommand ? "*" : " ")} {b.Name}  [ProgId={b.ProgId}]\n    {b.Command}");
                return ExitCodes.Ok;
            }

            case "set-browser":
            {
                if (positional.Count == 0) { Report(L.S.CliSetBrowserUsage); return ExitCodes.Usage; }
                BrowserCandidate? pick = Installer.FindBrowser(BrowserLocator.Enumerate(WindowsRegistry.Instance), string.Join(' ', positional));
                if (pick is null) { Report(L.S.CliBrowserNotFound); return ExitCodes.Usage; }
                AppConfig cfg = AppConfig.Load();
                Installer.Assign(cfg, pick);
                cfg.Save();
                Report(L.F(L.S.CliBrowserSet, pick.Name, pick.Command));
                return ExitCodes.Ok;
            }

            case "default-apps":
                return Installer.OpenDefaultAppsSettings(WindowsShell.Instance).Success ? ExitCodes.Ok : ExitCodes.Error;

            case "dry-run":
                return DryRun(positional);

            case "bench":
                return Bench(positional);

            case "timing":
            case "legacy":
            {
                bool on = positional.FirstOrDefault()?.Equals("on", StringComparison.OrdinalIgnoreCase) == true;
                if (cmd == "timing")
                {
                    Directory.CreateDirectory(AppInfo.DataDirectory);
                    if (on) File.WriteAllText(AppInfo.TimingFlagPath, "");
                    else File.Delete(AppInfo.TimingFlagPath);
                    Report(L.F(L.S.CliTiming, on ? L.S.On : L.S.Off, AppInfo.TimingLogPath));
                }
                else
                {
                    AppConfig cfg = AppConfig.Load();
                    cfg.LegacyPrivateServerLinks = on;
                    cfg.Save();
                    Report(L.F(L.S.CliLegacy, on ? L.S.On : L.S.Off));
                }
                return ExitCodes.Ok;
            }

            default:
                Report(L.F(L.S.CliUnknownCommand, args[0]) + "\n\n" + L.S.Help);
                return ExitCodes.Usage;
        }
    }

    private static bool Confirmed(HashSet<string> flags, bool uninstall, string plan)
    {
        if (flags.Contains("--yes")) return true;
        if (_hasConsole)
        {
            Report(plan);
            Report(L.S.CliNeedYes);
            return false;
        }
        return Gui.Confirm(uninstall ? L.S.ConfirmUninstallTitle : L.S.ConfirmInstallTitle, L.S.ConfirmGenericBody, plan,
            uninstall ? L.S.BtnUninstall : L.S.BtnInstall, danger: uninstall);
    }

    /// <summary>Full routing + browser resolution, but nothing is launched. Prints elapsed time since process creation.</summary>
    private static int DryRun(List<string> positional)
    {
        if (positional.Count == 0) { Report(L.S.CliDryRunUsage); return ExitCodes.Usage; }
        var shell = new RecordingShell(WindowsShell.Instance);
        var handler = new LinkHandler(shell, WindowsRegistry.Instance, () => AppConfig.Load(), AppInfo.CurrentExePath);
        int code = handler.Handle(positional[0]);
        double sinceStart = Native.MillisecondsSinceProcessCreation();
        RouteDecision d = handler.LastDecision;
        var sb = new StringBuilder();
        sb.AppendLine($"decision : {d.Kind} ({d.Reason})");
        sb.AppendLine($"target   : {d.Target}");
        foreach (string u in shell.OpenedUris) sb.AppendLine($"would ShellExecute : {u}");
        foreach (ProcessInvocation p in shell.StartedProcesses) sb.AppendLine($"would CreateProcess: {p}");
        foreach (string e in shell.Errors) sb.AppendLine($"would show error   : {e.Replace('\n', ' ')}");
        sb.AppendLine($"config loaded      : {handler.ConfigWasLoaded}");
        sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"elapsed since process creation: {sinceStart:F2} ms"));
        sb.Append($"exit code: {code}");
        Report(sb.ToString());
        return code;
    }

    /// <summary>In-process micro benchmark of the routing step (no launch, no I/O).</summary>
    private static int Bench(List<string> positional)
    {
        string url = positional.FirstOrDefault() ?? "https://www.roblox.com/share?code=0123456789abcdef0123456789abcdef&type=Server";
        int n = positional.Count > 1 && int.TryParse(positional[1], out int parsed) && parsed > 0 ? parsed : 100_000;

        RouteDecision first = UrlRouter.Route(url); // first call (cold) measured separately
        long t0 = Stopwatch.GetTimestamp();
        RouteDecision cold = UrlRouter.Route(url + "#c");
        double coldUs = Stopwatch.GetElapsedTime(t0).TotalMicroseconds;

        var samples = new double[Math.Min(n, 1_000_000)];
        for (int i = 0; i < samples.Length; i++)
        {
            long s = Stopwatch.GetTimestamp();
            _ = UrlRouter.Route(url);
            samples[i] = Stopwatch.GetElapsedTime(s).TotalMicroseconds;
        }
        Array.Sort(samples);
        double mean = samples.Average();
        Report(string.Create(CultureInfo.InvariantCulture,
            $"url      : {url}\ndecision : {first.Kind} -> {first.Target}\niterations: {samples.Length}\n" +
            $"2nd call : {coldUs:F2} us ({cold.Kind})\nmean     : {mean:F3} us\np50      : {samples[samples.Length / 2]:F3} us\n" +
            $"p99      : {samples[(int)(samples.Length * 0.99)]:F3} us\nmax      : {samples[^1]:F3} us\n" +
            $"process age at end: {Native.MillisecondsSinceProcessCreation():F1} ms"));
        return ExitCodes.Ok;
    }

    private static void EnsureConsole()
    {
        // WinExe: when started from a terminal, borrow the parent's console for CLI output.
        // If stdout is already redirected (pipes, tests), keep it.
        if (Native.GetStdHandle(Native.STD_OUTPUT_HANDLE) != 0)
        {
            _hasConsole = true;
            Console.OutputEncoding = new UTF8Encoding(false); // pipes / redirection: always UTF-8
            return;
        }
        _hasConsole = Native.AttachConsole(Native.ATTACH_PARENT_PROCESS);
        if (_hasConsole)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            Console.WriteLine();
        }
    }

    private static void Report(string text)
    {
        if (_hasConsole || AppInfo.Silent) { Console.WriteLine(text); return; }
        Gui.Message(AppInfo.AppName, text, MessageKind.Info);
    }

}
