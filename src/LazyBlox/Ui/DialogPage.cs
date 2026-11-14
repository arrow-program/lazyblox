namespace LazyBlox.Ui;

internal sealed record DialogButton(string Label, ButtonKind Kind, Action<AppWindow> Click, string? Glyph = null);

/// <summary>Generic message / confirmation screen: big status icon, title, body, optional scrollable details.</summary>
internal sealed class DialogPage(MessageKind kind, string title, string body, string? details,
    IReadOnlyList<DialogButton> buttons, Action<AppWindow>? onEscape = null) : Page
{
    public override float FooterHeight => 72;

    public override void Render(Ui ui)
    {
        ui.Y = 30;
        ui.BigIcon(Ui.Pad + 30, ui.Y + 30, kind);
        float tx = Ui.Pad + 80, tw = ui.Width - tx - Ui.Pad;
        float th = ui.G.Text(title, tx, ui.Y + 4, tw, FontKind.Title, T.Text);
        float bh = ui.G.Text(body, tx, ui.Y + 4 + th + 8, tw, FontKind.Body, T.SubText);
        ui.Y += Math.Max(64, 4 + th + 8 + bh) + 20;

        if (!string.IsNullOrEmpty(details))
        {
            float inner = ui.Inner - 32;
            float dh = ui.G.Measure(details, inner, FontKind.Mono);
            RectF card = new(Ui.Pad, ui.Y, ui.Inner, dh + 28);
            ui.CardBg(card, T.Dark ? 0xFF202024 : 0xFFFAFAFC, 10);
            ui.G.Text(details, Ui.Pad + 16, ui.Y + 14, inner, FontKind.Mono, T.Text);
            ui.Y += card.H + 20;
        }
        ui.Y += 4;
    }

    public override void RenderFooter(Ui ui, RectF area) =>
        ui.FooterButtons(area, [.. buttons.Select((b, i) => ($"dlg{i}", b.Label, b.Kind, (Action)(() => b.Click(Host)), b.Glyph))]);

    public override void OnEscape()
    {
        if (onEscape is not null) onEscape(Host);
        else Host.Close();
    }
}
