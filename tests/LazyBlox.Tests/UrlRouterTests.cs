namespace LazyBlox.Tests;

public class UrlRouterTests
{
    private const string Code = "0123456789abcdef0123456789abcdef";
    private const string Deep = "roblox://navigation/share_links?code=" + Code + "&type=Server";

    // ---- 1. valid private server share links ----
    [Theory]
    [InlineData("https://www.roblox.com/share?code=" + Code + "&type=Server")]
    [InlineData("https://roblox.com/share?code=" + Code + "&type=Server")]
    [InlineData("https://WWW.ROBLOX.COM/share?code=" + Code + "&type=Server")]
    [InlineData("https://www.roblox.com/SHARE?code=" + Code + "&type=server")]
    [InlineData("https://www.roblox.com/share/?code=" + Code + "&type=Server")]
    [InlineData("https://www.roblox.com/share?type=Server&code=" + Code)]
    [InlineData("https://www.roblox.com/share?code=" + Code + "&type=Server&utm_source=discord&x")]
    [InlineData("https://www.roblox.com/share?code=" + Code + "&type=Server#frag")]
    [InlineData("  https://www.roblox.com/share?code=" + Code + "&type=Server  ")]
    [InlineData("http://www.roblox.com/share?code=" + Code + "&type=Server")]
    [InlineData("https://www.roblox.com:443/share?code=" + Code + "&type=Server")]
    [InlineData("https://www.roblox.com/share-links?code=" + Code + "&type=Server")]
    [InlineData("https://www.roblox.com/share?code=%30123456789abcdef0123456789abcdef&type=%53erver")] // percent-encoded
    public void ValidShareLinks_GoToRoblox(string url)
    {
        RouteDecision d = UrlRouter.Route(url);
        Assert.Equal(RouteKind.Roblox, d.Kind);
        Assert.Equal(Deep, d.Target);
    }

    // ---- 2. general Roblox pages and other share types -> browser ----
    [Theory]
    [InlineData("https://www.roblox.com/games/920587237/Adopt-Me")]
    [InlineData("https://www.roblox.com/home")]
    [InlineData("https://www.roblox.com/")]
    [InlineData("https://www.roblox.com/share?code=" + Code + "&type=ExperienceInvite")]
    [InlineData("https://www.roblox.com/share?code=" + Code + "&type=Profile")]
    [InlineData("https://www.roblox.com/share?code=" + Code)]
    [InlineData("https://www.roblox.com/share?type=Server")]
    [InlineData("https://www.roblox.com/shared?code=" + Code + "&type=Server")]
    [InlineData("https://www.roblox.com/ja/share?code=" + Code + "&type=Server")]
    public void OtherRobloxPages_GoToBrowser(string url) =>
        Assert.Equal(RouteKind.Browser, UrlRouter.Route(url).Kind);

    // ---- 3. normal https ----
    [Theory]
    [InlineData("https://example.com/")]
    [InlineData("https://discord.com/channels/1/2")]
    [InlineData("http://localhost:8080/x")]
    public void NormalSites_GoToBrowser(string url)
    {
        RouteDecision d = UrlRouter.Route(url);
        Assert.Equal(RouteKind.Browser, d.Kind);
        Assert.StartsWith("http", d.Target);
    }

    // ---- spoofed domains / origins ----
    [Theory]
    [InlineData("https://www.roblox.com.evil.com/share?code=" + Code + "&type=Server")]
    [InlineData("https://evilroblox.com/share?code=" + Code + "&type=Server")]
    [InlineData("https://roblox.co/share?code=" + Code + "&type=Server")]
    [InlineData("https://web.roblox.com/share?code=" + Code + "&type=Server")]
    [InlineData("https://www.roblox.com@evil.com/share?code=" + Code + "&type=Server")]
    [InlineData("https://user@www.roblox.com/share?code=" + Code + "&type=Server")]
    [InlineData("https://www.roblox.com:8443/share?code=" + Code + "&type=Server")]
    [InlineData("https://evil.com/share?code=" + Code + "&type=Server&host=www.roblox.com")]
    [InlineData("https://evil.com/www.roblox.com/share?code=" + Code + "&type=Server")]
    [InlineData("https://www.rоblox.com/share?code=" + Code + "&type=Server")] // Cyrillic 'о'
    [InlineData("https://www.roblox.com./share?code=" + Code + "&type=Server")]
    public void SpoofedHosts_NeverGoToRoblox(string url) =>
        Assert.NotEqual(RouteKind.Roblox, UrlRouter.Route(url).Kind);

    // ---- 4. invalid URLs -> reject ----
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("not a url")]
    [InlineData("www.roblox.com/share?code=abc&type=Server")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("roblox://navigation/share_links?code=abc&type=Server")]
    [InlineData("ftp://example.com/")]
    [InlineData("https://example.com/\r\nInjected")]
    [InlineData("https://exa\0mple.com/")]
    [InlineData("https://")]
    public void InvalidInput_IsRejected(string? url) =>
        Assert.Equal(RouteKind.Reject, UrlRouter.Route(url).Kind);

    [Fact]
    public void OversizeUrl_IsRejected() =>
        Assert.Equal(RouteKind.Reject, UrlRouter.Route("https://example.com/?q=" + new string('a', UrlRouter.MaxUrlLength)).Kind);

    // ---- 5. invalid share codes -> browser (website shows its own error) ----
    [Theory]
    [InlineData("abc")]                                   // too short
    [InlineData("abc<script>")]
    [InlineData("abc def ghi")]
    [InlineData("abcd%26type%3DServer")]                  // encoded & / = must not smuggle parameters
    [InlineData("abcd%22%20--flag")]
    [InlineData("abcd1234%00")]
    [InlineData("%E3%81%82%E3%81%84%E3%81%86%E3%81%88")]   // non-ASCII
    [InlineData("%zz")]                                   // malformed escape
    public void InvalidCodes_GoToBrowser(string code) =>
        Assert.Equal(RouteKind.Browser, UrlRouter.Route($"https://www.roblox.com/share?code={code}&type=Server").Kind);

    [Fact]
    public void OverlongCode_GoesToBrowser() =>
        Assert.Equal(RouteKind.Browser,
            UrlRouter.Route($"https://www.roblox.com/share?code={new string('a', RobloxLinks.MaxCodeLength + 1)}&type=Server").Kind);

    [Fact]
    public void DuplicateCode_IsAmbiguous_GoesToBrowser() =>
        Assert.Equal(RouteKind.Browser, UrlRouter.Route($"https://www.roblox.com/share?code={Code}&code=ffffffff&type=Server").Kind);

    [Fact]
    public void DuplicateType_IsAmbiguous_GoesToBrowser() =>
        Assert.Equal(RouteKind.Browser, UrlRouter.Route($"https://www.roblox.com/share?code={Code}&type=Server&type=ExperienceInvite").Kind);

    // ---- 8. spaces / special characters are normalized safely ----
    [Theory]
    [InlineData("https://example.com/a b", "https://example.com/a%20b")]
    [InlineData("https://example.com/?q=\"x\"", "https://example.com/?q=%22x%22")]
    [InlineData("https://example.com/?q=%PATH%", "https://example.com/?q=%25PATH%25")]
    [InlineData("https://example.com/?a=1&b=2|3^4", "https://example.com/?a=1&b=2%7C3%5E4")]
    [InlineData("https://例え.jp/パス", "https://例え.jp/%E3%83%91%E3%82%B9")] // IDN host kept as Unicode; path escaped
    public void SpecialCharacters_AreNormalized(string input, string expected)
    {
        RouteDecision d = UrlRouter.Route(input);
        Assert.Equal(RouteKind.Browser, d.Kind);
        Assert.Equal(expected, d.Target);
        Assert.DoesNotContain(' ', d.Target);
        Assert.DoesNotContain('"', d.Target);
    }

    // ---- legacy private server links (opt-in) ----
    [Fact]
    public void LegacyLink_DisabledByDefault_GoesToBrowser() =>
        Assert.Equal(RouteKind.Browser,
            UrlRouter.Route("https://www.roblox.com/games/123456/My-Game?privateServerLinkCode=12345678901234567890").Kind);

    [Fact]
    public void LegacyLink_WhenEnabled_ConvertsToPlaceIdLinkCode()
    {
        RouteDecision d = UrlRouter.Route("https://www.roblox.com/games/123456/My-Game?privateServerLinkCode=12345678901234567890", () => true);
        Assert.Equal(RouteKind.Roblox, d.Kind);
        Assert.Equal("roblox://placeId=123456&linkCode=12345678901234567890", d.Target);
    }

    [Theory]
    [InlineData("https://www.roblox.com/games/abc/x?privateServerLinkCode=12345678")]
    [InlineData("https://www.roblox.com/games/0/x?privateServerLinkCode=12345678")]
    [InlineData("https://www.roblox.com/games/123/x?privateServerLinkCode=1;calc")]
    [InlineData("https://evil.com/games/123/x?privateServerLinkCode=12345678")]
    public void LegacyLink_Invalid_GoesToBrowser(string url) =>
        Assert.Equal(RouteKind.Browser, UrlRouter.Route(url, () => true).Kind);

    [Fact]
    public void ConfigIsNotConsulted_ForShareLinksOrNormalSites()
    {
        int calls = 0;
        bool Probe() { calls++; return true; }
        UrlRouter.Route($"https://www.roblox.com/share?code={Code}&type=Server", Probe);
        UrlRouter.Route("https://example.com/", Probe);
        Assert.Equal(0, calls);
    }
}

public class QueryStringTests
{
    [Theory]
    [InlineData("?a=1&b=2", "b", QueryLookup.Found, "2")]
    [InlineData("a=1&b=2", "A", QueryLookup.Found, "1")]
    [InlineData("?a=1&&b=2&", "b", QueryLookup.Found, "2")]
    [InlineData("?a=1", "b", QueryLookup.NotFound, null)]
    [InlineData("?b=1&b=2", "b", QueryLookup.Duplicate, null)]
    [InlineData("?b", "b", QueryLookup.Found, "")]
    [InlineData("?b=x+y%21", "b", QueryLookup.Found, "x y!")]
    [InlineData("?%62=1", "b", QueryLookup.Found, "1")]
    [InlineData("", "b", QueryLookup.NotFound, null)]
    public void Lookup(string query, string key, QueryLookup expected, string? value)
    {
        Assert.Equal(expected, QueryString.TryGetUnique(query, key, out string? v));
        Assert.Equal(value, v);
    }
}
