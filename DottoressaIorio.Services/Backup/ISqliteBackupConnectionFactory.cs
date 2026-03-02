using Microsoft.Data.Sqlite;

namespace DottoressaIorio.Services.Backup;

public interface ISqliteBackupConnectionFactory
{
    SqliteConnection CreateSourceConnection();
    SqliteConnection CreateDestinationConnection(string destinationPath);
}
