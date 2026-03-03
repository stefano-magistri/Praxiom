namespace DottoressaIorio.Services.Backup;

public interface IOnlineBackupService
{
    Task<OnlineBackupResult> TriggerBackupAsync(
        string reason,
        TimeSpan? minimumSuccessAge = null,
        CancellationToken cancellationToken = default);
}
