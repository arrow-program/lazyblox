using LazyBlox.Localization;

namespace LazyBlox.Ui;

/// <summary>Pick the browser that receives non-Roblox links.</summary>
internal sealed class BrowserPage(Page back) : Page
{
    private List<BrowserCandidate> _all = [];
    private int _selected = -1;

    public override void OnShown()
    {
        _all = BrowserLocator.Enumerate(WindowsRegistry.Instance);
        string current = AppConfig.Load().BrowserCommand;
        _selected = _all.FindIndex(b => b.Command == current);
    }

    public override float FooterHeight => 72;

    public override void Render(Ui ui)
    {
        ui.Y = 30;
        ui.G.Text(L.S.BrowserTitle, Ui.Pad, ui.Y, ui.Inner, FontKind.Title, T.Text);
        ui.Y += 36;
        ui.Paragraph(L.S.BrowserIntro,
            FontKind.Small, gapAfter: 18);

        if (_all.Count == 0)
        {
            ui.Paragraph(L.S.BrowserNone, FontKind.Body, T.Danger);
            return;
        }
        for (int i = 0; i < _all.Count; i++)
        {
            int idx = i;
            BrowserCandidate b = _all[i];
            List<string> tokens = BrowserCommand.Tokenize(b.Command);
            string exe = b.Command;
            if (tokens.Count > 0) BrowserCommand.ResolveExecutableTokens(tokens, out exe);
            ui.Radio($"b{i}", b.Name, exe, i == _selected, () => _selected = idx);
        }
        ui.Y += 12;
    }

    public override void RenderFooter(Ui ui, RectF area) =>
        ui.FooterButtons(area,
            ("cancel", L.S.Cancel, ButtonKind.Secondary, () => Host.Navigate(back), null),
            ("save", L.S.Save, ButtonKind.Primary, Save, null));

    private void Save()
    {
        if (_selected >= 0 && _selected < _all.Count)
        {
            AppConfig cfg = AppConfig.Load();
            Installer.Assign(cfg, _all[_selected]);
            cfg.Save();
        }
        Host.Navigate(back);
    }

    public override void OnEscape() => Host.Navigate(back);
}
