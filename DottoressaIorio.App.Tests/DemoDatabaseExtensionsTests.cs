using DottoressaIorio.App.Middleware;

namespace DottoressaIorio.App.Tests;

public class DemoDatabaseExtensionsTests
{
    [Theory]
    [InlineData(true, "app_old.db")]
    [InlineData(false, "app_unencrypted.db")]
    public void ResolveDemoDatabasePath_WhenPublishedDemoDatabaseExists_UsesContentRootDemoFolder(
        bool useEncryption,
        string expectedFileName)
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var publishedDemoPath = Path.Combine(tempDir, "Demo", expectedFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(publishedDemoPath)!);
            File.WriteAllText(publishedDemoPath, "demo");

            var result = DemoDatabaseExtensions.ResolveDemoDatabasePath(tempDir, useEncryption);

            Assert.Equal(publishedDemoPath, result);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Theory]
    [InlineData(true, "app_old.db")]
    [InlineData(false, "app_unencrypted.db")]
    public void ResolveDemoDatabasePath_WhenPublishedDemoDatabaseMissing_FallsBackToLegacyRelativePath(
        bool useEncryption,
        string expectedFileName)
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var result = DemoDatabaseExtensions.ResolveDemoDatabasePath(tempDir, useEncryption);

            Assert.Equal(
                Path.GetFullPath(Path.Combine(tempDir, "..", "Demo", expectedFileName)),
                result);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public void IsReadableSqliteDatabase_WhenDatabaseIsNotSqlite_ReturnsFalse()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var invalidDbPath = Path.Combine(tempDir, "app_unencrypted.db");
            File.WriteAllText(invalidDbPath, "not-a-database");

            var method = typeof(DemoDatabaseExtensions).GetMethod(
                "IsReadableSqliteDatabase",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var result = (bool)method!.Invoke(null, new object[] { invalidDbPath })!;

            Assert.False(result);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "DottoressaIorio.DemoDbTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup for transient file locks.
        }
    }
}
