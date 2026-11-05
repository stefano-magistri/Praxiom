using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class TherapyManagementTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Category", "E2ESmoke")]
    public async Task CreateTherapy_FromTemplate_ShouldShowActionsAndPrint()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var templateTitle = $"TPL-PREFILL-{seed}";
        var templateDescription = $"Template Desc {seed}";
        var patientDetails = BuildPatientDetails(seed, "Th");

        await CreateTherapyTemplateAsync(page, templateTitle, templateDescription);
        var patientId = await CreatePatientWithDetailsAsync(page, patientDetails);
        await page.GotoAsync($"/patient/{patientId}/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);
        var writeTherapyButton = page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Scrivi terapia" });
        await Expect(writeTherapyButton).ToBeVisibleAsync();
        await writeTherapyButton.ClickAsync();
        await page.Locator("#TemplateId").WaitForAsync();
        Assert.Contains($"/patient/{patientId}/create-therapy", page.Url, StringComparison.Ordinal);
        await AssertPatientContextCollapsedAsync(page, patientDetails);
        await Expect(page.Locator("#TemplateId option").Filter(new LocatorFilterOptions { HasText = templateTitle }))
            .ToHaveCountAsync(1);
        await page.Locator("#TemplateId").SelectOptionAsync(new SelectOptionValue { Label = templateTitle });
        await Expect(page.GetByTestId("therapy-description-editor")).ToContainTextAsync(templateDescription);
        Assert.Equal(["Annulla", "Stampa", "Salva"], await GetTherapyFormActionLabelsAsync(page));

        var popup = await ClickPrintExpectPopupAsync(page);
        await popup.CloseAsync();

        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Modifica terapia");
        Assert.Matches(@".*/patient/\d+/edit-therapy/(\?.*)?$", page.Url);
        await AssertPatientContextCollapsedAsync(page, patientDetails);
        await Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync(
            "Terapia salvata e PDF aperto in una nuova scheda. Ora puoi continuare da questa schermata di modifica.");
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
        var patientDetails = BuildPatientDetails(seed, "EditTh");
        var patientId = await CreatePatientWithDetailsAsync(page, patientDetails);
        var originalDescription = $"THER-ORIG-{seed}";
        var editedDescription = $"THER-EDIT-{seed}";

        await CreateTherapyAsync(page, patientId, originalDescription);

        var row = page.Locator($"//tbody/tr[contains(., '{originalDescription}')]").First;
        await Expect(row).ToBeVisibleAsync();
        await row.Locator("button").Nth(1).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Modifica terapia");
        await AssertPatientContextCollapsedAsync(page, patientDetails);
        Assert.Equal(["Annulla", "Stampa", "Salva"], await GetTherapyFormActionLabelsAsync(page));

        await FillTherapyDescriptionAsync(page, editedDescription);
        var popup = await ClickPrintExpectPopupAsync(page);
        await popup.CloseAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Modifica terapia");
        Assert.Matches(@".*/patient/\d+/edit-therapy/?$", page.Url);
        await AssertPatientContextCollapsedAsync(page, patientDetails);
        await Expect(page.GetByRole(AriaRole.Status)).ToContainTextAsync(
            "PDF aperto in una nuova scheda. La terapia resta modificabile in questa pagina.");
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

    private static async Task<int> CreatePatientWithDetailsAsync(IPage page, PatientDetails details)
    {
        var patientId = await CreatePatientAsync(page, details.FirstName, details.LastName, details.Email, details.DateOfBirth);

        await page.GotoAsync($"/patient/{patientId}/edit/",
            new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);
        await FillBoundFieldAsync(page.Locator("#PlaceOfBirth"), details.PlaceOfBirth);
        await page.Locator(details.Gender == "Femmina" ? "#GenderFemale" : "#GenderMale").CheckAsync();
        await FillBoundFieldAsync(page.Locator("#PhoneNumber"), details.PhoneNumber);
        await FillBoundFieldAsync(page.Locator("#Notes"), details.Notes);
        await SubmitEditPatientFormAsync(page);

        return patientId;
    }

    private static async Task AssertPatientContextCollapsedAsync(IPage page, PatientDetails details)
    {
        var compact = page.GetByTestId("patient-context-compact");
        var summary = page.GetByTestId("patient-context-summary");
        var toggle = page.GetByTestId("patient-context-toggle");
        var expectedDisplayName = $"{FormatTitleCase(details.FirstName)} {FormatTitleCase(details.LastName)}";

        await Expect(compact).ToBeVisibleAsync();
        await Expect(compact).ToContainTextAsync(expectedDisplayName);
        await Expect(summary).ToContainTextAsync(details.ExpectedAgeLabel);
        await Expect(summary).ToContainTextAsync(details.Gender);
        await Expect(toggle).ToHaveTextAsync("Mostra dettagli");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(page.GetByTestId("patient-context-details")).ToHaveCountAsync(0);
        await Expect(page.GetByText("Numero di telefono")).ToHaveCountAsync(0);
        await Expect(page.GetByText(details.PhoneNumber)).ToHaveCountAsync(0);
        await Expect(page.GetByText("Note")).ToHaveCountAsync(0);
        await Expect(page.GetByText(details.Notes)).ToHaveCountAsync(0);

        await toggle.ClickAsync();
        await Expect(toggle).ToHaveTextAsync("Nascondi dettagli");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(page.GetByTestId("patient-context-details")).ToBeVisibleAsync();
        await Expect(page.GetByText("Numero di telefono")).ToBeVisibleAsync();
        await Expect(page.GetByText(details.PhoneNumber)).ToBeVisibleAsync();
        await Expect(page.GetByText(details.Email)).ToBeVisibleAsync();
        await Expect(page.GetByText(details.Notes)).ToBeVisibleAsync();

        await toggle.ClickAsync();
        await Expect(page.GetByTestId("patient-context-details")).ToHaveCountAsync(0);
        await Expect(toggle).ToHaveTextAsync("Mostra dettagli");
        await Expect(toggle).ToHaveAttributeAsync("aria-expanded", "false");
    }

    private static PatientDetails BuildPatientDetails(string seed, string prefix)
    {
        var firstName = $"{prefix}Nome{seed}";
        var lastName = $"{prefix}Cog{seed}";
        var email = $"{seed}.{prefix.ToLowerInvariant()}@example.test";
        var phone = $"333{Random.Shared.Next(100000, 999999)}";
        var notes = $"Note terapia {seed}";
        const string dateOfBirth = "1980-05-05";
        var age = CalculateAge(new DateTime(1980, 5, 5));

        return new PatientDetails(
            firstName,
            lastName,
            email,
            dateOfBirth,
            "Perugia",
            "Femmina",
            phone,
            notes,
            $"{age} anni");
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    private sealed record PatientDetails(
        string FirstName,
        string LastName,
        string Email,
        string DateOfBirth,
        string PlaceOfBirth,
        string Gender,
        string PhoneNumber,
        string Notes,
        string ExpectedAgeLabel);
}
