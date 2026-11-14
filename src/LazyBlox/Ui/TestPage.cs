namespace LazyBlox.Ui;

/// <summary>Shows how a URL would be routed, without launching anything.</summary>
internal sealed class TestPage(Page back) : Page
{
    private const string Sample = "https://www.roblox.com/share?code=0123456789abcdef0123456789abcdef&type=Server";
    private string _url = Sample;
    private RouteDecision _decision;
    private string _action = "";
    private string? _note;

    public override void OnShown() => Evaluate();

    private void Evaluate()
    {
        var shell = new RecordingShell(WindowsShell.Instance);
        var handler = new LinkHandler(shell, WindowsRegistry.Instance, () => AppConfig.Load(), AppInfo.CurrentExePath);
        handler.Handle(_url);
        _decision = handler.LastDecision;
        _action = shell.OpenedUris.Count > 0 ? "Roblox に渡す: " + shell.OpenedUris[0]
            : shell.StartedProcesses.Count > 0 ? "ブラウザを起動: " + shell.StartedProcesses[0]
            : shell.Errors.Count > 0 ? "エラー表示: " + shell.Errors[0].Split('\n')[0]
            : "";
    }

    public override float FooterHeight => 72;

    public override void Render(Ui ui)
    {
        ui.Y = 30;
        ui.G.Text("リンクの判定テスト", Ui.Pad, ui.Y, ui.Inner, FontKind.Title, T.Text);
        ui.Y += 36;
        ui.Paragraph("URL をコピーしてから「貼り付け」を押すと、そのリンクがどう処理されるかを表示します。実際には何も起動しません。",
            FontKind.Small, gapAfter: 16);

        float bw = (ui.Inner - 10) / 2;
        ui.Button("paste", new RectF(Ui.Pad, ui.Y, bw, 36), "貼り付けて判定", ButtonKind.Primary, Paste, Glyph.Paste);
        ui.Button("sample", new RectF(Ui.Pad + bw + 10, ui.Y, bw, 36), "サンプル", ButtonKind.Secondary, () => { _url = Sample; _note = null; Evaluate(); });
        ui.Y += 36 + 10;
        if (_note is not null) ui.Paragraph(_note, FontKind.Small, T.Danger, 6);
        ui.Y += 8;

        // Input
        Field(ui, "入力", _url, mono: true);

        // Result
        (string label, uint c, uint soft) = _decision.Kind switch
        {
            RouteKind.Roblox => ("Roblox で直接開く", T.Success, T.SuccessSoft),
            RouteKind.Browser => ("ブラウザで開く", T.Info, T.InfoSoft),
            _ => ("開かない（拒否）", T.Danger, T.DangerSoft),
        };
        float inner = ui.Inner - 32;
        string target = _decision.Kind == RouteKind.Reject ? _decision.Reason : _decision.Target;
        float th = ui.G.Measure(target, inner, FontKind.Mono);
        float ah = ui.G.Measure(_action, inner, FontKind.Small);
        RectF card = new(Ui.Pad, ui.Y, ui.Inner, 16 + 24 + 14 + 18 + th + 12 + 18 + ah + 18);
        ui.CardBg(card);
        float y = ui.Y + 16;
        ui.Pill(Ui.Pad + 16, y, label, c, soft);
        y += 24 + 14;
        ui.G.Text(_decision.Kind == RouteKind.Reject ? "理由" : _decision.Kind == RouteKind.Roblox ? "変換後のディープリンク" : "転送する URL",
            Ui.Pad + 16, y, inner, FontKind.Caption, T.SubText, wrap: false);
        y += 18;
        y += ui.G.Text(target, Ui.Pad + 16, y, inner, FontKind.Mono, T.Text) + 12;
        ui.G.Text("実行予定", Ui.Pad + 16, y, inner, FontKind.Caption, T.SubText, wrap: false);
        y += 18;
        ui.G.Text(_action, Ui.Pad + 16, y, inner, FontKind.Small, T.SubText);
        ui.Y += card.H + 20;
    }

    private void Field(Ui ui, string label, string value, bool mono)
    {
        ui.G.Text(label, Ui.Pad + 2, ui.Y, ui.Inner, FontKind.Caption, T.SubText, wrap: false);
        ui.Y += 20;
        FontKind f = mono ? FontKind.Mono : FontKind.Body;
        float h = ui.G.Measure(value, ui.Inner - 28, f);
        RectF r = new(Ui.Pad, ui.Y, ui.Inner, h + 22);
        ui.CardBg(r, T.Dark ? 0xFF202024 : 0xFFFAFAFC, 8);
        ui.G.Text(value, Ui.Pad + 14, ui.Y + 11, ui.Inner - 28, f, T.Text);
        ui.Y += r.H + 14;
    }

    private void Paste()
    {
        string? text = W.GetClipboardText(Host.Handle)?.Trim();
        if (string.IsNullOrEmpty(text)) { _note = "クリップボードにテキストがありません。"; return; }
        _note = null;
        _url = text.Length > 2000 ? text[..2000] : text;
        Evaluate();
    }

    public override void RenderFooter(Ui ui, RectF area) =>
        ui.FooterButtons(area, ("back", "戻る", ButtonKind.Secondary, () => Host.Navigate(back), Glyph.Back));

    public override void OnEscape() => Host.Navigate(back);
}
