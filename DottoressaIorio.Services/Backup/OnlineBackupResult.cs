namespace DottoressaIorio.Services.Backup;

public sealed record OnlineBackupResult(
    OnlineBackupOutcome Outcome,
    string Reason,
    string? BackupPath,
    TimeSpan Duration,
    bool ColdCopyFallbackRecommended)
{
    public static OnlineBackupResult Skipped(string reason)
    {
        return new OnlineBackupResult(OnlineBackupOutcome.Skipped, reason, null, TimeSpan.Zero, false);
    }
}
