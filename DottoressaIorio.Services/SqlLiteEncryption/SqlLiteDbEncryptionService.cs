using Microsoft.Data.Sqlite;
using System.IO;

namespace DottoressaIorio.Services.SqlLiteEncryption;

public class SqlLiteDbEncryptionService(string key)
{
    private const string EncryptedDbPath = "app.db";
    private const string UnencryptedDbPath = "app_unencrypted.db";


    /// <summary>
    /// Returns the *raw* connection string that EF Core will use.
    /// No PRAGMA applied here — this is for testing / debugging / DI config.
    /// </summary>
    public string GetConnectionString()
    {
        var dbPath = GetDbPath();
        return $"Data Source={dbPath}";
    }

    /// <summary>
    ///     Returns a ready-to-use SQLite connection.
    ///     If _key is provided → SQLCipher encrypted DB (app.db)
    ///     If _key is null/empty → normal SQLite DB (app_unencrypted.db)
    /// </summary>
    public SqliteConnection CreateConnection()
    {
        var dbPath = GetDbPath();

        var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        // Only apply encryption PRAGMA when key exists
        if (!string.IsNullOrEmpty(key))
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA key = '{key}';";
            cmd.ExecuteNonQuery();
        }

        return connection;
    }

    private string GetDbPath()
    {
        var fileName = string.IsNullOrEmpty(key) ? UnencryptedDbPath : EncryptedDbPath;
        return Path.Combine(AppContext.BaseDirectory, fileName);
    }
}
