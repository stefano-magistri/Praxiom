using DottoressaIorio.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DottoressaIorio.Services.Licensing;

/// <summary>
/// Defines the supported startup overrides for local license and trial behavior.
/// </summary>
public enum LicenseLaunchMode
{
    None = 0,
    TrialActive,
    TrialExpired
}

/// <summary>
/// Overrides the runtime license mode for startup scenarios such as demos and tests.
/// </summary>
public sealed class LicenseLaunchOverride
{
    /// <summary>
    /// Gets the override mode to apply during initialization.
    /// </summary>
    public LicenseLaunchMode Mode { get; init; } = LicenseLaunchMode.None;
}

/// <summary>
/// Describes the effective license or trial state after initialization.
/// </summary>
public sealed class LicenseStatus
{
    /// <summary>
    /// Gets a value indicating whether a valid installed license was found.
    /// </summary>
    public bool IsLicensed { get; init; }

    /// <summary>
    /// Gets a value indicating whether the application is running in trial mode.
    /// </summary>
    public bool IsTrial { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current trial period has expired.
    /// </summary>
    public bool IsTrialExpired { get; init; }

    /// <summary>
    /// Gets the UTC expiration date for the current trial when applicable.
    /// </summary>
    public DateTime? TrialExpiresUtc { get; init; }

    /// <summary>
    /// Gets the remaining whole trial days, clamped to zero.
    /// </summary>
    public int? RemainingTrialDays { get; init; }
}

/// <summary>
/// Provides the current runtime license state and creation limits.
/// </summary>
public interface ILicenseService
{
    bool IsLicensed { get; }
    bool IsTrial { get; }
    bool IsTrialExpired { get; }
    DateTime? TrialExpiresUtc { get; }
    int? RemainingTrialDays { get; }
    string StatusLabel { get; }
    string TrialExpiredMessage { get; }
    string TrialExpiredCreationMessage { get; }
    string TrialLimitsSummary { get; }
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<string?> GetPatientCreationBlockMessageAsync(CancellationToken cancellationToken = default);
    Task<string?> GetTherapyTemplateCreationBlockMessageAsync(CancellationToken cancellationToken = default);
    Task<string?> GetTherapyCreationBlockMessageAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Computes the effective license state from development rules, startup overrides, the installed license file, and trial data.
/// </summary>
public sealed class LicenseService(
    ILocalLicenseService localLicenseService,
    IClinicSettingsRepository clinicSettingsRepository,
    PatientRepository patientRepository,
    TherapyTemplateRepository therapyTemplateRepository,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    LicenseLaunchOverride launchOverride,
    TimeProvider timeProvider) : ILicenseService
{
    public const int TrialDurationDays = 7;
    public const int TrialPatientLimit = 10;
    public const int TrialTherapyTemplateLimit = 10;
    public const string TrialExpiredMessage =
        "Periodo di prova scaduto. Inserire una licenza per continuare ad utilizzare il software.";
    public const string TrialExpiredCreationMessage =
        "Periodo di prova scaduto. Inserire una licenza per sbloccare la creazione.";
    public const string TrialPatientLimitMessage = "Limite versione di prova raggiunto: massimo 10 pazienti.";
    public const string TrialTherapyTemplateLimitMessage =
        "Limite versione di prova raggiunto: massimo 10 terapie precompilate.";
    public const string TrialLimitsSummaryText = "Massimo 10 pazienti, massimo 10 terapie precompilate";

    private LicenseStatus? current;

    public bool IsLicensed => current?.IsLicensed ?? false;
    public bool IsTrial => current?.IsTrial ?? false;
    public bool IsTrialExpired => current?.IsTrialExpired ?? false;
    public DateTime? TrialExpiresUtc => current?.TrialExpiresUtc;
    public int? RemainingTrialDays => current?.RemainingTrialDays;
    public string StatusLabel => IsLicensed ? "Licenziato" : IsTrialExpired ? "Scaduto" : IsTrial ? "Versione di prova" : "Non disponibile";
    string ILicenseService.TrialExpiredMessage => TrialExpiredMessage;
    string ILicenseService.TrialExpiredCreationMessage => TrialExpiredCreationMessage;
    public string TrialLimitsSummary => TrialLimitsSummaryText;

    /// <summary>
    /// Initializes the cached license state for the current application lifetime.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (current is not null)
        {
            return;
        }

        // Plain Development runs stay licensed unless an explicit startup override is requested.
        if (hostEnvironment.IsDevelopment() && launchOverride.Mode == LicenseLaunchMode.None)
        {
            current = new LicenseStatus
            {
                IsLicensed = true
            };
            return;
        }

        // Startup overrides bypass file-based validation so trial scenarios can be tested deterministically.
        var developmentOverride = GetLaunchOverride();
        if (developmentOverride is not null)
        {
            current = developmentOverride;
            return;
        }

        var allowedUsername = configuration["Authentication:AllowedUsername"] ?? string.Empty;
        var validation = localLicenseService.ValidateCurrentLicense(allowedUsername);
        if (validation.IsValid)
        {
            current = new LicenseStatus
            {
                IsLicensed = true
            };
            return;
        }

        var settings = await clinicSettingsRepository.GetAsync();
        if (!settings.TrialStartUtc.HasValue)
        {
            // The first boot without a valid license stamps the singleton settings row and starts the trial clock.
            settings.TrialStartUtc = timeProvider.GetUtcNow().UtcDateTime;
            await clinicSettingsRepository.SaveAsync(settings);
        }

        var trialExpiresUtc = settings.TrialStartUtc.Value.AddDays(TrialDurationDays);
        current = new LicenseStatus
        {
            IsTrial = true,
            IsTrialExpired = timeProvider.GetUtcNow().UtcDateTime > trialExpiresUtc,
            TrialExpiresUtc = trialExpiresUtc,
            RemainingTrialDays = CalculateRemainingTrialDays(trialExpiresUtc)
        };
    }

    /// <summary>
    /// Gets the message that blocks new patient creation under the current license state, if any.
    /// </summary>
    public async Task<string?> GetPatientCreationBlockMessageAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        if (IsTrialExpired)
        {
            return TrialExpiredCreationMessage;
        }

        if (!IsTrial)
        {
            return null;
        }

        var patientCount = await patientRepository.CountAsync();
        return patientCount >= TrialPatientLimit ? TrialPatientLimitMessage : null;
    }

    /// <summary>
    /// Gets the message that blocks new therapy-template creation under the current license state, if any.
    /// </summary>
    public async Task<string?> GetTherapyTemplateCreationBlockMessageAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        if (IsTrialExpired)
        {
            return TrialExpiredCreationMessage;
        }

        if (!IsTrial)
        {
            return null;
        }

        var templateCount = await therapyTemplateRepository.CountAsync();
        return templateCount >= TrialTherapyTemplateLimit ? TrialTherapyTemplateLimitMessage : null;
    }

    /// <summary>
    /// Gets the message that blocks therapy creation when the trial has expired.
    /// </summary>
    public async Task<string?> GetTherapyCreationBlockMessageAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        return IsTrialExpired ? TrialExpiredCreationMessage : null;
    }

    private LicenseStatus? GetLaunchOverride()
    {
        var mode = launchOverride.Mode;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return mode switch
        {
            LicenseLaunchMode.TrialActive => new LicenseStatus
            {
                IsTrial = true,
                IsTrialExpired = false,
                TrialExpiresUtc = now.AddDays(TrialDurationDays),
                RemainingTrialDays = TrialDurationDays
            },
            LicenseLaunchMode.TrialExpired => new LicenseStatus
            {
                IsTrial = true,
                IsTrialExpired = true,
                TrialExpiresUtc = now.AddDays(-1),
                RemainingTrialDays = 0
            },
            _ => null
        };
    }

    private int CalculateRemainingTrialDays(DateTime trialExpiresUtc)
    {
        var remaining = trialExpiresUtc - timeProvider.GetUtcNow().UtcDateTime;
        if (remaining <= TimeSpan.Zero)
        {
            return 0;
        }

        return Math.Max(0, (int)Math.Ceiling(remaining.TotalDays));
    }
}
