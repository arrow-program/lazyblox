using LazyBlox.Localization;
namespace LazyBlox;

/// <summary>A fully resolved process invocation (no shell involved).</summary>
public sealed record ProcessInvocation(string FileName, IReadOnlyList<string> Arguments)
{
    public override string ToString() =>
        Quote(FileName) + (Arguments.Count == 0 ? "" : " " + string.Join(' ', Arguments.Select(Quote)));

    private static string Quote(string s) => s.Length == 0 || s.AsSpan().IndexOfAny(' ', '\t', '"') >= 0 ? $"\"{s}\"" : s;
}

/// <summary>Builds a Windows command line whose CommandLineToArgvW parse yields exactly the given argv.</summary>
public static class CommandLineBuilder
{
    public static string Build(string fileName, IReadOnlyList<string> args)
    {
        var sb = new System.Text.StringBuilder(fileName.Length + 16 + args.Sum(a => a.Length + 3));
        Append(sb, fileName, isProgramName: true);
        foreach (string a in args) { sb.Append(' '); Append(sb, a, isProgramName: false); }
        return sb.ToString();
    }

    private static void Append(System.Text.StringBuilder sb, string arg, bool isProgramName)
    {
        if (isProgramName)
        {
            // argv[0] parsing has no escape rules: quotes simply delimit. Paths cannot contain '"'.
            sb.Append('"').Append(arg).Append('"');
            return;
        }
        if (arg.Length > 0 && arg.AsSpan().IndexOfAny(" \t\n\v\"") < 0) { sb.Append(arg); return; }
        sb.Append('"');
        for (int i = 0; i < arg.Length; i++)
        {
            int backslashes = 0;
            while (i < arg.Length && arg[i] == '\\') { backslashes++; i++; }
            if (i == arg.Length) { sb.Append('\\', backslashes * 2); break; }
            if (arg[i] == '"') sb.Append('\\', backslashes * 2 + 1).Append('"');
            else sb.Append('\\', backslashes).Append(arg[i]);
        }
        sb.Append('"');
    }
}

/// <summary>
/// Turns a registry-style open command template (e.g. <c>"C:\...\chrome.exe" --single-argument %1</c>)
/// into an argv array with the URL substituted as a single, separate argument.
///
/// Security model:
///  * The template is tokenized BEFORE the URL is inserted, so the URL can never create extra arguments.
///  * Environment variables are expanded only in the executable path, never after URL substitution
///    (a URL like https://x/%PATH% stays literal).
///  * The process is started with CreateProcess + ArgumentList (proper quoting), never through cmd.exe.
///  * Shell dispatchers (rundll32, explorer, cmd, ...) are refused because they would route the URL
///    back through the https association, i.e. back to this launcher (infinite loop).
/// </summary>
public static class BrowserCommand
{
    private static readonly string[] Placeholders = ["%1", "%l", "%L"];

    /// <summary>Executables that dispatch through ShellExecute (would re-enter the https association) or run scripts.</summary>
    private static readonly HashSet<string> ForbiddenExecutables = new(StringComparer.OrdinalIgnoreCase)
    {
        "rundll32.exe", "explorer.exe", "cmd.exe", "powershell.exe", "pwsh.exe", "conhost.exe",
        "wscript.exe", "cscript.exe", "mshta.exe", "start.exe", "openwith.exe", "launchwinapp.exe",
    };

    public static bool TryBuild(string template, string url, string? selfExePath,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ProcessInvocation? invocation,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out string? error)
    {
        invocation = null;
        List<string> tokens = Tokenize(template);
        if (tokens.Count == 0) { error = L.S.CmdEmpty; return false; }

        int exeTokens = ResolveExecutableTokens(tokens, out string exe);
        if (exe.Length == 0) { error = L.S.CmdEmpty; return false; }
        if (IsForbidden(exe, selfExePath, out error)) return false;
        if (!Path.IsPathFullyQualified(exe))
        {
            error = L.F(L.S.CmdNotAbsolute, exe);
            return false;
        }

        var args = new List<string>(tokens.Count);
        bool substituted = false;
        for (int i = exeTokens; i < tokens.Count; i++)
        {
            string token = tokens[i];
            if (token == "%*") continue; // "all remaining args" – nothing else to pass.
            foreach (string ph in Placeholders)
            {
                if (token.Contains(ph, StringComparison.Ordinal))
                {
                    token = token.Replace(ph, url, StringComparison.Ordinal);
                    substituted = true;
                    break;
                }
            }
            args.Add(token);
        }
        if (!substituted) args.Add(url);

        invocation = new ProcessInvocation(exe, args);
        error = null;
        return true;
    }

    /// <summary>
    /// Executable path from a tokenized command. Handles legacy unquoted paths with spaces
    /// (e.g. <c>C:\Program Files\Internet Explorer\iexplore.exe %1</c>) the way CreateProcess does:
    /// join tokens until an existing "*.exe" is found. Returns how many tokens form the path.
    /// </summary>
    public static int ResolveExecutableTokens(List<string> tokens, out string exe)
    {
        exe = Environment.ExpandEnvironmentVariables(tokens[0]).Trim();
        if (File.Exists(exe)) return 1;
        for (int k = 2; k <= tokens.Count; k++)
        {
            string candidate = Environment.ExpandEnvironmentVariables(string.Join(' ', tokens.Take(k))).Trim();
            if (candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(candidate))
            {
                exe = candidate;
                return k;
            }
        }
        return 1;
    }

    /// <summary>Loop / safety guard for a candidate browser executable.</summary>
    public static bool IsForbidden(string exe, string? selfExePath, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? error)
    {
        string fileName = Path.GetFileName(exe);
        if (fileName.Equals(AppInfo.ExeName, StringComparison.OrdinalIgnoreCase) || PathsEqual(exe, selfExePath))
        {
            error = L.S.CmdIsSelf;
            return true;
        }
        if (ForbiddenExecutables.Contains(fileName))
        {
            error = L.F(L.S.CmdShellDispatcher, fileName);
            return true;
        }
        error = null;
        return false;
    }

    public static bool PathsEqual(string? a, string? b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        try
        {
            return string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>
    /// Splits a command line using the CommandLineToArgvW rules (quotes, 2n / 2n+1 backslashes).
    /// </summary>
    public static List<string> Tokenize(string commandLine)
    {
        var result = new List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false, hasToken = false;
        int i = 0;
        while (i < commandLine.Length)
        {
            char c = commandLine[i];
            if (c == '\\')
            {
                int n = 0;
                while (i < commandLine.Length && commandLine[i] == '\\') { n++; i++; }
                if (i < commandLine.Length && commandLine[i] == '"')
                {
                    sb.Append('\\', n / 2);
                    if (n % 2 == 1) { sb.Append('"'); i++; }
                }
                else
                {
                    sb.Append('\\', n);
                }
                hasToken = true;
                continue;
            }
            if (c == '"')
            {
                // "" inside quotes is a literal quote (MSVC rule).
                if (inQuotes && i + 1 < commandLine.Length && commandLine[i + 1] == '"') { sb.Append('"'); i += 2; continue; }
                inQuotes = !inQuotes;
                hasToken = true;
                i++;
                continue;
            }
            if (!inQuotes && (c == ' ' || c == '\t'))
            {
                if (hasToken) { result.Add(sb.ToString()); sb.Clear(); hasToken = false; }
                i++;
                continue;
            }
            sb.Append(c);
            hasToken = true;
            i++;
        }
        if (hasToken) result.Add(sb.ToString());
        return result;
    }
}
