using System.Globalization;

namespace DottoressaIorio.Services.Backup;

public sealed class FileOnlineBackupStateStore(OnlineBackupOptions options) : IOnlineBackupStateStore
{
    public Task<OnlineBackupState> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lastSuccessfulBackupUtc = ReadTimestamp(options.LastBackupFilePath);
        var consecutiveFailures = ReadInt(options.ConsecutiveFailureFilePath);
        return Task.FromResult(new OnlineBackupState(lastSuccessfulBackupUtc, consecutiveFailures));
    }

    public Task WriteSuccessAsync(DateTimeOffset successTimestampUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureRuntimeDirectoryExists();
        File.WriteAllText(options.LastBackupFilePath, successTimestampUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        File.WriteAllText(options.ConsecutiveFailureFilePath, "0");
        return Task.CompletedTask;
    }

    public Task WriteFailureAsync(int consecutiveFailures, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureRuntimeDirectoryExists();
        File.WriteAllText(options.ConsecutiveFailureFilePath, Math.Max(0, consecutiveFailures).ToString(CultureInfo.InvariantCulture));
        return Task.CompletedTask;
    }

    private void EnsureRuntimeDirectoryExists()
    {
        Directory.CreateDirectory(options.RuntimeDirectoryPath);
    }

    private static DateTimeOffset? ReadTimestamp(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var raw = File.ReadAllText(path).Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value)
            ? value.ToUniversalTime()
            : null;
    }

    private static int ReadInt(string path)
    {
        if (!File.Exists(path))
        {
            return 0;
        }

        var raw = File.ReadAllText(path).Trim();
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }
}
