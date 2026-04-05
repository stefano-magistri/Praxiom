using System.Diagnostics;
using System.Runtime.InteropServices;
using DottoressaIorio.Models;
using DottoressaIorio.Repositories;
using DottoressaIorio.Services.Licensing;
using Microsoft.Playwright;

namespace DottoressaIorio.Services.Pdf;

/// <summary>
/// Generates therapy PDFs by rendering HTML through Playwright Chromium.
/// </summary>
public class PdfService(PdfHtmlBuilder builder, IClinicSettingsRepository clinicSettingsRepository, ILicenseService licenseService)
{
    /// <summary>
    /// Generates the printable therapy PDF for the supplied therapy.
    /// </summary>
    public async Task<byte[]> GenerateTherapyPdfAsync(Therapy therapy)
    {
        ConfigurePlaywrightEnvironment();
        using var playwright = await Playwright.CreateAsync();

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

    /// <summary>
    /// Builds the HTML snippet used for the PDF header template.
    /// </summary>
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
        // The footer shows the trial marker, so PDF generation must observe the same cached license state as the app shell.
        await licenseService.InitializeAsync();
        return builder.BuildFooterHtml(await File.ReadAllTextAsync(footerPath), therapy, settings, licenseService.IsTrial);
    }

    internal static void ConfigurePlaywrightEnvironment()
    {
        ConfigurePlaywrightDriverSearchPath();
        ConfigurePlaywrightBrowsersPath();
    }

    internal static void ConfigurePlaywrightDriverSearchPath()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH")))
            return;

        var bundledDriverPath = AppContext.BaseDirectory;
        var bundledNodePath = Path.Combine(bundledDriverPath, ".playwright", "node", "win32_x64", "node.exe");

        if (!File.Exists(bundledNodePath))
            return;

        Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", bundledDriverPath);
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

        var playwrightPath = GetDefaultPlaywrightBrowsersPath();
        if (playwrightPath is null)
            return;

        Directory.CreateDirectory(playwrightPath);
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", playwrightPath);
    }

    internal static string? GetDefaultPlaywrightBrowsersPath()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return null;

        if (IsAzureEnvironment())
        {
            var homePath = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrWhiteSpace(homePath))
            {
                // Azure App Service needs a writable shared location instead of ProgramData.
                return Path.Combine(homePath, "ms-playwright");
            }
        }

        if (IsProductionEnvironment())
        {
            // Production runs as an installed app or service, so browsers live in ProgramData rather than a user profile cache.
            return @"C:\ProgramData\Praxiom\ms-playwright";
        }

        return null;
    }

    internal static bool IsAzureEnvironment()
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                              ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        return string.Equals(environmentName, "Azure", StringComparison.OrdinalIgnoreCase);
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
        var command = GetPlaywrightInstallCommand(baseDir);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command.FileName,
                Arguments = command.Arguments,
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

    internal static (string FileName, string Arguments) GetPlaywrightInstallCommand(string baseDir)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var scriptPath = Path.Combine(baseDir, "playwright.ps1");
            var shellName = IsCommandAvailable("pwsh") ? "pwsh" : "powershell";
            return (shellName, $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" install chromium");
        }

        return ("bash", $"\"{Path.Combine(baseDir, "playwright.sh")}\" install chromium");
    }

    internal static bool IsCommandAvailable(string commandName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
            return false;

        var extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[] { string.Empty, ".exe", ".cmd", ".bat" }
            : new[] { string.Empty };

        foreach (var pathEntry in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(pathEntry.Trim(), commandName + extension);
                if (File.Exists(candidate))
                    return true;
            }
        }

        return false;
    }
}
