using DottoressaIorio.App.Middleware;
using DottoressaIorio.Data;
using DottoressaIorio.Repositories;
using DottoressaIorio.Services.Backup;
using DottoressaIorio.Services.Licensing;
using DottoressaIorio.Services.Patients;
using DottoressaIorio.Services.Pdf;
using DottoressaIorio.Services.SqlLiteEncryption;
using DottoressaIorio.Shared.Branding;
using DottoressaIorio.Shared.State;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using SQLitePCL;
using Serilog;

try
{
    raw.SetProvider(new SQLite3Provider_e_sqlcipher());
    Batteries_V2.Init();

    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;
    var isWindowsService = OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService();
    var runOnlineBackup = args.Any(argument => argument.Equals("--online-backup", StringComparison.OrdinalIgnoreCase));
    var useDemoDatabase = args.Any(argument => argument.Equals("--demo", StringComparison.OrdinalIgnoreCase));
    var useTrialActive = args.Any(argument => argument.Equals("--trial-active", StringComparison.OrdinalIgnoreCase));
    var useTrialExpired = args.Any(argument => argument.Equals("--trial-expired", StringComparison.OrdinalIgnoreCase));
    var databaseRootPath = configuration["DatabaseRootPath"]
                           ?? (isWindowsService ? AppContext.BaseDirectory : builder.Environment.ContentRootPath);

    if (isWindowsService) builder.Host.UseWindowsService();

    var encryptionKeyResolver = new EncryptionKeyResolver(configuration, builder.Environment.EnvironmentName);
    var encryptionKey = encryptionKeyResolver.Resolve();
    var allowedUsername = configuration["Authentication:AllowedUsername"]!;

    var useNegotiate = configuration.GetValue<bool>("Authentication:UseNegotiate");

    builder.AddDottoressaLogging();

    if (useNegotiate && string.IsNullOrWhiteSpace(allowedUsername))
        Log.Warning(
            "Authentication:UseNegotiate is enabled but Authentication:AllowedUsername is empty. Access will be denied by SpecificUserOnly.");

    if (!useNegotiate && builder.Environment.IsProduction())
        Log.Warning(
            "Authentication:UseNegotiate is disabled in Production. SpecificUserOnly allows access when Negotiate is disabled.");

    builder.Services.AddDottoressaIorioAuthorization(configuration, useNegotiate, allowedUsername);
    builder.Services.AddDottoressaIorioAuthentication(configuration, useNegotiate);

    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddScoped<ClinicSettingsState>();
    builder.Services.AddSingleton(configuration.GetSection("Branding").Get<ProductBrandingOptions>() ??
                                  new ProductBrandingOptions());

    builder.Services.AddSqlLiteDbEncryption<ApplicationDbContext>(
        encryptionKey,
        databaseRootPath);

    builder.Services.AddDottoressaRepositories();
    builder.Services.AddScoped<PatientCsvExportService>();

    builder.Services.AddPdfServices();
    builder.Services.AddLocalLicenseServices(configuration);
    builder.Services.AddSingleton(new LicenseLaunchOverride
    {
        Mode = useTrialExpired
            ? LicenseLaunchMode.TrialExpired
            : useTrialActive
                ? LicenseLaunchMode.TrialActive
                : LicenseLaunchMode.None
    });
    builder.Services.AddOnlineSqliteBackup(options =>
    {
        options.AppRootPath = databaseRootPath;
        options.PreferredWindowsUsername = OnlineBackupExtensions.ExtractUsername(allowedUsername);
    });

    if (runOnlineBackup && useDemoDatabase)
        throw new InvalidOperationException("Cannot use --online-backup and --demo together.");

    if (useTrialActive && useTrialExpired)
        throw new InvalidOperationException("Cannot use --trial-active and --trial-expired together.");

    if (useDemoDatabase)
        builder.ConfigureDemoDatabase();

    var app = builder.Build();
    var onlineBackupService = app.Services.GetRequiredService<IOnlineBackupService>();
    var onlineBackupOptions = app.Services.GetRequiredService<OnlineBackupOptions>();

    if (runOnlineBackup)
    {
        // Scheduled-task mode exits after one backup attempt instead of starting the web host.
        var result = await onlineBackupService.TriggerBackupAsync("scheduled-task");
        Environment.ExitCode = result.Outcome == OnlineBackupOutcome.Failed ? 1 : 0;
        return;
    }

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // The database schema must be current before license initialization because trial state is stored in ClinicSettings.
        dbContext.Database.Migrate();
        var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseService>();
        await licenseService.InitializeAsync();

        if (useTrialActive || useTrialExpired)
        {
            Log.Warning("License launch override enabled: {Mode}.", useTrialExpired ? "TrialExpired" : "TrialActive");
        }
        else if (!app.Environment.IsDevelopment() && !licenseService.IsLicensed)
        {
            Log.Warning("License file missing or invalid. Trial mode is active.");
        }
    }

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Log.Information("Application started.");
        _ = Task.Run(async () =>
        {
            try
            {
                // Startup backups are opportunistic and respect the configured minimum age to avoid noisy repeats.
                await onlineBackupService.TriggerBackupAsync("application-startup", onlineBackupOptions.StartupMinimumAge);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to execute startup online backup.");
            }
        });
    });

    app.Lifetime.ApplicationStopping.Register(() =>
    {
        Log.Information("Application stopping.");
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            // Shutdown backup is best-effort only so service stop does not hang indefinitely.
            _ = onlineBackupService.TriggerBackupAsync(
                    "application-stopping",
                    onlineBackupOptions.ShutdownMinimumAge,
                    cts.Token)
                .GetAwaiter()
                .GetResult();
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Application stopping backup timed out.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Application stopping online backup failed.");
        }
    });
    app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    if (!isWindowsService) app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    // When Negotiate is disabled, the current authorization rules intentionally run without authentication middleware.
    if (useNegotiate) app.UseAuthentication();
    app.UseAuthorization();

    app.MapDottoressaPatientExportEndpoints();
    app.MapDottoressaPdfEndpoints();

    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
