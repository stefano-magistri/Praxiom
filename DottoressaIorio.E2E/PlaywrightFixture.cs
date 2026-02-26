using Microsoft.Playwright;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DottoressaIorio.E2E;

[CollectionDefinition(Name)]
public sealed class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
    public const string Name = "playwright";
}

public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IBrowser? _browser;

    public IPlaywright Playwright { get; private set; } = null!;

    public E2eSettings Settings { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Settings = E2eSettings.Load();
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await LaunchBrowserWithInstallFallbackAsync();
    }

    public async Task<IBrowserContext> NewContextAsync()
    {
        if (_browser is null)
        {
            throw new InvalidOperationException("Browser is not initialized.");
        }

        return await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = Settings.BaseUrl,
            IgnoreHTTPSErrors = true,
            AcceptDownloads = true
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        Playwright.Dispose();
    }

    private async Task<IBrowser> LaunchBrowserWithInstallFallbackAsync()
    {
        try
        {
            return await LaunchBrowserAsync();
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
        {
            await InstallPlaywrightBrowsersAsync();
            return await LaunchBrowserAsync();
        }
    }

    private Task<IBrowser> LaunchBrowserAsync()
    {
        return Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Settings.Headless
        });
    }

    private static async Task InstallPlaywrightBrowsersAsync()
    {
        var baseDir = AppContext.BaseDirectory;
        string fileName;
        string arguments;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            fileName = "pwsh";
            arguments = $"\"{Path.Combine(baseDir, "playwright.ps1")}\" install chromium";
        }
        else
        {
            fileName = "bash";
            arguments = $"\"{Path.Combine(baseDir, "playwright.sh")}\" install chromium";
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to install Playwright browsers. Exit code: {process.ExitCode}. {error}".Trim());
        }
    }
}
