using System.Runtime.InteropServices;

namespace LazyBlox.Ui;

/// <summary>Win32 windowing / GDI / DWM interop used only by the GUI (never touched on the link hot path).</summary>
internal static unsafe partial class W
{
    [StructLayout(LayoutKind.Sequential)]
    public struct WNDCLASSEXW
    {
        public uint cbSize, style;
        public delegate* unmanaged<nint, uint, nint, nint, nint> lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public nint hInstance, hIcon, hCursor, hbrBackground;
        public char* lpszMenuName, lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct MSG { public nint hwnd; public uint message; public nint wParam, lParam; public uint time; public POINT pt; public uint lPrivate; }
    [StructLayout(LayoutKind.Sequential)] public struct PAINTSTRUCT { public nint hdc; public int fErase; public RECT rcPaint; public int fRestore, fIncUpdate; public fixed byte rgbReserved[32]; }
    [StructLayout(LayoutKind.Sequential)] public struct TRACKMOUSEEVENT { public uint cbSize, dwFlags; public nint hwndTrack; public uint dwHoverTime; }
    [StructLayout(LayoutKind.Sequential)] public struct MONITORINFO { public uint cbSize; public RECT rcMonitor, rcWork; public uint dwFlags; }
    [StructLayout(LayoutKind.Sequential)] public struct MINMAXINFO { public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize; }

    public const uint WM_DESTROY = 0x0002, WM_SIZE = 0x0005, WM_ACTIVATE = 0x0006, WM_PAINT = 0x000F, WM_CLOSE = 0x0010, WM_ERASEBKGND = 0x0014,
        WM_SETTINGCHANGE = 0x001A, WM_SETCURSOR = 0x0020, WM_GETMINMAXINFO = 0x0024, WM_SETICON = 0x0080, WM_KEYDOWN = 0x0100,
        WM_MOUSEMOVE = 0x0200, WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202, WM_MOUSEWHEEL = 0x020A, WM_MOUSELEAVE = 0x02A3,
        WM_DPICHANGED = 0x02E0, WM_THEMECHANGED = 0x031A;

    public const uint WS_OVERLAPPEDWINDOW = 0x00CF0000, WS_CAPTION = 0x00C00000, WS_SYSMENU = 0x00080000, WS_MINIMIZEBOX = 0x00020000,
        WS_THICKFRAME = 0x00040000, WS_MAXIMIZEBOX = 0x00010000;
    public const uint CS_HREDRAW = 0x2, CS_VREDRAW = 0x1;
    public const int SW_SHOW = 5, SW_HIDE = 0;
    public const uint SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10, SWP_NOMOVE = 0x2;
    public const uint TME_LEAVE = 0x2;
    public const int IDC_ARROW = 32512, IDC_HAND = 32649;
    public const int VK_TAB = 0x09, VK_RETURN = 0x0D, VK_ESCAPE = 0x1B, VK_SPACE = 0x20, VK_SHIFT = 0x10,
        VK_UP = 0x26, VK_DOWN = 0x28, VK_PRIOR = 0x21, VK_NEXT = 0x22, VK_HOME = 0x24, VK_END = 0x23;
    public const uint SRCCOPY = 0x00CC0020;
    public const uint CF_UNICODETEXT = 13, GMEM_MOVEABLE = 0x2;

    [LibraryImport("user32.dll")] public static partial ushort RegisterClassExW(WNDCLASSEXW* wc);
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int w, int h, nint parent, nint menu, nint inst, nint param);
    [LibraryImport("user32.dll")] public static partial nint DefWindowProcW(nint hwnd, uint msg, nint wParam, nint lParam);
    [LibraryImport("user32.dll")] public static partial int GetMessageW(MSG* msg, nint hwnd, uint min, uint max);
    [LibraryImport("user32.dll")] public static partial int TranslateMessage(MSG* msg);
    [LibraryImport("user32.dll")] public static partial nint DispatchMessageW(MSG* msg);
    [LibraryImport("user32.dll")] public static partial void PostQuitMessage(int code);
    [LibraryImport("user32.dll")] public static partial int ShowWindow(nint hwnd, int cmd);
    [LibraryImport("user32.dll")] public static partial int InvalidateRect(nint hwnd, RECT* rect, int erase);
    [LibraryImport("user32.dll")] public static partial nint BeginPaint(nint hwnd, PAINTSTRUCT* ps);
    [LibraryImport("user32.dll")] public static partial int EndPaint(nint hwnd, PAINTSTRUCT* ps);
    [LibraryImport("user32.dll")] public static partial int GetClientRect(nint hwnd, RECT* rect);
    [LibraryImport("user32.dll")] public static partial nint LoadCursorW(nint inst, nint name);
    [LibraryImport("user32.dll")] public static partial nint SetCursor(nint cursor);
    [LibraryImport("user32.dll")] public static partial int TrackMouseEvent(TRACKMOUSEEVENT* tme);
    [LibraryImport("user32.dll")] public static partial uint GetDpiForWindow(nint hwnd);
    [LibraryImport("user32.dll")] public static partial int SetWindowPos(nint hwnd, nint after, int x, int y, int w, int h, uint flags);
    [LibraryImport("user32.dll")] public static partial int DestroyWindow(nint hwnd);
    [LibraryImport("user32.dll")] public static partial int AdjustWindowRectExForDpi(RECT* rect, uint style, int menu, uint exStyle, uint dpi);
    [LibraryImport("user32.dll")] public static partial int GetCursorPos(POINT* pt);
    [LibraryImport("user32.dll")] public static partial nint MonitorFromPoint(POINT pt, uint flags);
    [LibraryImport("user32.dll")] public static partial int GetMonitorInfoW(nint monitor, MONITORINFO* info);
    [LibraryImport("user32.dll")] public static partial nint SetCapture(nint hwnd);
    [LibraryImport("user32.dll")] public static partial int ReleaseCapture();
    [LibraryImport("user32.dll")] public static partial int SetForegroundWindow(nint hwnd);
    [LibraryImport("user32.dll")] public static partial short GetKeyState(int vk);
    [LibraryImport("user32.dll")] public static partial nint SendMessageW(nint hwnd, uint msg, nint wParam, nint lParam);
    [LibraryImport("user32.dll")] public static partial int OpenClipboard(nint hwnd);
    [LibraryImport("user32.dll")] public static partial int CloseClipboard();
    [LibraryImport("user32.dll")] public static partial int EmptyClipboard();
    [LibraryImport("user32.dll")] public static partial nint GetClipboardData(uint format);
    [LibraryImport("user32.dll")] public static partial nint SetClipboardData(uint format, nint mem);

    [LibraryImport("kernel32.dll")] public static partial nint GlobalAlloc(uint flags, nuint bytes);
    [LibraryImport("kernel32.dll")] public static partial void* GlobalLock(nint mem);
    [LibraryImport("kernel32.dll")] public static partial int GlobalUnlock(nint mem);
    [LibraryImport("kernel32.dll")] public static partial nint GetModuleHandleW(char* name);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial uint ExtractIconExW(string file, int index, nint* large, nint* small, uint count);

    [LibraryImport("gdi32.dll")] public static partial nint CreateCompatibleDC(nint hdc);
    [LibraryImport("gdi32.dll")] public static partial nint CreateCompatibleBitmap(nint hdc, int w, int h);
    [LibraryImport("gdi32.dll")] public static partial nint SelectObject(nint hdc, nint obj);
    [LibraryImport("gdi32.dll")] public static partial int BitBlt(nint dst, int x, int y, int w, int h, nint src, int sx, int sy, uint rop);
    [LibraryImport("gdi32.dll")] public static partial int DeleteObject(nint obj);
    [LibraryImport("gdi32.dll")] public static partial int DeleteDC(nint hdc);
    [LibraryImport("user32.dll")] public static partial nint GetDC(nint hwnd);
    [LibraryImport("user32.dll")] public static partial int ReleaseDC(nint hwnd, nint hdc);

    public const uint DWMWA_USE_IMMERSIVE_DARK_MODE = 20, DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWMWA_BORDER_COLOR = 34,
        DWMWA_CAPTION_COLOR = 35, DWMWA_TEXT_COLOR = 36;
    [LibraryImport("dwmapi.dll")] public static partial int DwmSetWindowAttribute(nint hwnd, uint attr, void* value, uint size);

    public static void SetDwm(nint hwnd, uint attr, uint value) => DwmSetWindowAttribute(hwnd, attr, &value, sizeof(uint));

    public static short HiWord(nint v) => (short)(((long)v >> 16) & 0xFFFF);
    public static short LoWord(nint v) => (short)((long)v & 0xFFFF);

    // ---------------- clipboard helpers ----------------

    public static string? GetClipboardText(nint hwnd)
    {
        if (OpenClipboard(hwnd) == 0) return null;
        try
        {
            nint h = GetClipboardData(CF_UNICODETEXT);
            if (h == 0) return null;
            char* p = (char*)GlobalLock(h);
            try { return p is null ? null : new string(p); }
            finally { GlobalUnlock(h); }
        }
        finally { CloseClipboard(); }
    }

    public static bool SetClipboardText(nint hwnd, string text)
    {
        if (OpenClipboard(hwnd) == 0) return false;
        try
        {
            EmptyClipboard();
            nint mem = GlobalAlloc(GMEM_MOVEABLE, (nuint)((text.Length + 1) * 2));
            if (mem == 0) return false;
            char* dst = (char*)GlobalLock(mem);
            fixed (char* src = text) Buffer.MemoryCopy(src, dst, (text.Length + 1) * 2, text.Length * 2);
            dst[text.Length] = '\0';
            GlobalUnlock(mem);
            return SetClipboardData(CF_UNICODETEXT, mem) != 0;
        }
        finally { CloseClipboard(); }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct RectF(float x, float y, float w, float h)
{
    public float X = x, Y = y, W = w, H = h;
    public readonly float Right => X + W;
    public readonly float Bottom => Y + H;
    public readonly bool Contains(float px, float py) => px >= X && px < X + W && py >= Y && py < Y + H;
    public readonly RectF Inflate(float d) => new(X - d, Y - d, W + 2 * d, H + 2 * d);
}

[StructLayout(LayoutKind.Sequential)]
internal struct PointF(float x, float y) { public float X = x, Y = y; }

/// <summary>GDI+ flat API (gdiplus.dll). Every call returns a Status (0 = Ok).</summary>
internal static unsafe partial class Gdip
{
    [StructLayout(LayoutKind.Sequential)]
    public struct StartupInput { public uint Version; public nint DebugCallback; public int SuppressBackgroundThread, SuppressExternalCodecs; }

    private const string Dll = "gdiplus.dll";
    [LibraryImport(Dll)] public static partial int GdiplusStartup(nuint* token, StartupInput* input, nint output);
    [LibraryImport(Dll)] public static partial int GdipCreateFromHDC(nint hdc, nint* g);
    [LibraryImport(Dll)] public static partial int GdipDeleteGraphics(nint g);
    [LibraryImport(Dll)] public static partial int GdipSetSmoothingMode(nint g, int mode);
    [LibraryImport(Dll)] public static partial int GdipSetTextRenderingHint(nint g, int hint);
    [LibraryImport(Dll)] public static partial int GdipSetPixelOffsetMode(nint g, int mode);
    [LibraryImport(Dll)] public static partial int GdipSetCompositingQuality(nint g, int q);
    [LibraryImport(Dll)] public static partial int GdipGraphicsClear(nint g, uint argb);
    [LibraryImport(Dll)] public static partial int GdipResetWorldTransform(nint g);
    [LibraryImport(Dll)] public static partial int GdipScaleWorldTransform(nint g, float sx, float sy, int order);
    [LibraryImport(Dll)] public static partial int GdipTranslateWorldTransform(nint g, float dx, float dy, int order);
    [LibraryImport(Dll)] public static partial int GdipSetClipRect(nint g, float x, float y, float w, float h, int combine);
    [LibraryImport(Dll)] public static partial int GdipResetClip(nint g);

    [LibraryImport(Dll)] public static partial int GdipCreateSolidFill(uint argb, nint* brush);
    [LibraryImport(Dll)] public static partial int GdipSetSolidFillColor(nint brush, uint argb);
    [LibraryImport(Dll)] public static partial int GdipCreateLineBrushFromRect(RectF* rect, uint c1, uint c2, int mode, int wrap, nint* brush);
    [LibraryImport(Dll)] public static partial int GdipDeleteBrush(nint brush);
    [LibraryImport(Dll)] public static partial int GdipCreatePen1(uint argb, float width, int unit, nint* pen);
    [LibraryImport(Dll)] public static partial int GdipSetPenColor(nint pen, uint argb);
    [LibraryImport(Dll)] public static partial int GdipSetPenWidth(nint pen, float width);
    [LibraryImport(Dll)] public static partial int GdipSetPenStartCap(nint pen, int cap);
    [LibraryImport(Dll)] public static partial int GdipSetPenEndCap(nint pen, int cap);
    [LibraryImport(Dll)] public static partial int GdipSetPenLineJoin(nint pen, int join);
    [LibraryImport(Dll)] public static partial int GdipDeletePen(nint pen);

    [LibraryImport(Dll)] public static partial int GdipCreatePath(int fillMode, nint* path);
    [LibraryImport(Dll)] public static partial int GdipDeletePath(nint path);
    [LibraryImport(Dll)] public static partial int GdipResetPath(nint path);
    [LibraryImport(Dll)] public static partial int GdipAddPathArc(nint path, float x, float y, float w, float h, float start, float sweep);
    [LibraryImport(Dll)] public static partial int GdipAddPathLine(nint path, float x1, float y1, float x2, float y2);
    [LibraryImport(Dll)] public static partial int GdipAddPathPolygon(nint path, PointF* points, int count);
    [LibraryImport(Dll)] public static partial int GdipAddPathLine2(nint path, PointF* points, int count);
    [LibraryImport(Dll)] public static partial int GdipClosePathFigure(nint path);
    [LibraryImport(Dll)] public static partial int GdipFillPath(nint g, nint brush, nint path);
    [LibraryImport(Dll)] public static partial int GdipDrawPath(nint g, nint pen, nint path);
    [LibraryImport(Dll)] public static partial int GdipFillEllipse(nint g, nint brush, float x, float y, float w, float h);
    [LibraryImport(Dll)] public static partial int GdipDrawEllipse(nint g, nint pen, float x, float y, float w, float h);
    [LibraryImport(Dll)] public static partial int GdipDrawLine(nint g, nint pen, float x1, float y1, float x2, float y2);
    [LibraryImport(Dll)] public static partial int GdipFillRectangle(nint g, nint brush, float x, float y, float w, float h);

    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int GdipCreateFontFamilyFromName(string name, nint collection, nint* family);
    [LibraryImport(Dll)] public static partial int GdipDeleteFontFamily(nint family);
    [LibraryImport(Dll)] public static partial int GdipCreateFont(nint family, float size, int style, int unit, nint* font);
    [LibraryImport(Dll)] public static partial int GdipDeleteFont(nint font);
    [LibraryImport(Dll)] public static partial int GdipCreateStringFormat(int flags, ushort lang, nint* format);
    [LibraryImport(Dll)] public static partial int GdipSetStringFormatAlign(nint format, int align);
    [LibraryImport(Dll)] public static partial int GdipSetStringFormatLineAlign(nint format, int align);
    [LibraryImport(Dll)] public static partial int GdipSetStringFormatTrimming(nint format, int trimming);
    [LibraryImport(Dll)] public static partial int GdipSetStringFormatFlags(nint format, int flags);
    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int GdipDrawString(nint g, string text, int length, nint font, RectF* layout, nint format, nint brush);
    [LibraryImport(Dll, StringMarshalling = StringMarshalling.Utf16)]
    public static partial int GdipMeasureString(nint g, string text, int length, nint font, RectF* layout, nint format, RectF* bounds, int* codepoints, int* lines);

    public const int SmoothingAntiAlias = 4, PixelOffsetHighQuality = 2, TextClearTypeGridFit = 5, TextAntiAliasGridFit = 3,
        UnitPixel = 2, UnitWorld = 0, MatrixOrderPrepend = 0, CombineReplace = 0, CapRound = 2, JoinRound = 2,
        FontRegular = 0, FontBold = 1, FlagsNoWrap = 0x1000, FlagsNoClip = 0x4000, TrimmingEllipsisChar = 3, TrimmingNone = 0,
        LinearGradientForwardDiagonal = 2, LinearGradientVertical = 1;

    private static nuint _token;

    public static void EnsureStarted()
    {
        if (_token != 0) return;
        var input = new StartupInput { Version = 1 };
        nuint token;
        if (GdiplusStartup(&token, &input, 0) != 0) throw new InvalidOperationException("GdiplusStartup failed");
        _token = token;
    }
}
