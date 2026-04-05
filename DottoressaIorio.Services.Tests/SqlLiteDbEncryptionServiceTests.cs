using DottoressaIorio.Services.SqlLiteEncryption;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
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

    [Fact]
    public void ResolveKey_LocalStoragePresent_ReturnsStoredKey()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var keyFilePath = Path.Combine(tempDir, "secrets", "encryption-key.json");
            Directory.CreateDirectory(Path.GetDirectoryName(keyFilePath)!);
            File.WriteAllText(keyFilePath, """{"EncryptionKey":"stored-key"}""");

            var resolver = new EncryptionKeyResolver(new ConfigurationBuilder().Build(), "Production", keyFilePath);

            var encryptionKey = resolver.Resolve();

            Assert.Equal("stored-key", encryptionKey);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public void ResolveKey_NonDevelopmentWithoutConfiguredOrLocalKey_ThrowsClearError()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var keyFilePath = Path.Combine(tempDir, "secrets", "encryption-key.json");
            var resolver = new EncryptionKeyResolver(new ConfigurationBuilder().Build(), "Production", keyFilePath);

            var ex = Assert.Throws<InvalidOperationException>(() => resolver.Resolve());

            Assert.Contains(keyFilePath, ex.Message);
            Assert.Contains("Checked configuration key 'EncryptionKey'", ex.Message);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public void ResolveKey_DevelopmentWithoutLocalStorage_FallsBackToConfiguration()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var keyFilePath = Path.Combine(tempDir, "secrets", "encryption-key.json");
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["EncryptionKey"] = "dev-config-key" })
                .Build();

            var resolver = new EncryptionKeyResolver(configuration, "Development", keyFilePath);

            var encryptionKey = resolver.Resolve();

            Assert.Equal("dev-config-key", encryptionKey);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public void ResolveKey_ProductionExplicitEmptyConfiguration_DisablesEncryption()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var keyFilePath = Path.Combine(tempDir, "secrets", "encryption-key.json");
            Directory.CreateDirectory(Path.GetDirectoryName(keyFilePath)!);
            File.WriteAllText(keyFilePath, """{"EncryptionKey":"stored-key"}""");

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["EncryptionKey"] = string.Empty })
                .Build();

            var resolver = new EncryptionKeyResolver(configuration, "Production", keyFilePath);

            var encryptionKey = resolver.Resolve();

            Assert.Equal(string.Empty, encryptionKey);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public void ResolveKey_DevelopmentExplicitEmptyConfiguration_OverridesLocalStorage()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var keyFilePath = Path.Combine(tempDir, "secrets", "encryption-key.json");
            Directory.CreateDirectory(Path.GetDirectoryName(keyFilePath)!);
            File.WriteAllText(keyFilePath, """{"EncryptionKey":"stored-key"}""");

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["EncryptionKey"] = string.Empty })
                .Build();

            var resolver = new EncryptionKeyResolver(configuration, "Development", keyFilePath);

            var encryptionKey = resolver.Resolve();

            Assert.Equal(string.Empty, encryptionKey);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public void ResolveKey_MovedKeySource_StillOpensExistingEncryptedDatabase()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var appRoot = Path.Combine(tempDir, "app-root");
            Directory.CreateDirectory(appRoot);

            var originalService = new SqlLiteDbEncryptionService("moved-key", appRoot);
            using (var connection = originalService.CreateConnection())
            {
                using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE IF NOT EXISTS __moved_key_test (id INTEGER PRIMARY KEY);";
                command.ExecuteNonQuery();
            }

            var keyFilePath = Path.Combine(tempDir, "secrets", "encryption-key.json");
            Directory.CreateDirectory(Path.GetDirectoryName(keyFilePath)!);
            File.WriteAllText(keyFilePath, """{"EncryptionKey":"moved-key"}""");

            var resolver = new EncryptionKeyResolver(new ConfigurationBuilder().Build(), "Production", keyFilePath);
            var reopenedService = new SqlLiteDbEncryptionService(resolver.Resolve(), appRoot);

            using var reopenedConnection = reopenedService.CreateConnection();
            using var verifyCommand = reopenedConnection.CreateCommand();
            verifyCommand.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name='__moved_key_test';";
            var result = Convert.ToInt32(verifyCommand.ExecuteScalar());

            Assert.Equal(1, result);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    private static string ReadAsciiHeader(string path)
    {
        using var stream = File.OpenRead(path);
        var buffer = new byte[16];
        _ = stream.Read(buffer, 0, buffer.Length);
        return System.Text.Encoding.ASCII.GetString(buffer);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "DottoressaIorio.EncryptionTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (!Directory.Exists(path))
            return;

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup for transient SQLite locks.
        }
    }
}
