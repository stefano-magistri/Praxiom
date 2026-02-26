using Microsoft.Playwright;

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

        _browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Settings.Headless
        });
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
}
