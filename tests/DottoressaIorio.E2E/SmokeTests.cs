using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class SmokeTests(PlaywrightFixture fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task Navigazione_ShouldMatchSeleniumFlow()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await PerformLoginIfNeededAsync(page, fixture.Settings);

        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
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

    [Fact]
    [Trait("Category", "E2E")]
    public async Task CrudTerapiaPrecompilata_ShouldCreateEditAndDelete()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await PerformLoginIfNeededAsync(page, fixture.Settings);
        var seed = Guid.NewGuid().ToString("N")[..8];
        var addedTitle = $"AAA ADD TEST {seed}";
        var editedTitle = $"AAA MODIFICA TEST {seed}";
        var addedDescription = $"AGGIUNGI TEST Descrizione: {seed}";
        var editedDescription = $"MODIFICA TEST Descrizione: {seed}";

        await page.GotoAsync("/therapy-templates", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByTestId("create-therapy-template-nav-link").ClickAsync();

        await page.Locator("#Title").FillAsync(addedTitle);
        await page.Locator("#Description").FillAsync(addedDescription);
        await SubmitTemplateFormAsync(page);

        await FilterTemplatesAsync(page, addedTitle);
        var addedRow = page.Locator($"//tbody/tr[contains(., '{addedTitle}')]").First;
        await Expect(addedRow).ToBeVisibleAsync();

        await addedRow.Locator("button").First.ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Modifica Terapia Precompilata");

        await page.Locator("#Title").FillAsync(editedTitle);
        await page.Locator("#Description").FillAsync(editedDescription);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salva" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Terapie Precompilate");

        await FilterTemplatesAsync(page, editedTitle);
        var editedRow = page.Locator($"//tbody/tr[contains(., '{editedTitle}')]").First;
        await Expect(editedRow).ToBeVisibleAsync();

        await editedRow.Locator("button").Nth(1).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Terapia Precompilata");

        await page.GetByRole(AriaRole.Button, new() { Name = "Rimuovi" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Terapie Precompilate");

        await FilterTemplatesAsync(page, editedTitle);
        await Expect(page.Locator($"//tbody/tr[contains(., '{editedTitle}')]"))
            .ToHaveCountAsync(0);
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task AggiungiTerapiaPrecompilata_ShouldCreateTemplate()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await PerformLoginIfNeededAsync(page, fixture.Settings);
        var seed = Guid.NewGuid().ToString("N")[..8];
        var title = $"ADD TEMPLATE {seed}";
        var description = $"Template description {seed}";

        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByTestId("create-therapy-template-nav-link").ClickAsync();

        await page.Locator("#Title").FillAsync(title);
        await page.Locator("#Description").FillAsync(description);
        await SubmitTemplateFormAsync(page);

        await FilterTemplatesAsync(page, title);
        await Expect(page.Locator($"//tbody/tr[contains(., '{title}')]").First).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task AggiungiPaziente_ShouldCreateAndEditPatient()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await PerformLoginIfNeededAsync(page, fixture.Settings);
        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"Nome{seed}";
        var lastName = $"Cognome{seed}";
        var email = $"{seed}@example.test";
        const string dateOfBirth = "1985-05-12";
        var placeOfBirth = $"Citta{seed}";
        var phone = $"333{Random.Shared.Next(100000, 999999)}";
        var notes = $"Note test {seed}";

        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.GetByTestId("create-patient-nav-link").ClickAsync();

        var createFormRows = page.Locator("form .row.g-3");
        await createFormRows.Nth(0).Locator("input.form-control").FillAsync(firstName);
        await createFormRows.Nth(1).Locator("input.form-control").FillAsync(lastName);
        await createFormRows.Nth(2).Locator("input.form-control[type='date']").FillAsync(dateOfBirth);
        await createFormRows.Nth(3).Locator("input.form-control[type='email']").FillAsync(email);
        await SubmitCreatePatientFormAsync(page);

        await page.GetByRole(AriaRole.Link, new() { Name = "Modifica" }).ClickAsync();
        await page.WaitForURLAsync("**/patient/*/edit/");

        await page.Locator("#Title").SelectOptionAsync(new SelectOptionValue { Label = "Dottoressa" });
        await page.Locator("#PlaceOfBirth").FillAsync(placeOfBirth);
        await page.Locator("input[name='Gender']").Nth(1).CheckAsync();
        await page.Locator("input[type='tel']").FillAsync(phone);
        await page.Locator("input[type='email']").FillAsync(email);
        await page.Locator("#Notes").FillAsync(notes);
        await page.GetByRole(AriaRole.Button, new() { Name = "Salva" }).ClickAsync();
        await page.WaitForURLAsync("**/patient/*/");

        await Expect(page.GetByText(placeOfBirth)).ToBeVisibleAsync();
        await Expect(page.GetByText(notes)).ToBeVisibleAsync();
    }

    private static async Task PerformLoginIfNeededAsync(IPage page, E2eSettings settings)
    {
        await page.GotoAsync("/guide", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var loginForm = page.GetByTestId("login-form");
        if (await loginForm.IsVisibleAsync())
        {
            if (string.IsNullOrWhiteSpace(settings.Login.Username) || string.IsNullOrWhiteSpace(settings.Login.Password))
            {
                throw new InvalidOperationException(
                    "Login form detected but E2E credentials are missing. Set E2E__Login__Username and E2E__Login__Password.");
            }

            await page.GetByTestId("login-username").FillAsync(settings.Login.Username);
            await page.GetByTestId("login-password").FillAsync(settings.Login.Password);
            await page.GetByTestId("login-submit").ClickAsync();
        }

        var notAuthorized = page.GetByText("You are not authorized to view this content.");
        if (await notAuthorized.IsVisibleAsync())
        {
            throw new InvalidOperationException(
                "App returned 'Not authorized'. For automated E2E, run the app with Authentication__UseNegotiate=false or configure CI authentication.");
        }

        // With Authentication__UseNegotiate=false, the <AuthorizeView> in LoginDisplay
        // may not render the greeting, but app navigation is still available.
        await Expect(page.GetByTestId("patients-nav-link")).ToBeVisibleAsync();
    }

    private static async Task SubmitTemplateFormAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Salva" }).ClickAsync();

        var duplicateTitleWarning = page.GetByRole(AriaRole.Alert).Filter(new()
        {
            HasText = "Il titolo giá esiste"
        });

        for (var i = 0; i < 40; i++)
        {
            if (page.Url.Contains("/therapy-templates", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (await duplicateTitleWarning.IsVisibleAsync())
            {
                await page.GetByRole(AriaRole.Button, new() { Name = "Salva" }).ClickAsync();
                await page.WaitForURLAsync("**/therapy-templates*");
                return;
            }

            await page.WaitForTimeoutAsync(250);
        }

        throw new TimeoutException(
            $"Template form did not navigate to /therapy-templates. Current URL: {page.Url}");
    }

    private static async Task FilterTemplatesAsync(IPage page, string term)
    {
        await page.Locator("#templateSearch").FillAsync(term);
        await page.Locator("#templateSearch").PressAsync("Tab");
    }

    private static async Task SubmitCreatePatientFormAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Salva" }).ClickAsync();

        var duplicatePatientWarning = page.GetByRole(AriaRole.Alert).Filter(new()
        {
            HasText = "Il paziente giá esiste"
        });

        for (var i = 0; i < 40; i++)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(page.Url, @"/patient/\d+/?$"))
            {
                return;
            }

            if (await duplicatePatientWarning.IsVisibleAsync())
            {
                await page.GetByRole(AriaRole.Button, new() { Name = "Salva" }).ClickAsync();
                await page.WaitForURLAsync("**/patient/*/");
                return;
            }

            await page.WaitForTimeoutAsync(250);
        }

        var warnings = await page.Locator(".alert").AllInnerTextsAsync();
        var validation = await page.Locator(".validation-message").AllInnerTextsAsync();
        var details = string.Join(" | ", warnings.Concat(validation).Where(x => !string.IsNullOrWhiteSpace(x)));

        throw new TimeoutException(
            $"Patient form did not navigate to /patient/{{id}}/. Current URL: {page.Url}. Messages: {details}");
    }
}
