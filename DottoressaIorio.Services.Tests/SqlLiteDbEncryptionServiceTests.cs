namespace DottoressaIorio.Services.Tests;

public class SqlLiteDbEncryptionServiceTests
{
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