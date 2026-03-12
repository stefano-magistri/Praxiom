using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class NavigationTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task Navigazione_ShouldMatchSeleniumFlow()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        await page.GetByTestId("patients-nav-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Pazienti");

        await page.GetByTestId("create-patient-page-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Nuovo paziente");

        await page.GetByTestId("therapy-templates-nav-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Terapie Precompilate");

        await page.GetByTestId("create-therapy-template-page-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Aggiungi Terapia Precompilata");

        await page.GetByTestId("guide-nav-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Guida");

        await page.GetByTestId("settings-nav-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Impostazioni");
    }
}
