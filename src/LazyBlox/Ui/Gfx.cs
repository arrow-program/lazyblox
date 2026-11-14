namespace LazyBlox.Ui;

/// <summary>Font sizes / weights used by the UI (in DIPs).</summary>
internal enum FontKind { Body, BodyBold, Small, SmallBold, Caption, Title, Display, Mono, MonoSmall, Glyph }

/// <summary>Thin drawing API over a GDI+ Graphics in device-independent pixels (world transform does the DPI scaling).</summary>
internal sealed unsafe class Gfx : IDisposable
{
    private readonly nint _g;
    private readonly nint _brush, _pen, _path;

    private static readonly Dictionary<FontKind, nint> Fonts = [];
    private static nint _fmtWrap, _fmtLine, _fmtCenter;

    public Gfx(nint hdc, float scale)
    {
        Gdip.EnsureStarted();
        nint g;
        Check(Gdip.GdipCreateFromHDC(hdc, &g));
        _g = g;
        Gdip.GdipSetSmoothingMode(g, Gdip.SmoothingAntiAlias);
        Gdip.GdipSetPixelOffsetMode(g, Gdip.PixelOffsetHighQuality);
        Gdip.GdipSetTextRenderingHint(g, Gdip.TextClearTypeGridFit);
        Gdip.GdipScaleWorldTransform(g, scale, scale, Gdip.MatrixOrderPrepend);
        Scale = scale;
        nint b, p, path;
        Gdip.GdipCreateSolidFill(0xFF000000, &b); _brush = b;
        Gdip.GdipCreatePen1(0xFF000000, 1, Gdip.UnitWorld, &p); _pen = p;
        Gdip.GdipSetPenStartCap(p, Gdip.CapRound); Gdip.GdipSetPenEndCap(p, Gdip.CapRound); Gdip.GdipSetPenLineJoin(p, Gdip.JoinRound);
        Gdip.GdipCreatePath(0, &path); _path = path;
        EnsureFonts();
    }

    public float Scale { get; }

    public void Clear(uint c) => Gdip.GdipGraphicsClear(_g, c);

    public void Translate(float dx, float dy) => Gdip.GdipTranslateWorldTransform(_g, dx, dy, Gdip.MatrixOrderPrepend);

    public void ResetTransform()
    {
        Gdip.GdipResetWorldTransform(_g);
        Gdip.GdipScaleWorldTransform(_g, Scale, Scale, Gdip.MatrixOrderPrepend);
    }

    public void Clip(RectF r) => Gdip.GdipSetClipRect(_g, r.X, r.Y, r.W, r.H, Gdip.CombineReplace);
    public void ResetClip() => Gdip.GdipResetClip(_g);

    // ---------------- shapes ----------------

    private void RoundPath(RectF r, float radius)
    {
        Gdip.GdipResetPath(_path);
        float d = Math.Min(radius * 2, Math.Min(r.W, r.H));
        if (d <= 0.5f)
        {
            Gdip.GdipAddPathLine(_path, r.X, r.Y, r.Right, r.Y);
            Gdip.GdipAddPathLine(_path, r.Right, r.Bottom, r.X, r.Bottom);
        }
        else
        {
            Gdip.GdipAddPathArc(_path, r.X, r.Y, d, d, 180, 90);
            Gdip.GdipAddPathArc(_path, r.Right - d, r.Y, d, d, 270, 90);
            Gdip.GdipAddPathArc(_path, r.Right - d, r.Bottom - d, d, d, 0, 90);
            Gdip.GdipAddPathArc(_path, r.X, r.Bottom - d, d, d, 90, 90);
        }
        Gdip.GdipClosePathFigure(_path);
    }

    public void Fill(RectF r, uint color, float radius = 0)
    {
        if ((color >> 24) == 0) return;
        Gdip.GdipSetSolidFillColor(_brush, color);
        if (radius <= 0) { Gdip.GdipFillRectangle(_g, _brush, r.X, r.Y, r.W, r.H); return; }
        RoundPath(r, radius);
        Gdip.GdipFillPath(_g, _brush, _path);
    }

    public void Stroke(RectF r, uint color, float width, float radius = 0)
    {
        if ((color >> 24) == 0) return;
        SetPen(color, width);
        // Align hairlines to the pixel grid for crisp borders.
        RoundPath(new RectF(r.X + width / 2, r.Y + width / 2, r.W - width, r.H - width), Math.Max(0, radius - width / 2));
        Gdip.GdipDrawPath(_g, _pen, _path);
    }

    public void Gradient(RectF r, uint c1, uint c2, float radius)
    {
        nint lb;
        RectF rr = r;
        Gdip.GdipCreateLineBrushFromRect(&rr, c1, c2, Gdip.LinearGradientForwardDiagonal, 0, &lb);
        RoundPath(r, radius);
        Gdip.GdipFillPath(_g, lb, _path);
        Gdip.GdipDeleteBrush(lb);
    }

    public void Circle(float cx, float cy, float radius, uint color)
    {
        Gdip.GdipSetSolidFillColor(_brush, color);
        Gdip.GdipFillEllipse(_g, _brush, cx - radius, cy - radius, radius * 2, radius * 2);
    }

    public void Ring(float cx, float cy, float radius, uint color, float width)
    {
        SetPen(color, width);
        Gdip.GdipDrawEllipse(_g, _pen, cx - radius, cy - radius, radius * 2, radius * 2);
    }

    public void Line(float x1, float y1, float x2, float y2, uint color, float width)
    {
        SetPen(color, width);
        Gdip.GdipDrawLine(_g, _pen, x1, y1, x2, y2);
    }

    public void Polyline(ReadOnlySpan<PointF> pts, uint color, float width)
    {
        SetPen(color, width);
        Gdip.GdipResetPath(_path);
        fixed (PointF* p = pts) Gdip.GdipAddPathLine2(_path, p, pts.Length);
        Gdip.GdipDrawPath(_g, _pen, _path);
    }

    public void Polygon(ReadOnlySpan<PointF> pts, uint color)
    {
        Gdip.GdipSetSolidFillColor(_brush, color);
        Gdip.GdipResetPath(_path);
        fixed (PointF* p = pts) Gdip.GdipAddPathPolygon(_path, p, pts.Length);
        Gdip.GdipFillPath(_g, _brush, _path);
    }

    private void SetPen(uint color, float width)
    {
        Gdip.GdipSetPenColor(_pen, color);
        Gdip.GdipSetPenWidth(_pen, width);
    }

    // ---------------- text ----------------

    /// <summary>Draws text inside a box; returns the height used.</summary>
    public float Text(string s, float x, float y, float w, FontKind f, uint color, bool wrap = true, bool center = false, float h = 0)
    {
        if (s.Length == 0) return 0;
        float used = h > 0 ? h : Measure(s, w, f, wrap);
        RectF r = new(x, y, w, used + 2);
        Gdip.GdipSetSolidFillColor(_brush, color);
        Gdip.GdipDrawString(_g, s, s.Length, Fonts[f], &r, center ? _fmtCenter : wrap ? _fmtWrap : _fmtLine, _brush);
        return used;
    }

    public float Measure(string s, float w, FontKind f, bool wrap = true)
    {
        if (s.Length == 0) return 0;
        RectF layout = new(0, 0, w, 100_000), bounds;
        int cp, lines;
        Gdip.GdipMeasureString(_g, s, s.Length, Fonts[f], &layout, wrap ? _fmtWrap : _fmtLine, &bounds, &cp, &lines);
        return MathF.Ceiling(bounds.H);
    }

    public float MeasureWidth(string s, FontKind f)
    {
        if (s.Length == 0) return 0;
        RectF layout = new(0, 0, 100_000, 100_000), bounds;
        int cp, lines;
        Gdip.GdipMeasureString(_g, s, s.Length, Fonts[f], &layout, _fmtLine, &bounds, &cp, &lines);
        return MathF.Ceiling(bounds.W);
    }

    private static void EnsureFonts()
    {
        if (Fonts.Count > 0) return;
        // zh / ko: the script's UI font for body too (fallback from Segoe UI spaces Han glyphs unevenly);
        // these fonts draw "\" correctly. Japanese keeps Segoe UI because Yu Gothic draws "\" as "¥".
        nint regular = LazyBlox.Localization.L.Code switch
        {
            "zh-Hans" => Family(out _, "Microsoft YaHei UI", "Segoe UI"),
            "zh-Hant" => Family(out _, "Microsoft JhengHei UI", "Segoe UI"),
            "ko" => Family(out _, "Malgun Gothic", "Segoe UI"),
            _ => Family(out _, "Segoe UI", "Yu Gothic UI", "Meiryo UI"),
        };
        // Body text: Segoe UI (correct "\" in paths; Japanese via system font fallback).
        // Headings: Yu Gothic UI Semibold so Japanese keeps its weight (headings never contain paths).
        // CJK headings use the script's own UI font so glyph shapes match the language (Han unification).
        string[] headings = LazyBlox.Localization.L.Code switch
        {
            "ja" => ["Yu Gothic UI Semibold", "Yu Gothic UI", "Segoe UI Semibold"],
            "zh-Hans" => ["Microsoft YaHei UI", "Segoe UI Semibold"],
            "zh-Hant" => ["Microsoft JhengHei UI", "Segoe UI Semibold"],
            "ko" => ["Malgun Gothic", "Segoe UI Semibold"],
            _ => ["Segoe UI Semibold", "Segoe UI"],
        };
        nint semibold = Family(out string boldName, headings);
        // Families without a semibold face get GDI+ bold instead.
        bool fakeBold = boldName is not ("Yu Gothic UI Semibold" or "Segoe UI Semibold");
        // Japanese UI fonts draw "\" as "¥", so paths / code use a Latin monospace font (GDI+ falls back for CJK).
        nint mono = Family(out _, "Cascadia Mono", "Consolas", "Courier New");
        nint glyph = Family(out _, "Segoe Fluent Icons", "Segoe MDL2 Assets", "Segoe UI Symbol");

        nint Font(nint fam, float size, bool bold)
        {
            nint font;
            Gdip.GdipCreateFont(fam, size, bold ? Gdip.FontBold : Gdip.FontRegular, Gdip.UnitPixel, &font);
            return font;
        }

        Fonts[FontKind.Body] = Font(regular, 14, false);
        Fonts[FontKind.BodyBold] = Font(semibold, 14, fakeBold);
        Fonts[FontKind.Small] = Font(regular, 12.5f, false);
        Fonts[FontKind.SmallBold] = Font(semibold, 12.5f, fakeBold);
        Fonts[FontKind.Caption] = Font(semibold, 11.5f, fakeBold);
        Fonts[FontKind.Title] = Font(semibold, 20, fakeBold);
        Fonts[FontKind.Display] = Font(semibold, 26, fakeBold);
        Fonts[FontKind.Mono] = Font(mono, 12, false);
        Fonts[FontKind.MonoSmall] = Font(mono, 11.5f, false);
        Fonts[FontKind.Glyph] = Font(glyph, 16, false);

        nint f;
        Gdip.GdipCreateStringFormat(0, 0, &f); _fmtWrap = f;
        Gdip.GdipSetStringFormatTrimming(f, Gdip.TrimmingNone);
        Gdip.GdipCreateStringFormat(Gdip.FlagsNoWrap, 0, &f); _fmtLine = f;
        Gdip.GdipSetStringFormatTrimming(f, Gdip.TrimmingEllipsisChar);
        Gdip.GdipCreateStringFormat(Gdip.FlagsNoWrap, 0, &f); _fmtCenter = f;
        Gdip.GdipSetStringFormatAlign(f, 1);
        Gdip.GdipSetStringFormatLineAlign(f, 1);
    }

    private static nint Family(out string chosen, params string[] names)
    {
        foreach (string n in names)
        {
            nint fam;
            if (Gdip.GdipCreateFontFamilyFromName(n, 0, &fam) == 0) { chosen = n; return fam; }
        }
        throw new InvalidOperationException("No usable font found");
    }

    private static void Check(int status)
    {
        if (status != 0) throw new InvalidOperationException($"GDI+ error {status}");
    }

    public void Dispose()
    {
        Gdip.GdipDeletePath(_path);
        Gdip.GdipDeletePen(_pen);
        Gdip.GdipDeleteBrush(_brush);
        Gdip.GdipDeleteGraphics(_g);
    }
}
