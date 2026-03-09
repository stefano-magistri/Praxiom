using DottoressaIorio.App.Middleware;
using DottoressaIorio.Data;
using DottoressaIorio.Repositories;
using DottoressaIorio.Services.Backup;
using DottoressaIorio.Services.Licensing;
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
    var databaseRootPath = configuration["DatabaseRootPath"]
                           ?? (isWindowsService ? AppContext.BaseDirectory : builder.Environment.ContentRootPath);

    if (isWindowsService) builder.Host.UseWindowsService();

    var encryptionKey = configuration["EncryptionKey"]!;
    var allowedUsername = configuration["Authentication:AllowedUsername"]!;

    // Read the "UseNegotiate" setting from the configuration file
    var useNegotiate = configuration.GetValue<bool>("Authentication:UseNegotiate");

    // Configure Serilog for logging
    builder.AddDottoressaLogging();

    if (useNegotiate && string.IsNullOrWhiteSpace(allowedUsername))
        Log.Warning(
            "Authentication:UseNegotiate is enabled but Authentication:AllowedUsername is empty. Access will be denied by SpecificUserOnly.");

    if (!useNegotiate && builder.Environment.IsProduction())
        Log.Warning(
            "Authentication:UseNegotiate is disabled in Production. SpecificUserOnly allows access when Negotiate is disabled.");

    // Configure authorization policies
    builder.Services.AddDottoressaIorioAuthorization(configuration, useNegotiate, allowedUsername);
    builder.Services.AddDottoressaIorioAuthentication(configuration, useNegotiate);


    // Add services to the container.
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddScoped<ClinicSettingsState>();
    builder.Services.AddSingleton(configuration.GetSection("Branding").Get<ProductBrandingOptions>() ??
                                  new ProductBrandingOptions());

    builder.Services.AddSqlLiteDbEncryption<ApplicationDbContext>(
        encryptionKey,
        databaseRootPath);

    builder.Services.AddDottoressaRepositories();

    builder.Services.AddPdfServices();
    builder.Services.AddLocalLicenseServices(configuration);
    builder.Services.AddOnlineSqliteBackup(options =>
    {
        options.AppRootPath = databaseRootPath;
        options.PreferredWindowsUsername = OnlineBackupExtensions.ExtractUsername(allowedUsername);
    });

    if (runOnlineBackup && useDemoDatabase)
        throw new InvalidOperationException("Cannot use --online-backup and --demo together.");

    if (useDemoDatabase)
        builder.ConfigureDemoDatabase();

    var app = builder.Build();
    var licenseService = app.Services.GetRequiredService<ILocalLicenseService>();
    var onlineBackupService = app.Services.GetRequiredService<IOnlineBackupService>();
    var onlineBackupOptions = app.Services.GetRequiredService<OnlineBackupOptions>();

    if (app.Environment.IsDevelopment())
    {
        Log.Warning("Skipping local license validation in Development.");
    }
    else
    {
        var licenseValidationResult = licenseService.ValidateCurrentLicense(allowedUsername);
        if (!licenseValidationResult.IsValid)
        {
            throw new InvalidOperationException($"Local license validation failed: {licenseValidationResult.Failure}.");
        }
    }

    if (runOnlineBackup)
    {
        var result = await onlineBackupService.TriggerBackupAsync("scheduled-task");
        Environment.ExitCode = result.Outcome == OnlineBackupOutcome.Failed ? 1 : 0;
        return;
    }

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
    }

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Log.Information("Application started.");
        _ = Task.Run(async () =>
        {
            try
            {
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

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    if (!isWindowsService) app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    if (useNegotiate) app.UseAuthentication();
    app.UseAuthorization();

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
