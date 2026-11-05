using Serilog;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Data.Sqlite;

namespace DottoressaIorio.App.Middleware;

public static class DemoDatabaseExtensions
{
    public static string ResolveDemoDatabasePath(string contentRootPath, bool useEncryption)
    {
        var demoFileName = useEncryption ? "app_old.db" : "app_unencrypted.db";
        var deployedDemoDbPath = Path.Combine(contentRootPath, "Demo", demoFileName);
        if (File.Exists(deployedDemoDbPath))
        {
            return deployedDemoDbPath;
        }

        return Path.GetFullPath(Path.Combine(contentRootPath, "..", "Demo", demoFileName));
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
        var demoDbPath = ResolveDemoDatabasePath(builder.Environment.ContentRootPath, useEncryption);
        if (!File.Exists(demoDbPath))
            throw new FileNotFoundException($"Demo database not found: {demoDbPath}");

        Directory.CreateDirectory(databaseRootPath);

        var targetDbPath = Path.Combine(databaseRootPath, GetDemoDatabaseTargetFileName(useEncryption));
        if (!overwriteExisting && File.Exists(targetDbPath) && IsReadableSqliteDatabase(targetDbPath))
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

    private static bool IsReadableSqliteDatabase(string dbPath)
    {
        try
        {
            using var connection = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = dbPath,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                }.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
            _ = command.ExecuteScalar();
            return true;
        }
        catch (Exception ex) when (ex is SqliteException or InvalidOperationException)
        {
            Log.Warning(ex, "Existing demo database at {DatabasePath} is unreadable and will be replaced.", dbPath);
            return false;
        }
    }
}
