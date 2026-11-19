using System.Text;

namespace LazyBlox;

/// <summary>
/// User settings stored as a tiny UTF-8 key=value file. Deliberately not JSON/reflection based:
/// it is trivially AOT/trim safe and cheap to parse. The Roblox fast path never reads this file.
/// </summary>
public sealed class AppConfig
{
    /// <summary>Display name of the browser that non-Roblox links are forwarded to.</summary>
    public string BrowserName { get; set; } = "";
    /// <summary>ProgID of that browser (e.g. ChromeHTML). Used to re-resolve the command if the stored one breaks.</summary>
    public string BrowserProgId { get; set; } = "";
    /// <summary>Open command template, e.g. "C:\...\chrome.exe" --single-argument %1</summary>
    public string BrowserCommand { get; set; } = "";
    /// <summary>The https default ProgID that was active before installation (for restore guidance).</summary>
    public string OriginalHttpsProgId { get; set; } = "";
    /// <summary>The http default ProgID that was active before installation.</summary>
    public string OriginalHttpProgId { get; set; } = "";
    /// <summary>If the configured browser fails, try other registered browsers.</summary>
    public bool AutoDetectFallback { get; set; } = true;
    /// <summary>Also convert /games/ID?privateServerLinkCode=... links (opt-in).</summary>
    public bool LegacyPrivateServerLinks { get; set; }
    /// <summary>If a running Roblox ignores a share link, ask whether to restart it into that server.</summary>
    public bool RestartIfStuck { get; set; } = true;

    public static AppConfig Parse(string text)
    {
        var cfg = new AppConfig();
        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] is '#' or ';') continue;
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            string key = line[..eq].Trim();
            string value = line[(eq + 1)..].Trim();
            switch (key.ToLowerInvariant())
            {
                case "browsername": cfg.BrowserName = value; break;
                case "browserprogid": cfg.BrowserProgId = value; break;
                case "browsercommand": cfg.BrowserCommand = value; break;
                case "originalhttpsprogid": cfg.OriginalHttpsProgId = value; break;
                case "originalhttpprogid": cfg.OriginalHttpProgId = value; break;
                case "autodetectfallback": cfg.AutoDetectFallback = ParseBool(value, true); break;
                case "legacyprivateserverlinks": cfg.LegacyPrivateServerLinks = ParseBool(value, false); break;
                case "restartifstuck": cfg.RestartIfStuck = ParseBool(value, true); break;
            }
        }
        return cfg;
    }

    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# LazyBlox settings (UTF-8). Edit with care; 'LazyBlox.exe' with no arguments opens the settings UI.");
        Append(sb, "BrowserName", BrowserName);
        Append(sb, "BrowserProgId", BrowserProgId);
        Append(sb, "BrowserCommand", BrowserCommand);
        Append(sb, "OriginalHttpsProgId", OriginalHttpsProgId);
        Append(sb, "OriginalHttpProgId", OriginalHttpProgId);
        Append(sb, "AutoDetectFallback", AutoDetectFallback ? "true" : "false");
        Append(sb, "LegacyPrivateServerLinks", LegacyPrivateServerLinks ? "true" : "false");
        Append(sb, "RestartIfStuck", RestartIfStuck ? "true" : "false");
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string key, string value)
    {
        // Values are single-line by construction; strip anything that would break the format.
        sb.Append(key).Append('=').Append(value.Replace("\r", "").Replace("\n", "")).Append("\r\n");
    }

    private static bool ParseBool(string v, bool fallback) =>
        v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1" ? true
        : v.Equals("false", StringComparison.OrdinalIgnoreCase) || v == "0" ? false
        : fallback;

    public static AppConfig Load(string? path = null)
    {
        path ??= AppInfo.ConfigPath;
        try
        {
            return File.Exists(path) ? Parse(File.ReadAllText(path, Encoding.UTF8)) : new AppConfig();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new AppConfig();
        }
    }

    /// <summary>Atomic write (temp file + replace) so a crash never leaves a half-written config.</summary>
    public void Save(string? path = null)
    {
        path ??= AppInfo.ConfigPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, Serialize(), new UTF8Encoding(false));
        File.Move(tmp, path, overwrite: true);
    }
}
