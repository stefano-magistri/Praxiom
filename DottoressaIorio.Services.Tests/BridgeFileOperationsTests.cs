using DottoressaIorio.DbUpgradeBridge;

namespace DottoressaIorio.Services.Tests;

public sealed class BridgeFileOperationsTests : IDisposable
{
    private readonly string _tempDir;

    public BridgeFileOperationsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DottoressaIorio.BridgeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void TryBackupCurrentDbIfPresent_ReturnsTrue_WhenSourceDoesNotExist()
    {
        var appDbPath = Path.Combine(_tempDir, "app.db");
        var backupDbPath = Path.Combine(_tempDir, "app.db.pre_bridge_backup");

        var result = BridgeFileOperations.TryBackupCurrentDbIfPresent(appDbPath, backupDbPath);

        Assert.True(result);
        Assert.False(File.Exists(backupDbPath));
    }

    [Fact]
    public void TryBackupCurrentDbIfPresent_MovesSourceToBackup_WhenSourceExists()
    {
        var appDbPath = Path.Combine(_tempDir, "app.db");
        var backupDbPath = Path.Combine(_tempDir, "app.db.pre_bridge_backup");
        File.WriteAllText(appDbPath, "current-db");

        var result = BridgeFileOperations.TryBackupCurrentDbIfPresent(appDbPath, backupDbPath);

        Assert.True(result);
        Assert.False(File.Exists(appDbPath));
        Assert.True(File.Exists(backupDbPath));
        Assert.Equal("current-db", File.ReadAllText(backupDbPath));
    }

    [Fact]
    public void TryPromoteMigratedDb_OverwritesExistingDestination()
    {
        var tempDbPath = Path.Combine(_tempDir, "app.db.bridge_migrating");
        var appDbPath = Path.Combine(_tempDir, "app.db");
        File.WriteAllText(tempDbPath, "migrated-db");
        File.WriteAllText(appDbPath, "existing-db");

        var result = BridgeFileOperations.TryPromoteMigratedDb(tempDbPath, appDbPath);

        Assert.True(result);
        Assert.False(File.Exists(tempDbPath));
        Assert.Equal("migrated-db", File.ReadAllText(appDbPath));
    }

    [Fact]
    public void TryArchiveLegacyBackup_RenamesLegacyBackup_WhenSourceExists()
    {
        var legacyBackupPath = Path.Combine(_tempDir, "app.db");
        File.WriteAllText(legacyBackupPath, "legacy-db");

        var result = BridgeFileOperations.TryArchiveLegacyBackup(legacyBackupPath, out var archivedPath);

        Assert.True(result);
        Assert.False(File.Exists(legacyBackupPath));
        Assert.True(File.Exists(archivedPath));
        Assert.Contains(".migrated.", archivedPath);
        Assert.Equal("legacy-db", File.ReadAllText(archivedPath));
    }

    [Fact]
    public void TryArchiveLegacyBackup_ReturnsTrue_WhenSourceDoesNotExist()
    {
        var legacyBackupPath = Path.Combine(_tempDir, "missing-app.db");

        var result = BridgeFileOperations.TryArchiveLegacyBackup(legacyBackupPath, out var archivedPath);

        Assert.True(result);
        Assert.Equal(string.Empty, archivedPath);
    }

    public void Dispose()
    {
        if (!Directory.Exists(_tempDir))
            return;

        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // Cleanup best effort for transient file locks on CI.
        }
    }
}
