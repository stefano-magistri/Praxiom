using DottoressaIorio.Services.SqlLiteEncryption;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using System.IO;

namespace DottoressaIorio.Services.Tests;

/// <summary>
/// Contains unit tests for the SqlLiteDbEncryptionService class.
/// </summary>
public class SqlLiteDbEncryptionServiceTests
{
    static SqlLiteDbEncryptionServiceTests()
    {
        raw.SetProvider(new SQLite3Provider_e_sqlcipher());
        Batteries_V2.Init();
    }

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
        var builder = new SqliteConnectionStringBuilder(cnStr);
        Assert.Equal("app_unencrypted.db", Path.GetFileName(builder.DataSource));
        Assert.True(string.IsNullOrEmpty(builder.Password));
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
        var builder = new SqliteConnectionStringBuilder(cnStr);
        Assert.Equal("app.db", Path.GetFileName(builder.DataSource));
        Assert.True(string.IsNullOrEmpty(builder.Password));
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
    public void CreateConnection_WithEncryptionKey_CreatesEncryptedDatabase()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "DottoressaIorio.EncryptionTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new SqlLiteDbEncryptionService("key123", tempDir);
            using (var conn = service.CreateConnection())
            {
                Assert.Equal(System.Data.ConnectionState.Open, conn.State);
                using var command = conn.CreateCommand();
                command.CommandText = "CREATE TABLE IF NOT EXISTS __test (id INTEGER PRIMARY KEY);";
                command.ExecuteNonQuery();
            }

            var appDbPath = Path.Combine(tempDir, "app.db");
            Assert.True(File.Exists(appDbPath));

            var header = ReadAsciiHeader(appDbPath);
            Assert.DoesNotContain("SQLite format 3", header);

            using (var keyedConnection = new SqliteConnection($"Data Source={appDbPath};Mode=ReadWrite;Pooling=false"))
            {
                keyedConnection.Open();
                using (var keyCommand = keyedConnection.CreateCommand())
                {
                    keyCommand.CommandText = "PRAGMA key = 'key123';";
                    keyCommand.ExecuteNonQuery();
                }

                using var command = keyedConnection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
                _ = command.ExecuteScalar();
            }

            using var plainConnection = new SqliteConnection($"Data Source={appDbPath};Mode=ReadWrite;Pooling=false");
            var ex = Assert.Throws<SqliteException>(() =>
            {
                plainConnection.Open();
                using var command = plainConnection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
                _ = command.ExecuteScalar();
            });
            Assert.Equal(26, ex.SqliteErrorCode);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch (IOException)
                {
                    // Best effort cleanup for transient SQLite locks.
                }
            }
        }
    }

    private static string ReadAsciiHeader(string path)
    {
        using var stream = File.OpenRead(path);
        var buffer = new byte[16];
        _ = stream.Read(buffer, 0, buffer.Length);
        return System.Text.Encoding.ASCII.GetString(buffer);
    }
}
