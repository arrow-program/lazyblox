using LazyBlox.Localization;
using System.Diagnostics;

namespace LazyBlox;

public readonly record struct LaunchResult(bool Success, string? Error)
{
    public static readonly LaunchResult Ok = new(true, null);
    public static LaunchResult Fail(string error) => new(false, error);
}

/// <summary>Side-effecting operations, behind an interface so the routing logic can be tested with mocks.</summary>
public interface ISystemShell
{
    /// <summary>True when a URL protocol (e.g. "roblox") has a handler registered for this user.</summary>
    bool IsProtocolRegistered(string scheme);
    /// <summary>Hands a URI to Windows (ShellExecuteEx). Used for roblox: deep links and ms-settings: only.</summary>
    LaunchResult OpenUri(string uri);
    /// <summary>Starts an executable directly with an argv list (CreateProcess, no shell).</summary>
    LaunchResult StartProcess(ProcessInvocation invocation, IReadOnlyDictionary<string, string>? extraEnvironment = null);
    void ShowError(string message);
}

public sealed class WindowsShell : ISystemShell
{
    public static readonly WindowsShell Instance = new();

    public unsafe bool IsProtocolRegistered(string scheme)
    {
        // Equivalent to HKCR\<scheme> having a "URL Protocol" value (HKCU overrides HKLM).
        // Direct RegGetValueW: avoids loading the managed registry stack on the hot path.
        string key = $@"Software\Classes\{scheme}";
        return Native.RegGetValueW(Native.HKEY_CURRENT_USER, key, "URL Protocol", Native.RRF_RT_ANY, null, null, null) == 0
               || Native.RegGetValueW(Native.HKEY_LOCAL_MACHINE, key, "URL Protocol", Native.RRF_RT_ANY, null, null, null) == 0;
    }

    public unsafe LaunchResult OpenUri(string uri)
    {
        fixed (char* file = uri)
        fixed (char* verb = "open")
        {
            var info = new Native.SHELLEXECUTEINFOW
            {
                cbSize = sizeof(Native.SHELLEXECUTEINFOW),
                // NOASYNC: we exit right after; FLAG_NO_UI: report errors to us instead of a shell dialog.
                fMask = Native.SEE_MASK_NOASYNC | Native.SEE_MASK_FLAG_NO_UI | Native.SEE_MASK_NO_CONSOLE,
                lpVerb = verb,
                lpFile = file,
                nShow = Native.SW_SHOWNORMAL,
            };
            Native.AllowSetForegroundWindow(Native.ASFW_ANY);
            if (Native.ShellExecuteExW(&info)) return LaunchResult.Ok;
            int err = System.Runtime.InteropServices.Marshal.GetLastPInvokeError();
            return LaunchResult.Fail(L.F(L.S.LaunchShellExecuteFailed, err));
        }
    }

    /// <summary>
    /// CreateProcessW directly (no cmd.exe, no association lookup -> no re-entry into the https handler).
    /// Cheaper than System.Diagnostics.Process at startup. Handles are not inherited, so the browser
    /// never holds our stdout/stderr pipes.
    /// </summary>
    public unsafe LaunchResult StartProcess(ProcessInvocation invocation, IReadOnlyDictionary<string, string>? extraEnvironment = null)
    {
        if (!File.Exists(invocation.FileName))
            return LaunchResult.Fail(L.F(L.S.LaunchExeNotFound, invocation.FileName));

        // The child inherits our environment block; set the extra variables on ourselves (we exit right after).
        if (extraEnvironment is not null)
            foreach (KeyValuePair<string, string> kv in extraEnvironment) Environment.SetEnvironmentVariable(kv.Key, kv.Value);

        string commandLine = CommandLineBuilder.Build(invocation.FileName, invocation.Arguments) + "\0";
        string dir = Path.GetDirectoryName(invocation.FileName) ?? "";
        var si = new Native.STARTUPINFOW { cb = sizeof(Native.STARTUPINFOW) };
        Native.PROCESS_INFORMATION pi;
        char[] cmdBuffer = commandLine.ToCharArray(); // CreateProcessW may modify the buffer
        Native.AllowSetForegroundWindow(Native.ASFW_ANY);
        fixed (char* app = invocation.FileName)
        fixed (char* cmd = cmdBuffer)
        fixed (char* cwd = dir)
        {
            if (!Native.CreateProcessW(app, cmd, 0, 0, false, 0, 0, dir.Length == 0 ? null : cwd, &si, &pi))
            {
                int err = System.Runtime.InteropServices.Marshal.GetLastPInvokeError();
                return LaunchResult.Fail(L.F(L.S.LaunchCreateProcessFailed, err, invocation.FileName));
            }
        }
        Native.CloseHandle(pi.hThread);
        Native.CloseHandle(pi.hProcess);
        return LaunchResult.Ok;
    }

    public void ShowError(string message)
    {
        if (AppInfo.Silent)
        {
            Console.OutputEncoding = new System.Text.UTF8Encoding(false); // also applies to stderr
            Console.Error.WriteLine(message);
            return;
        }
        Ui.Gui.Message(L.S.ErrWindowTitle, message, Ui.MessageKind.Error, copyText: message);
    }
}

/// <summary>Records instead of executing. Used by "dry-run" and by tests.</summary>
public sealed class RecordingShell(ISystemShell? inner = null, bool robloxRegistered = true, Func<ProcessInvocation, LaunchResult>? onStart = null,
    Func<string, LaunchResult>? onOpen = null) : ISystemShell
{
    public List<string> OpenedUris { get; } = [];
    public List<ProcessInvocation> StartedProcesses { get; } = [];
    public List<string> Errors { get; } = [];
    public List<IReadOnlyDictionary<string, string>?> Environments { get; } = [];

    public bool IsProtocolRegistered(string scheme) => inner?.IsProtocolRegistered(scheme) ?? robloxRegistered;

    public LaunchResult OpenUri(string uri)
    {
        OpenedUris.Add(uri);
        return onOpen?.Invoke(uri) ?? LaunchResult.Ok;
    }

    public LaunchResult StartProcess(ProcessInvocation invocation, IReadOnlyDictionary<string, string>? extraEnvironment = null)
    {
        StartedProcesses.Add(invocation);
        Environments.Add(extraEnvironment);
        return onStart?.Invoke(invocation) ?? LaunchResult.Ok;
    }

    public void ShowError(string message) => Errors.Add(message);
}
