using DottoressaIorio.App.Middleware;
using DottoressaIorio.App.Startup;
using DottoressaIorio.Data;
using DottoressaIorio.Services.Backup;
using DottoressaIorio.Services.SqlLiteEncryption;
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
    var launchOptions = LaunchOptions.Parse(args, configuration);
    launchOptions.Validate();
    var databaseRootPath = AppHostingEnvironmentExtensions.ResolveDatabaseRootPath(
        configuration,
        builder.Environment,
        isWindowsService);

    if (isWindowsService) builder.Host.UseWindowsService();

    var encryptionKeyResolver = new EncryptionKeyResolver(configuration, builder.Environment.EnvironmentName);
    var encryptionKey = encryptionKeyResolver.Resolve();
    var allowedUsername = configuration["Authentication:AllowedUsername"]!;

    var useNegotiate = configuration.GetValue<bool>("Authentication:UseNegotiate");

    builder.AddDottoressaLogging();
    ProgramStartupExtensions.EnsureDatabaseRootPathExists(databaseRootPath);
    builder.ValidateStartupConfiguration(useNegotiate, allowedUsername);

    builder.Services.AddDottoressaSecurity(configuration, useNegotiate, allowedUsername);
    builder.Services.AddDottoressaPresentation(configuration);
    builder.Services.AddDottoressaApplicationServices(
        configuration,
        encryptionKey,
        databaseRootPath,
        launchOptions,
        allowedUsername);

    if (launchOptions.UseDemoDatabase)
        builder.ConfigureDemoDatabase(
            databaseRootPath,
            useEncryption: !string.IsNullOrWhiteSpace(encryptionKey),
            overwriteExisting: launchOptions.UseDemoDatabaseFromCommandLine);

    var app = builder.Build();
    var onlineBackupService = app.Services.GetRequiredService<IOnlineBackupService>();
    var onlineBackupOptions = app.Services.GetRequiredService<OnlineBackupOptions>();

    if (launchOptions.RunOnlineBackup)
    {
        // Scheduled-task mode exits after one backup attempt instead of starting the web host.
        var result = await onlineBackupService.TriggerBackupAsync("scheduled-task");
        Environment.ExitCode = result.Outcome == OnlineBackupOutcome.Failed ? 1 : 0;
        return;
    }

    await app.InitializeDatabaseAndLicenseAsync(launchOptions);

    app.RegisterDottoressaLifetimeEvents(onlineBackupService, onlineBackupOptions);
    app.ConfigureDottoressaPipeline(isWindowsService, useNegotiate);
    app.MapDottoressaEndpoints();

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
