using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class PdfDownloadTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task DownloadTherapyPdf_ShouldOpenPdfInNewTab()
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

        var popupTask = page.WaitForPopupAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Scarica ultima terapia" }).ClickAsync();
        var popup = await popupTask;
        
        var frame = popup.Locator("iframe[title='therapy-pdf']");
        await Expect(frame).ToHaveCountAsync(1);
        var src = await frame.GetAttributeAsync("src");
        Assert.NotNull(src);
        Assert.StartsWith("data:application/pdf;base64,", src, StringComparison.OrdinalIgnoreCase);
        await popup.CloseAsync();
    }
}
