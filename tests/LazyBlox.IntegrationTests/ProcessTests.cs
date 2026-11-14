using System.Diagnostics;

namespace LazyBlox.IntegrationTests;

/// <summary>Runs the real LazyBlox.exe; the "browser" is FakeBrowser.exe. No real browser / Roblox is launched.</summary>
public class ProcessTests
{
    private const string Share = "https://www.roblox.com/share?code=0123456789abcdef0123456789abcdef&type=Server";

    [Fact] // 1: Roblox share link decision through the real exe (dry-run, nothing launched)
    public void DryRun_ShareLink_ProducesDeepLink_WithoutConfig()
    {
        using var h = new Harness();
        var (exit, output, _) = h.Run("dry-run", Share);
        Assert.Equal(0, exit);
        Assert.Contains("roblox://navigation/share_links?code=0123456789abcdef0123456789abcdef&type=Server", output);
        Assert.Contains("config loaded      : False", output);
    }

    [Theory] // 3 + 8: special characters reach the browser as exactly one, intact argument
    [InlineData("https://example.com/", "https://example.com/")]
    [InlineData("https://example.com/a b c", "https://example.com/a%20b%20c")]
    [InlineData("https://example.com/?q=\"x\" --incognito", "https://example.com/?q=%22x%22%20--incognito")]
    [InlineData("https://example.com/?a=1&b=2|3^4<5>6", "https://example.com/?a=1&b=2%7C3%5E4%3C5%3E6")]
    [InlineData("https://example.com/?p=%PATH%&e=!x!", "https://example.com/?p=%25PATH%25&e=!x!")]
    [InlineData("https://example.com/日本語?x=ä", "https://example.com/%E6%97%A5%E6%9C%AC%E8%AA%9E?x=%C3%A4")]
    [InlineData("https://www.roblox.com/games/920587237/Adopt-Me", "https://www.roblox.com/games/920587237/Adopt-Me")] // 2
    public void Forward_DeliversSingleIntactArgument(string url, string expected)
    {
        using var h = new Harness();
        h.UseFakeBrowser();
        var (exit, _, err) = h.Run(url);
        Assert.True(exit == 0, err);
        var inv = Assert.Single(h.BrowserInvocations(1));
        Assert.Equal(["--single-argument", expected], inv.Args);
        Assert.NotEmpty(inv.Guard); // loop-guard marker is passed to the browser
    }

    [Fact] // 4
    public void InvalidUrl_ExitsWithRejected_NoBrowser()
    {
        using var h = new Harness();
        h.UseFakeBrowser();
        var (exit, _, err) = h.Run("javascript://alert(1)");
        Assert.Equal(ExitCodes.Rejected, exit);
        Assert.Contains("開けません", err);
        Assert.Empty(h.BrowserInvocations(0, 500));
    }

    [Fact] // 5
    public void InvalidShareCode_ForwardsToBrowser()
    {
        using var h = new Harness();
        h.UseFakeBrowser();
        string url = "https://www.roblox.com/share?code=%3Cscript%3E&type=Server";
        Assert.Equal(0, h.Run(url).Exit);
        Assert.Equal(url, Assert.Single(h.BrowserInvocations(1)).Args[^1]);
    }

    [Fact] // 7
    public void BrowserNotFound_ReportsErrorAndExitCode()
    {
        using var h = new Harness();
        h.WriteConfig("\"C:\\does\\not\\exist\\browser.exe\" %1");
        var (exit, _, err) = h.Run("https://example.com/");
        Assert.Equal(ExitCodes.BrowserFailed, exit);
        Assert.Contains("ブラウザを起動できませんでした", err);
    }

    [Fact] // 9: configured "browser" is the launcher itself
    public void SelfAsBrowser_IsRefused_NoRecursion()
    {
        using var h = new Harness();
        h.WriteConfig($"\"{Harness.LauncherExe}\" \"%1\"");
        var sw = Stopwatch.StartNew();
        var (exit, _, err) = h.Run("https://example.com/");
        Assert.Equal(ExitCodes.BrowserFailed, exit);
        Assert.Contains("ループ", err);
        Assert.True(sw.ElapsedMilliseconds < 10_000);
    }

    [Fact] // 9: a renamed copy of the launcher configured as browser -> loop guard env stops the 2nd hop
    public void RenamedCopyAsBrowser_StopsAtSecondHop()
    {
        using var h = new Harness();
        string copyDir = Path.Combine(h.Root, "copy");
        Directory.CreateDirectory(copyDir);
        // The apphost embeds "LazyBlox.dll", so keep the managed files as-is and only rename the exe.
        foreach (string f in Directory.GetFiles(AppContext.BaseDirectory, "LazyBlox.*"))
            File.Copy(f, Path.Combine(copyDir, Path.GetFileName(f)));
        string copy = Path.Combine(copyDir, "NotABrowser.exe");
        File.Copy(Harness.LauncherExe, copy);
        h.WriteConfig($"\"{copy}\" \"%1\"");
        var (exit, _, _) = h.Run("https://example.com/loop");
        Assert.Equal(0, exit); // first hop "succeeds" in starting the copy...
        // ...the copy (same config via LAZYBLOX_HOME) receives the guard marker and refuses to forward again.
        Thread.Sleep(3000);
        Assert.Empty(h.BrowserInvocations(0, 100));
        int copies = Process.GetProcessesByName("NotABrowser").Length;
        Assert.Equal(0, copies);
    }

    [Fact] // 11: several URLs in one invocation
    public void MultipleUrlsInOneInvocation_AllForwarded()
    {
        using var h = new Harness();
        h.UseFakeBrowser();
        Assert.Equal(0, h.Run("https://example.com/1", "https://example.com/2", "https://example.com/3").Exit);
        var got = h.BrowserInvocations(3).Select(i => i.Args[^1]).Order().ToList();
        Assert.Equal(["https://example.com/1", "https://example.com/2", "https://example.com/3"], got);
    }

    [Fact] // 11: many launcher processes at the same time
    public void ConcurrentInvocations_AreIndependent()
    {
        using var h = new Harness();
        h.UseFakeBrowser();
        const int n = 12;
        var procs = Enumerable.Range(0, n).Select(i => Process.Start(h.StartInfo($"https://example.com/c/{i}"))!).ToList();
        foreach (Process p in procs) { Assert.True(p.WaitForExit(30_000)); Assert.Equal(0, p.ExitCode); p.Dispose(); }
        var got = h.BrowserInvocations(n).Select(i => i.Args[^1]).ToHashSet();
        Assert.Equal(n, got.Count);
        for (int i = 0; i < n; i++) Assert.Contains($"https://example.com/c/{i}", got);
    }

    [Fact]
    public void Install_WithoutYes_ChangesNothing()
    {
        using var h = new Harness();
        var (exit, output, _) = h.Run("install");
        Assert.Equal(ExitCodes.Usage, exit);
        Assert.Contains("--yes", output);
        Assert.False(File.Exists(Path.Combine(h.Home, "config.ini")));
    }

    [Fact]
    public void Bench_Runs()
    {
        using var h = new Harness();
        var (exit, output, _) = h.Run("bench", Share, "2000");
        Assert.Equal(0, exit);
        Assert.Contains("p99", output);
    }
}
