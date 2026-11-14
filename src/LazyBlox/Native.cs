using System.Runtime.InteropServices;

namespace LazyBlox;

/// <summary>Win32 interop. Source-generated (LibraryImport) so it is trim/AOT safe and avoids runtime marshalling stubs.</summary>
internal static unsafe partial class Native
{
    // ---------------- shell32 ----------------

    [StructLayout(LayoutKind.Sequential)]
    public struct SHELLEXECUTEINFOW
    {
        public int cbSize;
        public uint fMask;
        public nint hwnd;
        public char* lpVerb;
        public char* lpFile;
        public char* lpParameters;
        public char* lpDirectory;
        public int nShow;
        public nint hInstApp;
        public nint lpIDList;
        public char* lpClass;
        public nint hkeyClass;
        public uint dwHotKey;
        public nint hIconOrMonitor;
        public nint hProcess;
    }

    public const uint SEE_MASK_NOASYNC = 0x00000100;
    public const uint SEE_MASK_FLAG_NO_UI = 0x00000400;
    public const uint SEE_MASK_NO_CONSOLE = 0x00008000;
    public const int SW_SHOWNORMAL = 1;

    [LibraryImport("shell32.dll", EntryPoint = "ShellExecuteExW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShellExecuteExW(SHELLEXECUTEINFOW* info);

    public const int SHCNE_ASSOCCHANGED = 0x08000000;
    public const uint SHCNF_IDLIST = 0x0000;

    [LibraryImport("shell32.dll")]
    public static partial void SHChangeNotify(int wEventId, uint uFlags, nint dwItem1, nint dwItem2);

    // ---------------- user32 ----------------

    public const uint MB_OK = 0x0, MB_OKCANCEL = 0x1, MB_ICONERROR = 0x10, MB_ICONWARNING = 0x30, MB_ICONINFORMATION = 0x40,
        MB_SETFOREGROUND = 0x10000, MB_TOPMOST = 0x40000;
    public const int IDOK = 1, IDCANCEL = 2, IDYES = 6, IDNO = 7, IDCLOSE = 8;

    [LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int MessageBoxW(nint hWnd, string text, string caption, uint type);

    public const int ASFW_ANY = -1;

    /// <summary>Lets the process we launch (browser / Roblox) take the foreground, as if the user launched it.</summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AllowSetForegroundWindow(int processId);

    // ---------------- advapi32 ----------------

    public static readonly nint HKEY_CURRENT_USER = unchecked((int)0x80000001);
    public static readonly nint HKEY_LOCAL_MACHINE = unchecked((int)0x80000002);
    public const uint RRF_RT_ANY = 0x0000ffff;

    [LibraryImport("advapi32.dll", EntryPoint = "RegGetValueW", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int RegGetValueW(nint hkey, string subKey, string? value, uint flags, uint* type, void* data, uint* cbData);

    // ---------------- kernel32: processes ----------------

    [StructLayout(LayoutKind.Sequential)]
    public struct STARTUPINFOW
    {
        public int cb;
        public char* lpReserved, lpDesktop, lpTitle;
        public uint dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public ushort wShowWindow, cbReserved2;
        public nint lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public nint hProcess, hThread;
        public uint dwProcessId, dwThreadId;
    }

    [LibraryImport("kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CreateProcessW(char* applicationName, char* commandLine, nint processAttributes, nint threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles, uint creationFlags, nint environment, char* currentDirectory,
        STARTUPINFOW* startupInfo, PROCESS_INFORMATION* processInformation);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint handle);

    // ---------------- kernel32 ----------------

    public const int ATTACH_PARENT_PROCESS = -1;
    public const int STD_OUTPUT_HANDLE = -11;

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachConsole(int processId);

    [LibraryImport("kernel32.dll")]
    public static partial nint GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll")]
    public static partial nint GetCurrentProcess();

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetProcessTimes(nint hProcess, long* creation, long* exit, long* kernel, long* user);

    [LibraryImport("kernel32.dll")]
    public static partial void GetSystemTimePreciseAsFileTime(long* fileTime);

    /// <summary>Milliseconds since the OS created this process (includes runtime startup). -1 if unavailable.</summary>
    public static double MillisecondsSinceProcessCreation()
    {
        long creation, exit, kernel, user, now;
        if (!GetProcessTimes(GetCurrentProcess(), &creation, &exit, &kernel, &user)) return -1;
        GetSystemTimePreciseAsFileTime(&now);
        return (now - creation) / 10_000.0;
    }
}
