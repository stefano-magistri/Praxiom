using Microsoft.Extensions.Hosting;

namespace DottoressaIorio.App.Middleware;

public static class AppHostingEnvironmentExtensions
{
    public const string AzureEnvironmentName = "Azure";

    public static bool IsAzureEnvironment(this IHostEnvironment environment)
    {
        return string.Equals(
            environment.EnvironmentName,
            AzureEnvironmentName,
            StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveDatabaseRootPath(
        IConfiguration configuration,
        IHostEnvironment environment,
        bool isWindowsService)
    {
        var configuredPath = configuration["DatabaseRootPath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return configuredPath;
        }

        if (isWindowsService)
        {
            return AppContext.BaseDirectory;
        }

        if (environment.IsAzureEnvironment())
        {
            var homePath = configuration["HOME"];
            if (!string.IsNullOrWhiteSpace(homePath))
            {
                return Path.Combine(homePath, "data", "Praxiom");
            }
        }

        return environment.ContentRootPath;
    }
}
