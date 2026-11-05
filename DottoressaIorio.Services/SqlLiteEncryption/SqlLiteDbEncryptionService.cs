using Microsoft.Data.Sqlite;

namespace DottoressaIorio.Services.SqlLiteEncryption;

public class SqlLiteDbEncryptionService(string key, string? databaseRootPath = null)
{
    private const string EncryptedDbPath = "app.db";
    private const string UnencryptedDbPath = "app_unencrypted.db";


    /// <summary>
    ///     Returns the *raw* connection string that EF Core will use.
    ///     No PRAGMA applied here — this is for testing / debugging / DI config.
    /// </summary>
    public string GetConnectionString()
    {
        var builder = BuildConnectionStringBuilder(GetDatabasePath());
        return builder.ToString();
    }

    /// <summary>
    ///     Returns a ready-to-use SQLite connection.
    ///     If _key is provided → SQLCipher encrypted DB (app.db)
    ///     If _key is null/empty → normal SQLite DB (app_unencrypted.db)
    /// </summary>
    public SqliteConnection CreateConnection()
    {
        return CreateConnectionForPath(GetDatabasePath());
    }

    public SqliteConnection CreateConnectionForPath(string dbPath)
    {
        if (!string.IsNullOrWhiteSpace(key) && !File.Exists(dbPath))
            CreateEmptyEncryptedDatabase(dbPath, key);

        var connection = new SqliteConnection(BuildConnectionStringBuilder(dbPath).ToString());
        connection.Open();
        if (!string.IsNullOrWhiteSpace(key))
            ExecuteNonQuery(connection, $"PRAGMA key = '{EscapeSqlLiteral(key)}';");
        EnsureReadable(connection);
        return connection;
    }

    public string GetDatabasePath()
    {
        var fileName = string.IsNullOrEmpty(key) ? UnencryptedDbPath : EncryptedDbPath;
        var rootPath = string.IsNullOrWhiteSpace(databaseRootPath) ? AppContext.BaseDirectory : databaseRootPath;
        return Path.Combine(rootPath, fileName);
    }

    private SqliteConnectionStringBuilder BuildConnectionStringBuilder(string dbPath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        };

        return builder;
    }

    private static void EnsureReadable(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
        _ = cmd.ExecuteScalar();
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static string EscapeSqlLiteral(string value)
    {
        return value.Replace("'", "''");
    }

    private static void CreateEmptyEncryptedDatabase(string dbPath, string key)
    {
        using var bootstrapConnection = new SqliteConnection("Data Source=:memory:;Mode=Memory;Pooling=false");
        bootstrapConnection.Open();
        ExecuteNonQuery(
            bootstrapConnection,
            $"ATTACH DATABASE '{EscapeSqlLiteral(dbPath)}' AS encrypted KEY '{EscapeSqlLiteral(key)}';");
        ExecuteNonQuery(bootstrapConnection, "DETACH DATABASE encrypted;");
    }

}
