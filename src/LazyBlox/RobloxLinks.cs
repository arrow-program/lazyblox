namespace LazyBlox;

/// <summary>
/// Recognizes Roblox web links and converts them to Roblox client deep links.
///
/// Share link (primary target):
///   https://www.roblox.com/share?code=CODE&amp;type=Server
///   -> roblox://navigation/share_links?code=CODE&amp;type=Server
///   The web page <c>/share-links</c> is the documented web counterpart of the
///   <c>roblox://navigation/share_links</c> deep link (type + code parameters).
///
/// Legacy private server link (opt-in):
///   https://www.roblox.com/games/PLACEID/Name?privateServerLinkCode=CODE
///   -> roblox://placeId=PLACEID&amp;linkCode=CODE
///   (Roblox "Direct to app" deep link format; linkCode = private server link code.)
/// </summary>
public static class RobloxLinks
{
    public const int MinCodeLength = 4;
    public const int MaxCodeLength = 128;

    public const string ShareDeepLinkPrefix = "roblox://navigation/share_links?code=";
    public const string ServerType = "Server";

    /// <summary>Exact host match only. <see cref="Uri.Host"/> is already lower-cased by the parser.</summary>
    public static bool IsRobloxHost(Uri uri) =>
        uri.Host.Equals("www.roblox.com", StringComparison.OrdinalIgnoreCase)
        || uri.Host.Equals("roblox.com", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Common origin checks: http(s), exact Roblox host, default port, no user-info
    /// (rejects tricks like https://www.roblox.com:8443/ or https://x@www.roblox.com/).
    /// </summary>
    private static bool IsTrustedRobloxOrigin(Uri uri) =>
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && IsRobloxHost(uri)
        && uri.IsDefaultPort
        && uri.UserInfo.Length == 0;

    public static string? TryConvertShareLink(Uri uri)
    {
        if (!IsTrustedRobloxOrigin(uri)) return null;

        string path = uri.AbsolutePath;
        if (!PathEquals(path, "/share") && !PathEquals(path, "/share-links")) return null;

        string query = uri.Query;
        if (QueryString.TryGetUnique(query, "type", out string? type) != QueryLookup.Found) return null;
        if (!string.Equals(type, ServerType, StringComparison.OrdinalIgnoreCase)) return null;

        if (QueryString.TryGetUnique(query, "code", out string? code) != QueryLookup.Found) return null;
        if (!IsValidCode(code)) return null;

        // code is restricted to [A-Za-z0-9_-] so it needs no escaping. Any other query
        // parameters (utm_*, tracking, ...) and the fragment are intentionally dropped.
        return string.Concat(ShareDeepLinkPrefix, code, "&type=", ServerType);
    }

    /// <summary>Cheap shape check so config is only loaded for URLs that could match.</summary>
    public static bool LooksLikeLegacyPrivateServer(Uri uri) =>
        IsTrustedRobloxOrigin(uri)
        && uri.AbsolutePath.StartsWith("/games/", StringComparison.OrdinalIgnoreCase)
        && uri.Query.Contains("privateServerLinkCode", StringComparison.OrdinalIgnoreCase);

    public static string? TryConvertLegacyPrivateServerLink(Uri uri)
    {
        if (!IsTrustedRobloxOrigin(uri)) return null;

        // /games/<placeId>[/<slug>...]
        ReadOnlySpan<char> rest = uri.AbsolutePath.AsSpan();
        const string prefix = "/games/";
        if (!rest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        rest = rest[prefix.Length..];
        int slash = rest.IndexOf('/');
        ReadOnlySpan<char> idText = slash >= 0 ? rest[..slash] : rest;
        if (idText.Length is 0 or > 19) return null;
        foreach (char c in idText) if (c is < '0' or > '9') return null;
        if (!ulong.TryParse(idText, out ulong placeId) || placeId == 0) return null;

        if (QueryString.TryGetUnique(uri.Query, "privateServerLinkCode", out string? code) != QueryLookup.Found) return null;
        if (!IsValidCode(code)) return null;

        return $"roblox://placeId={placeId}&linkCode={code}";
    }

    /// <summary>Share/link codes observed in practice are hex or digits. Accept the URL-safe alphabet only.</summary>
    public static bool IsValidCode([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] string? code)
    {
        if (code is null || code.Length < MinCodeLength || code.Length > MaxCodeLength) return false;
        foreach (char c in code)
        {
            bool ok = c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_';
            if (!ok) return false;
        }
        return true;
    }

    private static bool PathEquals(string path, string expected) =>
        path.Equals(expected, StringComparison.OrdinalIgnoreCase)
        || (path.Length == expected.Length + 1 && path[^1] == '/'
            && path.AsSpan(0, expected.Length).Equals(expected, StringComparison.OrdinalIgnoreCase));
}

public enum QueryLookup { NotFound, Found, Duplicate, Malformed }

/// <summary>Minimal, allocation-conscious query string reader (no System.Web dependency).</summary>
public static class QueryString
{
    /// <summary>
    /// Finds exactly one occurrence of <paramref name="key"/> (case-insensitive, percent-decoded).
    /// Duplicate keys are reported so callers can refuse ambiguous links.
    /// </summary>
    public static QueryLookup TryGetUnique(string query, string key, out string? value)
    {
        value = null;
        ReadOnlySpan<char> q = query.AsSpan();
        if (q.Length > 0 && q[0] == '?') q = q[1..];

        bool found = false;
        while (!q.IsEmpty)
        {
            int amp = q.IndexOf('&');
            ReadOnlySpan<char> pair = amp >= 0 ? q[..amp] : q;
            q = amp >= 0 ? q[(amp + 1)..] : ReadOnlySpan<char>.Empty;
            if (pair.IsEmpty) continue;

            int eq = pair.IndexOf('=');
            ReadOnlySpan<char> rawName = eq >= 0 ? pair[..eq] : pair;
            ReadOnlySpan<char> rawValue = eq >= 0 ? pair[(eq + 1)..] : ReadOnlySpan<char>.Empty;

            string? name = Decode(rawName);
            if (name is null) return QueryLookup.Malformed;
            if (!name.Equals(key, StringComparison.OrdinalIgnoreCase)) continue;

            if (found) { value = null; return QueryLookup.Duplicate; }
            value = Decode(rawValue);
            if (value is null) return QueryLookup.Malformed;
            found = true;
        }
        return found ? QueryLookup.Found : QueryLookup.NotFound;
    }

    private static string? Decode(ReadOnlySpan<char> s)
    {
        if (s.IndexOfAny('%', '+') < 0) return s.ToString();
        string text = s.ToString().Replace('+', ' ');
        try { return Uri.UnescapeDataString(text); }
        catch (UriFormatException) { return null; }
    }
}
