using DottoressaIorio.Services.Backup;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;

namespace DottoressaIorio.Services.Tests;

public class OnlineBackupServiceIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task TriggerBackupAsync_CreatesConsistentBackupFromLiveDatabase()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "dottoressa-online-backup-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var sourceDbPath = Path.Combine(tempRoot, "source.db");
            await SeedSourceDatabaseAsync(sourceDbPath);

            var options = new OnlineBackupOptions
            {
                AppRootPath = tempRoot,
                RetentionCount = 10,
                DebounceWindow = TimeSpan.FromMinutes(15)
            };

            var stateStore = new FileOnlineBackupStateStore(options);
            var connectionFactory = new TestSqliteBackupConnectionFactory(sourceDbPath);
            var service = new OnlineBackupService(connectionFactory, stateStore, options, NullLogger<OnlineBackupService>.Instance);

            var result = await service.TriggerBackupAsync("integration-test");

            Assert.Equal(OnlineBackupOutcome.Succeeded, result.Outcome);
            Assert.NotNull(result.BackupPath);
            Assert.True(File.Exists(result.BackupPath));

            await using var backupConnection = new SqliteConnection($"Data Source={result.BackupPath};Pooling=False");
            await backupConnection.OpenAsync();
            await using var countCommand = backupConnection.CreateCommand();
            countCommand.CommandText = "SELECT COUNT(*) FROM Patients;";
            var count = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

            Assert.Equal(1, count);
            Assert.True(File.Exists(options.LastBackupFilePath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                try
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
                catch (IOException)
                {
                    // Best effort cleanup for transient SQLite file locks.
                }
            }
        }
    }

    private static async Task SeedSourceDatabaseAsync(string sourceDbPath)
    {
        await using var connection = new SqliteConnection($"Data Source={sourceDbPath};Pooling=False");
        await connection.OpenAsync();

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = "CREATE TABLE Patients (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);";
        await createCommand.ExecuteNonQueryAsync();

        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO Patients (Name) VALUES ('Alice');";
        await insertCommand.ExecuteNonQueryAsync();
    }

    private sealed class TestSqliteBackupConnectionFactory(string sourceDbPath) : ISqliteBackupConnectionFactory
    {
        public SqliteConnection CreateSourceConnection()
        {
            var connection = new SqliteConnection($"Data Source={sourceDbPath};Pooling=False");
            connection.Open();
            return connection;
        }

        public SqliteConnection CreateDestinationConnection(string destinationPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            var connection = new SqliteConnection($"Data Source={destinationPath};Pooling=False");
            connection.Open();
            return connection;
        }
    }
}
