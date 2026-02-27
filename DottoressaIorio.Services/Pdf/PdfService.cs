using System.Runtime.InteropServices;
using DottoressaIorio.Models;
using Microsoft.Playwright;

namespace DottoressaIorio.Services.Pdf;

public class PdfService(PdfHtmlBuilder builder)
{
    public async Task<byte[]> GenerateTherapyPdfAsync(Therapy therapy)
    {
        ConfigurePlaywrightBrowsersPath();

        using var playwright = await Playwright.CreateAsync();
        await using var browser =
            await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        var page = await browser.NewPageAsync();

        var html = builder.BuildHtml(therapy);
        await page.SetContentAsync(html);

        var templateRoot = Path.Combine(AppContext.BaseDirectory, "_pdf_therapy");
        var headerPath = Path.Combine(templateRoot, "header.html");
        var footerPath = Path.Combine(templateRoot, "footer.html");

        var header = builder.BuildHeaderHtml(await File.ReadAllTextAsync(headerPath), therapy);
        var footer = builder.BuildFooterHtml(await File.ReadAllTextAsync(footerPath), therapy);

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

    private static void ConfigurePlaywrightBrowsersPath()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        const string playwrightPath = @"C:\ProgramData\DottoressaIorio\ms-playwright";
        Directory.CreateDirectory(playwrightPath);
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", playwrightPath);
    }
}