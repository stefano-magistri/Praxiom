using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DottoressaIorio.Services.SqlLiteEncryption;

public static class SqlLiteEncryptionExtensions
{
    /// <summary>
    ///     Registers SqlLiteDbEncryptionService and configures ApplicationDbContext
    ///     with either encrypted or unencrypted SQLite.
    /// </summary>
    public static IServiceCollection AddSqlLiteDbEncryption<TContext>(
        this IServiceCollection services,
        string encryptionKey,
        string? databaseRootPath = null)
        where TContext : DbContext
    {
        services.AddSingleton(new SqlLiteDbEncryptionService(encryptionKey, databaseRootPath));

        services.AddDbContext<TContext>((sp, options) =>
        {
            var encryption = sp.GetRequiredService<SqlLiteDbEncryptionService>();
            var connection = encryption.CreateConnection();
            options.UseSqlite(connection);
        });

        return services;
    }
}
