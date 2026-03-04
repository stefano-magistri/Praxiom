using Serilog;

namespace DottoressaIorio.App.Middleware;

public static class DemoDatabaseExtensions
{
    public static void ConfigureDemoDatabase(this WebApplicationBuilder builder)
    {
        var demoDbPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "Demo", "_db_backup", "app.db"));
        if (!File.Exists(demoDbPath))
            throw new FileNotFoundException($"Demo database not found: {demoDbPath}");

        var targetDbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
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