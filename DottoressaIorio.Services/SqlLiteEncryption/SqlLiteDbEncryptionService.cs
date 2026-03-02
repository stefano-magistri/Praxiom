using Microsoft.Data.Sqlite;
using System.Text;

namespace DottoressaIorio.Services.SqlLiteEncryption;

public class SqlLiteDbEncryptionService(string key)
{
    private const string EncryptedDbPath = "app.db";
    private const string UnencryptedDbPath = "app_unencrypted.db";


    /// <summary>
    ///     Returns the *raw* connection string that EF Core will use.
    ///     No PRAGMA applied here — this is for testing / debugging / DI config.
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
        return CreateConnectionForPath(dbPath);
    }

    public SqliteConnection CreateConnectionForPath(string dbPath)
    {
        var errors = new StringBuilder();

        if (string.IsNullOrEmpty(key))
        {
            var plainConnection = new SqliteConnection($"Data Source={dbPath}");
            plainConnection.Open();
            return plainConnection;
        }

        if (TryOpenWithLegacyPassword(dbPath, out var legacyPasswordConnection, out var legacyError))
            return legacyPasswordConnection;
        errors.Append("LegacyPassword=").Append(legacyError).Append("; ");

        if (TryOpenWithPragmaKey(dbPath, out var pragmaKeyConnection, out var pragmaError))
            return pragmaKeyConnection;
        errors.Append("PragmaKey=").Append(pragmaError).Append("; ");

        if (TryOpenWithPragmaKeyCompat3(dbPath, out var pragmaCompat3Connection, out var compat3Error))
            return pragmaCompat3Connection;
        errors.Append("PragmaKeyCompat3=").Append(compat3Error).Append("; ");

        if (TryOpenWithPragmaKeyCompat4(dbPath, out var pragmaCompat4Connection, out var compat4Error))
            return pragmaCompat4Connection;
        errors.Append("PragmaKeyCompat4=").Append(compat4Error).Append(";");

        if (TryOpenWithoutKey(dbPath, out var fallbackPlainConnection, out var plainError))
            return fallbackPlainConnection;
        errors.Append("Plain=").Append(plainError).Append(";");

        throw new SqliteException(
            $"Unable to open SQLite database with available key strategies. Details: {errors}",
            26);
    }

    private string GetDbPath()
    {
        var fileName = string.IsNullOrEmpty(key) ? UnencryptedDbPath : EncryptedDbPath;
        return Path.Combine(AppContext.BaseDirectory, fileName);
    }

    private bool TryOpenWithLegacyPassword(string dbPath, out SqliteConnection connection, out string error)
    {
        SqliteConnection? candidate = null;
        try
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Password = key,
                Pooling = false
            };

            candidate = new SqliteConnection(builder.ToString());
            candidate.Open();
            EnsureReadable(candidate);
            connection = candidate;
            error = "ok";
            return true;
        }
        catch (Exception ex)
        {
            candidate?.Dispose();
            connection = null!;
            error = ex.Message;
            return false;
        }
    }

    private bool TryOpenWithPragmaKey(string dbPath, out SqliteConnection connection, out string error)
    {
        SqliteConnection? candidate = null;
        try
        {
            candidate = new SqliteConnection($"Data Source={dbPath}");
            candidate.Open();
            ExecuteNonQuery(candidate, $"PRAGMA key = '{EscapeSqlLiteral(key)}';");
            EnsureReadable(candidate);
            connection = candidate;
            error = "ok";
            return true;
        }
        catch (Exception ex)
        {
            candidate?.Dispose();
            connection = null!;
            error = ex.Message;
            return false;
        }
    }

    private bool TryOpenWithPragmaKeyCompat3(string dbPath, out SqliteConnection connection, out string error)
    {
        SqliteConnection? candidate = null;
        try
        {
            candidate = new SqliteConnection($"Data Source={dbPath}");
            candidate.Open();
            ExecuteNonQuery(candidate, "PRAGMA cipher_compatibility = 3;");
            ExecuteNonQuery(candidate, $"PRAGMA key = '{EscapeSqlLiteral(key)}';");
            EnsureReadable(candidate);
            connection = candidate;
            error = "ok";
            return true;
        }
        catch (Exception ex)
        {
            candidate?.Dispose();
            connection = null!;
            error = ex.Message;
            return false;
        }
    }

    private bool TryOpenWithPragmaKeyCompat4(string dbPath, out SqliteConnection connection, out string error)
    {
        SqliteConnection? candidate = null;
        try
        {
            candidate = new SqliteConnection($"Data Source={dbPath}");
            candidate.Open();
            ExecuteNonQuery(candidate, "PRAGMA cipher_compatibility = 4;");
            ExecuteNonQuery(candidate, $"PRAGMA key = '{EscapeSqlLiteral(key)}';");
            EnsureReadable(candidate);
            connection = candidate;
            error = "ok";
            return true;
        }
        catch (Exception ex)
        {
            candidate?.Dispose();
            connection = null!;
            error = ex.Message;
            return false;
        }
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

    private bool TryOpenWithoutKey(string dbPath, out SqliteConnection connection, out string error)
    {
        SqliteConnection? candidate = null;
        try
        {
            candidate = new SqliteConnection($"Data Source={dbPath}");
            candidate.Open();
            EnsureReadable(candidate);
            connection = candidate;
            error = "ok";
            return true;
        }
        catch (Exception ex)
        {
            candidate?.Dispose();
            connection = null!;
            error = ex.Message;
            return false;
        }
    }
}
