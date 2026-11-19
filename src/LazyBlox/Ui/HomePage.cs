using LazyBlox.Localization;
using System.Text;

namespace LazyBlox.Ui;

/// <summary>Main settings screen: status overview + actions.</summary>
internal sealed class HomePage : Page
{
    private InstallStatus _status = null!;
    private List<BrowserCandidate> _browsers = [];

    public override void OnShown() => Refresh();

    private void Refresh()
    {
        _status = new Installer(WindowsRegistry.Instance, _ => { }).GetStatus();
        _browsers = BrowserLocator.Enumerate(WindowsRegistry.Instance);
    }

    private string FriendlyProgId(string? progId)
    {
        if (progId is null) return L.S.NotSet;
        if (progId == AppInfo.ProgId) return "LazyBlox";
        return _browsers.FirstOrDefault(b => string.Equals(b.ProgId, progId, StringComparison.OrdinalIgnoreCase))?.Name ?? progId;
    }

    public override void Render(Ui ui)
    {
        InstallStatus s = _status;
        ui.Y = 28;

        // ---- header ----
        ui.Logo(Ui.Pad, ui.Y, 48);
        ui.G.Text("LazyBlox", Ui.Pad + 64, ui.Y - 1, ui.Inner - 64, FontKind.Display, T.Text, wrap: false);
        ui.G.Text(L.S.AppTagline, Ui.Pad + 65, ui.Y + 32, ui.Inner - 65,
            FontKind.Small, T.SubText, wrap: false);
        ui.Y += 48 + 26;

        // ---- hero status card ----
        (MessageKind kind, string pill, string headline, string desc, string? cta, Action? ctaAction) = Hero(s);
        float pad = 20, innerW = ui.Inner - pad * 2;
        float headH = ui.G.Measure(headline, innerW, FontKind.Title);
        float descH = ui.G.Measure(desc, innerW, FontKind.Small);
        float cardH = pad + 24 + 12 + headH + 4 + descH + (cta is null ? 0 : 16 + 36) + pad;
        RectF card = new(Ui.Pad, ui.Y, ui.Inner, cardH);
        ui.CardBg(card);
        (uint c, uint soft) = kind switch
        {
            MessageKind.Success => (T.Success, T.SuccessSoft),
            MessageKind.Warning => (T.Warning, T.WarningSoft),
            MessageKind.Error => (T.Danger, T.DangerSoft),
            _ => (T.Info, T.InfoSoft),
        };
        float y = ui.Y + pad;
        ui.Pill(card.X + pad, y, pill, c, soft);
        y += 24 + 12;
        y += ui.G.Text(headline, card.X + pad, y, innerW, FontKind.Title, T.Text) + 4;
        y += ui.G.Text(desc, card.X + pad, y, innerW, FontKind.Small, T.SubText);
        if (cta is not null && ctaAction is not null)
        {
            y += 16;
            float bw = ui.G.MeasureWidth(cta, FontKind.BodyBold) + 44;
            ui.Button("hero", new RectF(card.X + pad, y, bw, 36), cta, ButtonKind.Primary, ctaAction);
        }
        ui.Y += cardH + 12;

        // ---- facts ----
        float gap = 10, tileW = (ui.Inner - gap * 2) / 3;
        Tile(ui, Ui.Pad, L.S.TileHttps, FriendlyProgId(s.HttpsProgId), s.HttpsIsLazyBlox ? T.Success : T.Faint, tileW);
        Tile(ui, Ui.Pad + tileW + gap, L.S.TileBrowser,
            string.IsNullOrEmpty(s.BrowserName) ? L.S.NotSet : s.BrowserName, string.IsNullOrEmpty(s.BrowserName) ? T.Faint : T.Info, tileW);
        Tile(ui, Ui.Pad + (tileW + gap) * 2, "Roblox", s.RobloxRegistered ? L.S.RobloxDetected : L.S.RobloxMissing,
            s.RobloxRegistered ? T.Success : T.Danger, tileW);
        ui.Y += 66 + 18;

        // ---- actions ----
        ui.SectionLabel(L.S.SectionActions);
        if (s.Registered)
        {
            ui.Row("defaultapps", Glyph.Settings, L.S.RowDefaultApps, L.S.RowDefaultAppsDesc,
                OpenDefaultApps, RowTrail.External);
        }
        ui.Row("install", s.Registered ? Glyph.Repair : Glyph.Download,
            s.Registered ? L.S.RowRepair : L.S.RowInstall,
            L.S.RowInstallDesc, ConfirmInstall);
        ui.Row("browser", Glyph.Globe, L.S.RowBrowser,
            L.F(L.S.RowBrowserDesc, string.IsNullOrEmpty(s.BrowserName) ? L.S.NotSet : s.BrowserName),
            () => Host.Navigate(new BrowserPage(this)));
        ui.Row("legacy", Glyph.History, L.S.RowLegacy,
            L.S.RowLegacyDesc, ToggleLegacy, RowTrail.Toggle, on: s.LegacyEnabled);
        ui.Row("restart", Glyph.Game, L.S.RowRestartIfStuck,
            L.S.RowRestartIfStuckDesc, ToggleRestartIfStuck, RowTrail.Toggle, on: s.RestartIfStuck);
        ui.Row("test", Glyph.Search, L.S.RowTest, L.S.RowTestDesc,
            () => Host.Navigate(new TestPage(this)));
        if (s.Registered)
            ui.Row("uninstall", Glyph.Delete, L.S.BtnUninstall, L.S.RowUninstallDesc,
                ConfirmUninstall, danger: true);

        ui.Y += 8;
        ui.G.Text($"v{AppInfo.Version}  ·  {AppInfo.ConfigPath}", Ui.Pad, ui.Y, ui.Inner, FontKind.MonoSmall, T.Faint, wrap: false);
        ui.Y += 40;
    }

    private (MessageKind, string, string, string, string?, Action?) Hero(InstallStatus s)
    {
        string robloxNote = s.RobloxRegistered ? "" : "\n" + L.S.HeroRobloxMissing;
        if (!s.Registered)
            return (MessageKind.Info, L.S.HeroNotInstalledPill, L.S.HeroNotInstalledTitle,
                L.S.HeroNotInstalledDesc + robloxNote,
                L.S.BtnInstall, ConfirmInstall);
        if (s.Problems.Count > 0)
            return (MessageKind.Error, L.S.HeroBrokenPill, L.S.HeroBrokenTitle, string.Join("\n", s.Problems), L.S.BtnRepair, ConfirmInstall);
        if (!s.HttpsIsLazyBlox)
            return (MessageKind.Warning, L.S.HeroOneStepPill, L.S.HeroOneStepTitle,
                L.S.HeroOneStepDesc + robloxNote,
                L.S.BtnOpenDefaultApps, OpenDefaultApps);
        return (MessageKind.Success, L.S.HeroReadyPill, L.S.HeroReadyTitle,
            L.F(L.S.HeroReadyDesc, string.IsNullOrEmpty(s.BrowserName) ? L.S.GenericBrowser : s.BrowserName)
            + (s.HttpIsLazyBlox ? "" : "\n" + L.S.HeroReadyHttpHint) + robloxNote,
            null, null);
    }

    private void Tile(Ui ui, float x, string label, string value, uint dot, float w)
    {
        RectF r = new(x, ui.Y, w, 66);
        ui.CardBg(r);
        ui.G.Text(label, x + 14, ui.Y + 12, w - 28, FontKind.Caption, T.SubText, wrap: false);
        ui.G.Circle(x + 18, ui.Y + 45, 4, dot);
        ui.G.Text(value, x + 28, ui.Y + 35, w - 40, FontKind.BodyBold, T.Text, wrap: false);
    }

    // ---------------- actions ----------------

    private void OpenDefaultApps() => Installer.OpenDefaultAppsSettings(WindowsShell.Instance);

    private void ToggleLegacy()
    {
        AppConfig cfg = AppConfig.Load();
        cfg.LegacyPrivateServerLinks = !cfg.LegacyPrivateServerLinks;
        cfg.Save();
        Refresh();
    }

    private void ToggleRestartIfStuck()
    {
        AppConfig cfg = AppConfig.Load();
        cfg.RestartIfStuck = !cfg.RestartIfStuck;
        cfg.Save();
        Refresh();
    }

    private void ConfirmInstall()
    {
        var installer = new Installer(WindowsRegistry.Instance, _ => { });
        string plan = installer.DescribeInstall(new InstallOptions());
        Host.Navigate(new DialogPage(MessageKind.Info,
            _status.Registered ? L.S.ConfirmRepairTitle : L.S.ConfirmInstallTitle,
            L.S.ConfirmInstallBody,
            plan,
            [
                new(L.S.Cancel, ButtonKind.Secondary, w => w.Navigate(this)),
                new(_status.Registered ? L.S.BtnRepair : L.S.BtnInstall, ButtonKind.Primary, _ => RunInstall()),
            ],
            w => w.Navigate(this)));
    }

    private void RunInstall()
    {
        var log = new StringBuilder();
        bool ok = Guard(() => new Installer(WindowsRegistry.Instance, l => log.AppendLine(l)).Install(new InstallOptions()), log);
        Refresh();
        Host.Navigate(ok
            ? new DialogPage(MessageKind.Success, L.S.InstalledTitle,
                L.S.InstalledBody, log.ToString().TrimEnd(),
                [
                    new(L.S.Later, ButtonKind.Secondary, w => w.Navigate(this)),
                    new(L.S.BtnOpenDefaultApps, ButtonKind.Primary, w => { OpenDefaultApps(); w.Navigate(this); }, Glyph.OpenExternal),
                ], w => w.Navigate(this))
            : new DialogPage(MessageKind.Error, L.S.InstallFailedTitle, L.S.InstallFailedBody, log.ToString().TrimEnd(),
                [new(L.S.Back, ButtonKind.Primary, w => w.Navigate(this))], w => w.Navigate(this)));
    }

    private void ConfirmUninstall()
    {
        var installer = new Installer(WindowsRegistry.Instance, _ => { });
        Host.Navigate(new DialogPage(MessageKind.Warning, L.S.ConfirmUninstallTitle,
            L.S.ConfirmUninstallBody,
            installer.DescribeUninstall(),
            [
                new(L.S.Cancel, ButtonKind.Secondary, w => w.Navigate(this)),
                new(L.S.BtnUninstall, ButtonKind.Danger, _ => RunUninstall()),
            ],
            w => w.Navigate(this)));
    }

    private void RunUninstall()
    {
        var log = new StringBuilder();
        bool wasDefault = _status.HttpsIsLazyBlox;
        bool ok = Guard(() => new Installer(WindowsRegistry.Instance, l => log.AppendLine(l)).Uninstall(keepConfig: false), log);
        Refresh();
        Host.Navigate(new DialogPage(ok ? MessageKind.Success : MessageKind.Error,
            ok ? L.S.UninstalledTitle : L.S.UninstallPartialTitle,
            wasDefault ? L.S.UninstalledStillDefault : L.S.UninstalledThanks,
            log.ToString().TrimEnd(),
            wasDefault
                ? [new(L.S.Close, ButtonKind.Secondary, w => w.Close()),
                   new(L.S.BtnOpenDefaultApps, ButtonKind.Primary, w => { Installer.OpenDefaultAppsSettings(WindowsShell.Instance, null); w.Close(); }, Glyph.OpenExternal)]
                : [new(L.S.Close, ButtonKind.Primary, w => w.Close())]));
    }

    private static bool Guard(Func<bool> action, StringBuilder log)
    {
        try { return action(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            log.AppendLine(L.F(L.S.ErrorPrefix, e.Message));
            return false;
        }
    }
}
