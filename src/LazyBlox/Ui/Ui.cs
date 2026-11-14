namespace LazyBlox.Ui;

internal enum ButtonKind { Primary, Secondary, Danger, Ghost }
internal enum RowTrail { Chevron, External, Toggle, None }

/// <summary>Segoe Fluent Icons code points.</summary>
internal static class Glyph
{
    public const string ChevronRight = "", Check = "", Cancel = "", Info = "", Warning = "",
        Error = "", Settings = "", Globe = "", Link = "", Delete = "", Download = "",
        Play = "", Copy = "", Paste = "", Shield = "", Back = "", OpenExternal = "",
        Repair = "", Lightning = "", Search = "", History = "", Game = "";
}

/// <summary>
/// Immediate-mode UI context for one paint pass. Widgets draw themselves and register a hit region with the
/// action to run on click; the host window does hover / press / keyboard focus bookkeeping between passes.
/// Coordinates are DIPs; content coordinates are pre-scroll.
/// </summary>
internal sealed class Ui(Gfx g, Theme t, float width, string? hover, string? pressed, string? focus, bool focusVisible)
{
    public const float Pad = 28;

    public readonly record struct Hit(string Id, RectF Rect, Action Click, bool Footer);

    public Gfx G { get; } = g;
    public Theme T { get; } = t;
    public float Width { get; } = width;
    public float Inner => Width - Pad * 2;
    public float Y { get; set; }
    public bool InFooter { get; set; }
    public List<Hit> Hits { get; } = [];

    private bool IsHover(string id) => hover == id;
    private bool IsPressed(string id) => pressed == id && hover == id;
    private bool IsFocused(string id) => focusVisible && focus == id;

    public void Register(string id, RectF r, Action click) => Hits.Add(new Hit(id, r, click, InFooter));

    private void FocusRing(string id, RectF r, float radius)
    {
        if (IsFocused(id)) G.Stroke(r.Inflate(3), T.Text, 2, radius + 3);
    }

    // ---------------- layout helpers ----------------

    public void Space(float h) => Y += h;

    public void SectionLabel(string text)
    {
        Y += 6;
        G.Text(text, Pad + 2, Y, Inner, FontKind.Caption, T.SubText, wrap: false);
        Y += 24;
    }

    public void Paragraph(string text, FontKind f = FontKind.Body, uint? color = null, float gapAfter = 10)
    {
        Y += G.Text(text, Pad, Y, Inner, f, color ?? T.SubText) + gapAfter;
    }

    /// <summary>Card background with soft shadow and hairline border.</summary>
    public void CardBg(RectF r, uint? fill = null, float radius = 10)
    {
        G.Fill(new RectF(r.X, r.Y + 1.5f, r.W, r.H), T.Shadow, radius);
        G.Fill(r, fill ?? T.Surface, radius);
        G.Stroke(r, T.Border, 1, radius);
    }

    // ---------------- widgets ----------------

    public void Button(string id, RectF r, string label, ButtonKind kind, Action click, string? glyph = null)
    {
        bool hov = IsHover(id), prs = IsPressed(id);
        (uint bg, uint fg, uint border) = kind switch
        {
            ButtonKind.Primary => (prs ? T.AccentPress : hov ? T.AccentHover : T.Accent, T.OnAccent, 0u),
            ButtonKind.Danger => (prs ? Theme.Mix(T.Danger, 0xFF000000, 0.15f) : hov ? Theme.Mix(T.Danger, 0xFFFFFFFF, 0.08f) : T.Danger,
                0xFFFFFFFF, 0u),
            ButtonKind.Ghost => (prs ? T.SurfacePress : hov ? T.SurfaceHover : 0u, T.Text, 0u),
            _ => (prs ? T.SurfacePress : hov ? T.SurfaceHover : T.Surface, T.Text, T.Border),
        };
        const float radius = 7;
        if (kind != ButtonKind.Ghost) G.Fill(new RectF(r.X, r.Y + 1, r.W, r.H), T.Shadow, radius);
        G.Fill(r, bg, radius);
        if (border != 0) G.Stroke(r, border, 1, radius);

        float textW = G.MeasureWidth(label, FontKind.BodyBold);
        float glyphW = glyph is null ? 0 : 22;
        float x = r.X + (r.W - textW - glyphW) / 2;
        if (glyph is not null)
            G.Text(glyph, x, r.Y + (r.H - 18) / 2 + 1, 20, FontKind.Glyph, fg, wrap: false);
        G.Text(label, x + glyphW, r.Y + (r.H - 20) / 2, textW + 4, FontKind.BodyBold, fg, wrap: false);
        FocusRing(id, r, radius);
        Register(id, r, click);
    }

    /// <summary>A full-width action row: icon tile, title, description, trailing chevron / toggle.</summary>
    public void Row(string id, string glyph, string title, string? desc, Action click,
        RowTrail trail = RowTrail.Chevron, bool on = false, bool danger = false)
    {
        float x = Pad, w = Inner;
        float textX = x + 64, textW = w - 64 - (trail == RowTrail.Toggle ? 70 : 40);
        float descH = desc is null ? 0 : G.Measure(desc, textW, FontKind.Small);
        float h = Math.Max(64, 20 + 21 + (desc is null ? 0 : descH + 2) + 20 - 4);
        RectF r = new(x, Y, w, h);

        bool hov = IsHover(id), prs = IsPressed(id);
        CardBg(r, prs ? T.SurfacePress : hov ? T.SurfaceHover : T.Surface);

        uint tone = danger ? T.Danger : T.Accent;
        RectF tile = new(x + 16, Y + (h - 36) / 2, 36, 36);
        G.Fill(tile, danger ? T.DangerSoft : T.AccentSoft, 9);
        G.Text(glyph, tile.X, tile.Y, tile.W, FontKind.Glyph, tone, center: true, h: tile.H);

        float contentH = 21 + (desc is null ? 0 : descH + 2);
        float ty = Y + (h - contentH) / 2 - 1;
        G.Text(title, textX, ty, textW, FontKind.BodyBold, danger ? T.Danger : T.Text, wrap: false);
        if (desc is not null) G.Text(desc, textX, ty + 23, textW, FontKind.Small, T.SubText);

        float cy = Y + h / 2;
        switch (trail)
        {
            case RowTrail.Chevron:
            case RowTrail.External:
                string gl = trail == RowTrail.Chevron ? Glyph.ChevronRight : Glyph.OpenExternal;
                G.Text(gl, r.Right - 38, cy - 10, 20, FontKind.Glyph, hov ? T.Text : T.Faint, center: true, h: 20);
                break;
            case RowTrail.Toggle:
                Toggle(r.Right - 62, cy - 11, on);
                break;
        }
        FocusRing(id, r, 10);
        Register(id, r, click);
        Y += h + 8;
    }

    public void Toggle(float x, float y, bool on)
    {
        RectF track = new(x, y, 44, 22);
        if (on) G.Fill(track, T.Accent, 11);
        else G.Stroke(track, T.SubText, 1.5f, 11);
        float knob = on ? 7 : 5.5f;
        float cx = on ? track.Right - 11 : track.X + 11;
        G.Circle(cx, y + 11, knob, on ? T.OnAccent : T.SubText);
    }

    /// <summary>Selectable row with a radio indicator.</summary>
    public void Radio(string id, string title, string? desc, bool selected, Action click)
    {
        float x = Pad, w = Inner;
        float h = desc is null ? 52 : 64;
        RectF r = new(x, Y, w, h);
        bool hov = IsHover(id), prs = IsPressed(id);
        CardBg(r, selected ? T.AccentSoft : prs ? T.SurfacePress : hov ? T.SurfaceHover : T.Surface);
        if (selected) G.Stroke(r, T.Accent, 1.5f, 10);

        float cx = x + 28, cy = Y + h / 2;
        if (selected)
        {
            G.Circle(cx, cy, 10, T.Accent);
            G.Circle(cx, cy, 4.5f, T.OnAccent);
        }
        else G.Ring(cx, cy, 9.25f, hov ? T.Text : T.SubText, 1.5f);

        float tx = x + 52, tw = w - 52 - 16;
        if (desc is null) G.Text(title, tx, cy - 11, tw, FontKind.BodyBold, T.Text, wrap: false);
        else
        {
            G.Text(title, tx, Y + 12, tw, FontKind.BodyBold, T.Text, wrap: false);
            G.Text(desc, tx, Y + 36, tw, FontKind.MonoSmall, T.SubText, wrap: false);
        }
        FocusRing(id, r, 10);
        Register(id, r, click);
        Y += h + 8;
    }

    /// <summary>Small colored status pill: ● text.</summary>
    public float Pill(float x, float y, string text, uint color, uint soft)
    {
        float tw = G.MeasureWidth(text, FontKind.SmallBold);
        RectF r = new(x, y, tw + 30, 24);
        G.Fill(r, soft, 12);
        G.Circle(x + 12, y + 12, 3.5f, color);
        G.Text(text, x + 21, y + 3, tw + 6, FontKind.SmallBold, color, wrap: false);
        return r.W;
    }

    /// <summary>Large round status icon (check / cross / info / warning) drawn with paths.</summary>
    public void BigIcon(float cx, float cy, MessageKind kind)
    {
        (uint c, uint soft) = kind switch
        {
            MessageKind.Success => (T.Success, T.SuccessSoft),
            MessageKind.Error => (T.Danger, T.DangerSoft),
            MessageKind.Warning => (T.Warning, T.WarningSoft),
            _ => (T.Accent, T.AccentSoft),
        };
        G.Circle(cx, cy, 30, soft);
        G.Circle(cx, cy, 21, c);
        uint fg = kind == MessageKind.Warning && !T.Dark ? 0xFFFFFFFF : T.Dark ? 0xFF16161A : 0xFFFFFFFF;
        switch (kind)
        {
            case MessageKind.Success:
                G.Polyline([new(cx - 8.5f, cy + 0.5f), new(cx - 2.5f, cy + 6.5f), new(cx + 9, cy - 6)], fg, 3.2f);
                break;
            case MessageKind.Error:
                G.Line(cx - 7, cy - 7, cx + 7, cy + 7, fg, 3.2f);
                G.Line(cx + 7, cy - 7, cx - 7, cy + 7, fg, 3.2f);
                break;
            case MessageKind.Warning:
                G.Line(cx, cy - 9, cx, cy + 2, fg, 3.4f);
                G.Circle(cx, cy + 8.5f, 2.1f, fg);
                break;
            default:
                G.Circle(cx, cy - 8.5f, 2.1f, fg);
                G.Line(cx, cy - 2, cx, cy + 9, fg, 3.4f);
                break;
        }
    }

    /// <summary>App logo: gradient squircle with a tilted block and speed lines (matches assets/icon.png).</summary>
    public void Logo(float x, float y, float size)
    {
        RectF r = new(x, y, size, size);
        G.Fill(new RectF(x, y + 2, size, size), T.Shadow, size * 0.28f);
        // Same geometry as assets/icon.png (1024px design mapped to a 44-unit grid).
        G.Gradient(r, 0xFF3F80F7, 0xFFF57AED, size * 0.28f);
        float s = size / 44f;
        G.Polygon([new(x + 21.2f * s, y + 12.3f * s), new(x + 36.0f * s, y + 16.8f * s),
                   new(x + 31.5f * s, y + 31.7f * s), new(x + 16.7f * s, y + 27.1f * s)], 0xFFFFFFFF);
        const uint speed = 0xFFDEE3FD;
        G.Line(x + 10.0f * s, y + 17.0f * s, x + 14.0f * s, y + 17.0f * s, speed, 2.4f * s);
        G.Line(x + 8.0f * s, y + 22.0f * s, x + 14.0f * s, y + 22.0f * s, speed, 2.4f * s);
        G.Line(x + 10.0f * s, y + 27.0f * s, x + 14.0f * s, y + 27.0f * s, speed, 2.4f * s);
    }

    /// <summary>Lays out buttons right-aligned in a footer bar.</summary>
    public void FooterButtons(RectF area, params (string Id, string Label, ButtonKind Kind, Action Click, string? Glyph)[] buttons)
    {
        float x = area.Right - Pad;
        float y = area.Y + (area.H - 36) / 2;
        for (int i = buttons.Length - 1; i >= 0; i--)
        {
            var b = buttons[i];
            float w = Math.Max(104, G.MeasureWidth(b.Label, FontKind.BodyBold) + 40 + (b.Glyph is null ? 0 : 22));
            x -= w;
            Button(b.Id, new RectF(x, y, w, 36), b.Label, b.Kind, b.Click, b.Glyph);
            x -= 10;
        }
    }
}

internal enum MessageKind { Info, Success, Warning, Error }
