namespace DottoressaIorio.Services.Backup;

public static class OnlineBackupPolicy
{
    public static bool IsDebounced(DateTimeOffset nowUtc, DateTimeOffset? lastSuccessfulBackupUtc, TimeSpan debounceWindow)
    {
        if (lastSuccessfulBackupUtc is null)
        {
            return false;
        }

        return nowUtc - lastSuccessfulBackupUtc.Value < debounceWindow;
    }

    public static bool MeetsMinimumAge(
        DateTimeOffset nowUtc,
        DateTimeOffset? lastSuccessfulBackupUtc,
        TimeSpan minimumAge)
    {
        if (lastSuccessfulBackupUtc is null)
        {
            return true;
        }

        return nowUtc - lastSuccessfulBackupUtc.Value >= minimumAge;
    }

    public static bool ShouldRecommendColdCopyFallback(
        DateTimeOffset nowUtc,
        OnlineBackupState state,
        int consecutiveFailuresThreshold,
        TimeSpan noSuccessWindow)
    {
        if (state.ConsecutiveFailures >= consecutiveFailuresThreshold)
        {
            return true;
        }

        if (state.LastSuccessfulBackupUtc is null)
        {
            return false;
        }

        return nowUtc - state.LastSuccessfulBackupUtc.Value >= noSuccessWindow;
    }
}
