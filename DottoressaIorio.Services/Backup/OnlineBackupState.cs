namespace DottoressaIorio.Services.Backup;

public sealed record OnlineBackupState(DateTimeOffset? LastSuccessfulBackupUtc, int ConsecutiveFailures);
