namespace LazyBlox.Ui;

/// <summary>Colors (ARGB) following the Windows light/dark app mode and the user's accent color.</summary>
internal sealed class Theme
{
    public bool Dark { get; private init; }
    public uint Bg, Surface, SurfaceHover, SurfacePress, Border, Text, SubText, Faint,
        Accent, AccentHover, AccentPress, OnAccent, AccentSoft,
        Danger, DangerSoft, Success, SuccessSoft, Warning, WarningSoft, Info, InfoSoft, Shadow;

    public static Theme Load()
    {
        bool dark = Environment.GetEnvironmentVariable("LAZYBLOX_UI_THEME") switch
        {
            "dark" => true,
            "light" => false,
            _ => ReadDword(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme") is 0,
        };
        uint accent = ReadAccent() ?? 0xFF0067C0;
        if (dark) accent = Mix(accent, 0xFFFFFFFF, 0.38f);

        var t = new Theme { Dark = dark };
        if (dark)
        {
            t.Bg = 0xFF1B1B1F; t.Surface = 0xFF26262B; t.SurfaceHover = 0xFF313138; t.SurfacePress = 0xFF222226;
            t.Border = 0xFF34343B; t.Text = 0xFFF2F2F5; t.SubText = 0xFFA3A3AD; t.Faint = 0xFF6E6E78;
            t.Danger = 0xFFFF7A7A; t.Success = 0xFF5FD38D; t.Warning = 0xFFFFC857; t.Info = 0xFF7FB8FF; t.Shadow = 0x40000000;
        }
        else
        {
            t.Bg = 0xFFF4F4F7; t.Surface = 0xFFFFFFFF; t.SurfaceHover = 0xFFF2F2F6; t.SurfacePress = 0xFFEAEAEF;
            t.Border = 0xFFE3E3E8; t.Text = 0xFF17171C; t.SubText = 0xFF5E5E6A; t.Faint = 0xFF9A9AA5;
            t.Danger = 0xFFD13438; t.Success = 0xFF138A4B; t.Warning = 0xFFA86400; t.Info = 0xFF1F6FD1; t.Shadow = 0x14000000;
        }
        t.Accent = accent;
        t.AccentHover = Mix(accent, dark ? 0xFFFFFFFF : 0xFF000000, 0.10f);
        t.AccentPress = Mix(accent, dark ? 0xFF000000 : 0xFF000000, 0.18f);
        t.OnAccent = Luminance(accent) > 0.55f ? 0xFF101014 : 0xFFFFFFFF;
        t.AccentSoft = Mix(t.Surface, accent, dark ? 0.20f : 0.12f);
        t.DangerSoft = Mix(t.Surface, t.Danger, dark ? 0.18f : 0.10f);
        t.SuccessSoft = Mix(t.Surface, t.Success, dark ? 0.18f : 0.10f);
        t.WarningSoft = Mix(t.Surface, t.Warning, dark ? 0.18f : 0.12f);
        t.InfoSoft = Mix(t.Surface, t.Info, dark ? 0.18f : 0.10f);
        return t;
    }

    /// <summary>COLORREF (0x00BBGGRR) for DWM title bar colors.</summary>
    public static uint ToColorRef(uint argb) => ((argb & 0xFF) << 16) | (argb & 0xFF00) | ((argb >> 16) & 0xFF);

    public static uint WithAlpha(uint argb, byte a) => (argb & 0x00FFFFFF) | ((uint)a << 24);

    public static uint Mix(uint a, uint b, float t)
    {
        static uint C(uint x, int s) => (x >> s) & 0xFF;
        uint R(int s) => (uint)Math.Round(C(a, s) + (C(b, s) - (float)C(a, s)) * t);
        return 0xFF000000 | (R(16) << 16) | (R(8) << 8) | R(0);
    }

    private static float Luminance(uint c) =>
        (0.2126f * ((c >> 16) & 0xFF) + 0.7152f * ((c >> 8) & 0xFF) + 0.0722f * (c & 0xFF)) / 255f;

    private static uint? ReadAccent()
    {
        // HKCU\Software\Microsoft\Windows\DWM\AccentColor is ABGR.
        uint? abgr = ReadDword(@"Software\Microsoft\Windows\DWM", "AccentColor");
        if (abgr is not uint v) return null;
        return 0xFF000000 | ((v & 0xFF) << 16) | (v & 0xFF00) | ((v >> 16) & 0xFF);
    }

    private static unsafe uint? ReadDword(string key, string value)
    {
        uint data, size = sizeof(uint);
        const uint RRF_RT_REG_DWORD = 0x10;
        int rc = Native.RegGetValueW(Native.HKEY_CURRENT_USER, key, value, RRF_RT_REG_DWORD, null, &data, &size);
        return rc == 0 ? data : null;
    }
}
