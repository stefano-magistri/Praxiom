using DottoressaIorio.Services.Licensing;

namespace DottoressaIorio.App.Startup;

internal sealed record LaunchOptions(
    bool RunOnlineBackup,
    bool UseDemoDatabaseFromCommandLine,
    bool UseTrialActive,
    bool UseTrialExpired,
    bool UseDemoDatabase)
{
    public static LaunchOptions Parse(string[] args, IConfiguration configuration)
    {
        return new LaunchOptions(
            RunOnlineBackup: HasArgument(args, "--online-backup"),
            UseDemoDatabaseFromCommandLine: HasArgument(args, "--demo"),
            UseTrialActive: HasArgument(args, "--trial-active"),
            UseTrialExpired: HasArgument(args, "--trial-expired"),
            UseDemoDatabase: HasArgument(args, "--demo") || configuration.GetValue<bool>("UseDemoDatabase"));
    }

    public LicenseLaunchOverride CreateLicenseLaunchOverride()
    {
        return new LicenseLaunchOverride
        {
            Mode = UseTrialExpired
                ? LicenseLaunchMode.TrialExpired
                : UseTrialActive
                    ? LicenseLaunchMode.TrialActive
                    : LicenseLaunchMode.None
        };
    }

    public void Validate()
    {
        if (RunOnlineBackup && UseDemoDatabase)
            throw new InvalidOperationException("Cannot use --online-backup and --demo together.");

        if (RunOnlineBackup && (UseTrialActive || UseTrialExpired))
            throw new InvalidOperationException(
                "Cannot use --online-backup with --trial-active or --trial-expired because backup mode does not initialize licensing.");

        if (UseTrialActive && UseTrialExpired)
            throw new InvalidOperationException("Cannot use --trial-active and --trial-expired together.");
    }

    public string? GetTrialOverrideLogMode()
    {
        if (UseTrialExpired)
        {
            return "TrialExpired";
        }

        if (UseTrialActive)
        {
            return "TrialActive";
        }

        return null;
    }

    private static bool HasArgument(IEnumerable<string> args, string expectedArgument)
    {
        return args.Any(argument => argument.Equals(expectedArgument, StringComparison.OrdinalIgnoreCase));
    }
}
