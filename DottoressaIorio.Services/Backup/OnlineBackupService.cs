using Microsoft.Extensions.Logging;

namespace DottoressaIorio.Services.Backup;

public sealed class OnlineBackupService(
    ISqliteBackupConnectionFactory connectionFactory,
    IOnlineBackupStateStore stateStore,
    OnlineBackupOptions options,
    ILogger<OnlineBackupService> logger)
    : IOnlineBackupService
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<OnlineBackupResult> TriggerBackupAsync(
        string reason,
        TimeSpan? minimumSuccessAge = null,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var state = await stateStore.ReadAsync(cancellationToken);
            var nowUtc = DateTimeOffset.UtcNow;

            if (minimumSuccessAge.HasValue &&
                !OnlineBackupPolicy.MeetsMinimumAge(nowUtc, state.LastSuccessfulBackupUtc, minimumSuccessAge.Value))
            {
                var skipped = OnlineBackupResult.Skipped(
                    $"Skipped backup for '{reason}': last success is newer than required age {minimumSuccessAge.Value}.");
                logger.LogInformation("{Reason}", skipped.Reason);
                return skipped;
            }

            if (OnlineBackupPolicy.IsDebounced(nowUtc, state.LastSuccessfulBackupUtc, options.DebounceWindow))
            {
                var skipped = OnlineBackupResult.Skipped(
                    $"Skipped backup for '{reason}': debounced for {options.DebounceWindow}.");
                logger.LogInformation("{Reason}", skipped.Reason);
                return skipped;
            }

            var backupPath = BuildBackupPath(nowUtc);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                Directory.CreateDirectory(options.BackupDirectoryPath);
                Directory.CreateDirectory(options.RuntimeDirectoryPath);

                using var sourceConnection = connectionFactory.CreateSourceConnection();
                using var destinationConnection = connectionFactory.CreateDestinationConnection(backupPath);
                sourceConnection.BackupDatabase(destinationConnection);

                ApplyRetention();
                MirrorBackupToOneDrive(backupPath);

                await stateStore.WriteSuccessAsync(nowUtc, cancellationToken);
                stopwatch.Stop();

                logger.LogInformation(
                    "Online backup succeeded. Reason={Reason}, DurationMs={DurationMs}, BackupPath={BackupPath}",
                    reason,
                    stopwatch.ElapsedMilliseconds,
                    backupPath);

                return new OnlineBackupResult(
                    OnlineBackupOutcome.Succeeded,
                    $"Backup completed for '{reason}'.",
                    backupPath,
                    stopwatch.Elapsed,
                    false);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var consecutiveFailures = state.ConsecutiveFailures + 1;
                await stateStore.WriteFailureAsync(consecutiveFailures, cancellationToken);

                var stateAfterFailure =
                    new OnlineBackupState(state.LastSuccessfulBackupUtc, consecutiveFailures);
                var fallbackRecommended = OnlineBackupPolicy.ShouldRecommendColdCopyFallback(
                    nowUtc,
                    stateAfterFailure,
                    options.FallbackConsecutiveFailuresThreshold,
                    options.FallbackNoSuccessWindow);

                logger.LogError(
                    ex,
                    "Online backup failed. Reason={Reason}, DurationMs={DurationMs}, ConsecutiveFailures={ConsecutiveFailures}, FallbackRecommended={FallbackRecommended}",
                    reason,
                    stopwatch.ElapsedMilliseconds,
                    consecutiveFailures,
                    fallbackRecommended);

                if (fallbackRecommended)
                {
                    logger.LogCritical(
                        "Cold-copy fallback should be scheduled: online backup failed repeatedly or no successful backup in {NoSuccessDays} days.",
                        options.FallbackNoSuccessWindow.TotalDays);
                }

                return new OnlineBackupResult(
                    OnlineBackupOutcome.Failed,
                    $"Backup failed for '{reason}': {ex.Message}",
                    null,
                    stopwatch.Elapsed,
                    fallbackRecommended);
            }
        }
        finally
        {
            _gate.Release();
            var totalDuration = DateTimeOffset.UtcNow - startedAt;
            logger.LogDebug("Backup request handling completed in {DurationMs} ms.", totalDuration.TotalMilliseconds);
        }
    }

    private string BuildBackupPath(DateTimeOffset nowUtc)
    {
        var fileName = $"{options.BackupFilePrefix}{nowUtc:yyyyMMddHHmmss}.db";
        return Path.Combine(options.BackupDirectoryPath, fileName);
    }

    private void ApplyRetention()
    {
        ApplyRetentionForDirectory(options.BackupDirectoryPath);

        var oneDriveBackupRoot = ResolveOneDriveBackupRoot();
        if (!string.IsNullOrWhiteSpace(oneDriveBackupRoot))
        {
            ApplyRetentionForDirectory(oneDriveBackupRoot);
        }
    }

    private void ApplyRetentionForDirectory(string backupDirectoryPath)
    {
        if (options.RetentionCount <= 0 || !Directory.Exists(backupDirectoryPath))
        {
            return;
        }

        var files = new DirectoryInfo(backupDirectoryPath)
            .GetFiles($"{options.BackupFilePrefix}*.db")
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Skip(options.RetentionCount);

        foreach (var file in files)
        {
            try
            {
                file.Delete();
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Could not delete old backup file {BackupPath}.", file.FullName);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(ex, "Could not delete old backup file {BackupPath}.", file.FullName);
            }
        }
    }

    private void MirrorBackupToOneDrive(string localBackupPath)
    {
        if (!options.EnableOneDriveMirror)
        {
            return;
        }

        var oneDriveBackupRoot = ResolveOneDriveBackupRoot();
        if (string.IsNullOrWhiteSpace(oneDriveBackupRoot))
        {
            logger.LogWarning("OneDrive path not available. Local backup still created at {BackupPath}.", localBackupPath);
            return;
        }

        var backupFileName = Path.GetFileName(localBackupPath);
        var oneDriveBackupPath = Path.Combine(oneDriveBackupRoot, backupFileName);

        try
        {
            Directory.CreateDirectory(oneDriveBackupRoot);
            File.Copy(localBackupPath, oneDriveBackupPath, overwrite: true);
            logger.LogInformation("OneDrive mirror backup copied to {BackupPath}.", oneDriveBackupPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(ex, "Could not mirror backup to OneDrive path {BackupPath}.", oneDriveBackupPath);
        }
    }

    private string? ResolveOneDriveBackupRoot()
    {
        var oneDriveCandidates = new List<string?>();

        var preferredConfiguredCandidate = BuildPreferredUserOneDriveCandidate(options.PreferredWindowsUsername);
        if (!string.IsNullOrWhiteSpace(preferredConfiguredCandidate))
        {
            oneDriveCandidates.Add(preferredConfiguredCandidate);
        }

        oneDriveCandidates.AddRange(new[]
        {
            Environment.GetEnvironmentVariable("OneDriveConsumer"),
            Environment.GetEnvironmentVariable("OneDriveCommercial"),
            Environment.GetEnvironmentVariable("OneDrive"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive")
        });

        oneDriveCandidates.AddRange(GetUserProfileOneDriveCandidates());

        var existingCandidates = oneDriveCandidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Directory.Exists)
            .ToList();

        if (existingCandidates.Count == 0)
        {
            return null;
        }

        // Prefer candidate already containing the application backup folder.
        var preferredWithBackupFolder = existingCandidates.FirstOrDefault(candidate =>
            Directory.Exists(Path.Combine(candidate, options.OneDriveBackupRelativePath)));

        if (!string.IsNullOrWhiteSpace(preferredWithBackupFolder))
        {
            return Path.Combine(preferredWithBackupFolder, options.OneDriveBackupRelativePath);
        }

        return Path.Combine(existingCandidates[0], options.OneDriveBackupRelativePath);
    }

    private static IEnumerable<string> GetUserProfileOneDriveCandidates()
    {
        var usersRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "..");
        string normalizedUsersRoot;
        try
        {
            normalizedUsersRoot = Path.GetFullPath(usersRoot);
        }
        catch
        {
            yield break;
        }

        if (!Directory.Exists(normalizedUsersRoot))
        {
            yield break;
        }

        var ignoredProfileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "All Users",
            "Default",
            "Default User",
            "Public"
        };

        IEnumerable<string> profileDirectories;
        try
        {
            profileDirectories = Directory.EnumerateDirectories(normalizedUsersRoot);
        }
        catch
        {
            yield break;
        }

        foreach (var profileDirectory in profileDirectories)
        {
            if (string.IsNullOrWhiteSpace(profileDirectory))
            {
                continue;
            }

            var profileName = Path.GetFileName(profileDirectory);
            if (string.IsNullOrWhiteSpace(profileName) || ignoredProfileNames.Contains(profileName))
            {
                continue;
            }

            yield return Path.Combine(profileDirectory, "OneDrive");
        }
    }

    private static string? BuildPreferredUserOneDriveCandidate(string? preferredWindowsUsername)
    {
        if (string.IsNullOrWhiteSpace(preferredWindowsUsername))
        {
            return null;
        }

        var username = preferredWindowsUsername.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return Path.Combine("C:\\Users", username, "OneDrive");
    }
}
