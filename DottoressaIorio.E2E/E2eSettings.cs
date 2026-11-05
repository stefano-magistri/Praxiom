using Microsoft.Extensions.Configuration;

namespace DottoressaIorio.E2E;

/// <summary>
/// Stores the configurable settings used by the E2E test host and browser session.
/// </summary>
public sealed class E2eSettings
{
    public string BaseUrl { get; init; } = "https://127.0.0.1:0";
    public bool Headless { get; init; } = true;
    public LoginSettings Login { get; init; } = new();

    /// <summary>
    /// Loads E2E settings from appsettings and environment variables.
    /// </summary>
    public static E2eSettings Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables()
            .Build();

        var settings = configuration.GetSection("E2E").Get<E2eSettings>() ?? new E2eSettings();

        return new E2eSettings
        {
            BaseUrl = (settings.BaseUrl ?? string.Empty).TrimEnd('/'),
            Headless = settings.Headless,
            Login = settings.Login ?? new LoginSettings()
        };
    }
}

/// <summary>
/// Optional UI login credentials for environments that expose a form-based sign-in step.
/// </summary>
public sealed class LoginSettings
{
    public string? Username { get; init; }
    public string? Password { get; init; }
}
