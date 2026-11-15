namespace LazyBlox.Tests;

/// <summary>Routing + launching logic with a mocked shell and registry (no processes are started).</summary>
public class LinkHandlerTests
{
    private const string ShareUrl = "https://www.roblox.com/share?code=0123456789abcdef0123456789abcdef&type=Server";
    private const string Deep = "roblox://navigation/share_links?code=0123456789abcdef0123456789abcdef&type=Server";
    private const string Chrome = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

    private static AppConfig ChromeConfig() => new()
    {
        BrowserName = "Google Chrome", BrowserProgId = "ChromeHTML", BrowserCommand = $"\"{Chrome}\" --single-argument %1",
    };

    private static (LinkHandler, RecordingShell) Make(AppConfig? cfg = null, bool roblox = true, FakeRegistry? reg = null,
        Func<ProcessInvocation, LaunchResult>? onStart = null, Func<string, LaunchResult>? onOpen = null, string? self = null)
    {
        var shell = new RecordingShell(robloxRegistered: roblox, onStart: onStart, onOpen: onOpen);
        var handler = new LinkHandler(shell, reg ?? FakeRegistry.TypicalMachine(), () => cfg ?? ChromeConfig(), self);
        return (handler, shell);
    }

    [Fact] // 1
    public void ShareLink_LaunchesDeepLink_WithoutTouchingBrowserOrConfig()
    {
        var (h, shell) = Make();
        Assert.Equal(ExitCodes.Ok, h.Handle(ShareUrl));
        Assert.Equal([Deep], shell.OpenedUris);
        Assert.Empty(shell.StartedProcesses);
        Assert.False(h.ConfigWasLoaded);
    }

    [Fact] // 2 + 3
    public void NormalLink_ForwardsToConfiguredBrowserExecutable()
    {
        var (h, shell) = Make();
        Assert.Equal(ExitCodes.Ok, h.Handle("https://www.roblox.com/games/1/x"));
        Assert.Empty(shell.OpenedUris); // never ShellExecute an https URL (would loop)
        ProcessInvocation p = Assert.Single(shell.StartedProcesses);
        Assert.Equal(Chrome, p.FileName);
        Assert.Equal(["--single-argument", "https://www.roblox.com/games/1/x"], p.Arguments);
        Assert.Contains(AppInfo.ForwardGuardEnvVar, shell.Environments[0]!.Keys);
    }

    [Fact] // 4
    public void InvalidUrl_ShowsError_LaunchesNothing()
    {
        var (h, shell) = Make();
        Assert.Equal(ExitCodes.Rejected, h.Handle("javascript:alert(1)"));
        Assert.Single(shell.Errors);
        Assert.Empty(shell.OpenedUris);
        Assert.Empty(shell.StartedProcesses);
    }

    [Fact] // 5
    public void InvalidShareCode_GoesToBrowser()
    {
        var (h, shell) = Make();
        h.Handle("https://www.roblox.com/share?code=<bad>&type=Server");
        Assert.Empty(shell.OpenedUris);
        Assert.Single(shell.StartedProcesses);
    }

    [Fact] // 6
    public void RobloxNotInstalled_FallsBackToBrowserWithOriginalLink()
    {
        var (h, shell) = Make(roblox: false);
        Assert.Equal(ExitCodes.Ok, h.Handle(ShareUrl));
        Assert.Empty(shell.OpenedUris);
        Assert.Equal(ShareUrl, Assert.Single(shell.StartedProcesses).Arguments[^1]);
    }

    [Fact] // 12
    public void DeepLinkLaunchFailure_FallsBackToBrowser()
    {
        var (h, shell) = Make(onOpen: _ => LaunchResult.Fail("boom"));
        Assert.Equal(ExitCodes.Ok, h.Handle(ShareUrl));
        Assert.Single(shell.OpenedUris);
        Assert.Equal(ShareUrl, Assert.Single(shell.StartedProcesses).Arguments[^1]);
    }

    [Fact] // 7
    public void BrowserMissing_AndNoFallback_ShowsError()
    {
        var cfg = new AppConfig { BrowserCommand = @"""C:\nope\nobrowser.exe"" %1", AutoDetectFallback = false };
        var (h, shell) = Make(cfg, onStart: _ => LaunchResult.Fail("not found"));
        Assert.Equal(ExitCodes.BrowserFailed, h.Handle("https://example.com/"));
        Assert.Contains("ブラウザを起動できませんでした", Assert.Single(shell.Errors));
    }

    [Fact] // 7
    public void ConfiguredBrowserFails_FallsBackToOtherRegisteredBrowser()
    {
        var cfg = new AppConfig { BrowserCommand = @"""C:\nope\gone.exe"" %1", AutoDetectFallback = true };
        var (h, shell) = Make(cfg, onStart: p => p.FileName.Contains("gone") ? LaunchResult.Fail("missing") : LaunchResult.Ok);
        Assert.Equal(ExitCodes.Ok, h.Handle("https://example.com/"));
        Assert.Equal(Chrome, shell.StartedProcesses[^1].FileName); // current default (ChromeHTML)
    }

    [Fact] // 7
    public void NoBrowserConfigured_NoBrowsersInstalled_ShowsError()
    {
        var (h, shell) = Make(new AppConfig(), reg: new FakeRegistry());
        Assert.Equal(ExitCodes.BrowserFailed, h.Handle("https://example.com/"));
        Assert.Single(shell.Errors);
        Assert.Empty(shell.StartedProcesses);
    }

    [Fact] // 9
    public void BrowserPointingToSelf_IsRefused_NoProcessStarted()
    {
        string self = @"C:\Users\u\AppData\Local\Programs\LazyBlox\LazyBlox.exe";
        var cfg = new AppConfig { BrowserCommand = $"\"{self}\" \"%1\"", AutoDetectFallback = false };
        var (h, shell) = Make(cfg, self: self);
        Assert.Equal(ExitCodes.BrowserFailed, h.Handle("https://example.com/"));
        Assert.Empty(shell.StartedProcesses);
        Assert.Contains("ループ", shell.Errors[0]);
    }

    [Fact] // 9
    public void ProgIdFallback_ThatResolvesToSelf_IsSkipped()
    {
        var reg = FakeRegistry.TypicalMachine();
        reg.Set(RegHive.CurrentUser, @"Software\Classes\Hijack\shell\open\command", null, "rundll32.exe url.dll,FileProtocolHandler %1");
        var cfg = new AppConfig { BrowserProgId = "Hijack", AutoDetectFallback = false };
        var (h, shell) = Make(cfg, reg: reg);
        Assert.Equal(ExitCodes.BrowserFailed, h.Handle("https://example.com/"));
        Assert.Empty(shell.StartedProcesses);
    }

    [Fact] // 11
    public void MultipleLinks_AreHandledIndependently()
    {
        var (h, shell) = Make();
        h.Handle(ShareUrl);
        h.Handle("https://example.com/1");
        h.Handle("https://www.roblox.com/share?code=ffffffffffffffff&type=Server");
        h.Handle("https://example.com/2");
        Assert.Equal(2, shell.OpenedUris.Count);
        Assert.Equal(2, shell.StartedProcesses.Count);
    }
}

public class BrowserLocatorTests
{
    [Fact]
    public void Enumerate_FindsBrowsers_PrefersProgIdCommand()
    {
        List<BrowserCandidate> all = BrowserLocator.Enumerate(FakeRegistry.TypicalMachine());
        BrowserCandidate chrome = Assert.Single(all, b => b.ProgId == "ChromeHTML");
        Assert.Contains("--single-argument", chrome.Command);
        Assert.Equal("Google Chrome", chrome.RegisteredAppName);
    }

    [Fact]
    public void Enumerate_ExcludesLazyBloxItself()
    {
        FakeRegistry reg = FakeRegistry.TypicalMachine();
        reg.Set(RegHive.CurrentUser, @"Software\Clients\StartMenuInternet\LazyBlox", null, "LazyBlox");
        reg.Set(RegHive.CurrentUser, @"Software\Clients\StartMenuInternet\LazyBlox\Capabilities\URLAssociations", "https", AppInfo.ProgId);
        reg.Set(RegHive.CurrentUser, @"Software\Classes\LazyBloxURL\shell\open\command", null, "\"C:\\x\\LazyBlox.exe\" \"%1\"");
        Assert.DoesNotContain(BrowserLocator.Enumerate(reg), b => b.Name == "LazyBlox");
    }

    [Fact]
    public void DetectDefault_UsesCurrentUserChoice()
    {
        FakeRegistry reg = FakeRegistry.TypicalMachine();
        Assert.Equal("ChromeHTML", BrowserLocator.DetectDefault(reg, BrowserLocator.Enumerate(reg))!.ProgId);
    }

    [Fact]
    public void DetectDefault_WhenUserChoiceIsLazyBlox_FallsBackToEdge()
    {
        FakeRegistry reg = FakeRegistry.TypicalMachine();
        reg.Set(RegHive.CurrentUser, @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice", "ProgId", AppInfo.ProgId);
        Assert.Equal("MSEdgeHTM", BrowserLocator.DetectDefault(reg, BrowserLocator.Enumerate(reg))!.ProgId);
    }

    [Fact]
    public void UserChoiceLatest_WinsOverStaleUserChoice()
    {
        // Seen on a real Windows 11 machine: UserChoice still says Chrome after switching to LazyBlox.
        FakeRegistry reg = FakeRegistry.TypicalMachine();
        reg.Set(RegHive.CurrentUser, @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoiceLatest\ProgId", "ProgId", AppInfo.ProgId);
        Assert.Equal(AppInfo.ProgId, BrowserLocator.GetUserChoiceProgId(reg, "https"));
        Assert.Equal("MSEdgeHTM", BrowserLocator.DetectDefault(reg, BrowserLocator.Enumerate(reg))!.ProgId);
    }

    [Fact]
    public void UserChoice_UsedWhenNoLatestKey()
    {
        FakeRegistry reg = FakeRegistry.TypicalMachine();
        Assert.Equal("ChromeHTML", BrowserLocator.GetUserChoiceProgId(reg, "https"));
    }

    [Fact]
    public void ProgIdCommand_HkcuOverridesHklm_AndRejectsPathTricks()
    {
        FakeRegistry reg = FakeRegistry.TypicalMachine();
        reg.Set(RegHive.CurrentUser, @"Software\Classes\ChromeHTML\shell\open\command", null, "\"C:\\user\\chrome.exe\" %1");
        Assert.Equal("\"C:\\user\\chrome.exe\" %1", BrowserLocator.GetProgIdCommand(reg, "ChromeHTML"));
        Assert.Null(BrowserLocator.GetProgIdCommand(reg, @"..\ChromeHTML"));
    }
}

public class RegistrationPlanTests
{
    private const string Exe = @"C:\Users\u\AppData\Local\Programs\LazyBlox\LazyBlox.exe";

    [Fact]
    public void InstallPlan_OnlyTouchesOwnedKeys_AndNeverUserChoice()
    {
        List<RegOp> plan = RegistrationPlan.BuildInstall(Exe);
        Assert.All(plan, op =>
        {
            Assert.DoesNotContain("UserChoice", op.Key);
            Assert.True(RegistrationPlan.OwnedKeys.Any(k => op.Key.StartsWith(k, StringComparison.Ordinal))
                        || (op.Key == RegistrationPlan.RegisteredAppsKey && op.Name == AppInfo.AppName), op.Key);
        });
    }

    [Fact]
    public void InstallPlan_RegistersHttpAndHttpsCapabilities_AndQuotedCommand()
    {
        List<RegOp> plan = RegistrationPlan.BuildInstall(Exe);
        Assert.Contains(plan, o => o.Key.EndsWith(@"Capabilities\URLAssociations") && o.Name == "https" && (string)o.Value! == AppInfo.ProgId);
        Assert.Contains(plan, o => o.Key.EndsWith(@"Capabilities\URLAssociations") && o.Name == "http");
        Assert.Contains(plan, o => o.Key.EndsWith(@"shell\open\command") && o.Key.Contains(AppInfo.ProgId) && (string)o.Value! == $"\"{Exe}\" \"%1\"");
        Assert.Contains(plan, o => o.Key == RegistrationPlan.RegisteredAppsKey && (string)o.Value! == RegistrationPlan.CapabilitiesKey);
    }

    [Fact]
    public void UninstallPlan_RemovesExactlyWhatInstallCreated()
    {
        List<RegOp> un = RegistrationPlan.BuildUninstall();
        Assert.All(un, op => Assert.NotEqual(RegOpKind.SetValue, op.Kind));
        Assert.Contains(un, o => o.Kind == RegOpKind.DeleteValue && o.Key == RegistrationPlan.RegisteredAppsKey && o.Name == AppInfo.AppName);
        Assert.DoesNotContain(un, o => o.Kind == RegOpKind.DeleteKeyTree && o.Key == RegistrationPlan.RegisteredAppsKey);
        foreach (string k in RegistrationPlan.OwnedKeys) Assert.Contains(un, o => o.Kind == RegOpKind.DeleteKeyTree && o.Key == k);
    }

    [Fact]
    public void Describe_ListsEveryOperation()
    {
        List<RegOp> plan = RegistrationPlan.BuildInstall(Exe);
        string text = RegistrationPlan.Describe(plan);
        foreach (RegOp op in plan)
        {
            Assert.Contains("HKCU\\" + op.Key, text);
            Assert.Contains((string.IsNullOrEmpty(op.Name) ? "(既定)" : op.Name) + " = ", text);
        }
        string un = RegistrationPlan.Describe(RegistrationPlan.BuildUninstall());
        Assert.All(RegistrationPlan.BuildUninstall(), op => Assert.Contains("- HKCU\\" + op.Key, un));
    }
}
