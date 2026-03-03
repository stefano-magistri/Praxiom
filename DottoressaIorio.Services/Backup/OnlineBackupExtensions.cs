using Microsoft.Extensions.DependencyInjection;

namespace DottoressaIorio.Services.Backup;

public static class OnlineBackupExtensions
{
    public static IServiceCollection AddOnlineSqliteBackup(this IServiceCollection services, Action<OnlineBackupOptions>? configure = null)
    {
        var options = new OnlineBackupOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IOnlineBackupStateStore, FileOnlineBackupStateStore>();
        services.AddSingleton<ISqliteBackupConnectionFactory, SqliteBackupConnectionFactory>();
        services.AddSingleton<IOnlineBackupService, OnlineBackupService>();
        return services;
    }
}
