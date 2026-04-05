using DottoressaIorio.App.Middleware;

namespace DottoressaIorio.App.Tests;

public class DemoDatabaseExtensionsTests
{
    [Fact]
    public void ResolveDemoDatabasePath_WhenPublishedDemoDatabaseExists_UsesContentRootDemoFolder()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var publishedDemoPath = Path.Combine(tempDir, "Demo", "app_old.db");
            Directory.CreateDirectory(Path.GetDirectoryName(publishedDemoPath)!);
            File.WriteAllText(publishedDemoPath, "demo");

            var result = DemoDatabaseExtensions.ResolveDemoDatabasePath(tempDir);

            Assert.Equal(publishedDemoPath, result);
        }
        finally
        {
            DeleteDirectoryIfExists(tempDir);
        }
    }

    [Fact]
    public void ResolveDemoDatabasePath_WhenPublishedDemoDatabaseMissing_FallsBackToLegacyRelativePath()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var result = DemoDatabaseExtensions.ResolveDemoDatabasePath(tempDir);

            Assert.Equal(
                Path.GetFullPath(Path.Combine(tempDir, "..", "Demo", "app_old.db")),
                result);
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
