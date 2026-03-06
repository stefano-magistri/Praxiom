using System.Diagnostics;
using System.Text;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace DottoressaIorio.Services.Tests;

public sealed class DbUpgradeBridgeIntegrationTests : IDisposable
{
    private readonly string _tempDir;

    static DbUpgradeBridgeIntegrationTests()
    {
        raw.SetProvider(new SQLite3Provider_e_sqlcipher());
        Batteries_V2.Init();
    }

    public DbUpgradeBridgeIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DottoressaIorio.DbUpgradeBridge.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void Bridge_MigratesLegacyBackup_ToEncryptedAppDb()
    {
        var appRoot = Path.Combine(_tempDir, "app-root");
        var legacyRoot = Path.Combine(_tempDir, "legacy-root");
        Directory.CreateDirectory(appRoot);
        Directory.CreateDirectory(legacyRoot);

        var encryptionKey = "bridge-test-key";
        var legacyDbPath = Path.Combine(legacyRoot, "app.db");
        var appDbPath = Path.Combine(appRoot, "app.db");

        File.WriteAllText(
            Path.Combine(appRoot, "appsettings.json"),
            $$"""
              {
                "EncryptionKey": "{{encryptionKey}}"
              }
              """);

        CreateLegacyEncryptedDatabase(legacyDbPath, encryptionKey);

        var bridgeAssemblyPath = typeof(DottoressaIorio.DbUpgradeBridge.BridgeFileOperations).Assembly.Location;
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{bridgeAssemblyPath}\" --app-root \"{appRoot}\" --legacy-db-path \"{legacyDbPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0, $"Bridge exit code was {process.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}");
        Assert.True(File.Exists(appDbPath));
        Assert.DoesNotContain("SQLite format 3", ReadAsciiHeader(appDbPath));
        Assert.False(File.Exists(legacyDbPath));
        Assert.Single(Directory.GetFiles(legacyRoot, "app.db.migrated.*"));

        using (var keyedConnection = new SqliteConnection($"Data Source={appDbPath};Mode=ReadWrite;Pooling=false"))
        {
            keyedConnection.Open();
            using var keyCommand = keyedConnection.CreateCommand();
            keyCommand.CommandText = $"PRAGMA key = '{encryptionKey}';";
            keyCommand.ExecuteNonQuery();

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
            // Best effort cleanup for transient file locks.
        }
    }

    private static void CreateLegacyEncryptedDatabase(string dbPath, string encryptionKey)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
            Password = encryptionKey
        }.ToString();

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS Patients (Id INTEGER PRIMARY KEY, FirstName TEXT NOT NULL);";
        command.ExecuteNonQuery();
    }

    private static string ReadAsciiHeader(string path)
    {
        using var stream = File.OpenRead(path);
        var buffer = new byte[16];
        _ = stream.Read(buffer, 0, buffer.Length);
        return Encoding.ASCII.GetString(buffer);
    }
}
