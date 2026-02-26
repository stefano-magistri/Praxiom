using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class PdfDownloadTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task DownloadTherapyPdf_ShouldDownloadNonEmptyPdfFile()
    {
        var setup = await NewAuthorizedPageAsync("/");
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"PdfNome{seed}";
        var lastName = $"PdfCognome{seed}";
        var email = $"{seed}.pdf@example.test";
        var therapyDescription = $"Terapia PDF E2E {seed}";
        const string dateOfBirth = "1987-02-10";

        var patientId = await CreatePatientAsync(page, firstName, lastName, email, dateOfBirth);
        await CreateTherapyAsync(page, patientId, therapyDescription);

        await Expect(page.GetByText(therapyDescription)).ToBeVisibleAsync();

        var downloadTask = page.WaitForDownloadAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Scarica ultima terapia" }).ClickAsync();
        var download = await downloadTask;

        Assert.EndsWith(".pdf", download.SuggestedFilename, StringComparison.OrdinalIgnoreCase);

        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf");
        await download.SaveAsAsync(tempFile);
        var fileInfo = new FileInfo(tempFile);
        Assert.True(fileInfo.Exists, "PDF file was not saved.");
        Assert.True(fileInfo.Length > 0, "Downloaded PDF is empty.");
    }
}
