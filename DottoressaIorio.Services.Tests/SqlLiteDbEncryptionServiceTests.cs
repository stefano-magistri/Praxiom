using DottoressaIorio.Services.SqlLiteEncryption;

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
        Assert.Contains("Data Source=app_unencrypted.db", cnStr);
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
        Assert.Contains("Data Source=app.db", cnStr);
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
}
