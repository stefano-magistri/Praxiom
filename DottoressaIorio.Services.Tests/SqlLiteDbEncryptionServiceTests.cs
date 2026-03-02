using DottoressaIorio.Services.SqlLiteEncryption;
using Microsoft.Data.Sqlite;
using System.IO;

namespace DottoressaIorio.Services.Tests;

/// <summary>
/// Contains unit tests for the SqlLiteDbEncryptionService class.
/// </summary>
public class SqlLiteDbEncryptionServiceTests
{
    /// <summary>
    /// When no encryption key is provided, the service should point to the unencrypted DB file.
    /// </summary>
    [Fact]
    public void GetConnectionString_NoEncryptionKey_UsesUnencryptedDatabase()
    {
        // Arrange
        var service = new SqlLiteDbEncryptionService(null);

        // Act
        var cnStr = service.GetConnectionString();

        // Assert
        var dataSource = cnStr["Data Source=".Length..];
        Assert.Equal("app_unencrypted.db", Path.GetFileName(dataSource));
    }

    /// <summary>
    /// When an encryption key is provided, the service should use the encrypted database file.
    /// </summary>
    [Fact]
    public void GetConnectionString_WithEncryptionKey_UsesEncryptedDatabase()
    {
        // Arrange
        var service = new SqlLiteDbEncryptionService("myKey");

        // Act
        var cnStr = service.GetConnectionString();

        // Assert
        var dataSource = cnStr["Data Source=".Length..];
        Assert.Equal("app.db", Path.GetFileName(dataSource));
    }

    /// <summary>
    /// CreateConnection should successfully open a SQLite connection without throwing.
    /// </summary>
    [Fact]
    public void CreateConnection_NoEncryptionKey_DoesNotThrow()
    {
        // Arrange
        var service = new SqlLiteDbEncryptionService(null);

        // Act & Assert
        using var conn = service.CreateConnection();
        Assert.NotNull(conn);
        Assert.Equal(System.Data.ConnectionState.Open, conn.State);
    }

    /// <summary>
    /// CreateConnection should open a SQLCipher-enabled connection when a key is provided.
    /// (We only test that the connection opens without error.)
    /// </summary>
    [Fact]
    public void CreateConnection_WithEncryptionKey_DoesNotThrow()
    {
        // Arrange
        var service = new SqlLiteDbEncryptionService("key123");

        // Act & Assert
        using var conn = service.CreateConnection();
        Assert.NotNull(conn);
        Assert.Equal(System.Data.ConnectionState.Open, conn.State);
    }

    [Fact]
    public void CreateConnection_WithEncryptionKey_AndPlainAppDb_FallsBackToPlainDatabase()
    {
        var appDbPath = Path.Combine(AppContext.BaseDirectory, "app.db");
        var backupPath = $"{appDbPath}.test_backup";

        if (File.Exists(appDbPath))
        {
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            File.Move(appDbPath, backupPath);
        }

        try
        {
            using (var seedConnection = new SqliteConnection($"Data Source={appDbPath}"))
            {
                seedConnection.Open();
                using var command = seedConnection.CreateCommand();
                command.CommandText = "CREATE TABLE IF NOT EXISTS __test (id INTEGER PRIMARY KEY);";
                command.ExecuteNonQuery();
            }

            var service = new SqlLiteDbEncryptionService("key123");
            using var conn = service.CreateConnection();
            Assert.Equal(System.Data.ConnectionState.Open, conn.State);
        }
        finally
        {
            if (File.Exists(appDbPath))
            {
                try
                {
                    File.Delete(appDbPath);
                }
                catch (IOException)
                {
                    // Best effort cleanup: pooled SQLite handles can transiently lock the file.
                }
            }

            if (File.Exists(backupPath))
            {
                try
                {
                    File.Move(backupPath, appDbPath);
                }
                catch (IOException)
                {
                    // Best effort cleanup for the same reason.
                }
            }
        }
    }
}
