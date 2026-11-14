using System.Globalization;
using System.Runtime.InteropServices;

namespace LazyBlox.Localization;

/// <summary>
/// UI language selection. Follows the Windows display language (GetUserDefaultUILanguage):
/// CultureInfo cannot be used because the app runs with InvariantGlobalization for startup speed.
/// Resolved lazily, so the Roblox hot path never touches it unless an error must be shown.
/// Override with LAZYBLOX_LANG=en|ja|zh-Hans|zh-Hant|ko|es|pt (tests, screenshots).
/// </summary>
internal static partial class L
{
    public const string LangEnvVar = "LAZYBLOX_LANG";
    public static readonly string[] Supported = ["en", "ja", "zh-Hans", "zh-Hant", "ko", "es", "pt"];

    private static string? _code;
    private static Strings? _strings;

    /// <summary>Current language code (one of <see cref="Supported"/>).</summary>
    public static string Code => _code ??= Detect();

    /// <summary>Strings for the current language.</summary>
    public static Strings S => _strings ??= For(Code);

    public static string F(string format, params object?[] args) => string.Format(CultureInfo.InvariantCulture, format, args);

    public static void Override(string code)
    {
        _code = Normalize(code);
        _strings = null;
    }

    public static Strings For(string code) => Normalize(code) switch
    {
        "ja" => Lang.Ja,
        "zh-Hans" => Lang.ZhHans,
        "zh-Hant" => Lang.ZhHant,
        "ko" => Lang.Ko,
        "es" => Lang.Es,
        "pt" => Lang.Pt,
        _ => Lang.En,
    };

    private static string Detect()
    {
        string? env = Environment.GetEnvironmentVariable(LangEnvVar);
        if (!string.IsNullOrWhiteSpace(env)) return Normalize(env);
        try { return FromLangId(GetUserDefaultUILanguage()); }
        catch (EntryPointNotFoundException) { return "en"; }
    }

    /// <summary>Maps a Windows LANGID to a supported language code.</summary>
    public static string FromLangId(ushort langId)
    {
        int primary = langId & 0x3FF;
        return primary switch
        {
            0x11 => "ja",
            0x12 => "ko",
            0x0A => "es",
            0x16 => "pt",
            // Chinese: Taiwan / Hong Kong / Macao (and zh-Hant neutral) use Traditional.
            0x04 => langId is 0x0404 or 0x0C04 or 0x1404 or 0x7C04 ? "zh-Hant" : "zh-Hans",
            _ => "en",
        };
    }

    /// <summary>Normalizes BCP-47 style tags ("ja-JP", "zh-TW", "pt-BR", ...) to a supported code.</summary>
    public static string Normalize(string tag)
    {
        string t = tag.Trim().Replace('_', '-').ToLowerInvariant();
        if (t.StartsWith("zh", StringComparison.Ordinal))
            return t.Contains("hant") || t.EndsWith("-tw") || t.EndsWith("-hk") || t.EndsWith("-mo") ? "zh-Hant" : "zh-Hans";
        foreach (string code in Supported)
        {
            string c = code.ToLowerInvariant();
            if (t == c || t.StartsWith(c + "-", StringComparison.Ordinal)) return code;
        }
        return "en";
    }

    [LibraryImport("kernel32.dll")]
    private static partial ushort GetUserDefaultUILanguage();
}
