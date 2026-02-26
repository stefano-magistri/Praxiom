using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class NavigationTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task Navigazione_ShouldMatchSeleniumFlow()
    {
        var setup = await NewAuthorizedPageAsync("/");
        await using var context = setup.Context;
        var page = setup.Page;

        await page.Locator(".navbar-brand").ClickAsync();

        await page.GetByTestId("patients-nav-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Pazienti");

        await page.GetByTestId("create-patient-nav-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Nuovo Paziente");

        await page.GetByTestId("therapy-templates-nav-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Terapie Precompilate");

        await page.GetByTestId("create-therapy-template-nav-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Aggiungi Terapia Precompilata");

        await page.GetByTestId("guide-nav-link").ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Guida");
    }
}
