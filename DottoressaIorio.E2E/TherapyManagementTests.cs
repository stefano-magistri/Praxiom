using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class TherapyManagementTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task CreateTherapy_FromTemplate_ShouldShowActionsAndPrint()
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
        await page.GotoAsync($"/patient/{patientId}/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);
        var writeTherapyButton = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Scrivi terapia" });
        await Expect(writeTherapyButton).ToBeVisibleAsync();
        await writeTherapyButton.ClickAsync();
        await page.Locator("#TemplateId").WaitForAsync();
        Assert.Contains($"/patient/{patientId}/create-therapy", page.Url, StringComparison.Ordinal);
        await Expect(page.Locator("#TemplateId option").Filter(new LocatorFilterOptions { HasText = templateTitle }))
            .ToHaveCountAsync(1);
        await page.Locator("#TemplateId").SelectOptionAsync(new SelectOptionValue { Label = templateTitle });
        await Expect(page.GetByTestId("therapy-description-editor")).ToContainTextAsync(templateDescription);
        Assert.Equal(["Annulla", "Stampa", "Salva"], await GetTherapyFormActionLabelsAsync(page));

        var popup = await ClickPrintExpectPopupAsync(page);
        await popup.CloseAsync();

        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Modifica terapia");
        Assert.Matches(@".*/patient/\d+/edit-therapy/?$", page.Url);
        await Expect(page.GetByTestId("therapy-description-editor")).ToContainTextAsync(templateDescription);
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task EditTherapy_ShouldPrintLatestChangesAndSaveOnClose()
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
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Modifica terapia");
        Assert.Equal(["Annulla", "Stampa", "Salva"], await GetTherapyFormActionLabelsAsync(page));

        await FillTherapyDescriptionAsync(page, editedDescription);
        var popup = await ClickPrintExpectPopupAsync(page);
        await popup.CloseAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Modifica terapia");
        Assert.Matches(@".*/patient/\d+/edit-therapy/?$", page.Url);
        await Expect(page.GetByTestId("therapy-description-editor")).ToContainTextAsync(editedDescription);

        await SubmitEditTherapyFormAsync(page);
        await Expect(page.GetByText(editedDescription)).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task EditAndDeleteTherapy_ShouldRemoveRow()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var patientId = await CreatePatientAsync(page, $"EditDelNome{seed}", $"EditDelCog{seed}",
            $"{seed}.eddel@example.test", "1980-05-05");
        var description = $"THER-DEL-{seed}";

        await CreateTherapyAsync(page, patientId, description);

        var editedRow = page.Locator($"//tbody/tr[contains(., '{description}')]").First;
        await editedRow.Locator("button").Nth(2).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Terapia");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rimuovi" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Scrivi terapia" }))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 10000 });
        await Expect(page.Locator($"//tbody/tr[contains(., '{description}')]")).ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task CreateTherapy_ClearingTemplateSelection_ShouldClearDescriptionAndSaveClose()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var templateTitle = $"TPL-CLEAR-{seed}";
        var templateDescription = $"Template to clear {seed}";
        var manualDescription = $"Manual therapy {seed}";

        await CreateTherapyTemplateAsync(page, templateTitle, templateDescription);
        var patientId = await CreatePatientAsync(page, $"ClrNome{seed}", $"ClrCognome{seed}", $"{seed}.clr@example.test",
            "1982-02-02");

        await page.GotoAsync($"/patient/{patientId}/create-therapy",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);

        await Expect(page.Locator("#TemplateId option").Filter(new LocatorFilterOptions { HasText = templateTitle }))
            .ToHaveCountAsync(1);
        await page.Locator("#TemplateId").SelectOptionAsync(new SelectOptionValue { Label = templateTitle });
        await Expect(page.GetByTestId("therapy-description-editor")).ToContainTextAsync(templateDescription);

        await page.Locator("#TemplateId").SelectOptionAsync("0");
        await Expect(page.GetByTestId("therapy-description-editor")).ToHaveTextAsync(string.Empty);

        await FillTherapyDescriptionAsync(page, manualDescription);
        await SubmitCreateTherapyFormAsync(page);
        await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Scrivi terapia" }))
            .ToBeVisibleAsync();
        await Expect(page.GetByText(manualDescription)).ToBeVisibleAsync();
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
        await FillLiveFieldAsync(page.Locator("#therapySearch"), firstRowDate);
        await Expect(page.Locator($"//tbody/tr[contains(., '{description}')]").First).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task CreateTherapy_WhenTemplateConflicts_ShouldCancelThenAppendAndPersistCombinedContent()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstTemplateTitle = $"TPL-BASE-{seed}";
        var secondTemplateTitle = $"TPL-APPEND-{seed}";
        var firstTemplateDescription = $"Prima parte {seed}";
        var secondTemplateDescription = $"Seconda parte {seed}";

        await CreateTherapyTemplateAsync(page, firstTemplateTitle, firstTemplateDescription);
        await CreateTherapyTemplateAsync(page, secondTemplateTitle, secondTemplateDescription);

        var patientId = await CreatePatientAsync(page, $"ConfNome{seed}", $"ConfCognome{seed}",
            $"{seed}.conf@example.test", "1984-04-04");

        await page.GotoAsync($"/patient/{patientId}/create-therapy",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);

        await page.Locator("#TemplateId").SelectOptionAsync(new SelectOptionValue { Label = firstTemplateTitle });
        await Expect(page.GetByTestId("therapy-description-editor")).ToContainTextAsync(firstTemplateDescription);

        await page.Locator("#TemplateId").SelectOptionAsync(new SelectOptionValue { Label = secondTemplateTitle });
        await Expect(page.GetByRole(AriaRole.Dialog)).ToContainTextAsync(
            "Sostituire il testo esistente oppure aggiungerlo?");

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Annulla" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await Expect(page.Locator("#TemplateId option:checked")).ToHaveTextAsync(firstTemplateTitle);
        await Expect(page.GetByTestId("therapy-description-editor")).ToContainTextAsync(firstTemplateDescription);

        await page.Locator("#TemplateId").SelectOptionAsync(new SelectOptionValue { Label = secondTemplateTitle });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Aggiungi" }).ClickAsync();
        await Expect(page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await Expect(page.Locator("#TemplateId option:checked")).ToHaveTextAsync(secondTemplateTitle);
        await Expect(page.GetByTestId("therapy-description-editor")).ToContainTextAsync(firstTemplateDescription);
        await Expect(page.GetByTestId("therapy-description-editor")).ToContainTextAsync(secondTemplateDescription);

        await SubmitCreateTherapyFormAsync(page);

        var combinedDescriptionPreview = page.Locator("table tbody tr").First.Locator("td").Nth(1);
        await Expect(combinedDescriptionPreview).ToContainTextAsync(firstTemplateDescription);
        await Expect(combinedDescriptionPreview).ToContainTextAsync(secondTemplateDescription);
    }
}
