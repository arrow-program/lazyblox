namespace LazyBlox.Ui;

/// <summary>Entry points for everything visual. Silent: custom windows never play system sounds.</summary>
internal static class Gui
{
    private const string Title = AppInfo.AppName;

    public static int RunSettings()
    {
        // LAZYBLOX_UI_PAGE opens a specific screen directly (used for screenshots / UI checks).
        Page start = Environment.GetEnvironmentVariable("LAZYBLOX_UI_PAGE") switch
        {
            "browser" => new BrowserPage(new HomePage()),
            "test" => new TestPage(new HomePage()),
            "message" => Demo(MessageKind.Success, "インストールしました", "最後に、Windows の設定で既定アプリに LazyBlox を選んでください。"),
            "error" => Demo(MessageKind.Error, "リンクを開けませんでした", "ブラウザを起動できませんでした。\n設定で転送先ブラウザを選び直してください。"),
            "confirm" => new DialogPage(MessageKind.Info, "インストールしますか？",
                "以下の変更を、現在のユーザーにだけ行います。既定のブラウザや他のアプリの設定は変更しません。",
                new Installer(WindowsRegistry.Instance, _ => { }).DescribeInstall(new InstallOptions()),
                [new("キャンセル", ButtonKind.Secondary, w => w.Close()), new("インストール", ButtonKind.Primary, w => w.Close())]),
            _ => new HomePage(),
        };
        AppWindow.Run(start, Title, 600, 780);
        return ExitCodes.Ok;
    }

    private static DialogPage Demo(MessageKind kind, string title, string body) =>
        new(kind, title, body, null, [new("閉じる", ButtonKind.Primary, w => w.Close())]);

    /// <summary>Message window sized to its content. Falls back to a plain MessageBox if the custom UI cannot start.</summary>
    public static void Message(string title, string body, MessageKind kind, string? copyText = null)
    {
        try
        {
            var buttons = new List<DialogButton>();
            if (copyText is not null)
                buttons.Add(new DialogButton("コピー", ButtonKind.Secondary, w => W.SetClipboardText(w.Handle, copyText), Glyph.Copy));
            buttons.Add(new DialogButton("閉じる", ButtonKind.Primary, w => w.Close()));
            AppWindow.Run(new DialogPage(kind, title, body, null, buttons), Title, 520, 640, fitContent: true);
        }
        catch (Exception e) when (e is InvalidOperationException or DllNotFoundException or EntryPointNotFoundException)
        {
            Native.MessageBoxW(0, title + "\n\n" + body, Title, Native.MB_OK | Native.MB_SETFOREGROUND);
        }
    }

    /// <summary>Yes/no confirmation with an optional details box (e.g. registry plan).</summary>
    public static bool Confirm(string title, string body, string? details, string confirmLabel, bool danger)
    {
        try
        {
            object? result = AppWindow.Run(new DialogPage(danger ? MessageKind.Warning : MessageKind.Info, title, body, details,
            [
                new("キャンセル", ButtonKind.Secondary, w => { w.Result = false; w.Close(); }),
                new(confirmLabel, danger ? ButtonKind.Danger : ButtonKind.Primary, w => { w.Result = true; w.Close(); }),
            ]), Title, 640, 720, fitContent: true);
            return result is true;
        }
        catch (Exception e) when (e is InvalidOperationException or DllNotFoundException or EntryPointNotFoundException)
        {
            return Native.MessageBoxW(0, title + "\n\n" + body, Title, Native.MB_OKCANCEL | Native.MB_SETFOREGROUND) == Native.IDOK;
        }
    }
}
