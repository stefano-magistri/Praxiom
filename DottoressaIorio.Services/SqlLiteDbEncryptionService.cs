using DottoressaIorio.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Services;

public class SqlLiteDbEncryptionService
{
    private const string EncryptedDbPath = "app.db";
    private const string UnencryptedDbPath = "app_unencrypted.db";
    private readonly string _connectionString;
    private readonly string _key;

    public SqlLiteDbEncryptionService(string key)
    {
        _key = key;
        _connectionString = $"Data Source={EncryptedDbPath};Password={key};Pooling=false";
    }

    public string GetDatabaseConnectionString()
    {
        SqliteConnectionStringBuilder connectionStringBuilder;
        if (string.IsNullOrEmpty(_key))
            connectionStringBuilder = new SqliteConnectionStringBuilder($"Data Source={EncryptedDbPath}")
            {
                Mode = SqliteOpenMode.ReadWriteCreate
            };
        else
            connectionStringBuilder = new SqliteConnectionStringBuilder($"Data Source={EncryptedDbPath}")
            {
                Mode = SqliteOpenMode.ReadWriteCreate,
                Password = _key // Replace with your desired password
            };
        return connectionStringBuilder.ToString();
    }

    // Method to encrypt existing database if not encrypted
    public void EncryptDatabaseIfNotExists()
    {
        if (string.IsNullOrEmpty(_key)) return; // If encrypted key not exists, return

        if (!File.Exists(EncryptedDbPath)) return; // If encrypted database not exists, return

        // Check if the existing database is encrypted
        var isExistingDbEncrypted = IsDatabaseEncrypted();

        // If existing database is not encrypted, export data to the encrypted database
        if (!isExistingDbEncrypted)
        {
            // Rename the encrypted database file to the original database file name
            File.Move(EncryptedDbPath, UnencryptedDbPath);

            // Export data from existing database to the encrypted database
            ExportDataToEncryptedDatabase();

            // Delete the old unencrypted database file
            if (File.Exists(UnencryptedDbPath)) File.Delete(UnencryptedDbPath);
        }
    }

    // Method to export data from existing database to encrypted database
    private void ExportDataToEncryptedDatabase()
    {
        // Create a new encrypted database file
        using var encryptedDbContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connectionString)
            .Options);

        // Apply EF Core migrations to create database schema in the encrypted database
        encryptedDbContext.Database.Migrate();

        using var existingDbContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={UnencryptedDbPath};Pooling=false").Options);

        // Get all DbSet properties of ApplicationDbContext
        var dbSetProperties = typeof(ApplicationDbContext).GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToList();

        // Migrate data from existing database to the encrypted database for each DbSet
        foreach (var entity in dbSetProperties
                     .Select(dbSetProperty => (IEnumerable<object>)dbSetProperty.GetValue(existingDbContext))
                     .SelectMany(dbSet => dbSet))
            encryptedDbContext.Add(entity);

        encryptedDbContext.SaveChanges();
    }

    // Method to check if database is encrypted
    private bool IsDatabaseEncrypted()
    {
        using var connection = new SqliteConnection($"Data Source={EncryptedDbPath};Pooling=false");
        try
        {
            connection.Open();
            // Attempt to execute a query to determine if the database is encrypted
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
            command.ExecuteScalar();
            return false; // If the query executed successfully, the database is not encrypted
        }
        catch (SqliteException ex)
        {
            if (ex.SqliteErrorCode == 26) // Error code 26 indicates that the database is encrypted
                return true;
            throw; // Re-throw the exception if it's not due to encryption
        }
        finally
        {
            // Close the connection before renaming the file
            connection.Close();
        }
    }
}