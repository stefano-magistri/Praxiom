using Microsoft.Extensions.DependencyInjection;

namespace DottoressaIorio.Repositories;

public static class RepositoryExtensions
{
    public static IServiceCollection AddDottoressaRepositories(this IServiceCollection services)
    {
        services.AddScoped<TherapyTemplateRepository>();
        services.AddScoped<PatientTherapyRepository>();
        services.AddScoped<PatientRepository>();
        return services;
    }
}