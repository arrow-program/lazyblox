using LazyBlox.Localization;
namespace LazyBlox;

/// <summary>What the launcher should do with an incoming URL.</summary>
public enum RouteKind
{
    /// <summary>Launch the Roblox client through the <c>roblox:</c> protocol (Target = deep link).</summary>
    Roblox,
    /// <summary>Forward to the user's real web browser (Target = normalized absolute URL).</summary>
    Browser,
    /// <summary>Refuse: not something a web browser handler should open.</summary>
    Reject,
}

public readonly record struct RouteDecision(RouteKind Kind, string Target, string Reason)
{
    public static RouteDecision Reject(string reason) => new(RouteKind.Reject, string.Empty, reason);
}

/// <summary>
/// Pure, allocation-light URL classification. No I/O, no network, no registry.
/// Decisions are made on the parsed <see cref="Uri"/> (scheme / host / path / query),
/// never on substring matching of the raw text.
/// </summary>
public static class UrlRouter
{
    /// <summary>CreateProcess command lines are limited to 32767 chars; keep headroom for the browser path/flags.</summary>
    public const int MaxUrlLength = 30_000;

    public static RouteDecision Route(string? raw, Func<bool>? legacyPrivateServerLinksEnabled = null)
    {
        if (!TryNormalize(raw, out Uri? uri, out string? error))
            return RouteDecision.Reject(error);

        string? deepLink = RobloxLinks.TryConvertShareLink(uri);
        if (deepLink is not null)
            return new RouteDecision(RouteKind.Roblox, deepLink, "roblox share link (type=Server)");

        // The legacy "/games/<id>?privateServerLinkCode=" form is opt-in (see README) – the
        // config is only consulted for URLs that actually have that shape.
        if (RobloxLinks.LooksLikeLegacyPrivateServer(uri) && legacyPrivateServerLinksEnabled?.Invoke() == true)
        {
            deepLink = RobloxLinks.TryConvertLegacyPrivateServerLink(uri);
            if (deepLink is not null)
                return new RouteDecision(RouteKind.Roblox, deepLink, "legacy private server link");
        }

        return new RouteDecision(RouteKind.Browser, uri.AbsoluteUri, "not a roblox private server link");
    }

    /// <summary>
    /// Parses an incoming argument into an absolute http/https URI.
    /// Rejects relative URIs, non-web schemes (file:, javascript:, roblox:, ...), control characters and oversize input.
    /// </summary>
    public static bool TryNormalize(string? raw, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Uri? uri,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out string? error)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(raw)) { error = L.S.UrlEmpty; return false; }

        string text = raw.Trim();
        if (text.Length > MaxUrlLength) { error = L.S.UrlTooLong; return false; }

        foreach (char c in text)
        {
            // Control characters (incl. CR/LF/NUL) never belong in a URL handed over by the shell.
            if (char.IsControl(c)) { error = L.S.UrlControlChars; return false; }
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? parsed)) { error = L.S.UrlMalformed; return false; }
        if (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp)
        {
            error = L.F(L.S.UrlUnsupportedScheme, parsed.Scheme);
            return false;
        }
        if (string.IsNullOrEmpty(parsed.Host)) { error = L.S.UrlNoHost; return false; }

        uri = parsed;
        error = null;
        return true;
    }
}
