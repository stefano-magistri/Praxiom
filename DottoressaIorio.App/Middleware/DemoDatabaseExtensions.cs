using Serilog;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace DottoressaIorio.App.Middleware;

public static class DemoDatabaseExtensions
{
    public static string GetDatabaseRootPath(this WebApplicationBuilder builder)
    {
        var isWindowsService = OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService();
        return isWindowsService ? AppContext.BaseDirectory : builder.Environment.ContentRootPath;
    }

    public static void ConfigureDemoDatabase(this WebApplicationBuilder builder)
    {
        var demoDbPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "Demo", "app_old.db"));
        if (!File.Exists(demoDbPath))
            throw new FileNotFoundException($"Demo database not found: {demoDbPath}");

        var targetDbPath = Path.Combine(builder.GetDatabaseRootPath(), "app.db");
        var targetWalPath = $"{targetDbPath}-wal";
        var targetShmPath = $"{targetDbPath}-shm";

        if (File.Exists(targetWalPath))
            File.Delete(targetWalPath);
        if (File.Exists(targetShmPath))
            File.Delete(targetShmPath);

        File.Copy(demoDbPath, targetDbPath, true);
        Log.Information("Demo database loaded from {DemoDbPath} to {TargetDbPath}", demoDbPath, targetDbPath);
    }
}
