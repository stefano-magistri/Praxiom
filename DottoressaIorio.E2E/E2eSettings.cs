using Microsoft.Extensions.Configuration;

namespace DottoressaIorio.E2E;

public sealed class E2eSettings
{
    public string BaseUrl { get; init; } = "https://localhost:7013";
    public bool Headless { get; init; } = true;
    public LoginSettings Login { get; init; } = new();

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

public sealed class LoginSettings
{
    public string? Username { get; init; }
    public string? Password { get; init; }
}