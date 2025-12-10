using DottoressaIorio.Models;
using Microsoft.Playwright;

namespace DottoressaIorio.Services.Pdf;

public class PdfService(PdfHtmlBuilder builder)
{
    public async Task<byte[]> GenerateTherapyPdfAsync(Therapy therapy)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var page = await browser.NewPageAsync();

        var html = builder.BuildHtml(therapy);
        await page.SetContentAsync(html);

        var header = builder.BuildHeaderHtml(await File.ReadAllTextAsync("_pdf_therapy/header.html"), therapy);
        var footer = builder.BuildFooterHtml(await File.ReadAllTextAsync("_pdf_therapy/footer.html"), therapy);

        return await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            PrintBackground = true,
            Margin = new Margin
            {
                Top = "170px",
                Bottom = "60px",
                Left = "15px",
                Right = "15px"
            },
            DisplayHeaderFooter = true,
            HeaderTemplate = header,
            FooterTemplate = footer
        });
    }
}