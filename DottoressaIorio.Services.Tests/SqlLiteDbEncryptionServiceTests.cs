namespace DottoressaIorio.Services.Tests;

/// <summary>
/// Contains unit tests for the SqlLiteDbEncryptionService class.
/// </summary>
public class SqlLiteDbEncryptionServiceTests
{
    /// <summary>
    /// Tests that when no encryption key is provided, the connection string is returned without encryption settings.
    /// </summary>
    [Fact]
    public void GetDatabaseConnectionString_NoEncryptionKey_ReturnsUnencryptedConnectionString()
    {
        // Arrange
        var service = new SqlLiteDbEncryptionService(null);

        // Act
        var connectionString = service.GetDatabaseConnectionString();

        // Assert
        Assert.Contains("Data Source=app.db", connectionString);
        Assert.DoesNotContain("Password=", connectionString);
    }

    /// <summary>
    /// Tests that when an encryption key is provided, the connection string includes the encryption password.
    /// </summary>
    [Fact]
    public void GetDatabaseConnectionString_WithEncryptionKey_ReturnsEncryptedConnectionString()
    {
        // Arrange
        var encryptionKey = "testEncryptionKey";
        var service = new SqlLiteDbEncryptionService(encryptionKey);

        // Act
        var connectionString = service.GetDatabaseConnectionString();

        // Assert
        Assert.Contains("Data Source=app.db", connectionString);
        Assert.Contains($"Password={encryptionKey}", connectionString);
    }
}