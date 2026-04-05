using Serilog;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace DottoressaIorio.App.Middleware;

public static class DemoDatabaseExtensions
{
    public static string ResolveDemoDatabasePath(string contentRootPath)
    {
        var deployedDemoDbPath = Path.Combine(contentRootPath, "Demo", "app_old.db");
        if (File.Exists(deployedDemoDbPath))
        {
            return deployedDemoDbPath;
        }

        return Path.GetFullPath(Path.Combine(contentRootPath, "..", "Demo", "app_old.db"));
    }

    public static string GetDatabaseRootPath(this WebApplicationBuilder builder)
    {
        var isWindowsService = OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService();
        return isWindowsService ? AppContext.BaseDirectory : builder.Environment.ContentRootPath;
    }

    public static string GetDemoDatabaseTargetFileName(bool useEncryption)
    {
        return useEncryption ? "app.db" : "app_unencrypted.db";
    }

    public static void ConfigureDemoDatabase(
        this WebApplicationBuilder builder,
        string databaseRootPath,
        bool useEncryption,
        bool overwriteExisting)
    {
        var demoDbPath = ResolveDemoDatabasePath(builder.Environment.ContentRootPath);
        if (!File.Exists(demoDbPath))
            throw new FileNotFoundException($"Demo database not found: {demoDbPath}");

        Directory.CreateDirectory(databaseRootPath);

        var targetDbPath = Path.Combine(databaseRootPath, GetDemoDatabaseTargetFileName(useEncryption));
        if (!overwriteExisting && File.Exists(targetDbPath))
        {
            Log.Information("Demo database already present at {TargetDbPath}; keeping existing file.", targetDbPath);
            return;
        }

        var targetWalPath = $"{targetDbPath}-wal";
        var targetShmPath = $"{targetDbPath}-shm";

        if (File.Exists(targetWalPath))
            File.Delete(targetWalPath);
        if (File.Exists(targetShmPath))
            File.Delete(targetShmPath);

        File.Copy(demoDbPath, targetDbPath, true);
        Log.Information(
            "Demo database loaded from {DemoDbPath} to {TargetDbPath}. OverwriteExisting={OverwriteExisting}",
            demoDbPath,
            targetDbPath,
            overwriteExisting);
    }
}
