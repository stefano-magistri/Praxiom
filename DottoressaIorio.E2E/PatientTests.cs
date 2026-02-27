using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class PatientTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task AggiungiPaziente_ShouldCreateAndEditPatient()
    {
        var setup = await NewAuthorizedPageAsync("/");
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"Nome{seed}";
        var lastName = $"Cognome{seed}";
        var email = $"{seed}@example.test";
        const string dateOfBirth = "1985-05-12";
        var placeOfBirth = $"Citta{seed}";
        var phone = $"333{Random.Shared.Next(100000, 999999)}";
        var notes = $"Note test {seed}";

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

    [Fact]
    [Trait("Category", "E2E")]
    public async Task DeletePatient_ShouldRemovePatientFromIndex()
    {
        var setup = await NewAuthorizedPageAsync("/");
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"DelNome{seed}";
        var lastName = $"DelCognome{seed}";
        var email = $"{seed}.del@example.test";

        await CreatePatientAsync(page, firstName, lastName, email, "1982-02-02");

        await page.GetByRole(AriaRole.Link, new() { Name = "Rimuovi" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Paziente");
        await page.GetByRole(AriaRole.Button, new() { Name = "Rimuovi" }).ClickAsync();

        // Wait for the app-side navigation after SaveAsync completes.
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Pazienti");
        await Expect(page.Locator("#search")).ToBeVisibleAsync();

        await page.Locator("#search").FillAsync(firstName);
        await Expect(page.GetByText("Nessun paziente trovato.")).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task DeletePatient_FromIndexAction_ShouldRemovePatient()
    {
        var setup = await NewAuthorizedPageAsync("/");
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"IdxDelNome{seed}";
        var lastName = $"IdxDelCognome{seed}";
        var email = $"{seed}.idxdel@example.test";

        await CreatePatientAsync(page, firstName, lastName, email, "1984-04-04");

        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("#search").FillAsync(firstName);
        await page.Locator(".patient-row-actions .btn-delete-soft").First.ClickAsync();

        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Paziente");
        await page.GetByRole(AriaRole.Button, new() { Name = "Rimuovi" }).ClickAsync();

        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Pazienti");
        await page.Locator("#search").FillAsync(firstName);
        await Expect(page.GetByText("Nessun paziente trovato.")).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task DeletePatient_Cancel_ShouldReturnToPatientPage()
    {
        var setup = await NewAuthorizedPageAsync("/");
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"CancelNome{seed}";
        var lastName = $"CancelCognome{seed}";
        var email = $"{seed}.cancel@example.test";

        await CreatePatientAsync(page, firstName, lastName, email, "1983-03-03");

        await page.GetByRole(AriaRole.Link, new() { Name = "Rimuovi" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Paziente");
        await page.GetByRole(AriaRole.Link, new() { Name = "Annulla" }).ClickAsync();

        await Expect(page.GetByText(firstName)).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new() { Name = "Modifica" })).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task DeletePatient_WithTherapies_ShouldRemovePatientFromIndex()
    {
        var setup = await NewAuthorizedPageAsync("/");
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"TherDelNome{seed}";
        var lastName = $"TherDelCognome{seed}";
        var email = $"{seed}.therdel@example.test";
        var therapyDescription = $"Therapy before delete {seed}";

        var patientId = await CreatePatientAsync(page, firstName, lastName, email, "1981-01-01");
        await CreateTherapyAsync(page, patientId, therapyDescription);
        await Expect(page.GetByText(therapyDescription)).ToBeVisibleAsync();

        await page.GetByRole(AriaRole.Link, new() { Name = "Rimuovi" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Paziente");
        await page.GetByRole(AriaRole.Button, new() { Name = "Rimuovi" }).ClickAsync();

        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Pazienti");
        await page.Locator("#search").FillAsync(firstName);
        await Expect(page.GetByText("Nessun paziente trovato.")).ToBeVisibleAsync();
    }
}
