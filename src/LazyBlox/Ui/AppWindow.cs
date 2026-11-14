using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LazyBlox.Ui;

/// <summary>A screen shown inside an <see cref="AppWindow"/>.</summary>
internal abstract class Page
{
    public AppWindow Host { get; internal set; } = null!;
    public Theme T => Host.Theme;

    /// <summary>Draws the scrollable content starting at ui.Y and advances ui.Y.</summary>
    public abstract void Render(Ui ui);

    /// <summary>Height of the fixed bottom bar (0 = none).</summary>
    public virtual float FooterHeight => 0;
    public virtual void RenderFooter(Ui ui, RectF area) { }

    /// <summary>Esc key.</summary>
    public virtual void OnEscape() => Host.Close();
    public virtual void OnShown() { }
}

/// <summary>
/// Custom-drawn top-level window: no common controls, no system sounds. Double-buffered GDI+ rendering,
/// per-monitor DPI, light/dark following Windows, keyboard navigation (Tab / Enter / Space / Esc / wheel).
/// </summary>
internal sealed unsafe class AppWindow
{
    private const string ClassName = "LazyBlox.Window";
    private const uint Style = W.WS_OVERLAPPEDWINDOW;
    private static readonly Dictionary<nint, AppWindow> Windows = [];
    private static AppWindow? _creating;
    private static bool _registered;

    private nint _hwnd;
    private Page _page;
    private float _scroll, _contentH, _viewH, _scale = 1;
    private string? _hover, _pressed, _focus;
    private bool _focusVisible, _tracking;
    private List<Ui.Hit> _hits = [];
    private Exception? _error;
    private readonly float _minW, _minH;

    public Theme Theme { get; private set; } = Theme.Load();
    public object? Result { get; set; }
    public nint Handle => _hwnd;

    private AppWindow(Page page, float minW, float minH)
    {
        _page = page;
        page.Host = this;
        _minW = minW;
        _minH = minH;
    }

    /// <summary>Shows a window and runs its message loop until it closes. Returns <see cref="Result"/>.</summary>
    public static object? Run(Page page, string title, float width, float height, bool fitContent = false)
    {
        Gdip.EnsureStarted();
        RegisterClass();

        var w = new AppWindow(page, Math.Min(width, 420), Math.Min(height, 300));
        _creating = w;
        nint inst = W.GetModuleHandleW(null);
        nint hwnd = W.CreateWindowExW(0, ClassName, title, Style, unchecked((int)0x80000000), 0, 100, 100, 0, 0, inst, 0);
        _creating = null;
        if (hwnd == 0) throw new InvalidOperationException("CreateWindowEx failed");

        w.ApplyChrome();
        w.SetIcon();
        w.Place(width, height, fitContent);
        page.OnShown();
        W.ShowWindow(hwnd, W.SW_SHOW);
        W.SetForegroundWindow(hwnd);

        W.MSG msg;
        while (W.GetMessageW(&msg, 0, 0, 0) > 0)
        {
            W.TranslateMessage(&msg);
            W.DispatchMessageW(&msg);
        }
        if (w._error is not null) throw new InvalidOperationException("GUI error: " + w._error.Message, w._error);
        return w.Result;
    }

    public void Close() => W.DestroyWindow(_hwnd);

    public void Navigate(Page page)
    {
        _page = page;
        page.Host = this;
        _scroll = 0;
        _focus = _pressed = null;
        _hover = null;
        page.OnShown();
        Invalidate();
    }

    public void Invalidate() => W.InvalidateRect(_hwnd, null, 0);

    // ------------------------------------------------------------------ setup

    private static void RegisterClass()
    {
        if (_registered) return;
        fixed (char* name = ClassName)
        {
            var wc = new W.WNDCLASSEXW
            {
                cbSize = (uint)sizeof(W.WNDCLASSEXW),
                style = W.CS_HREDRAW | W.CS_VREDRAW,
                lpfnWndProc = &WndProc,
                hInstance = W.GetModuleHandleW(null),
                hCursor = W.LoadCursorW(0, W.IDC_ARROW),
                lpszClassName = name,
            };
            if (W.RegisterClassExW(&wc) == 0) throw new InvalidOperationException("RegisterClassEx failed");
        }
        _registered = true;
    }

    private void ApplyChrome()
    {
        W.SetDwm(_hwnd, W.DWMWA_USE_IMMERSIVE_DARK_MODE, Theme.Dark ? 1u : 0u);
        W.SetDwm(_hwnd, W.DWMWA_WINDOW_CORNER_PREFERENCE, 2); // round
        // Title bar blends into the window background (Windows 11; ignored on older builds).
        W.SetDwm(_hwnd, W.DWMWA_CAPTION_COLOR, Theme.ToColorRef(Theme.Bg));
        W.SetDwm(_hwnd, W.DWMWA_TEXT_COLOR, Theme.ToColorRef(Theme.Text));
        W.SetDwm(_hwnd, W.DWMWA_BORDER_COLOR, Theme.ToColorRef(Theme.Border));
    }

    private void SetIcon()
    {
        string? exe = Environment.ProcessPath;
        if (exe is null) return;
        nint large, small;
        if (W.ExtractIconExW(exe, 0, &large, &small, 1) == 0) return;
        if (large != 0) W.SendMessageW(_hwnd, W.WM_SETICON, 1, large);
        if (small != 0) W.SendMessageW(_hwnd, W.WM_SETICON, 0, small);
    }

    private void Place(float widthDip, float heightDip, bool fitContent)
    {
        uint dpi = W.GetDpiForWindow(_hwnd);
        float s = dpi / 96f;
        if (fitContent) heightDip = Math.Clamp(MeasureContent(widthDip) + _page.FooterHeight, 180, heightDip);

        W.POINT cursor;
        W.GetCursorPos(&cursor);
        var mi = new W.MONITORINFO { cbSize = (uint)sizeof(W.MONITORINFO) };
        W.GetMonitorInfoW(W.MonitorFromPoint(cursor, 2), &mi);
        W.RECT work = mi.rcWork;

        var r = new W.RECT { Right = (int)(widthDip * s), Bottom = (int)(heightDip * s) };
        W.AdjustWindowRectExForDpi(&r, Style, 0, 0, dpi);
        int ww = Math.Min(r.Right - r.Left, work.Right - work.Left);
        int wh = Math.Min(r.Bottom - r.Top, work.Bottom - work.Top);
        int x = work.Left + (work.Right - work.Left - ww) / 2;
        int y = work.Top + (work.Bottom - work.Top - wh) / 2;
        W.SetWindowPos(_hwnd, 0, x, y, ww, wh, W.SWP_NOZORDER | W.SWP_NOACTIVATE);
    }

    /// <summary>Lays the page out off-screen to learn its natural height (used to size message dialogs).</summary>
    private float MeasureContent(float widthDip)
    {
        nint screen = W.GetDC(0);
        nint mem = W.CreateCompatibleDC(screen);
        nint bmp = W.CreateCompatibleBitmap(screen, 1, 1);
        nint old = W.SelectObject(mem, bmp);
        try
        {
            using var g = new Gfx(mem, 1);
            var ui = new Ui(g, Theme, widthDip, null, null, null, false);
            _page.Render(ui);
            return ui.Y;
        }
        finally
        {
            W.SelectObject(mem, old);
            W.DeleteObject(bmp);
            W.DeleteDC(mem);
            W.ReleaseDC(0, screen);
        }
    }

    // ------------------------------------------------------------------ window procedure

    [UnmanagedCallersOnly]
    private static nint WndProc(nint hwnd, uint msg, nint wp, nint lp)
    {
        if (!Windows.TryGetValue(hwnd, out AppWindow? w))
        {
            if (_creating is null) return W.DefWindowProcW(hwnd, msg, wp, lp);
            w = _creating;
            w._hwnd = hwnd;
            Windows[hwnd] = w;
        }
        try
        {
            return w.OnMessage(msg, wp, lp);
        }
        catch (Exception e)
        {
            // Never let an exception unwind into user32.
            w._error ??= e;
            W.DestroyWindow(hwnd);
            return 0;
        }
    }

    private nint OnMessage(uint msg, nint wp, nint lp)
    {
        switch (msg)
        {
            case W.WM_PAINT:
                Paint();
                return 0;
            case W.WM_ERASEBKGND:
                return 1;
            case W.WM_SIZE:
                Invalidate();
                return 0;
            case W.WM_GETMINMAXINFO:
            {
                var mmi = (W.MINMAXINFO*)lp;
                float s = _hwnd == 0 ? 1 : W.GetDpiForWindow(_hwnd) / 96f;
                mmi->ptMinTrackSize.X = (int)(_minW * s);
                mmi->ptMinTrackSize.Y = (int)(_minH * s);
                return 0;
            }
            case W.WM_DPICHANGED:
            {
                var r = (W.RECT*)lp;
                W.SetWindowPos(_hwnd, 0, r->Left, r->Top, r->Right - r->Left, r->Bottom - r->Top, W.SWP_NOZORDER | W.SWP_NOACTIVATE);
                Invalidate();
                return 0;
            }
            case W.WM_SETTINGCHANGE:
            case W.WM_THEMECHANGED:
                Theme = Theme.Load();
                ApplyChrome();
                Invalidate();
                break;
            case W.WM_MOUSEMOVE:
            {
                if (!_tracking)
                {
                    var tme = new W.TRACKMOUSEEVENT { cbSize = (uint)sizeof(W.TRACKMOUSEEVENT), dwFlags = W.TME_LEAVE, hwndTrack = _hwnd };
                    W.TrackMouseEvent(&tme);
                    _tracking = true;
                }
                string? h = HitTest(W.LoWord(lp), W.HiWord(lp))?.Id;
                if (h != _hover) { _hover = h; Invalidate(); }
                return 0;
            }
            case W.WM_MOUSELEAVE:
                _tracking = false;
                if (_hover is not null) { _hover = null; Invalidate(); }
                return 0;
            case W.WM_SETCURSOR:
                if (W.LoWord(lp) == 1) // HTCLIENT
                {
                    W.SetCursor(W.LoadCursorW(0, _hover is null ? W.IDC_ARROW : W.IDC_HAND));
                    return 1;
                }
                break;
            case W.WM_LBUTTONDOWN:
                _pressed = HitTest(W.LoWord(lp), W.HiWord(lp))?.Id;
                _focusVisible = false;
                if (_pressed is not null) { _focus = _pressed; W.SetCapture(_hwnd); }
                Invalidate();
                return 0;
            case W.WM_LBUTTONUP:
            {
                W.ReleaseCapture();
                Ui.Hit? hit = HitTest(W.LoWord(lp), W.HiWord(lp));
                string? pressed = _pressed;
                _pressed = null;
                Invalidate();
                if (pressed is not null && hit?.Id == pressed) hit.Value.Click();
                return 0;
            }
            case W.WM_MOUSEWHEEL:
                ScrollBy(-W.HiWord(wp) / 120f * 60);
                return 0;
            case W.WM_KEYDOWN:
                OnKey((int)wp);
                return 0;
            case W.WM_DESTROY:
                Windows.Remove(_hwnd);
                W.PostQuitMessage(0);
                return 0;
        }
        return W.DefWindowProcW(_hwnd, msg, wp, lp);
    }

    private void OnKey(int vk)
    {
        switch (vk)
        {
            case W.VK_TAB:
                MoveFocus(W.GetKeyState(W.VK_SHIFT) < 0 ? -1 : 1);
                break;
            case W.VK_DOWN:
                MoveFocus(1);
                break;
            case W.VK_UP:
                MoveFocus(-1);
                break;
            case W.VK_RETURN:
            case W.VK_SPACE:
                if (_focus is not null && _hits.FindIndex(h => h.Id == _focus) is int i and >= 0) _hits[i].Click();
                break;
            case W.VK_ESCAPE:
                _page.OnEscape();
                break;
            case W.VK_NEXT: ScrollBy(_viewH * 0.85f); break;
            case W.VK_PRIOR: ScrollBy(-_viewH * 0.85f); break;
            case W.VK_HOME: ScrollBy(-1e6f); break;
            case W.VK_END: ScrollBy(1e6f); break;
        }
        Invalidate();
    }

    private void MoveFocus(int dir)
    {
        if (_hits.Count == 0) return;
        int i = _focus is null ? -1 : _hits.FindIndex(h => h.Id == _focus);
        i = i < 0 ? (dir > 0 ? 0 : _hits.Count - 1) : (i + dir + _hits.Count) % _hits.Count;
        _focus = _hits[i].Id;
        _focusVisible = true;
        Ui.Hit h = _hits[i];
        if (!h.Footer)
        {
            if (h.Rect.Y - 12 < _scroll) _scroll = Math.Max(0, h.Rect.Y - 12);
            else if (h.Rect.Bottom + 12 > _scroll + _viewH) _scroll = h.Rect.Bottom + 12 - _viewH;
        }
    }

    private void ScrollBy(float dy)
    {
        float max = Math.Max(0, _contentH - _viewH);
        float next = Math.Clamp(_scroll + dy, 0, max);
        if (next != _scroll) { _scroll = next; Invalidate(); }
    }

    private Ui.Hit? HitTest(int px, int py)
    {
        float x = px / _scale, y = py / _scale;
        bool footer = y >= _viewH;
        for (int i = _hits.Count - 1; i >= 0; i--)
        {
            Ui.Hit h = _hits[i];
            if (h.Footer != footer) continue;
            if (h.Rect.Contains(x, footer ? y : y + _scroll)) return h;
        }
        return null;
    }

    // ------------------------------------------------------------------ painting

    private void Paint()
    {
        W.PAINTSTRUCT ps;
        nint hdc = W.BeginPaint(_hwnd, &ps);
        try
        {
            W.RECT rc;
            W.GetClientRect(_hwnd, &rc);
            int wpx = rc.Right, hpx = rc.Bottom;
            if (wpx <= 0 || hpx <= 0) return;

            _scale = W.GetDpiForWindow(_hwnd) / 96f;
            nint mem = W.CreateCompatibleDC(hdc);
            nint bmp = W.CreateCompatibleBitmap(hdc, wpx, hpx);
            nint old = W.SelectObject(mem, bmp);
            try
            {
                using (var g = new Gfx(mem, _scale))
                {
                    RenderFrame(g, wpx / _scale, hpx / _scale);
                }
                W.BitBlt(hdc, 0, 0, wpx, hpx, mem, 0, 0, W.SRCCOPY);
            }
            finally
            {
                W.SelectObject(mem, old);
                W.DeleteObject(bmp);
                W.DeleteDC(mem);
            }
        }
        finally
        {
            W.EndPaint(_hwnd, &ps);
        }

        float max = Math.Max(0, _contentH - _viewH);
        if (_scroll > max) { _scroll = max; Invalidate(); }
    }

    private void RenderFrame(Gfx g, float width, float height)
    {
        Theme t = Theme;
        g.Clear(t.Bg);
        float footerH = _page.FooterHeight;
        _viewH = height - footerH;

        var ui = new Ui(g, t, width, _hover, _pressed, _focus, _focusVisible);
        g.Clip(new RectF(0, 0, width, _viewH));
        g.Translate(0, -_scroll);
        _page.Render(ui);
        _contentH = ui.Y;
        g.ResetTransform();
        g.ResetClip();

        if (footerH > 0)
        {
            RectF area = new(0, _viewH, width, footerH);
            g.Fill(area, t.Bg);
            if (_contentH > _viewH) g.Fill(new RectF(0, _viewH, width, 1), t.Border);
            ui.InFooter = true;
            _page.RenderFooter(ui, area);
        }

        if (_contentH > _viewH + 1)
        {
            float thumbH = Math.Max(32, _viewH * _viewH / _contentH);
            float thumbY = (_viewH - thumbH) * (_scroll / (_contentH - _viewH));
            g.Fill(new RectF(width - 7, thumbY + 4, 3.5f, thumbH - 8), Theme.WithAlpha(t.Faint, 150), 2);
        }
        _hits = ui.Hits;
    }
}
