using Microsoft.Extensions.DependencyInjection;

namespace DottoressaIorio.Repositories;

/// <summary>
/// Registers the repository layer and its shared unit-of-work abstraction.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Adds the repositories used by the application.
    /// </summary>
    public static IServiceCollection AddDottoressaRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IClinicSettingsRepository, ClinicSettingsRepository>();
        services.AddScoped<TherapyTemplateRepository>();
        services.AddScoped<PatientTherapyRepository>();
        services.AddScoped<PatientRepository>();
        return services;
    }
}
