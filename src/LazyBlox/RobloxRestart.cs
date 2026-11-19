using LazyBlox.Localization;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace LazyBlox;

/// <summary>What an already-running Roblox did with the deep link we handed to it.</summary>
public enum LinkReaction
{
    /// <summary>The log shows it started joining (launchUGCGame / "Joining game").</summary>
    Moved,
    /// <summary>The link reached Roblox (hand-off / "receiving deeplink") but it never started joining.</summary>
    Ignored,
    /// <summary>No usable log evidence either way (logs missing, unreadable or format changed): do nothing.</summary>
    Unknown,
}

public enum FallbackOutcome { NotRunning, Moved, NoEvidence, Declined, Restarted, RestartFailed }

public readonly record struct RunningRoblox(int Pid, DateTime StartUtc);

/// <summary>Roblox-specific side effects, behind an interface so the fallback flow can be tested.</summary>
public interface IRobloxHost
{
    /// <summary>RobloxPlayerBeta processes that were already running before <paramref name="utc"/>.</summary>
    IReadOnlyList<RunningRoblox> PlayersStartedBefore(DateTime utc);
    /// <summary>Watches the Roblox logs until the running instance reacts or the timeout passes.</summary>
    LinkReaction WaitForReaction(DateTime sinceUtc, IReadOnlyList<RunningRoblox> running, TimeSpan timeout);
    /// <summary>Asks every Roblox player window to close, then terminates what is left.</summary>
    LaunchResult CloseAll();
}

/// <summary>
/// A long-running Roblox sometimes receives a share link and silently ignores it. When that is detected in its log,
/// offer (never without asking) to quit Roblox and open the same link in a fresh instance.
/// Runs only after the deep link was already handed over, so it never delays the normal path.
/// </summary>
public static class StuckFallback
{
    /// <summary>A working instance logs launchUGCGame ~0.2 s after receiving the link (hand-off itself is 0.1–1 s).</summary>
    public static readonly TimeSpan ReactionTimeout = TimeSpan.FromSeconds(8);

    public static FallbackOutcome Run(string deepLink, DateTime launchedUtc, IRobloxHost host, ISystemShell shell, Func<bool> confirm)
    {
        IReadOnlyList<RunningRoblox> running = host.PlayersStartedBefore(launchedUtc);
        if (running.Count == 0) return FallbackOutcome.NotRunning; // fresh start: Roblox handles the link itself

        switch (host.WaitForReaction(launchedUtc, running, ReactionTimeout))
        {
            case LinkReaction.Moved: return FallbackOutcome.Moved;
            case LinkReaction.Unknown: return FallbackOutcome.NoEvidence;
        }

        if (!confirm()) return FallbackOutcome.Declined;

        LaunchResult closed = host.CloseAll();
        if (!closed.Success)
        {
            shell.ShowError(L.F(L.S.ErrRestartFailed, closed.Error ?? "?"));
            return FallbackOutcome.RestartFailed;
        }
        LaunchResult opened = shell.OpenUri(deepLink);
        if (!opened.Success)
        {
            shell.ShowError(L.F(L.S.ErrRestartFailed, opened.Error ?? "?"));
            return FallbackOutcome.RestartFailed;
        }
        return FallbackOutcome.Restarted;
    }
}

/// <summary>Parsing of Roblox client logs (%LOCALAPPDATA%\Roblox\logs). Pure functions; tested with real log lines.</summary>
public static class RobloxLog
{
    /// <summary>Lines written at or after <paramref name="sinceUtc"/> decide; older lines are ignored.</summary>
    public static LinkReaction Classify(IEnumerable<string> lines, DateTime sinceUtc)
    {
        bool reached = false;
        foreach (string line in lines)
        {
            if (!TryParseLineTime(line, out DateTime t) || t < sinceUtc) continue;
            if (line.Contains("launchUGCGame", StringComparison.Ordinal) || line.Contains("! Joining game", StringComparison.Ordinal))
                return LinkReaction.Moved;
            if (line.Contains("receiving deeplink", StringComparison.Ordinal) || line.Contains("Deeplinking NoReload", StringComparison.Ordinal))
                reached = true;
        }
        return reached ? LinkReaction.Ignored : LinkReaction.Unknown;
    }

    /// <summary>"2026-11-18T11:54:09.238Z,23579.23,50e8,6 ..." -> UTC time.</summary>
    public static bool TryParseLineTime(string line, out DateTime utc)
    {
        utc = default;
        return line.Length >= 24 && line[23] == 'Z' &&
               DateTime.TryParseExact(line.AsSpan(0, 24), "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture,
                   DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out utc);
    }

    /// <summary>"0.748.486.7481530_20261118T052109Z_Player_CA693_last.log" -> 2026-11-18 05:21:09 UTC.</summary>
    public static bool TryParseFileTime(string fileName, out DateTime utc)
    {
        utc = default;
        int i = fileName.IndexOf("Z_Player_", StringComparison.Ordinal);
        return i >= 15 &&
               DateTime.TryParseExact(fileName.AsSpan(i - 15, 16), "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture,
                   DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out utc);
    }

    /// <summary>
    /// Logs worth reading: the running instances' own logs (named a few seconds after they started) and the
    /// short-lived hand-off launcher logs created after <paramref name="sinceUtc"/>.
    /// </summary>
    public static bool IsRelevant(string fileName, DateTime sinceUtc, IReadOnlyList<RunningRoblox> running)
    {
        if (!TryParseFileTime(fileName, out DateTime t)) return false;
        if (t >= sinceUtc.AddSeconds(-2)) return true;
        foreach (RunningRoblox r in running)
            if (t >= r.StartUtc.AddSeconds(-5) && t <= r.StartUtc.AddMinutes(2)) return true;
        return false;
    }
}

public sealed class WindowsRobloxHost : IRobloxHost
{
    public static readonly WindowsRobloxHost Instance = new();
    private const string PlayerProcess = "RobloxPlayerBeta";
    private const int TailBytes = 128 * 1024;

    public static string LogDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "logs");

    public IReadOnlyList<RunningRoblox> PlayersStartedBefore(DateTime utc)
    {
        var list = new List<RunningRoblox>();
        foreach (Process p in Process.GetProcessesByName(PlayerProcess))
        {
            using (p)
            {
                try
                {
                    DateTime start = p.StartTime.ToUniversalTime();
                    if (start < utc) list.Add(new RunningRoblox(p.Id, start));
                }
                catch (Exception e) when (e is Win32Exception or InvalidOperationException or NotSupportedException) { }
            }
        }
        return list;
    }

    public LinkReaction WaitForReaction(DateTime sinceUtc, IReadOnlyList<RunningRoblox> running, TimeSpan timeout)
    {
        string dir = LogDirectory;
        if (!Directory.Exists(dir)) return LinkReaction.Unknown;
        DateTime since = sinceUtc.AddMilliseconds(-500); // the log clock and ours are the same, but be lenient
        var sw = Stopwatch.StartNew();
        LinkReaction last = LinkReaction.Unknown;
        while (true)
        {
            var lines = new List<string>();
            try
            {
                foreach (string path in Directory.EnumerateFiles(dir, "*_Player_*.log"))
                    if (RobloxLog.IsRelevant(Path.GetFileName(path), since, running)) ReadTail(path, lines);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }

            last = RobloxLog.Classify(lines, since);
            if (last == LinkReaction.Moved || sw.Elapsed >= timeout) return last;
            Thread.Sleep(250);
        }
    }

    private static void ReadTail(string path, List<string> into)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            long start = Math.Max(0, fs.Length - TailBytes);
            fs.Seek(start, SeekOrigin.Begin);
            using var sr = new StreamReader(fs, Encoding.UTF8);
            if (start > 0) sr.ReadLine(); // drop the partial first line
            while (sr.ReadLine() is { } line) into.Add(line);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }

    public LaunchResult CloseAll()
    {
        Process[] procs = Process.GetProcessesByName(PlayerProcess);
        try
        {
            foreach (Process p in procs)
                try { p.CloseMainWindow(); } catch (InvalidOperationException) { }
            var sw = Stopwatch.StartNew();
            foreach (Process p in procs)
            {
                try
                {
                    int left = (int)Math.Max(0, 5000 - sw.ElapsedMilliseconds);
                    if (p.WaitForExit(left)) continue;
                    p.Kill();
                    if (!p.WaitForExit(5000)) return LaunchResult.Fail(L.F(L.S.ErrRobloxStillRunning, p.Id));
                }
                catch (InvalidOperationException) { } // already exited
                catch (Win32Exception e) { return LaunchResult.Fail(e.Message); }
            }
            Thread.Sleep(500); // let Roblox release its single-instance lock before the new launch
            return LaunchResult.Ok;
        }
        finally
        {
            foreach (Process p in procs) p.Dispose();
        }
    }
}
