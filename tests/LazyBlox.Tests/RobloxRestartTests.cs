namespace LazyBlox.Tests;

/// <summary>Detection of a Roblox that ignored the link (from real log lines) and the confirm-then-restart flow.</summary>
public class RobloxRestartTests
{
    private const string Deep = "roblox://navigation/share_links?code=0123456789abcdef0123456789abcdef&type=Server";
    private static readonly DateTime Since = new(2026, 11, 18, 11, 54, 9, 100, DateTimeKind.Utc);

    // Excerpts of a real Roblox 0.748 log (2026-11-18).
    private const string Recv = "2026-11-18T11:54:09.238Z,23579.238281,50e8,6,Warning [FLog::Output] App NoReload receiving deeplink";
    private const string Launch = "2026-11-18T11:54:09.425Z,23579.425781,50e8,6 [FLog::SingleSurfaceApp] launchUGCGame: (stage:UGCGame).";
    private const string Joining = "2026-11-18T11:54:11.421Z,23581.421875,b5a0,6 [FLog::Output] ! Joining game '94bc2d98-6de1-49b3-94cd-8b049bb679fa' place 15532962292 at 10.205.10.23";
    private const string Heartbeat = "2026-11-18T11:54:12.000Z,23582.000000,f0c4,6,Info [FLog::SessionL2ValidationHelper] onSessionHeartbeat";
    private const string HandOff = "2026-11-18T11:54:09.142Z,0.142803,e568,6,Warning [FLog::Output] NoReload Detector: Deeplinking NoReload!";
    private const string OldLaunch = "2026-11-18T11:45:19.798Z,23049.798828,50e8,6 [FLog::SingleSurfaceApp] launchUGCGame: (stage:UGCGame).";

    [Fact]
    public void Classify_LaunchAfterReceive_IsMoved()
    {
        Assert.Equal(LinkReaction.Moved, RobloxLog.Classify([Recv, Launch, Joining], Since));
        Assert.Equal(LinkReaction.Moved, RobloxLog.Classify([Joining], Since));
    }

    [Fact]
    public void Classify_ReceivedButNothingHappened_IsIgnored()
    {
        Assert.Equal(LinkReaction.Ignored, RobloxLog.Classify([Recv, Heartbeat], Since));
        Assert.Equal(LinkReaction.Ignored, RobloxLog.Classify([HandOff], Since)); // launcher handed it off, main never reacted
    }

    [Fact]
    public void Classify_OlderLinesDoNotCount()
    {
        Assert.Equal(LinkReaction.Ignored, RobloxLog.Classify([OldLaunch, Recv], Since));
        Assert.Equal(LinkReaction.Unknown, RobloxLog.Classify([OldLaunch], Since));
    }

    [Fact]
    public void Classify_NoEvidence_IsUnknown()
    {
        Assert.Equal(LinkReaction.Unknown, RobloxLog.Classify([], Since));
        Assert.Equal(LinkReaction.Unknown, RobloxLog.Classify([Heartbeat, "garbage", "", "[FLog::Output] receiving deeplink"], Since));
    }

    [Fact]
    public void ParseLineTime()
    {
        Assert.True(RobloxLog.TryParseLineTime(Recv, out DateTime t));
        Assert.Equal(new DateTime(2026, 11, 18, 11, 54, 9, 238, DateTimeKind.Utc), t);
        Assert.Equal(DateTimeKind.Utc, t.Kind);
        Assert.False(RobloxLog.TryParseLineTime("[FLog::Output] All use of Roblox services", out _));
        Assert.False(RobloxLog.TryParseLineTime("2026-11-18", out _));
    }

    [Fact]
    public void ParseFileTime_AndRelevance()
    {
        const string main = "0.748.486.7481530_20261118T052109Z_Player_CA693_last.log";
        const string launcher = "0.748.486.7481530_20261118T115409Z_Player_F9FAE_last.log";
        const string oldLauncher = "0.748.486.7481530_20261118T113525Z_Player_C4774_last.log";
        Assert.True(RobloxLog.TryParseFileTime(main, out DateTime t));
        Assert.Equal(new DateTime(2026, 11, 18, 5, 21, 9, DateTimeKind.Utc), t);
        Assert.False(RobloxLog.TryParseFileTime("cacert.pem", out _));

        RunningRoblox[] running = [new(1, new DateTime(2026, 11, 18, 5, 21, 6, DateTimeKind.Utc))];
        Assert.True(RobloxLog.IsRelevant(main, Since, running));
        Assert.True(RobloxLog.IsRelevant(launcher, Since, running));
        Assert.False(RobloxLog.IsRelevant(oldLauncher, Since, running));
        Assert.False(RobloxLog.IsRelevant("cacert.pem", Since, running));
    }

    private sealed class FakeHost(LinkReaction reaction, bool running = true, LaunchResult? close = null) : IRobloxHost
    {
        public int Closes, Waits;
        public IReadOnlyList<RunningRoblox> PlayersStartedBefore(DateTime utc) =>
            running ? [new RunningRoblox(42, utc.AddHours(-23))] : [];
        public LinkReaction WaitForReaction(DateTime sinceUtc, IReadOnlyList<RunningRoblox> r, TimeSpan timeout) { Waits++; return reaction; }
        public LaunchResult CloseAll() { Closes++; return close ?? LaunchResult.Ok; }
    }

    private static (FallbackOutcome, RecordingShell, int asked) Run(FakeHost host, bool answer)
    {
        var shell = new RecordingShell();
        int asked = 0;
        FallbackOutcome o = StuckFallback.Run(Deep, Since, host, shell, () => { asked++; return answer; });
        return (o, shell, asked);
    }

    [Fact]
    public void NotRunningBefore_DoesNothing()
    {
        var host = new FakeHost(LinkReaction.Ignored, running: false);
        var (o, shell, asked) = Run(host, true);
        Assert.Equal(FallbackOutcome.NotRunning, o);
        Assert.Equal(0, host.Waits);
        Assert.Equal(0, asked);
        Assert.Empty(shell.OpenedUris);
    }

    [Theory]
    [InlineData(LinkReaction.Moved, FallbackOutcome.Moved)]
    [InlineData(LinkReaction.Unknown, FallbackOutcome.NoEvidence)]
    public void MovedOrUnknown_NeverAsksOrCloses(LinkReaction reaction, FallbackOutcome expected)
    {
        var host = new FakeHost(reaction);
        var (o, shell, asked) = Run(host, true);
        Assert.Equal(expected, o);
        Assert.Equal(0, asked);
        Assert.Equal(0, host.Closes);
        Assert.Empty(shell.OpenedUris);
    }

    [Fact]
    public void Ignored_Declined_LeavesRobloxAlone()
    {
        var host = new FakeHost(LinkReaction.Ignored);
        var (o, shell, asked) = Run(host, false);
        Assert.Equal(FallbackOutcome.Declined, o);
        Assert.Equal(1, asked);
        Assert.Equal(0, host.Closes);
        Assert.Empty(shell.OpenedUris);
    }

    [Fact]
    public void Ignored_Confirmed_ClosesThenOpensSameLink()
    {
        var host = new FakeHost(LinkReaction.Ignored);
        var (o, shell, asked) = Run(host, true);
        Assert.Equal(FallbackOutcome.Restarted, o);
        Assert.Equal(1, asked);
        Assert.Equal(1, host.Closes);
        Assert.Equal([Deep], shell.OpenedUris);
        Assert.Empty(shell.Errors);
    }

    [Fact]
    public void CloseFails_ShowsError_AndDoesNotRelaunch()
    {
        var host = new FakeHost(LinkReaction.Ignored, close: LaunchResult.Fail("access denied"));
        var (o, shell, _) = Run(host, true);
        Assert.Equal(FallbackOutcome.RestartFailed, o);
        Assert.Empty(shell.OpenedUris);
        Assert.Contains("access denied", Assert.Single(shell.Errors));
    }

    [Fact]
    public void Config_RestartIfStuck_DefaultsOn_AndRoundTrips()
    {
        Assert.True(new AppConfig().RestartIfStuck);
        Assert.True(AppConfig.Parse("BrowserName=x").RestartIfStuck);
        var c = new AppConfig { RestartIfStuck = false };
        Assert.False(AppConfig.Parse(c.Serialize()).RestartIfStuck);
    }

    [Fact]
    public void Handler_RecordsRobloxLaunch_OnlyForRobloxLinks()
    {
        var shell = new RecordingShell();
        var h = new LinkHandler(shell, FakeRegistry.TypicalMachine(), () => new AppConfig
        {
            BrowserCommand = "\"C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe\" %1",
        }, null);
        DateTime before = DateTime.UtcNow;
        h.Handle("https://www.roblox.com/share?code=0123456789abcdef0123456789abcdef&type=Server");
        Assert.NotNull(h.LastRobloxLaunch);
        Assert.Equal(Deep, h.LastRobloxLaunch.Value.DeepLink);
        Assert.InRange(h.LastRobloxLaunch.Value.LaunchedUtc, before, DateTime.UtcNow);

        var h2 = new LinkHandler(new RecordingShell(), FakeRegistry.TypicalMachine(), () => new AppConfig(), null);
        h2.Handle("https://example.com/");
        Assert.Null(h2.LastRobloxLaunch);

        var failing = new LinkHandler(new RecordingShell(onOpen: _ => LaunchResult.Fail("x")), FakeRegistry.TypicalMachine(), () => new AppConfig(), null);
        failing.Handle("https://www.roblox.com/share?code=0123456789abcdef0123456789abcdef&type=Server");
        Assert.Null(failing.LastRobloxLaunch);
    }
}
