namespace LazyBlox.Localization;

internal static partial class Lang
{
    private static Strings? _en;
    public static Strings En => _en ??= new Strings
    {
        AppTagline = "Join Roblox private servers directly — no browser in between",
        AppDescription = "Opens Roblox private server links directly in Roblox without a browser, and forwards all other links to your usual browser.",
        Cancel = "Cancel",
        Close = "Close",
        Back = "Back",
        Save = "Save",
        Copy = "Copy",
        Later = "Later",
        ErrorPrefix = "Error: {0}",
        On = "on",
        Off = "off",
        NotSet = "Not set",
        GenericBrowser = "your browser",

        UrlEmpty = "The URL is empty",
        UrlTooLong = "The URL is too long",
        UrlControlChars = "The URL contains control characters",
        UrlMalformed = "The URL is malformed",
        UrlUnsupportedScheme = "Unsupported scheme: {0}",
        UrlNoHost = "The URL has no host name",

        CmdEmpty = "The browser command is empty",
        CmdNotAbsolute = "The browser executable must be an absolute path: {0}",
        CmdIsSelf = "The target browser is this launcher itself (stopped to prevent an infinite loop)",
        CmdShellDispatcher = "{0} cannot be used as the browser because it would route the link back to this launcher (infinite loop prevention)",

        ErrWindowTitle = "Couldn't open the link",
        ErrCannotOpen = "This link can't be opened: {0}",
        ErrLoop = "Stopped because a forwarding loop was detected. Check the browser in settings.",
        ErrNoBrowser = "No browser is configured.",
        ErrBrowserFailed = "Couldn't start the browser.",
        ErrBrowserHint = "Choose the browser again in settings (run LazyBlox.exe without arguments).",
        LaunchShellExecuteFailed = "ShellExecuteEx failed (Win32 error {0})",
        LaunchExeNotFound = "Executable not found: {0}",
        LaunchCreateProcessFailed = "CreateProcess failed (Win32 error {0}): {1}",

        RegDefaultValue = "(Default)",
        RegDeleteKey = "(delete entire key)",
        RegDeleteValue = "delete value {0}",
        RegMismatch = "Mismatch: {0}",
        RegExeMissing = "Executable missing: {0}",
        RegLeftKey = "Leftover key: {0}",
        RegLeftValue = "Leftover value: {0}",

        PlanInstallHeader = "■ Changes made by installing (current user HKCU only, no administrator rights)",
        PlanCopy = "File: copy {0} → {1}",
        PlanRegistry = "Registry:",
        PlanConfig = "Settings file: {0}",
        PlanBrowser = "  Browser: {0} ({1})",
        PlanCommand = "  Command: {0}",
        PlanUnchanged = "Not changed: your default browser (UserChoice), other apps' registry, HKLM.",
        PlanSwitchYourself = "After installing, you switch the default app yourself in Windows Settings.",
        PlanUninstallHeader = "■ Changes made by uninstalling",
        PlanUninstallRegistry = "Registry (only what this app created):",
        PlanDeleteConfigDir = "Folder to delete: {0} (settings)",
        PlanDeleteInstallDir = "Folder to delete: {0} (install location, if present)",
        PlanUninstallUnchanged = "Not changed: other browsers' registration and settings. Re-select your default browser in Windows.",

        LogNoBrowser = "Error: no browser could be detected. Specify one with --browser.",
        LogBrowser = "Browser: {0} ({1})",
        LogVerifyProblems = "Verification found problems:",
        LogVerifyOk = "Registration verified: OK",
        LogNextStep = "Next: in Settings > Apps > Default apps > LazyBlox, set \"HTTPS\" and \"HTTP\" to LazyBlox.",
        LogRemovedClean = "Registry removal verified: nothing left",
        LogRemovedPartial = "Some registry entries remain",
        LogStillDefault = "Note: HTTPS still opens with LazyBlox. In Settings > Apps > Default apps, switch it back to your original browser ({0}).",
        LogBadCommand = "The registered command is invalid",
        LogCopied = "Copied: {0}",
        LogDeleted = "Deleted: {0}",
        LogDeleteFailed = "Couldn't delete: {0} ({1})",
        LogCannotAutoDelete = "This path can't be deleted automatically. Please delete it manually: {0}",
        LogDeleteAfterExit = "Will be deleted after exit: {0}",
        LogDeleteManually = "Please delete manually: {0}",
        NoteSpecifiedNotFound = "'{0}' was not found",
        NoteSpecified = "specified",
        NoteKeepExisting = "keeping existing setting",
        NoteNotDetected = "not detected",
        NoteDetected = "detected from current default browser",

        StRegistered = "Registered: {0}",
        StYes = "yes",
        StNo = "no",
        StVerify = "Verification: {0}",
        StExe = "Registered exe: {0}",
        StHttps = "Default HTTPS: {0}",
        StHttp = "Default HTTP:  {0}",
        StInUse = "  ← LazyBlox in use",
        StBrowser = "Browser: {0}",
        StCommand = "  Command: {0}",
        StRoblox = "Roblox (roblox: protocol): {0}",
        StRobloxFound = "registered",
        StRobloxMissing = "not found",

        CliNotInstalledClean = "Not installed (no leftover registry entries)",
        CliInstalledOk = "Installed, registration OK ({0})",
        CliSetBrowserUsage = "Usage: set-browser <ProgID | browser name | absolute exe path>",
        CliBrowserNotFound = "Not found. Run 'browsers' to see the list.",
        CliBrowserSet = "Browser: {0}\n  {1}",
        CliTiming = "Timing log: {0}  ({1})",
        CliLegacy = "Legacy private server link conversion: {0}",
        CliUnknownCommand = "Unknown command: {0}",
        CliNeedYes = "Re-run with --yes to apply the changes above (nothing was changed).",
        CliDryRunUsage = "Usage: dry-run <URL>",
        Help = """
            LazyBlox - fast launcher for Roblox private server links

            Usage:
              LazyBlox.exe                      Settings (GUI)
              LazyBlox.exe <URL>                Handle a link (how Windows calls it)
              LazyBlox.exe status               Show registration, default apps and browser
              LazyBlox.exe plan [uninstall]     Preview registry changes (changes nothing)
              LazyBlox.exe install [--yes] [--in-place] [--browser <name|ProgID|exe path>] [--no-settings]
              LazyBlox.exe uninstall [--yes] [--keep-config]
              LazyBlox.exe verify               Verify registration / leftovers
              LazyBlox.exe browsers             List detected browsers (* = current)
              LazyBlox.exe set-browser <name|ProgID|exe path>
              LazyBlox.exe default-apps         Open Windows "Default apps" settings
              LazyBlox.exe dry-run <URL>        Show decision and planned action (launches nothing)
              LazyBlox.exe bench [URL] [count]  Micro-benchmark of URL routing
              LazyBlox.exe timing on|off        Log processing time to timing.log
              LazyBlox.exe legacy on|off        Convert legacy /games/ID?privateServerLinkCode= links (experimental)

            Without --yes, install / uninstall only show the changes and do nothing.
            """,

        ConfirmInstallTitle = "Install LazyBlox?",
        ConfirmRepairTitle = "Repair the registration?",
        ConfirmInstallBody = "The following changes are made for the current user only. Your default browser and other apps' settings are not changed.",
        ConfirmUninstallTitle = "Uninstall LazyBlox?",
        ConfirmUninstallBody = "Only the registry entries and settings this app created are removed. If LazyBlox is still your default for links, choose your original browser again in Windows Settings afterwards.",
        ConfirmGenericBody = "The following changes will be made.",
        BtnInstall = "Install",
        BtnRepair = "Repair",
        BtnUninstall = "Uninstall",
        BtnOpenDefaultApps = "Open Default apps",
        InstalledTitle = "Installed",
        InstalledBody = "Last step: in Windows Settings, choose LazyBlox as the default app for \"HTTPS\" and \"HTTP\".",
        InstallFailedTitle = "Installation failed",
        InstallFailedBody = "See the details below.",
        UninstalledTitle = "Uninstalled",
        UninstallPartialTitle = "Some items couldn't be removed",
        UninstalledStillDefault = "HTTPS still opens with LazyBlox. Choose your original browser again in Windows Settings.",
        UninstalledThanks = "Thanks for using LazyBlox.",
        AfterUninstallHint = "If LazyBlox is still your default browser, choose your original browser again in Settings > Apps > Default apps.",

        SectionActions = "Actions",
        TileHttps = "HTTPS default",
        TileBrowser = "Browser",
        RobloxDetected = "Detected",
        RobloxMissing = "Not found",
        RowDefaultApps = "Open Default apps settings",
        RowDefaultAppsDesc = "Choose LazyBlox for HTTPS and HTTP (you switch it yourself)",
        RowInstall = "Install",
        RowRepair = "Repair / reinstall",
        RowInstallDesc = "Review the changes before anything happens. No administrator rights needed",
        RowBrowser = "Browser",
        RowBrowserDesc = "Browser for non-Roblox links: {0}",
        RowLegacy = "Also open legacy links in Roblox",
        RowLegacyDesc = "Supports /games/…?privateServerLinkCode= links (experimental)",
        RowTest = "Link tester",
        RowTestDesc = "See which links open in Roblox (launches nothing)",
        RowUninstallDesc = "Removes only the registry entries and settings this app created",
        HeroRobloxMissing = "Roblox wasn't found, so Roblox links will open in the browser too.",
        HeroNotInstalledPill = "Not installed",
        HeroNotInstalledTitle = "Install to get started",
        HeroNotInstalledDesc = "After installing, you can choose LazyBlox in Windows \"Default apps\". Your default browser isn't changed.",
        HeroBrokenPill = "Needs repair",
        HeroBrokenTitle = "There's a problem with the registration",
        HeroOneStepPill = "One more step",
        HeroOneStepTitle = "Switch the default app",
        HeroOneStepDesc = "Choose LazyBlox for \"HTTPS\" and \"HTTP\" in Settings so it can receive links from Discord and other apps.",
        HeroReadyPill = "Ready",
        HeroReadyTitle = "Roblox links launch directly",
        HeroReadyDesc = "Open a private server link in Discord or anywhere else and Roblox starts without a browser. Other links open in {0}.",
        HeroReadyHttpHint = "Set HTTP to LazyBlox too to handle http:// links.",

        BrowserTitle = "Browser",
        BrowserIntro = "Links that aren't Roblox private server links open in the browser you choose here. LazyBlox itself and launchers that would route back through the link association are not listed.",
        BrowserNone = "No registered browsers were found. You can set an exe path with the CLI command set-browser.",

        TestTitle = "Link tester",
        TestIntro = "Copy a URL, then press Paste to see how that link would be handled. Nothing is actually launched.",
        TestPaste = "Paste and test",
        TestSample = "Sample",
        TestInput = "Input",
        TestToRoblox = "Opens directly in Roblox",
        TestToBrowser = "Opens in the browser",
        TestRejected = "Not opened (rejected)",
        TestReason = "Reason",
        TestDeepLink = "Converted deep link",
        TestForwardUrl = "URL to forward",
        TestPlanned = "Planned action",
        TestActRoblox = "Hand to Roblox: {0}",
        TestActBrowser = "Start browser: {0}",
        TestActError = "Show error: {0}",
        TestClipboardEmpty = "There's no text on the clipboard.",
    };
}
