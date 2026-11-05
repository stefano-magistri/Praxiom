namespace DottoressaIorio.Services.Backup;

public sealed class OnlineBackupOptions
{
    public string AppRootPath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Praxiom");

    public string BackupDirectoryName { get; init; } = "_db_backup";
    public string RuntimeDirectoryName { get; init; } = "_runtime";
    public string LastBackupFileName { get; init; } = "last-backup.txt";
    public string ConsecutiveFailureFileName { get; init; } = "backup-consecutive-failures.txt";
    public string BackupFilePrefix { get; init; } = "app_backup_";
    public string OneDriveBackupRelativePath { get; init; } = Path.Combine("Praxiom", "_db_backup");
    public bool EnableOneDriveMirror { get; init; } = true;
    public string? PreferredWindowsUsername { get; set; }
    public int RetentionCount { get; init; } = 30;
    public TimeSpan DebounceWindow { get; init; } = TimeSpan.FromMinutes(15);
    public TimeSpan StartupMinimumAge { get; init; } = TimeSpan.FromDays(1);
    public TimeSpan ShutdownMinimumAge { get; init; } = TimeSpan.FromDays(1);
    public TimeSpan FallbackNoSuccessWindow { get; init; } = TimeSpan.FromDays(7);
    public int FallbackConsecutiveFailuresThreshold { get; init; } = 3;

    public string BackupDirectoryPath => Path.Combine(AppRootPath, BackupDirectoryName);
    public string RuntimeDirectoryPath => Path.Combine(AppRootPath, RuntimeDirectoryName);
    public string LastBackupFilePath => Path.Combine(RuntimeDirectoryPath, LastBackupFileName);
    public string ConsecutiveFailureFilePath => Path.Combine(RuntimeDirectoryPath, ConsecutiveFailureFileName);
}
