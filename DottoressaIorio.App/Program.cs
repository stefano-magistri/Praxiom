using DottoressaIorio.App.Middleware;
using DottoressaIorio.Data;
using DottoressaIorio.Repositories;
using DottoressaIorio.Services.Backup;
using DottoressaIorio.Services.Pdf;
using DottoressaIorio.Services.SqlLiteEncryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;
    var isWindowsService = OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService();

    if (isWindowsService) builder.Host.UseWindowsService();

    var encryptionKey = configuration["EncryptionKey"];
    var allowedUsername = configuration["Authentication:AllowedUsername"];

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

    builder.Services.AddSqlLiteDbEncryption<ApplicationDbContext>(encryptionKey);

    builder.Services.AddDottoressaRepositories();

    builder.Services.AddPdfServices();
    builder.Services.AddOnlineSqliteBackup(options =>
    {
        options.PreferredWindowsUsername = ExtractUsername(allowedUsername);
    });

    var app = builder.Build();
    var onlineBackupService = app.Services.GetRequiredService<IOnlineBackupService>();
    var onlineBackupOptions = app.Services.GetRequiredService<OnlineBackupOptions>();

    var runOnlineBackup = args.Any(argument => argument.Equals("--online-backup", StringComparison.OrdinalIgnoreCase));
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

static string? ExtractUsername(string? allowedUsername)
{
    if (string.IsNullOrWhiteSpace(allowedUsername))
    {
        return null;
    }

    var trimmed = allowedUsername.Trim();
    var backslashIndex = trimmed.LastIndexOf('\\');
    var slashIndex = trimmed.LastIndexOf('/');
    var separatorIndex = Math.Max(backslashIndex, slashIndex);

    return separatorIndex >= 0 && separatorIndex < trimmed.Length - 1
        ? trimmed[(separatorIndex + 1)..]
        : trimmed;
}
