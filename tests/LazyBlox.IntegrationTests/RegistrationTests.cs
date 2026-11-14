using Microsoft.Win32;

namespace LazyBlox.IntegrationTests;

/// <summary>
/// Writes the real registration into a sandbox key (HKCU\Software\LazyBloxTest\&lt;guid&gt;\...) so the user's
/// actual associations are never touched, then verifies install / uninstall / repeat behavior.
/// </summary>
public class RegistrationTests : IDisposable
{
    private readonly string _prefix = @"Software\LazyBloxTest\" + Guid.NewGuid().ToString("N");
    private readonly string _home = Path.Combine(Path.GetTempPath(), "lazyblox-reg-" + Guid.NewGuid().ToString("N"));
    private readonly string? _oldHome = Environment.GetEnvironmentVariable(AppInfo.HomeEnvVar);
    private readonly List<string> _log = [];

    public RegistrationTests() => Environment.SetEnvironmentVariable(AppInfo.HomeEnvVar, _home);

    private (Installer, WindowsRegistry) Make()
    {
        var reg = new WindowsRegistry(_prefix);
        return (new Installer(reg, _log.Add), reg);
    }

    [Fact]
    public void Install_Verify_Uninstall_LeavesNothing()
    {
        var (inst, reg) = Make();
        Assert.True(inst.Install(new InstallOptions(InPlace: true)), string.Join("\n", _log));
        Assert.Empty(RegistrationPlan.VerifyInstalled(reg, AppInfo.CurrentExePath!));
        Assert.Equal(RegistrationPlan.CapabilitiesKey,
            reg.GetString(RegHive.CurrentUser, RegistrationPlan.RegisteredAppsKey, AppInfo.AppName));
        AppConfig cfg = AppConfig.Load();
        Assert.False(string.IsNullOrEmpty(cfg.BrowserCommand)); // detected from HKLM browsers on this PC

        Assert.True(inst.Uninstall(keepConfig: false), string.Join("\n", _log));
        Assert.Empty(RegistrationPlan.VerifyRemoved(reg));
        Assert.False(Directory.Exists(_home));
    }

    [Fact] // 10
    public void RepeatedInstallUninstall_IsIdempotent()
    {
        var (inst, reg) = Make();
        for (int i = 0; i < 5; i++)
        {
            Assert.True(inst.Install(new InstallOptions(InPlace: true)));
            Assert.True(inst.Install(new InstallOptions(InPlace: true))); // reinstall over existing
            Assert.Empty(RegistrationPlan.VerifyInstalled(reg, AppInfo.CurrentExePath!));
            Assert.True(inst.Uninstall(keepConfig: true));
            Assert.True(inst.Uninstall(keepConfig: true)); // uninstall twice is harmless
            Assert.Empty(RegistrationPlan.VerifyRemoved(reg));
        }
    }

    [Fact]
    public void Uninstall_KeepsForeignValuesInSharedKeys()
    {
        var (inst, reg) = Make();
        reg.SetValue(RegistrationPlan.RegisteredAppsKey, "SomeOtherBrowser", @"Software\Other\Capabilities", RegistryValueKind.String);
        inst.Install(new InstallOptions(InPlace: true));
        inst.Uninstall(keepConfig: true);
        Assert.Equal(@"Software\Other\Capabilities", reg.GetString(RegHive.CurrentUser, RegistrationPlan.RegisteredAppsKey, "SomeOtherBrowser"));
    }

    [Fact]
    public void Reinstall_KeepsUserChosenBrowser()
    {
        var (inst, _) = Make();
        Directory.CreateDirectory(_home);
        string notepad = Path.Combine(Environment.SystemDirectory, "notepad.exe");
        new AppConfig { BrowserName = "Custom", BrowserCommand = $"\"{notepad}\" %1" }.Save();
        inst.Install(new InstallOptions(InPlace: true));
        Assert.Equal("Custom", AppConfig.Load().BrowserName);
    }

    [Fact]
    public void Install_DoesNotTouchRealUserChoice()
    {
        string? before = BrowserLocator.GetUserChoiceProgId(WindowsRegistry.Instance, "https");
        var (inst, _) = Make();
        inst.Install(new InstallOptions(InPlace: true));
        inst.Uninstall(keepConfig: false);
        Assert.Equal(before, BrowserLocator.GetUserChoiceProgId(WindowsRegistry.Instance, "https"));
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_prefix, throwOnMissingSubKey: false);
        using (RegistryKey? parent = Registry.CurrentUser.OpenSubKey(@"Software\LazyBloxTest", writable: true))
            if (parent is not null && parent.SubKeyCount == 0) Registry.CurrentUser.DeleteSubKey(@"Software\LazyBloxTest", false);
        Environment.SetEnvironmentVariable(AppInfo.HomeEnvVar, _oldHome);
        try { if (Directory.Exists(_home)) Directory.Delete(_home, true); } catch (IOException) { }
    }
}
