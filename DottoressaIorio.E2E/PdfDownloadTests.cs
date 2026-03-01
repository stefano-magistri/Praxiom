using System.Collections.Concurrent;
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
        var setup = await NewAuthorizedPageAsync();
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

        var matchedPdfUrls = new ConcurrentQueue<string>();
        void OnResponse(object? _, IResponse response)
        {
            if (response.Url.Contains("/pdf/therapy/", StringComparison.OrdinalIgnoreCase) && response.Status == 200)
            {
                matchedPdfUrls.Enqueue(response.Url);
            }
        }

        context.Response += OnResponse;

        var popupTask = page.WaitForPopupAsync();
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Scarica ultima terapia" })
            .ClickAsync();
        var popup = await popupTask;

        try
        {
            for (var i = 0; i < 120; i++)
            {
                if (!matchedPdfUrls.IsEmpty)
                {
                    break;
                }

                await page.WaitForTimeoutAsync(250);
            }
        }
        finally
        {
            context.Response -= OnResponse;
        }

        Assert.False(matchedPdfUrls.IsEmpty, $"No /pdf/therapy response captured. Popup URL: {popup.Url}");
        Assert.All(matchedPdfUrls, url => Assert.Contains("/pdf/therapy/", url, StringComparison.OrdinalIgnoreCase));

        await popup.CloseAsync();
    }
}
