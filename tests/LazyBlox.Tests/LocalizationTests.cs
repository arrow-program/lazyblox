using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using LazyBlox.Localization;

namespace LazyBlox.Tests;

internal static class TestLanguage
{
    /// <summary>Assertions in this project check Japanese messages, so pin the language regardless of the OS.</summary>
    [ModuleInitializer]
    internal static void PinJapanese() => L.Override("ja");
}

public partial class LocalizationTests
{
    public static TheoryData<string> Languages => [.. L.Supported];

    private static IEnumerable<PropertyInfo> Props =>
        typeof(Strings).GetProperties().Where(p => p.PropertyType == typeof(string));

    [GeneratedRegex(@"\{(\d+)\}")]
    private static partial Regex Placeholder();

    private static string[] Placeholders(string s) =>
        [.. Placeholder().Matches(s).Select(m => m.Value).Distinct().Order()];

    [Theory]
    [MemberData(nameof(Languages))]
    public void Every_string_is_filled_and_has_same_placeholders_as_english(string code)
    {
        Strings en = L.For("en"), t = L.For(code);
        foreach (PropertyInfo p in Props)
        {
            string ev = (string)p.GetValue(en)!, tv = (string)p.GetValue(t)!;
            Assert.False(string.IsNullOrWhiteSpace(tv), $"{code}.{p.Name} is empty");
            Assert.True(Placeholders(ev).SequenceEqual(Placeholders(tv)), $"{code}.{p.Name}: placeholders differ");
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Every_language_is_reachable(string code) => Assert.Equal(code, L.Normalize(code));

    [Theory]
    [InlineData("ja-JP", "ja")]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-TW", "zh-Hant")]
    [InlineData("zh-Hant-HK", "zh-Hant")]
    [InlineData("pt-BR", "pt")]
    [InlineData("es_MX", "es")]
    [InlineData("KO", "ko")]
    [InlineData("fr-FR", "en")]
    [InlineData("", "en")]
    public void Normalize_maps_tags(string tag, string expected) => Assert.Equal(expected, L.Normalize(tag));

    [Theory]
    [InlineData(0x0411, "ja")]
    [InlineData(0x0412, "ko")]
    [InlineData(0x0804, "zh-Hans")]
    [InlineData(0x1004, "zh-Hans")]
    [InlineData(0x0404, "zh-Hant")]
    [InlineData(0x0C04, "zh-Hant")]
    [InlineData(0x0C0A, "es")]
    [InlineData(0x0416, "pt")]
    [InlineData(0x0409, "en")]
    [InlineData(0x040C, "en")]
    public void LangId_maps_to_language(int langId, string expected) => Assert.Equal(expected, L.FromLangId((ushort)langId));
}
