using System.Diagnostics;
using System.Text;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LazyBlox.IntegrationTests;

/// <summary>
/// Runs the real LazyBlox.exe as a child process in an isolated data directory, with a fake browser.
/// Never passes Roblox share links to the real handler (that would start Roblox) – those go through dry-run only.
/// </summary>
internal sealed class Harness : IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "lazyblox-it-" + Guid.NewGuid().ToString("N"));
    public string Home => Path.Combine(Root, "home");
    public string BrowserOut => Path.Combine(Root, "browser-out");
    public static string LauncherExe => Path.Combine(AppContext.BaseDirectory, "LazyBlox.exe");
    public static string FakeBrowserExe => Path.Combine(AppContext.BaseDirectory, "FakeBrowser.exe");

    public Harness() => Directory.CreateDirectory(Home);

    public void WriteConfig(string browserCommand, bool autoDetect = false) =>
        new AppConfig { BrowserName = "Fake", BrowserCommand = browserCommand, AutoDetectFallback = autoDetect }
            .Save(Path.Combine(Home, "config.ini"));

    public void UseFakeBrowser(string template = "\"{0}\" --single-argument %1") =>
        WriteConfig(string.Format(template, FakeBrowserExe));

    public ProcessStartInfo StartInfo(params string[] args)
    {
        var psi = new ProcessStartInfo(LauncherExe)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true,
        };
        foreach (string a in args) psi.ArgumentList.Add(a);
        psi.Environment[AppInfo.HomeEnvVar] = Home;
        psi.Environment[AppInfo.SilentEnvVar] = "1";
        psi.Environment[LazyBlox.Localization.L.LangEnvVar] = "ja"; // assertions check Japanese messages
        psi.Environment["FAKEBROWSER_OUT"] = BrowserOut;
        psi.Environment.Remove(AppInfo.ForwardGuardEnvVar);
        return psi;
    }

    public (int Exit, string Out, string Err) Run(params string[] args) => Run(StartInfo(args));

    public static (int Exit, string Out, string Err) Run(ProcessStartInfo psi)
    {
        using Process p = Process.Start(psi)!;
        Task<string> o = p.StandardOutput.ReadToEndAsync();
        Task<string> e = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(30_000)) { p.Kill(true); throw new TimeoutException("launcher hung"); }
        return (p.ExitCode, o.Result, e.Result);
    }

    /// <summary>Waits for the fake browser outputs (it runs detached from the launcher).</summary>
    public List<(string Guard, List<string> Args)> BrowserInvocations(int expected, int timeoutMs = 15_000)
    {
        var sw = Stopwatch.StartNew();
        while (true)
        {
            string[] files = Directory.Exists(BrowserOut) ? Directory.GetFiles(BrowserOut, "*.txt") : [];
            if (files.Length >= expected || sw.ElapsedMilliseconds > timeoutMs)
            {
                Thread.Sleep(files.Length > expected ? 0 : 300); // allow late extra writes to show up
                files = Directory.Exists(BrowserOut) ? Directory.GetFiles(BrowserOut, "*.txt") : [];
                return files.Select(Parse).ToList();
            }
            Thread.Sleep(50);
        }
    }

    private static (string, List<string>) Parse(string file)
    {
        for (int i = 0; ; i++)
        {
            try
            {
                string[] lines = File.ReadAllLines(file);
                string guard = lines.First(l => l.StartsWith("guard:"))[6..];
                List<string> args = lines.Where(l => l.StartsWith("arg:"))
                    .Select(l => Encoding.UTF8.GetString(Convert.FromBase64String(l[4..]))).ToList();
                return (guard, args);
            }
            catch (IOException) when (i < 20) { Thread.Sleep(50); }
        }
    }

    public void Dispose()
    {
        try { Directory.Delete(Root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}

/// <summary>
/// Tests that talk to real external apps (Roblox client, the real browser). Skipped unless
/// LAZYBLOX_REAL_TESTS=1 is set, because they open windows / join servers on this machine.
/// </summary>
public sealed class RealExternalFactAttribute : FactAttribute
{
    public RealExternalFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("LAZYBLOX_REAL_TESTS") != "1")
            Skip = "Real external app test. Set LAZYBLOX_REAL_TESTS=1 to run (launches real apps).";
    }
}

internal static class TestLanguage
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void PinJapanese() => LazyBlox.Localization.L.Override("ja");
}
