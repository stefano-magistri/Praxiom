using System.Diagnostics;
using System.Runtime.InteropServices;
using DottoressaIorio.Models;
using DottoressaIorio.Repositories;
using Microsoft.Playwright;

namespace DottoressaIorio.Services.Pdf;

public class PdfService(PdfHtmlBuilder builder, IClinicSettingsRepository clinicSettingsRepository)
{
    public async Task<byte[]> GenerateTherapyPdfAsync(Therapy therapy)
    {
        using var playwright = await Playwright.CreateAsync();
        ConfigurePlaywrightBrowsersPath();

        await using var browser = await LaunchBrowserAsync(playwright);

        var page = await browser.NewPageAsync();

        var html = builder.BuildHtml(therapy);
        await page.SetContentAsync(html);

        var templateRoot = Path.Combine(AppContext.BaseDirectory, "_pdf_therapy");
        var header = await BuildHeaderHtmlAsync(therapy, templateRoot);
        var footer = await BuildFooterHtmlAsync(therapy, templateRoot);

        return await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            PrintBackground = true,
            Margin = new Margin
            {
                Top = "235px",
                Bottom = "60px",
                Left = "33px",
                Right = "33px"
            },
            DisplayHeaderFooter = true,
            HeaderTemplate = header,
            FooterTemplate = footer
        });
    }

    public async Task<string> BuildHeaderHtmlAsync(Therapy therapy, string? templateRoot = null)
    {
        var effectiveTemplateRoot = templateRoot ?? Path.Combine(AppContext.BaseDirectory, "_pdf_therapy");
        var headerPath = Path.Combine(effectiveTemplateRoot, "header.html");
        var settings = await clinicSettingsRepository.GetAsync();
        return builder.BuildHeaderHtml(await File.ReadAllTextAsync(headerPath), therapy, settings);
    }

    private async Task<string> BuildFooterHtmlAsync(Therapy therapy, string? templateRoot = null)
    {
        var effectiveTemplateRoot = templateRoot ?? Path.Combine(AppContext.BaseDirectory, "_pdf_therapy");
        var footerPath = Path.Combine(effectiveTemplateRoot, "footer.html");
        var settings = await clinicSettingsRepository.GetAsync();
        return builder.BuildFooterHtml(await File.ReadAllTextAsync(footerPath), therapy, settings);
    }

    private static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright)
    {
        try
        {
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
        catch (PlaywrightException ex) when (!IsProductionEnvironment() &&
                                             ex.Message.Contains(
                                                 "Executable doesn't exist",
                                                 StringComparison.OrdinalIgnoreCase))
        {
            await InstallPlaywrightBrowsersAsync();
            return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }
    }

    private static void ConfigurePlaywrightBrowsersPath()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH")))
            return;

        if (!IsProductionEnvironment())
            return;

        const string playwrightPath = @"C:\ProgramData\DottoressaIorio\ms-playwright";
        Directory.CreateDirectory(playwrightPath);
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", playwrightPath);
    }

    private static bool IsProductionEnvironment()
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        return string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);
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
            throw new InvalidOperationException(
                $"Failed to install Playwright browsers. Exit code: {process.ExitCode}. {error}".Trim());
        }
    }
}
