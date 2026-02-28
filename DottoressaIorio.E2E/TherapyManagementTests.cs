using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class TherapyManagementTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task CreateTherapy_FromTemplate_ShouldPrefillAndSave()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var templateTitle = $"TPL-PREFILL-{seed}";
        var templateDescription = $"Template Desc {seed}";

        await CreateTherapyTemplateAsync(page, templateTitle, templateDescription);
        var patientId = await CreatePatientAsync(page, $"ThNome{seed}", $"ThCognome{seed}", $"{seed}.th@example.test",
            "1981-01-01");

        await page.GotoAsync($"/patient/{patientId}/create-therapy",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#TemplateId").SelectOptionAsync(new SelectOptionValue { Label = templateTitle });
        await Expect(page.Locator("#Description")).ToHaveValueAsync(templateDescription);

        await SubmitCreateTherapyFormAsync(page);
        await Expect(page.GetByText(templateDescription)).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task EditAndDeleteTherapy_ShouldUpdateAndRemoveRow()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var patientId = await CreatePatientAsync(page, $"EditThNome{seed}", $"EditThCog{seed}",
            $"{seed}.edth@example.test", "1980-05-05");
        var originalDescription = $"THER-ORIG-{seed}";
        var editedDescription = $"THER-EDIT-{seed}";

        await CreateTherapyAsync(page, patientId, originalDescription);

        var row = page.Locator($"//tbody/tr[contains(., '{originalDescription}')]").First;
        await Expect(row).ToBeVisibleAsync();
        await row.Locator("button").Nth(1).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Modifica Terapia");

        await page.Locator("#Description").FillAsync(editedDescription);
        await SubmitEditTherapyFormAsync(page);
        await Expect(page.GetByText(editedDescription)).ToBeVisibleAsync();

        var editedRow = page.Locator($"//tbody/tr[contains(., '{editedDescription}')]").First;
        await editedRow.Locator("button").Nth(2).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Terapia");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rimuovi" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToContainTextAsync("Editthnome",
            new LocatorAssertionsToContainTextOptions { Timeout = 10000 });
        await Expect(page.Locator($"//tbody/tr[contains(., '{editedDescription}')]")).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task SearchTherapy_ByDate_ShouldFilterRows()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var patientId = await CreatePatientAsync(page, $"DateNome{seed}", $"DateCog{seed}", $"{seed}.date@example.test",
            "1986-06-06");
        var description = $"THER-DATE-{seed}";

        await CreateTherapyAsync(page, patientId, description);

        var firstRowDate = (await page.Locator("table tbody tr").First.Locator("td").Nth(0).InnerTextAsync()).Trim();
        await page.Locator("#therapySearch").FillAsync(firstRowDate);
        await Expect(page.Locator($"//tbody/tr[contains(., '{description}')]").First).ToBeVisibleAsync();
    }
}
