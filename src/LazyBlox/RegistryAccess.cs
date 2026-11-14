using Microsoft.Win32;

namespace LazyBlox;

public enum RegHive { CurrentUser, LocalMachine }

/// <summary>Read-only registry view (mockable for unit tests).</summary>
public interface IRegistryView
{
    string? GetString(RegHive hive, string subKey, string? valueName);
    bool KeyExists(RegHive hive, string subKey);
    bool ValueExists(RegHive hive, string subKey, string valueName);
    IReadOnlyList<string> GetSubKeyNames(RegHive hive, string subKey);
    IReadOnlyList<string> GetValueNames(RegHive hive, string subKey);
}

/// <summary>Real registry. HKCU paths may be redirected under <see cref="CurrentUserPrefix"/> (tests).</summary>
public sealed class WindowsRegistry : IRegistryView
{
    public static readonly WindowsRegistry Instance = new("");

    /// <summary>When non-empty, every HKCU path is rooted below this key (sandbox for integration tests).</summary>
    public string CurrentUserPrefix { get; }

    public WindowsRegistry(string currentUserPrefix) => CurrentUserPrefix = currentUserPrefix.Trim('\\');

    public string MapPath(RegHive hive, string subKey) =>
        hive == RegHive.CurrentUser && CurrentUserPrefix.Length > 0 ? CurrentUserPrefix + "\\" + subKey : subKey;

    private RegistryKey? Open(RegHive hive, string subKey, bool writable = false)
    {
        RegistryKey root = hive == RegHive.CurrentUser ? Registry.CurrentUser : Registry.LocalMachine;
        try { return root.OpenSubKey(MapPath(hive, subKey), writable); }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException or IOException) { return null; }
    }

    public string? GetString(RegHive hive, string subKey, string? valueName)
    {
        using RegistryKey? key = Open(hive, subKey);
        return key?.GetValue(valueName) as string; // REG_EXPAND_SZ is expanded by default.
    }

    public bool KeyExists(RegHive hive, string subKey)
    {
        using RegistryKey? key = Open(hive, subKey);
        return key is not null;
    }

    public bool ValueExists(RegHive hive, string subKey, string valueName)
    {
        using RegistryKey? key = Open(hive, subKey);
        return key?.GetValue(valueName) is not null;
    }

    public IReadOnlyList<string> GetSubKeyNames(RegHive hive, string subKey)
    {
        using RegistryKey? key = Open(hive, subKey);
        return key?.GetSubKeyNames() ?? [];
    }

    public IReadOnlyList<string> GetValueNames(RegHive hive, string subKey)
    {
        using RegistryKey? key = Open(hive, subKey);
        return key?.GetValueNames() ?? [];
    }

    // ---- writes (HKCU only; this app never writes HKLM) ----

    public void SetValue(string subKey, string valueName, object value, RegistryValueKind kind)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(MapPath(RegHive.CurrentUser, subKey), writable: true);
        key.SetValue(valueName, value, kind);
    }

    public void DeleteKeyTree(string subKey) =>
        Registry.CurrentUser.DeleteSubKeyTree(MapPath(RegHive.CurrentUser, subKey), throwOnMissingSubKey: false);

    public void DeleteValue(string subKey, string valueName)
    {
        using RegistryKey? key = Open(RegHive.CurrentUser, subKey, writable: true);
        key?.DeleteValue(valueName, throwOnMissingValue: false);
    }
}
