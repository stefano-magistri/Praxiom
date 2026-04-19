using DottoressaIorio.App.Startup;
using DottoressaIorio.Services.Licensing;
using Microsoft.Extensions.Configuration;

namespace DottoressaIorio.App.Tests;

public class LaunchOptionsTests
{
    [Fact]
    public void Parse_RecognizesFlagsCaseInsensitively_AndConfiguresDerivedDemoFlag()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["UseDemoDatabase"] = "false" })
            .Build();

        var result = LaunchOptions.Parse(
            ["--ONLINE-BACKUP", "--Demo", "--TRIAL-active"],
            configuration);

        Assert.True(result.RunOnlineBackup);
        Assert.True(result.UseDemoDatabaseFromCommandLine);
        Assert.True(result.UseTrialActive);
        Assert.False(result.UseTrialExpired);
        Assert.True(result.UseDemoDatabase);
    }

    [Fact]
    public void Parse_UsesConfiguredDemoDatabase_WhenDemoFlagIsNotPresent()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["UseDemoDatabase"] = "true" })
            .Build();

        var result = LaunchOptions.Parse([], configuration);

        Assert.False(result.UseDemoDatabaseFromCommandLine);
        Assert.True(result.UseDemoDatabase);
    }

    [Theory]
    [InlineData(true, true, false, "Cannot use --online-backup and --demo together.")]
    [InlineData(false, false, true, "Cannot use --trial-active and --trial-expired together.")]
    public void Validate_WhenFlagsConflict_ThrowsExpectedMessage(
        bool runOnlineBackup,
        bool useDemoDatabase,
        bool conflictingTrialFlags,
        string expectedMessage)
    {
        var options = new LaunchOptions(
            runOnlineBackup,
            UseDemoDatabaseFromCommandLine: useDemoDatabase,
            UseTrialActive: conflictingTrialFlags,
            UseTrialExpired: conflictingTrialFlags,
            UseDemoDatabase: useDemoDatabase);

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Validate_WhenOnlineBackupAndTrialOverrideAreCombined_ThrowsExpectedMessage(
        bool useTrialActive,
        bool useTrialExpired)
    {
        var options = new LaunchOptions(
            RunOnlineBackup: true,
            UseDemoDatabaseFromCommandLine: false,
            UseTrialActive: useTrialActive,
            UseTrialExpired: useTrialExpired,
            UseDemoDatabase: false);

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);

        Assert.Equal(
            "Cannot use --online-backup with --trial-active or --trial-expired because backup mode does not initialize licensing.",
            exception.Message);
    }

    [Theory]
    [InlineData(false, false, LicenseLaunchMode.None, null)]
    [InlineData(true, false, LicenseLaunchMode.TrialActive, "TrialActive")]
    [InlineData(false, true, LicenseLaunchMode.TrialExpired, "TrialExpired")]
    public void CreateLicenseLaunchOverride_MatchesTrialFlags(
        bool useTrialActive,
        bool useTrialExpired,
        LicenseLaunchMode expectedMode,
        string? expectedLogMode)
    {
        var options = new LaunchOptions(
            RunOnlineBackup: false,
            UseDemoDatabaseFromCommandLine: false,
            UseTrialActive: useTrialActive,
            UseTrialExpired: useTrialExpired,
            UseDemoDatabase: false);

        var overrideOptions = options.CreateLicenseLaunchOverride();

        Assert.Equal(expectedMode, overrideOptions.Mode);
        Assert.Equal(expectedLogMode, options.GetTrialOverrideLogMode());
    }
}
