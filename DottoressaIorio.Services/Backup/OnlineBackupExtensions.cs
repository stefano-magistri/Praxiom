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

    public static string? ExtractUsername(string? allowedUsername)
    {
        if (string.IsNullOrWhiteSpace(allowedUsername))
        {
            return null;
        }

        var trimmed = allowedUsername.Trim();
        var backslashIndex = trimmed.LastIndexOf('\\');
        var slashIndex = trimmed.LastIndexOf('/');
        var separatorIndex = Math.Max(backslashIndex, slashIndex);

        return separatorIndex >= 0 && separatorIndex < trimmed.Length - 1
            ? trimmed[(separatorIndex + 1)..]
            : trimmed;
    }
}
