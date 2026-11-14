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
        if (progId is null) return "未設定";
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
        ui.G.Text("Roblox のプライベートサーバーへ、ブラウザを経由せず直接参加", Ui.Pad + 65, ui.Y + 32, ui.Inner - 65,
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
        Tile(ui, Ui.Pad, "HTTPS の既定", FriendlyProgId(s.HttpsProgId), s.HttpsIsLazyBlox ? T.Success : T.Faint, tileW);
        Tile(ui, Ui.Pad + tileW + gap, "転送先ブラウザ",
            string.IsNullOrEmpty(s.BrowserName) ? "未設定" : s.BrowserName, string.IsNullOrEmpty(s.BrowserName) ? T.Faint : T.Info, tileW);
        Tile(ui, Ui.Pad + (tileW + gap) * 2, "Roblox", s.RobloxRegistered ? "検出済み" : "見つかりません",
            s.RobloxRegistered ? T.Success : T.Danger, tileW);
        ui.Y += 66 + 18;

        // ---- actions ----
        ui.SectionLabel("操作");
        if (s.Registered)
        {
            ui.Row("defaultapps", Glyph.Settings, "既定のアプリ設定を開く", "HTTPS と HTTP で LazyBlox を選びます（ご自身で切り替え）",
                OpenDefaultApps, RowTrail.External);
        }
        ui.Row("install", s.Registered ? Glyph.Repair : Glyph.Download,
            s.Registered ? "登録を修復 / 再インストール" : "インストール",
            "変更内容を確認してから実行します。管理者権限は不要です", ConfirmInstall);
        ui.Row("browser", Glyph.Globe, "転送先ブラウザ",
            "Roblox 以外のリンクを開くブラウザ: " + (string.IsNullOrEmpty(s.BrowserName) ? "未設定" : s.BrowserName),
            () => Host.Navigate(new BrowserPage(this)));
        ui.Row("legacy", Glyph.History, "旧形式リンクも Roblox で開く",
            "/games/…?privateServerLinkCode= 形式に対応（実験的）", ToggleLegacy, RowTrail.Toggle, on: s.LegacyEnabled);
        ui.Row("test", Glyph.Search, "リンクの判定テスト", "どのリンクが Roblox で開くかを確認します（起動はしません）",
            () => Host.Navigate(new TestPage(this)));
        if (s.Registered)
            ui.Row("uninstall", Glyph.Delete, "アンインストール", "このアプリが作成したレジストリと設定だけを削除します",
                ConfirmUninstall, danger: true);

        ui.Y += 8;
        ui.G.Text($"v{AppInfo.Version}  ·  {AppInfo.ConfigPath}", Ui.Pad, ui.Y, ui.Inner, FontKind.MonoSmall, T.Faint, wrap: false);
        ui.Y += 40;
    }

    private (MessageKind, string, string, string, string?, Action?) Hero(InstallStatus s)
    {
        string robloxNote = s.RobloxRegistered ? "" : "\nRoblox が見つからないため、Roblox のリンクもブラウザで開きます。";
        if (!s.Registered)
            return (MessageKind.Info, "未インストール", "まずはインストール",
                "インストールすると、Windows の「既定のアプリ」で LazyBlox を選べるようになります。既定のブラウザは変更しません。" + robloxNote,
                "インストール", ConfirmInstall);
        if (s.Problems.Count > 0)
            return (MessageKind.Error, "要修復", "登録に問題があります", string.Join("\n", s.Problems), "修復する", ConfirmInstall);
        if (!s.HttpsIsLazyBlox)
            return (MessageKind.Warning, "あと 1 ステップ", "既定のアプリを切り替えてください",
                "設定で「HTTPS」と「HTTP」に LazyBlox を選ぶと、Discord などのリンクを受け取れるようになります。" + robloxNote,
                "既定のアプリを開く", OpenDefaultApps);
        return (MessageKind.Success, "準備完了", "Roblox リンクは直接起動します",
            "Discord などでプライベートサーバーのリンクを開くと、ブラウザを経由せずに Roblox が起動します。それ以外のリンクは "
            + (string.IsNullOrEmpty(s.BrowserName) ? "ブラウザ" : s.BrowserName) + " で開きます。"
            + (s.HttpIsLazyBlox ? "" : "\nHTTP も LazyBlox にしておくと、http:// のリンクにも対応します。") + robloxNote,
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

    private void ConfirmInstall()
    {
        var installer = new Installer(WindowsRegistry.Instance, _ => { });
        string plan = installer.DescribeInstall(new InstallOptions());
        Host.Navigate(new DialogPage(MessageKind.Info,
            _status.Registered ? "登録を修復しますか？" : "インストールしますか？",
            "以下の変更を、現在のユーザーにだけ行います。既定のブラウザや他のアプリの設定は変更しません。",
            plan,
            [
                new("キャンセル", ButtonKind.Secondary, w => w.Navigate(this)),
                new(_status.Registered ? "修復する" : "インストール", ButtonKind.Primary, _ => RunInstall()),
            ],
            w => w.Navigate(this)));
    }

    private void RunInstall()
    {
        var log = new StringBuilder();
        bool ok = Guard(() => new Installer(WindowsRegistry.Instance, l => log.AppendLine(l)).Install(new InstallOptions()), log);
        Refresh();
        Host.Navigate(ok
            ? new DialogPage(MessageKind.Success, "インストールしました",
                "最後に、Windows の設定で「HTTPS」と「HTTP」の既定アプリに LazyBlox を選んでください。", log.ToString().TrimEnd(),
                [
                    new("あとで", ButtonKind.Secondary, w => w.Navigate(this)),
                    new("既定のアプリを開く", ButtonKind.Primary, w => { OpenDefaultApps(); w.Navigate(this); }, Glyph.OpenExternal),
                ], w => w.Navigate(this))
            : new DialogPage(MessageKind.Error, "インストールできませんでした", "詳細を確認してください。", log.ToString().TrimEnd(),
                [new("戻る", ButtonKind.Primary, w => w.Navigate(this))], w => w.Navigate(this)));
    }

    private void ConfirmUninstall()
    {
        var installer = new Installer(WindowsRegistry.Instance, _ => { });
        Host.Navigate(new DialogPage(MessageKind.Warning, "アンインストールしますか？",
            "このアプリが作成したレジストリと設定だけを削除します。既定のブラウザが LazyBlox のままの場合は、あとで Windows の設定から元のブラウザを選び直してください。",
            installer.DescribeUninstall(),
            [
                new("キャンセル", ButtonKind.Secondary, w => w.Navigate(this)),
                new("アンインストール", ButtonKind.Danger, _ => RunUninstall()),
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
            ok ? "アンインストールしました" : "一部を削除できませんでした",
            wasDefault ? "HTTPS の既定がまだ LazyBlox になっています。Windows の設定で元のブラウザを選び直してください。" : "ご利用ありがとうございました。",
            log.ToString().TrimEnd(),
            wasDefault
                ? [new("閉じる", ButtonKind.Secondary, w => w.Close()),
                   new("既定のアプリを開く", ButtonKind.Primary, w => { Installer.OpenDefaultAppsSettings(WindowsShell.Instance, null); w.Close(); }, Glyph.OpenExternal)]
                : [new("閉じる", ButtonKind.Primary, w => w.Close())]));
    }

    private static bool Guard(Func<bool> action, StringBuilder log)
    {
        try { return action(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            log.AppendLine("エラー: " + e.Message);
            return false;
        }
    }
}
