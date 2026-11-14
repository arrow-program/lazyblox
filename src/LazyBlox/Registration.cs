using LazyBlox.Localization;
using System.Text;
using Microsoft.Win32;

namespace LazyBlox;

public enum RegOpKind { SetValue, DeleteKeyTree, DeleteValue }

/// <summary>One registry change under HKCU. Plans are data so they can be previewed before being applied.</summary>
public sealed record RegOp(RegOpKind Kind, string Key, string? Name = null, object? Value = null,
    RegistryValueKind ValueKind = RegistryValueKind.String);

/// <summary>
/// Builds the per-user registration that makes LazyBlox selectable as the http/https handler in
/// Settings &gt; Apps &gt; Default apps (Default Programs "Capabilities" + RegisteredApplications model).
/// It never touches UserChoice: the user selects LazyBlox in Windows Settings themselves.
/// </summary>
public static class RegistrationPlan
{
    public const string ProgIdKey = @"Software\Classes\" + AppInfo.ProgId;
    public const string ClientKey = @"Software\Clients\StartMenuInternet\" + AppInfo.AppName;
    public const string CapabilitiesKey = ClientKey + @"\Capabilities";
    public const string RegisteredAppsKey = @"Software\RegisteredApplications";
    public const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\" + AppInfo.AppName;

    /// <summary>All keys this app owns (deleted wholesale on uninstall). Constant names only.</summary>
    public static readonly string[] OwnedKeys = [ProgIdKey, ClientKey, UninstallKey];

    public static List<RegOp> BuildInstall(string exePath)
    {
        string q = $"\"{exePath}\"";
        string icon = $"{exePath},0";
        return
        [
            // ProgID that receives http/https URLs.
            new(RegOpKind.SetValue, ProgIdKey, "", "LazyBlox URL"),
            new(RegOpKind.SetValue, ProgIdKey, "FriendlyTypeName", "LazyBlox URL"),
            new(RegOpKind.SetValue, ProgIdKey, "EditFlags", 2, RegistryValueKind.DWord),
            new(RegOpKind.SetValue, ProgIdKey + @"\DefaultIcon", "", icon),
            new(RegOpKind.SetValue, ProgIdKey + @"\Application", "ApplicationName", AppInfo.AppName),
            new(RegOpKind.SetValue, ProgIdKey + @"\Application", "ApplicationDescription", L.S.AppDescription),
            new(RegOpKind.SetValue, ProgIdKey + @"\Application", "ApplicationIcon", icon),
            new(RegOpKind.SetValue, ProgIdKey + @"\shell\open\command", "", $"{q} \"%1\""),

            // Client registration + capabilities (what Settings > Default apps lists).
            new(RegOpKind.SetValue, ClientKey, "", AppInfo.AppName),
            new(RegOpKind.SetValue, ClientKey + @"\DefaultIcon", "", icon),
            new(RegOpKind.SetValue, ClientKey + @"\shell\open\command", "", q),
            new(RegOpKind.SetValue, CapabilitiesKey, "ApplicationName", AppInfo.AppName),
            new(RegOpKind.SetValue, CapabilitiesKey, "ApplicationDescription", L.S.AppDescription),
            new(RegOpKind.SetValue, CapabilitiesKey, "ApplicationIcon", icon),
            new(RegOpKind.SetValue, CapabilitiesKey + @"\URLAssociations", "http", AppInfo.ProgId),
            new(RegOpKind.SetValue, CapabilitiesKey + @"\URLAssociations", "https", AppInfo.ProgId),
            new(RegOpKind.SetValue, RegisteredAppsKey, AppInfo.AppName, CapabilitiesKey),

            // Settings > Apps > Installed apps entry (per-user uninstall).
            new(RegOpKind.SetValue, UninstallKey, "DisplayName", AppInfo.AppName),
            new(RegOpKind.SetValue, UninstallKey, "DisplayVersion", AppInfo.Version),
            new(RegOpKind.SetValue, UninstallKey, "Publisher", AppInfo.AppName),
            new(RegOpKind.SetValue, UninstallKey, "DisplayIcon", icon),
            new(RegOpKind.SetValue, UninstallKey, "InstallLocation", Path.GetDirectoryName(exePath) ?? ""),
            new(RegOpKind.SetValue, UninstallKey, "UninstallString", $"{q} uninstall"),
            new(RegOpKind.SetValue, UninstallKey, "QuietUninstallString", $"{q} uninstall --yes --quiet"),
            new(RegOpKind.SetValue, UninstallKey, "NoModify", 1, RegistryValueKind.DWord),
            new(RegOpKind.SetValue, UninstallKey, "NoRepair", 1, RegistryValueKind.DWord),
        ];
    }

    public static List<RegOp> BuildUninstall() =>
    [
        new(RegOpKind.DeleteValue, RegisteredAppsKey, AppInfo.AppName),
        new(RegOpKind.DeleteKeyTree, ClientKey),
        new(RegOpKind.DeleteKeyTree, ProgIdKey),
        new(RegOpKind.DeleteKeyTree, UninstallKey),
    ];

    /// <summary>Human-readable plan, grouped by key: "+ key" (set values below it) / "- key" (delete).</summary>
    public static string Describe(IEnumerable<RegOp> ops)
    {
        var sb = new StringBuilder();
        string? currentKey = null;
        foreach (RegOp op in ops)
        {
            string name = string.IsNullOrEmpty(op.Name) ? L.S.RegDefaultValue : op.Name;
            switch (op.Kind)
            {
                case RegOpKind.SetValue:
                    if (op.Key != currentKey)
                    {
                        sb.Append("  + HKCU\\").AppendLine(op.Key);
                        currentKey = op.Key;
                    }
                    string value = op.ValueKind == RegistryValueKind.DWord ? $"dword:{op.Value}" : $"\"{op.Value}\"";
                    sb.Append("      ").Append(name).Append(" = ").AppendLine(value);
                    break;
                case RegOpKind.DeleteKeyTree:
                    sb.Append("  - HKCU\\").Append(op.Key).Append("  ").AppendLine(L.S.RegDeleteKey);
                    currentKey = null;
                    break;
                default:
                    sb.Append("  - HKCU\\").Append(op.Key).Append("  ").AppendLine(L.F(L.S.RegDeleteValue, name));
                    currentKey = null;
                    break;
            }
        }
        return sb.ToString();
    }

    public static void Apply(WindowsRegistry reg, IEnumerable<RegOp> ops)
    {
        foreach (RegOp op in ops)
        {
            switch (op.Kind)
            {
                case RegOpKind.SetValue: reg.SetValue(op.Key, op.Name ?? "", op.Value!, op.ValueKind); break;
                case RegOpKind.DeleteKeyTree: reg.DeleteKeyTree(op.Key); break;
                case RegOpKind.DeleteValue: reg.DeleteValue(op.Key, op.Name!); break;
            }
        }
    }

    /// <summary>Returns human-readable problems; empty = registry matches the install plan.</summary>
    public static List<string> VerifyInstalled(IRegistryView reg, string exePath)
    {
        var problems = new List<string>();
        foreach (RegOp op in BuildInstall(exePath))
        {
            if (op.ValueKind != RegistryValueKind.String) continue; // DWORD flags are cosmetic
            if (op.Name == "ApplicationDescription") continue; // localized at install time; may differ if the UI language changed
            string? actual = reg.GetString(RegHive.CurrentUser, op.Key, string.IsNullOrEmpty(op.Name) ? null : op.Name);
            if (!string.Equals(actual, op.Value as string, StringComparison.Ordinal))
                problems.Add(L.F(L.S.RegMismatch, $"HKCU\\{op.Key} [{(string.IsNullOrEmpty(op.Name) ? L.S.RegDefaultValue : op.Name)}]"));
        }
        if (!File.Exists(exePath)) problems.Add(L.F(L.S.RegExeMissing, exePath));
        return problems;
    }

    /// <summary>Returns leftovers after uninstall; empty = clean.</summary>
    public static List<string> VerifyRemoved(IRegistryView reg)
    {
        var left = new List<string>();
        foreach (string key in OwnedKeys)
            if (reg.KeyExists(RegHive.CurrentUser, key)) left.Add(L.F(L.S.RegLeftKey, "HKCU\\" + key));
        if (reg.ValueExists(RegHive.CurrentUser, RegisteredAppsKey, AppInfo.AppName))
            left.Add(L.F(L.S.RegLeftValue, $"HKCU\\{RegisteredAppsKey} [{AppInfo.AppName}]"));
        return left;
    }
}
