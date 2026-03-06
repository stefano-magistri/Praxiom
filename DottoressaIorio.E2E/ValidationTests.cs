using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class ValidationTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task CreatePatient_EmptyRequiredFields_ShouldShowValidation()
    {
        var setup = await NewAuthorizedPageAsync("/create-patient");
        await using var context = setup.Context;
        var page = setup.Page;

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();
        var validationTexts = await page.Locator(".validation-message").AllInnerTextsAsync();
        Assert.Contains(validationTexts, text => text.Contains("Nome", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validationTexts, text => text.Contains("Cognome", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task CreateTemplate_EmptyDescription_ShouldShowValidation()
    {
        var setup = await NewAuthorizedPageAsync("/create-therapy-template");
        await using var context = setup.Context;
        var page = setup.Page;

        await FillBoundFieldAsync(page.Locator("#Title"), $"VAL-TPL-{Guid.NewGuid():N}");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();

        await Expect(page.Locator(".validation-message")).ToContainTextAsync("descrizione");
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task CreateTherapy_EmptyDescription_ShouldShowValidation()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var patientId = await CreatePatientAsync(page, $"ValNome{seed}", $"ValCog{seed}", $"{seed}.val@example.test",
            "1988-08-08");

        await page.GotoAsync($"/patient/{patientId}/create-therapy",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();

        await Expect(page.Locator(".validation-message")).ToContainTextAsync("descrizione");
    }
}
