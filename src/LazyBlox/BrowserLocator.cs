namespace LazyBlox;

public sealed record BrowserCandidate(string Name, string? ProgId, string Command, string? RegisteredAppName);

/// <summary>Discovers installed browsers and the user's current default, read-only.</summary>
public static class BrowserLocator
{
    private const string StartMenuInternet = @"Software\Clients\StartMenuInternet";
    private const string RegisteredApplications = @"Software\RegisteredApplications";
    public const string UserChoiceKeyFormat = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\{0}\UserChoice";

    /// <summary>Reads (never writes) the ProgID Windows currently uses for a scheme. UserChoice is hash-protected; we only observe it.</summary>
    public static string? GetUserChoiceProgId(IRegistryView reg, string scheme)
    {
        string key = string.Format(System.Globalization.CultureInfo.InvariantCulture, UserChoiceKeyFormat, scheme);
        string? progId = reg.GetString(RegHive.CurrentUser, key, "ProgId");
        // Windows 11 24H2+ also maintains a "UserChoiceLatest" key.
        progId ??= reg.GetString(RegHive.CurrentUser, key.Replace("UserChoice", "UserChoiceLatest", StringComparison.Ordinal) , "ProgId");
        return string.IsNullOrWhiteSpace(progId) ? null : progId;
    }

    /// <summary>shell\open\command of a ProgID (HKCU\Software\Classes overrides HKLM, as in HKCR).</summary>
    public static string? GetProgIdCommand(IRegistryView reg, string progId)
    {
        if (string.IsNullOrWhiteSpace(progId) || progId.IndexOfAny(['\\', '/']) >= 0) return null;
        string key = $@"Software\Classes\{progId}\shell\open\command";
        string? cmd = reg.GetString(RegHive.CurrentUser, key, null) ?? reg.GetString(RegHive.LocalMachine, key, null);
        return string.IsNullOrWhiteSpace(cmd) ? null : cmd;
    }

    /// <summary>All registered browsers (HKCU first, then HKLM), excluding this launcher.</summary>
    public static List<BrowserCandidate> Enumerate(IRegistryView reg)
    {
        var list = new List<BrowserCandidate>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (RegHive hive in new[] { RegHive.CurrentUser, RegHive.LocalMachine })
        {
            foreach (string sub in reg.GetSubKeyNames(hive, StartMenuInternet))
            {
                if (sub.Equals(AppInfo.AppName, StringComparison.OrdinalIgnoreCase) || !seen.Add(sub)) continue;
                string baseKey = $@"{StartMenuInternet}\{sub}";
                string name = reg.GetString(hive, baseKey, null)
                              ?? reg.GetString(hive, baseKey + @"\Capabilities", "ApplicationName")
                              ?? sub;
                if (name.StartsWith('@')) name = sub; // indirect string resource – use key name instead
                string? progId = reg.GetString(hive, baseKey + @"\Capabilities\URLAssociations", "https");
                string? command = (progId is null ? null : GetProgIdCommand(reg, progId))
                                  ?? reg.GetString(hive, baseKey + @"\shell\open\command", null);
                if (string.IsNullOrWhiteSpace(command) || progId == AppInfo.ProgId) continue;

                // Never offer anything that resolves back to us.
                List<string> tokens = BrowserCommand.Tokenize(command);
                if (tokens.Count == 0) continue;
                BrowserCommand.ResolveExecutableTokens(tokens, out string exe);
                if (BrowserCommand.IsForbidden(exe, AppInfo.CurrentExePath, out _)) continue;
                if (reg is WindowsRegistry && !File.Exists(exe)) continue; // stale registration (only checked against the real machine)

                list.Add(new BrowserCandidate(name, progId, command, FindRegisteredAppName(reg, progId)));
            }
        }
        return list;
    }

    /// <summary>Finds the RegisteredApplications name whose Capabilities map https to <paramref name="progId"/>.</summary>
    public static string? FindRegisteredAppName(IRegistryView reg, string? progId)
    {
        if (string.IsNullOrEmpty(progId)) return null;
        foreach (RegHive hive in new[] { RegHive.CurrentUser, RegHive.LocalMachine })
        {
            foreach (string appName in reg.GetValueNames(hive, RegisteredApplications))
            {
                string? capPath = reg.GetString(hive, RegisteredApplications, appName);
                if (string.IsNullOrEmpty(capPath)) continue;
                string? https = reg.GetString(hive, capPath + @"\URLAssociations", "https");
                if (string.Equals(https, progId, StringComparison.OrdinalIgnoreCase)) return appName;
            }
        }
        return null;
    }

    /// <summary>
    /// Picks the browser to record at install time: the current https default (unless it is us),
    /// otherwise Edge, otherwise the first registered browser.
    /// </summary>
    public static BrowserCandidate? DetectDefault(IRegistryView reg, IReadOnlyList<BrowserCandidate> candidates)
    {
        string? current = GetUserChoiceProgId(reg, "https");
        if (current is not null && current != AppInfo.ProgId)
        {
            BrowserCandidate? match = candidates.FirstOrDefault(c => string.Equals(c.ProgId, current, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
            string? cmd = GetProgIdCommand(reg, current);
            if (cmd is not null)
            {
                List<string> t = BrowserCommand.Tokenize(cmd);
                if (t.Count > 0 && !BrowserCommand.IsForbidden(t[0], AppInfo.CurrentExePath, out _))
                    return new BrowserCandidate(current, current, cmd, FindRegisteredAppName(reg, current));
            }
        }
        return candidates.FirstOrDefault(c => string.Equals(c.ProgId, "MSEdgeHTM", StringComparison.OrdinalIgnoreCase))
               ?? candidates.FirstOrDefault();
    }
}
