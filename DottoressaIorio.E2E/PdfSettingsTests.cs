using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using UglyToad.PdfPig;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class PdfSettingsTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task UpdatingSettings_ShouldBeReflectedInGeneratedPdfHeader()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var doctorName = $"Dott Test {seed}";
        var canceledDoctorName = $"Canceled {seed}";
        var phone = $"555{seed[..4]}";
        var email = $"pdf-settings-{seed}@example.test";
        var footerText = $"Footer PDF {seed}";

        await page.GetByTestId("settings-nav-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Impostazioni");

        var doctorNameInput = page.Locator("#DoctorName");
        var phoneInput = page.Locator("#Phone");
        var emailInput = page.Locator("#Email");
        var footerTextInput = page.Locator("#FooterText");
        var originalDoctorName = await doctorNameInput.InputValueAsync();

        await Expect(page.GetByTestId("license-status-value")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("license-status-value")).ToHaveTextAsync("Licenziato");
        await Expect(page.GetByText("Stato licenza", new PageGetByTextOptions { Exact = true })).ToHaveCountAsync(1);
        await Expect(page.GetByText("Limiti", new PageGetByTextOptions { Exact = true })).ToHaveCountAsync(0);
        await Expect(page.GetByText("Piè di pagina PDF", new PageGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(doctorNameInput).ToHaveAttributeAsync("readonly", string.Empty);
        await Expect(phoneInput).ToHaveAttributeAsync("readonly", string.Empty);
        await Expect(emailInput).ToHaveAttributeAsync("readonly", string.Empty);
        await Expect(footerTextInput).ToHaveAttributeAsync("readonly", string.Empty);
        await Expect(doctorNameInput).ToHaveClassAsync(new Regex("settings-readonly-field"));
        Assert.Equal("default", await doctorNameInput.EvaluateAsync<string>("element => getComputedStyle(element).cursor"));
        await Expect(page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Modifica" })).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Modifica" }).ClickAsync();
        await Expect(doctorNameInput).Not.ToHaveAttributeAsync("readonly", string.Empty);
        await Expect(footerTextInput).Not.ToHaveAttributeAsync("readonly", string.Empty);
        await Expect(page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Annulla" })).ToBeVisibleAsync();

        await FillBoundFieldAsync(doctorNameInput, canceledDoctorName);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Annulla" }).ClickAsync();
        await Expect(doctorNameInput).ToHaveAttributeAsync("readonly", string.Empty);
        Assert.Equal(originalDoctorName, await doctorNameInput.InputValueAsync());

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Modifica" }).ClickAsync();
        await Expect(doctorNameInput).Not.ToHaveAttributeAsync("readonly", string.Empty);
        await FillBoundFieldAsync(doctorNameInput, doctorName);
        await FillBoundFieldAsync(phoneInput, phone);
        await FillBoundFieldAsync(emailInput, email);
        await FillBoundFieldAsync(footerTextInput, footerText);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();
        await Expect(page.GetByText("Impostazioni salvate.")).ToBeVisibleAsync();
        await Expect(doctorNameInput).ToHaveAttributeAsync("readonly", string.Empty);
        await Expect(page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Modifica" })).ToBeVisibleAsync();

        var firstName = $"PdfSetNome{seed}";
        var lastName = $"PdfSetCognome{seed}";
        var patientEmail = $"{seed}.pdfsettings@example.test";
        var therapyDescription = $"PDF settings therapy {seed}";
        var patientId = await CreatePatientAsync(page, firstName, lastName, patientEmail, "1987-02-10");
        await CreateTherapyAsync(page, patientId, therapyDescription);

        var pdfUrlCompletion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnResponse(object? _, IResponse response)
        {
            if (response.Url.Contains("/pdf/therapy/", StringComparison.OrdinalIgnoreCase) && response.Status == 200)
            {
                pdfUrlCompletion.TrySetResult(response.Url);
            }
        }

        context.Response += OnResponse;

        var pdfResponseTask = page.WaitForPopupAsync();
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Stampa" }).First
            .ClickAsync();
        var popup = await pdfResponseTask;

        try
        {
            var pdfUrl = await pdfUrlCompletion.Task.WaitAsync(TimeSpan.FromSeconds(30));
            var pdfBytes = await DownloadPdfAsync(pdfUrl);
            var pdfText = NormalizePdfTextForAssertions(ExtractPdfText(pdfBytes));

            Assert.Contains(NormalizePdfTextForAssertions(doctorName), pdfText, StringComparison.Ordinal);
            Assert.Contains(NormalizePdfTextForAssertions(phone), pdfText, StringComparison.Ordinal);
            Assert.Contains(NormalizePdfTextForAssertions(email), pdfText, StringComparison.Ordinal);
            Assert.Contains(NormalizePdfTextForAssertions(footerText), pdfText, StringComparison.Ordinal);
        }
        finally
        {
            context.Response -= OnResponse;
            await popup.CloseAsync();
        }
    }

    private static async Task<byte[]> DownloadPdfAsync(string pdfUrl)
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler);
        return await client.GetByteArrayAsync(pdfUrl);
    }

    private static string ExtractPdfText(byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        using var document = PdfDocument.Open(stream);
        return string.Join(Environment.NewLine, document.GetPages().Select(page => page.Text));
    }

    private static string NormalizePdfTextForAssertions(string value)
    {
        return value.Replace("ﬀ", "ff", StringComparison.Ordinal);
    }
}
