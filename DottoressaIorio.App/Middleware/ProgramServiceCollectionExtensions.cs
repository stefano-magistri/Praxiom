using DottoressaIorio.App.Startup;
using DottoressaIorio.Data;
using DottoressaIorio.Repositories;
using DottoressaIorio.Services.Backup;
using DottoressaIorio.Services.Licensing;
using DottoressaIorio.Services.Patients;
using DottoressaIorio.Services.Pdf;
using DottoressaIorio.Services.SqlLiteEncryption;
using DottoressaIorio.Shared.Branding;
using DottoressaIorio.Shared.State;

namespace DottoressaIorio.App.Middleware;

internal static class ProgramServiceCollectionExtensions
{
    public static IServiceCollection AddDottoressaSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useNegotiate,
        string allowedUsername)
    {
        services.AddDottoressaIorioAuthorization(configuration, useNegotiate, allowedUsername);
        services.AddDottoressaIorioAuthentication(configuration, useNegotiate);
        return services;
    }

    public static IServiceCollection AddDottoressaPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRazorPages();
        services.AddCascadingAuthenticationState();
        services.AddRazorComponents()
            .AddInteractiveServerComponents();
        services.AddScoped<ClinicSettingsState>();
        services.AddSingleton(configuration.GetSection("Branding").Get<ProductBrandingOptions>() ??
                              new ProductBrandingOptions());
        return services;
    }

    public static IServiceCollection AddDottoressaApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string encryptionKey,
        string databaseRootPath,
        LaunchOptions launchOptions,
        string allowedUsername)
    {
        services.AddSqlLiteDbEncryption<ApplicationDbContext>(
            encryptionKey,
            databaseRootPath);

        services.AddDottoressaRepositories();
        services.AddScoped<PatientCsvExportService>();

        services.AddPdfServices();
        services.AddLocalLicenseServices(configuration);
        services.AddSingleton(launchOptions.CreateLicenseLaunchOverride());
        services.AddOnlineSqliteBackup(options =>
        {
            options.AppRootPath = databaseRootPath;
            options.PreferredWindowsUsername = OnlineBackupExtensions.ExtractUsername(allowedUsername);
        });

        return services;
    }
}
