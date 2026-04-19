using DottoressaIorio.App.Startup;
using DottoressaIorio.Data;
using DottoressaIorio.Services.Licensing;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DottoressaIorio.App.Middleware;

internal static class ProgramStartupExtensions
{
    public static void EnsureDatabaseRootPathExists(string databaseRootPath)
    {
        try
        {
            Directory.CreateDirectory(databaseRootPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException(
                $"Unable to create database root directory '{databaseRootPath}'. Configure DatabaseRootPath to a writable location.",
                ex);
        }
    }

    public static void ValidateStartupConfiguration(
        this WebApplicationBuilder builder,
        bool useNegotiate,
        string allowedUsername)
    {
        if (builder.Environment.IsAzureEnvironment() && useNegotiate)
            throw new InvalidOperationException(
                "Authentication:UseNegotiate must be disabled for the Azure demo environment. Set Authentication__UseNegotiate=false.");

        if (useNegotiate && string.IsNullOrWhiteSpace(allowedUsername))
            Log.Warning(
                "Authentication:UseNegotiate is enabled but Authentication:AllowedUsername is empty. Access will be denied by SpecificUserOnly.");

        if (!useNegotiate && builder.Environment.IsProduction())
            Log.Warning(
                "Authentication:UseNegotiate is disabled in Production. SpecificUserOnly allows access when Negotiate is disabled.");
    }

    public static async Task InitializeDatabaseAndLicenseAsync(this WebApplication app, LaunchOptions launchOptions)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // The database schema must be current before license initialization because trial state is stored in ClinicSettings.
        dbContext.Database.Migrate();
        var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseService>();
        await licenseService.InitializeAsync();

        var trialOverrideLogMode = launchOptions.GetTrialOverrideLogMode();
        if (trialOverrideLogMode is not null)
        {
            Log.Warning("License launch override enabled: {Mode}.", trialOverrideLogMode);
        }
        else if (!app.Environment.IsDevelopment() && !licenseService.IsLicensed)
        {
            Log.Warning("License file missing or invalid. Trial mode is active.");
        }
    }
}
