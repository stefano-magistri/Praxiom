using Microsoft.Data.Sqlite;
using SQLitePCL;
using System.Text.Json;
using DottoressaIorio.DbUpgradeBridge;

Batteries_V2.Init();

var appRoot = GetAppRoot(args);
if (string.IsNullOrWhiteSpace(appRoot))
{
    Console.Error.WriteLine("Missing app root.");
    return 1;
}

var appDbPath = Path.Combine(appRoot, "app.db");
var defaultLegacyBackupDbPath = @"C:\Program Files\DottoressaIorio\_db_backup\app.db";
var configuredLegacyBackupDbPath = GetOptionValue(args, "--legacy-db-path");
var legacyBackupDbPath = string.IsNullOrWhiteSpace(configuredLegacyBackupDbPath)
    ? defaultLegacyBackupDbPath
    : configuredLegacyBackupDbPath;
var hasLegacyBackupDb = File.Exists(legacyBackupDbPath);
var appSettingsPath = Path.Combine(appRoot, "appsettings.json");

if (!hasLegacyBackupDb)
{
    Console.WriteLine($"No legacy backup DB found at '{legacyBackupDbPath}'. Skipping migration.");
    return 0;
}

var sourceDbPath = legacyBackupDbPath;
Console.WriteLine($"Detected legacy backup DB at '{legacyBackupDbPath}'. Forcing migration.");

if (File.Exists(appDbPath))
{
    Console.WriteLine($"Current DB already exists at '{appDbPath}'. Skipping legacy backup import.");
    if (!BridgeFileOperations.TryArchiveLegacyBackup(legacyBackupDbPath, out var archivedExistingLegacyPath))
    {
        Console.Error.WriteLine($"Bridge failed: could not archive unused legacy backup DB at '{legacyBackupDbPath}'.");
        return 1;
    }

    Console.WriteLine($"Legacy backup archived to '{archivedExistingLegacyPath}'.");
    return 0;
}

if (!File.Exists(appSettingsPath))
{
    Console.Error.WriteLine($"Missing appsettings.json at '{appSettingsPath}'.");
    return 1;
}

var key = ReadEncryptionKey(appSettingsPath);
if (string.IsNullOrWhiteSpace(key))
{
    Console.WriteLine("EncryptionKey is empty. Skipping migration.");
    return 0;
}

var tempDbPath = appDbPath + ".bridge_migrating";
var backupDbPath = appDbPath + ".pre_bridge_backup";
DeleteIfExists(tempDbPath);
DeleteIfExists(backupDbPath);

var sourceStrategies = new Func<string, string, SqliteConnection?>[]
{
    TryOpenWithLegacyPassword,
    TryOpenWithPragmaKey,
    TryOpenWithPragmaKeyCompat3,
    TryOpenWithPragmaKeyCompat4
};

var migrated = false;
foreach (var openStrategy in sourceStrategies)
{
    using var sourceConnection = openStrategy(sourceDbPath, key);
    if (sourceConnection is null) continue;

    if (!TryExportPlainSqlite(sourceConnection, tempDbPath)) continue;

    migrated = true;
    break;
}

if (!migrated)
{
    Console.Error.WriteLine("Bridge failed: could not migrate database into current app-readable format.");
    DeleteIfExists(tempDbPath);
    return 1;
}

if (!BridgeFileOperations.TryBackupCurrentDbIfPresent(appDbPath, backupDbPath))
{
    Console.Error.WriteLine("Bridge failed: could not backup existing app.db.");
    DeleteIfExists(tempDbPath);
    return 1;
}

if (!BridgeFileOperations.TryPromoteMigratedDb(tempDbPath, appDbPath))
{
    Console.Error.WriteLine("Bridge failed: could not place migrated app.db.");
    BridgeFileOperations.TryPromoteMigratedDb(backupDbPath, appDbPath);
    DeleteIfExists(tempDbPath);
    return 1;
}

if (hasLegacyBackupDb)
{
    if (!BridgeFileOperations.TryArchiveLegacyBackup(legacyBackupDbPath, out var archivedLegacyPath))
    {
        Console.Error.WriteLine($"Bridge failed: could not archive legacy backup DB at '{legacyBackupDbPath}'.");
        return 1;
    }

    Console.WriteLine($"Legacy backup archived to '{archivedLegacyPath}'.");
}

DeleteIfExists(backupDbPath);
DeleteIfExists(tempDbPath);
Console.WriteLine("Legacy DB bridge migration completed successfully.");
return 0;

static string GetAppRoot(string[] args)
{
    var value = GetOptionValue(args, "--app-root");
    if (!string.IsNullOrWhiteSpace(value)) return value;

    return Environment.CurrentDirectory;
}

static string? GetOptionValue(string[] args, string optionName)
{
    for (var i = 0; i < args.Length - 1; i++)
        if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];

    return null;
}

static string? ReadEncryptionKey(string appSettingsPath)
{
    using var stream = File.OpenRead(appSettingsPath);
    using var document = JsonDocument.Parse(stream);
    if (!document.RootElement.TryGetProperty("EncryptionKey", out var keyNode)) return null;
    return keyNode.GetString();
}

static SqliteConnection? TryOpenWithLegacyPassword(string dbPath, string key)
{
    try
    {
        var csBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Password = key,
            Pooling = false
        };

        var connection = new SqliteConnection(csBuilder.ToString());
        connection.Open();
        EnsureReadable(connection);
        return connection;
    }
    catch
    {
        return null;
    }
}

static SqliteConnection? TryOpenWithPragmaKey(string dbPath, string key)
{
    try
    {
        var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        ExecuteNonQuery(connection, $"PRAGMA key = '{EscapeSqlLiteral(key)}';");
        EnsureReadable(connection);
        return connection;
    }
    catch
    {
        return null;
    }
}

static SqliteConnection? TryOpenWithPragmaKeyCompat3(string dbPath, string key)
{
    try
    {
        var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        ExecuteNonQuery(connection, $"PRAGMA key = '{EscapeSqlLiteral(key)}';");
        ExecuteNonQuery(connection, "PRAGMA cipher_compatibility = 3;");
        EnsureReadable(connection);
        return connection;
    }
    catch
    {
        return null;
    }
}

static SqliteConnection? TryOpenWithPragmaKeyCompat4(string dbPath, string key)
{
    try
    {
        var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        ExecuteNonQuery(connection, $"PRAGMA key = '{EscapeSqlLiteral(key)}';");
        ExecuteNonQuery(connection, "PRAGMA cipher_compatibility = 4;");
        EnsureReadable(connection);
        return connection;
    }
    catch
    {
        return null;
    }
}

static bool TryExportPlainSqlite(SqliteConnection sourceConnection, string targetPath)
{
    try
    {
        DeleteIfExists(targetPath);

        ExecuteNonQuery(
            sourceConnection,
            $"ATTACH DATABASE '{EscapeSqlLiteral(targetPath)}' AS migrated KEY '';");
        ExecuteNonQuery(sourceConnection, "SELECT sqlcipher_export('migrated');");
        ExecuteNonQuery(sourceConnection, "DETACH DATABASE migrated;");

        return File.Exists(targetPath) && new FileInfo(targetPath).Length > 0;
    }
    catch
    {
        DeleteIfExists(targetPath);
        return false;
    }
}

static void EnsureReadable(SqliteConnection connection)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
    _ = cmd.ExecuteScalar();
}

static void ExecuteNonQuery(SqliteConnection connection, string sql)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = sql;
    cmd.ExecuteNonQuery();
}

static string EscapeSqlLiteral(string value)
{
    return value.Replace("'", "''");
}

static void DeleteIfExists(string path)
{
    if (!File.Exists(path)) return;

    for (var i = 0; i < 10; i++)
    {
        try
        {
            File.Delete(path);
            return;
        }
        catch (IOException)
        {
            Thread.Sleep(200);
        }
        catch (UnauthorizedAccessException)
        {
            Thread.Sleep(200);
        }
    }
}
