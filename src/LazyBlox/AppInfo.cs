namespace LazyBlox;

/// <summary>Constant identifiers. Every registry key this app ever writes is derived from these names.</summary>
public static class AppInfo
{
    public const string AppName = "LazyBlox";
    public const string ExeName = "LazyBlox.exe";
    public const string ProgId = "LazyBloxURL";
    public const string Version = "1.0.1";

    /// <summary>Env override for the data directory (used by integration tests).</summary>
    public const string HomeEnvVar = "LAZYBLOX_HOME";
    /// <summary>Env flag: never show message boxes (errors go to stderr / exit code). Used by tests.</summary>
    public const string SilentEnvVar = "LAZYBLOX_SILENT";
    /// <summary>Env marker put on forwarded browser processes to detect re-entry loops.</summary>
    public const string ForwardGuardEnvVar = "LAZYBLOX_FORWARDED";

    public static string DataDirectory
    {
        get
        {
            string? overrideDir = Environment.GetEnvironmentVariable(HomeEnvVar);
            return !string.IsNullOrWhiteSpace(overrideDir)
                ? overrideDir
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
        }
    }

    public static string ConfigPath => Path.Combine(DataDirectory, "config.ini");
    public static string TimingFlagPath => Path.Combine(DataDirectory, "timing.enabled");
    public static string TimingLogPath => Path.Combine(DataDirectory, "timing.log");

    /// <summary>Per-user install location (no admin rights needed).</summary>
    public static string InstallDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", AppName);

    public static string InstalledExePath => Path.Combine(InstallDirectory, ExeName);

    public static string? CurrentExePath => Environment.ProcessPath;

    public static bool Silent => Environment.GetEnvironmentVariable(SilentEnvVar) == "1";
}
