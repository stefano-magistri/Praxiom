namespace DottoressaIorio.DbUpgradeBridge;

public static class BridgeFileOperations
{
    public static bool TryBackupCurrentDbIfPresent(string appDbPath, string backupDbPath)
    {
        if (!File.Exists(appDbPath))
            return true;

        return TryMoveWithRetry(appDbPath, backupDbPath, overwrite: true);
    }

    public static bool TryPromoteMigratedDb(string tempDbPath, string appDbPath)
    {
        return TryMoveWithRetry(tempDbPath, appDbPath, overwrite: true);
    }

    private static bool TryMoveWithRetry(string source, string destination, bool overwrite)
    {
        for (var i = 0; i < 10; i++)
        {
            try
            {
                if (overwrite && File.Exists(destination))
                    File.Delete(destination);

                File.Move(source, destination);
                return true;
            }
            catch (IOException)
            {
                Thread.Sleep(200);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(200);
            }
        }

        return false;
    }
}
