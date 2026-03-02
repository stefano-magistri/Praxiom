using DottoressaIorio.Services.Backup;

namespace DottoressaIorio.Services.Tests;

public class OnlineBackupPolicyTests
{
    [Fact]
    public void IsDebounced_WhenLastBackupIsWithinWindow_ReturnsTrue()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var lastBackup = nowUtc.AddMinutes(-5);

        var isDebounced = OnlineBackupPolicy.IsDebounced(nowUtc, lastBackup, TimeSpan.FromMinutes(15));

        Assert.True(isDebounced);
    }

    [Fact]
    public void MeetsMinimumAge_WhenLastBackupIsOlderThanThreshold_ReturnsTrue()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var lastBackup = nowUtc.AddHours(-30);

        var meetsMinimumAge = OnlineBackupPolicy.MeetsMinimumAge(nowUtc, lastBackup, TimeSpan.FromHours(24));

        Assert.True(meetsMinimumAge);
    }

    [Fact]
    public void ShouldRecommendColdCopyFallback_WhenConsecutiveFailuresReachThreshold_ReturnsTrue()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var state = new OnlineBackupState(nowUtc.AddHours(-1), 3);

        var recommend = OnlineBackupPolicy.ShouldRecommendColdCopyFallback(
            nowUtc,
            state,
            consecutiveFailuresThreshold: 3,
            noSuccessWindow: TimeSpan.FromDays(7));

        Assert.True(recommend);
    }

    [Fact]
    public void ShouldRecommendColdCopyFallback_WhenNoSuccessForWindow_ReturnsTrue()
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var state = new OnlineBackupState(nowUtc.AddDays(-8), 0);

        var recommend = OnlineBackupPolicy.ShouldRecommendColdCopyFallback(
            nowUtc,
            state,
            consecutiveFailuresThreshold: 3,
            noSuccessWindow: TimeSpan.FromDays(7));

        Assert.True(recommend);
    }
}
