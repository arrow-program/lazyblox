namespace LazyBlox.Tests;

/// <summary>In-memory IRegistryView for unit tests. Keys are "HIVE\path" (case-insensitive).</summary>
internal sealed class FakeRegistry : IRegistryView
{
    private readonly Dictionary<string, Dictionary<string, string>> _keys = new(StringComparer.OrdinalIgnoreCase);

    private static string K(RegHive hive, string path) => (hive == RegHive.CurrentUser ? "HKCU\\" : "HKLM\\") + path.Trim('\\');

    public FakeRegistry Set(RegHive hive, string path, string? name, string value)
    {
        string key = K(hive, path);
        // create parents so enumeration works
        string[] parts = key.Split('\\');
        for (int i = 1; i <= parts.Length; i++)
        {
            string p = string.Join('\\', parts.Take(i));
            if (!_keys.ContainsKey(p)) _keys[p] = new(StringComparer.OrdinalIgnoreCase);
        }
        _keys[key][name ?? ""] = value;
        return this;
    }

    public string? GetString(RegHive hive, string subKey, string? valueName) =>
        _keys.TryGetValue(K(hive, subKey), out var v) && v.TryGetValue(valueName ?? "", out string? s) ? s : null;

    public bool KeyExists(RegHive hive, string subKey) => _keys.ContainsKey(K(hive, subKey));

    public bool ValueExists(RegHive hive, string subKey, string valueName) => GetString(hive, subKey, valueName) is not null;

    public IReadOnlyList<string> GetSubKeyNames(RegHive hive, string subKey)
    {
        string prefix = K(hive, subKey) + "\\";
        return _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(k => k[prefix.Length..].Split('\\')[0]).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public IReadOnlyList<string> GetValueNames(RegHive hive, string subKey) =>
        _keys.TryGetValue(K(hive, subKey), out var v) ? v.Keys.Where(n => n.Length > 0).ToList() : [];

    /// <summary>A machine with Chrome (default) and Edge installed, like a typical Windows 11 PC.</summary>
    public static FakeRegistry TypicalMachine(string chromeExe = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        string edgeExe = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe")
    {
        var r = new FakeRegistry();
        r.Set(RegHive.LocalMachine, @"Software\Clients\StartMenuInternet\Google Chrome", null, "Google Chrome");
        r.Set(RegHive.LocalMachine, @"Software\Clients\StartMenuInternet\Google Chrome\Capabilities\URLAssociations", "https", "ChromeHTML");
        r.Set(RegHive.LocalMachine, @"Software\Clients\StartMenuInternet\Google Chrome\shell\open\command", null, $"\"{chromeExe}\"");
        r.Set(RegHive.LocalMachine, @"Software\Classes\ChromeHTML\shell\open\command", null, $"\"{chromeExe}\" --single-argument %1");
        r.Set(RegHive.LocalMachine, @"Software\Clients\StartMenuInternet\Microsoft Edge", null, "Microsoft Edge");
        r.Set(RegHive.LocalMachine, @"Software\Clients\StartMenuInternet\Microsoft Edge\Capabilities\URLAssociations", "https", "MSEdgeHTM");
        r.Set(RegHive.LocalMachine, @"Software\Classes\MSEdgeHTM\shell\open\command", null, $"\"{edgeExe}\" --single-argument %1");
        r.Set(RegHive.LocalMachine, @"Software\RegisteredApplications", "Google Chrome", @"Software\Clients\StartMenuInternet\Google Chrome\Capabilities");
        r.Set(RegHive.CurrentUser, @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice", "ProgId", "ChromeHTML");
        r.Set(RegHive.CurrentUser, @"Software\Classes\roblox", "URL Protocol", "");
        return r;
    }
}
