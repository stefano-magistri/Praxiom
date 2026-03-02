namespace DottoressaIorio.Services.Backup;

public interface IOnlineBackupStateStore
{
    Task<OnlineBackupState> ReadAsync(CancellationToken cancellationToken = default);
    Task WriteSuccessAsync(DateTimeOffset successTimestampUtc, CancellationToken cancellationToken = default);
    Task WriteFailureAsync(int consecutiveFailures, CancellationToken cancellationToken = default);
}
