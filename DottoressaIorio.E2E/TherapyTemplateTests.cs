using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class TherapyTemplateTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task CrudTerapiaPrecompilata_ShouldCreateEditAndDelete()
    {
        var setup = await NewAuthorizedPageAsync("/therapy-templates");
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var addedTitle = $"AAA ADD TEST {seed}";
        var editedTitle = $"AAA MODIFICA TEST {seed}";
        var addedTitleDisplayed = FormatTitleCase(addedTitle);
        var editedTitleDisplayed = FormatTitleCase(editedTitle);
        var addedDescription = $"AGGIUNGI TEST Descrizione: {seed}";
        var editedDescription = $"MODIFICA TEST Descrizione: {seed}";

        await page.GetByTestId("create-therapy-template-nav-link").ClickAsync();
        await FillBoundFieldAsync(page.Locator("#Title"), addedTitle);
        await FillBoundFieldAsync(page.Locator("#Description"), addedDescription);
        await SubmitTemplateFormAsync(page);

        await FilterTemplatesAsync(page, addedTitle);
        var addedRow = page.Locator($"//tbody/tr[contains(., '{addedTitleDisplayed}')]").First;
        await Expect(addedRow).ToBeVisibleAsync();

        await addedRow.Locator("button").First.ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Modifica Terapia Precompilata");

        await FillBoundFieldAsync(page.Locator("#Title"), editedTitle);
        await FillBoundFieldAsync(page.Locator("#Description"), editedDescription);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Terapie Precompilate");

        await FilterTemplatesAsync(page, editedTitle);
        var editedRow = page.Locator($"//tbody/tr[contains(., '{editedTitleDisplayed}')]").First;
        await Expect(editedRow).ToBeVisibleAsync();

        await editedRow.Locator("button").Nth(1).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Terapia Precompilata");

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rimuovi" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Terapie Precompilate");

        await FilterTemplatesAsync(page, editedTitle);
        await Expect(page.Locator($"//tbody/tr[contains(., '{editedTitleDisplayed}')]")).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task AggiungiTerapiaPrecompilata_ShouldCreateTemplate()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var title = $"ADD TEMPLATE {seed}";
        var titleDisplayed = FormatTitleCase(title);
        var description = $"Template description {seed}";

        await page.GetByTestId("create-therapy-template-nav-link").ClickAsync();
        await FillBoundFieldAsync(page.Locator("#Title"), title);
        await FillBoundFieldAsync(page.Locator("#Description"), description);
        await SubmitTemplateFormAsync(page);

        await FilterTemplatesAsync(page, title);
        await Expect(page.Locator($"//tbody/tr[contains(., '{titleDisplayed}')]").First).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task AggiungiTerapiaPrecompilata_DuplicateTitle_ShouldStayOnCreateAndShowWarning()
    {
        var setup = await NewAuthorizedPageAsync("/create-therapy-template");
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var title = $"DUP TEMPLATE {seed}";
        var description = $"Template description {seed}";

        await FillBoundFieldAsync(page.Locator("#Title"), title);
        await FillBoundFieldAsync(page.Locator("#Description"), description);
        await SubmitTemplateFormAsync(page);

        await page.GotoAsync("/create-therapy-template", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await WaitForInteractiveReadyAsync(page);
        await FillBoundFieldAsync(page.Locator("#Title"), title);
        await FillBoundFieldAsync(page.Locator("#Description"), $"Another description {seed}");
        await SubmitTemplateFormExpectDuplicateAsync(page);

        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Aggiungi Terapia Precompilata");
    }
}
