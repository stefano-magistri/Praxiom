using DottoressaIorio.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DottoressaIorio.Services.Licensing;

public enum LicenseLaunchMode
{
    None = 0,
    TrialActive,
    TrialExpired
}

public sealed class LicenseLaunchOverride
{
    public LicenseLaunchMode Mode { get; init; } = LicenseLaunchMode.None;
}

public sealed class LicenseStatus
{
    public bool IsLicensed { get; init; }
    public bool IsTrial { get; init; }
    public bool IsTrialExpired { get; init; }
    public DateTime? TrialExpiresUtc { get; init; }
    public int? RemainingTrialDays { get; init; }
}

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

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (current is not null)
        {
            return;
        }

        if (hostEnvironment.IsDevelopment() && launchOverride.Mode == LicenseLaunchMode.None)
        {
            current = new LicenseStatus
            {
                IsLicensed = true
            };
            return;
        }

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
