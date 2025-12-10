using DottoressaIorio.Models;
using DottoressaIorio.Services.Pdf;
using Microsoft.Playwright;

public class PdfService(PdfHtmlBuilder builder)
{
    public async Task<byte[]> GenerateTherapyPdfAsync(Therapy therapy)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var page = await browser.NewPageAsync();

        string html = builder.BuildHtml(therapy);
        await page.SetContentAsync(html);

        var header = builder.BuildHeaderHtml(await File.ReadAllTextAsync("_pdf_therapy/header.html"), therapy);
        var footer = builder.BuildFooterHtml(await File.ReadAllTextAsync("_pdf_therapy/footer.html"), therapy);

        return await page.PdfAsync(new PagePdfOptions
        {
            Format = "A4",
            DisplayHeaderFooter = true,
            HeaderTemplate = header,
            FooterTemplate = footer,
            Margin = new() { Top = "80px", Bottom = "60px" }
        });
    }
}