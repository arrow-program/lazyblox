namespace LazyBlox.Localization;

/// <summary>
/// Every user-visible string. All members are <c>required</c>, so a language that misses a string
/// does not compile. "{0}" style placeholders are formatted with <see cref="L.F"/>; unit tests check
/// that each translation uses the same placeholders as English.
/// </summary>
internal sealed class Strings
{
    // ---- general ----
    public required string AppTagline { get; init; }
    public required string AppDescription { get; init; }
    public required string Cancel { get; init; }
    public required string Close { get; init; }
    public required string Back { get; init; }
    public required string Save { get; init; }
    public required string Copy { get; init; }
    public required string Later { get; init; }
    public required string ErrorPrefix { get; init; }          // {0} = message
    public required string On { get; init; }
    public required string Off { get; init; }
    public required string NotSet { get; init; }
    public required string GenericBrowser { get; init; }

    // ---- URL validation ----
    public required string UrlEmpty { get; init; }
    public required string UrlTooLong { get; init; }
    public required string UrlControlChars { get; init; }
    public required string UrlMalformed { get; init; }
    public required string UrlUnsupportedScheme { get; init; } // {0} = scheme
    public required string UrlNoHost { get; init; }

    // ---- browser command ----
    public required string CmdEmpty { get; init; }
    public required string CmdNotAbsolute { get; init; }       // {0} = exe
    public required string CmdIsSelf { get; init; }
    public required string CmdShellDispatcher { get; init; }   // {0} = file name

    // ---- link handling errors ----
    public required string ErrWindowTitle { get; init; }
    public required string ErrCannotOpen { get; init; }        // {0} = reason
    public required string ErrLoop { get; init; }
    public required string ErrNoBrowser { get; init; }
    public required string ErrBrowserFailed { get; init; }
    public required string ErrBrowserHint { get; init; }
    public required string LaunchShellExecuteFailed { get; init; } // {0} = win32 error
    public required string LaunchExeNotFound { get; init; }        // {0} = path
    public required string LaunchCreateProcessFailed { get; init; } // {0} = win32 error, {1} = path

    // ---- registry plan / verification ----
    public required string RegDefaultValue { get; init; }
    public required string RegDeleteKey { get; init; }
    public required string RegDeleteValue { get; init; }       // {0} = value name
    public required string RegMismatch { get; init; }          // {0} = key [value]
    public required string RegExeMissing { get; init; }        // {0} = path
    public required string RegLeftKey { get; init; }           // {0} = key
    public required string RegLeftValue { get; init; }         // {0} = key [value]

    // ---- install / uninstall plan ----
    public required string PlanInstallHeader { get; init; }
    public required string PlanCopy { get; init; }             // {0} = from, {1} = to
    public required string PlanRegistry { get; init; }
    public required string PlanConfig { get; init; }           // {0} = path
    public required string PlanBrowser { get; init; }          // {0} = name, {1} = note
    public required string PlanCommand { get; init; }          // {0} = command
    public required string PlanUnchanged { get; init; }
    public required string PlanSwitchYourself { get; init; }
    public required string PlanUninstallHeader { get; init; }
    public required string PlanUninstallRegistry { get; init; }
    public required string PlanDeleteConfigDir { get; init; }  // {0} = path
    public required string PlanDeleteInstallDir { get; init; } // {0} = path
    public required string PlanUninstallUnchanged { get; init; }

    // ---- installer log ----
    public required string LogNoBrowser { get; init; }
    public required string LogBrowser { get; init; }           // {0} = name, {1} = note
    public required string LogVerifyProblems { get; init; }
    public required string LogVerifyOk { get; init; }
    public required string LogNextStep { get; init; }
    public required string LogRemovedClean { get; init; }
    public required string LogRemovedPartial { get; init; }
    public required string LogStillDefault { get; init; }      // {0} = original browser
    public required string LogBadCommand { get; init; }
    public required string LogCopied { get; init; }            // {0} = path
    public required string LogDeleted { get; init; }           // {0} = path
    public required string LogDeleteFailed { get; init; }      // {0} = path, {1} = message
    public required string LogCannotAutoDelete { get; init; }  // {0} = path
    public required string LogDeleteAfterExit { get; init; }   // {0} = path
    public required string LogDeleteManually { get; init; }    // {0} = path
    public required string NoteSpecifiedNotFound { get; init; } // {0} = spec
    public required string NoteSpecified { get; init; }
    public required string NoteKeepExisting { get; init; }
    public required string NoteNotDetected { get; init; }
    public required string NoteDetected { get; init; }

    // ---- status (CLI) ----
    public required string StRegistered { get; init; }         // {0} = yes/no
    public required string StYes { get; init; }
    public required string StNo { get; init; }
    public required string StVerify { get; init; }             // {0} = result
    public required string StExe { get; init; }                // {0} = path
    public required string StHttps { get; init; }              // {0} = progid
    public required string StHttp { get; init; }               // {0} = progid
    public required string StInUse { get; init; }
    public required string StBrowser { get; init; }            // {0} = name
    public required string StCommand { get; init; }            // {0} = command
    public required string StRoblox { get; init; }             // {0} = state
    public required string StRobloxFound { get; init; }
    public required string StRobloxMissing { get; init; }

    // ---- CLI ----
    public required string CliNotInstalledClean { get; init; }
    public required string CliInstalledOk { get; init; }       // {0} = exe
    public required string CliSetBrowserUsage { get; init; }
    public required string CliBrowserNotFound { get; init; }
    public required string CliBrowserSet { get; init; }        // {0} = name, {1} = command
    public required string CliTiming { get; init; }            // {0} = on/off, {1} = path
    public required string CliLegacy { get; init; }            // {0} = on/off
    public required string CliUnknownCommand { get; init; }    // {0} = command
    public required string CliNeedYes { get; init; }
    public required string CliDryRunUsage { get; init; }
    public required string Help { get; init; }

    // ---- confirmations / results (GUI + CLI) ----
    public required string ConfirmInstallTitle { get; init; }
    public required string ConfirmRepairTitle { get; init; }
    public required string ConfirmInstallBody { get; init; }
    public required string ConfirmUninstallTitle { get; init; }
    public required string ConfirmUninstallBody { get; init; }
    public required string ConfirmGenericBody { get; init; }
    public required string BtnInstall { get; init; }
    public required string BtnRepair { get; init; }
    public required string BtnUninstall { get; init; }
    public required string BtnOpenDefaultApps { get; init; }
    public required string InstalledTitle { get; init; }
    public required string InstalledBody { get; init; }
    public required string InstallFailedTitle { get; init; }
    public required string InstallFailedBody { get; init; }
    public required string UninstalledTitle { get; init; }
    public required string UninstallPartialTitle { get; init; }
    public required string UninstalledStillDefault { get; init; }
    public required string UninstalledThanks { get; init; }
    public required string AfterUninstallHint { get; init; }

    // ---- home ----
    public required string SectionActions { get; init; }
    public required string TileHttps { get; init; }
    public required string TileBrowser { get; init; }
    public required string RobloxDetected { get; init; }
    public required string RobloxMissing { get; init; }
    public required string RowDefaultApps { get; init; }
    public required string RowDefaultAppsDesc { get; init; }
    public required string RowInstall { get; init; }
    public required string RowRepair { get; init; }
    public required string RowInstallDesc { get; init; }
    public required string RowBrowser { get; init; }
    public required string RowBrowserDesc { get; init; }       // {0} = browser
    public required string RowLegacy { get; init; }
    public required string RowLegacyDesc { get; init; }
    public required string RowRestartIfStuck { get; init; }
    public required string RowRestartIfStuckDesc { get; init; }
    public required string StuckTitle { get; init; }
    public required string StuckBody { get; init; }
    public required string BtnRestartRoblox { get; init; }
    public required string ErrRestartFailed { get; init; }     // {0} = detail
    public required string ErrRobloxStillRunning { get; init; } // {0} = process id
    public required string RowTest { get; init; }
    public required string RowTestDesc { get; init; }
    public required string RowUninstallDesc { get; init; }
    public required string HeroRobloxMissing { get; init; }
    public required string HeroNotInstalledPill { get; init; }
    public required string HeroNotInstalledTitle { get; init; }
    public required string HeroNotInstalledDesc { get; init; }
    public required string HeroBrokenPill { get; init; }
    public required string HeroBrokenTitle { get; init; }
    public required string HeroOneStepPill { get; init; }
    public required string HeroOneStepTitle { get; init; }
    public required string HeroOneStepDesc { get; init; }
    public required string HeroReadyPill { get; init; }
    public required string HeroReadyTitle { get; init; }
    public required string HeroReadyDesc { get; init; }        // {0} = browser
    public required string HeroReadyHttpHint { get; init; }

    // ---- browser page ----
    public required string BrowserTitle { get; init; }
    public required string BrowserIntro { get; init; }
    public required string BrowserNone { get; init; }

    // ---- test page ----
    public required string TestTitle { get; init; }
    public required string TestIntro { get; init; }
    public required string TestPaste { get; init; }
    public required string TestSample { get; init; }
    public required string TestInput { get; init; }
    public required string TestToRoblox { get; init; }
    public required string TestToBrowser { get; init; }
    public required string TestRejected { get; init; }
    public required string TestReason { get; init; }
    public required string TestDeepLink { get; init; }
    public required string TestForwardUrl { get; init; }
    public required string TestPlanned { get; init; }
    public required string TestActRoblox { get; init; }        // {0} = deep link
    public required string TestActBrowser { get; init; }       // {0} = command
    public required string TestActError { get; init; }         // {0} = message
    public required string TestClipboardEmpty { get; init; }
}
