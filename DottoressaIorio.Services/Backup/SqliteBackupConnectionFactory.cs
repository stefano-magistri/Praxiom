using DottoressaIorio.Services.SqlLiteEncryption;
using Microsoft.Data.Sqlite;

namespace DottoressaIorio.Services.Backup;

public sealed class SqliteBackupConnectionFactory(SqlLiteDbEncryptionService encryptionService)
    : ISqliteBackupConnectionFactory
{
    public SqliteConnection CreateSourceConnection()
    {
        return encryptionService.CreateConnection();
    }

    public SqliteConnection CreateDestinationConnection(string destinationPath)
    {
        return encryptionService.CreateConnectionForPath(destinationPath);
    }
}
