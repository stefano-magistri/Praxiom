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
        var setup = await NewAuthorizedPageAsync();
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

        await page.GetByTestId("create-patient-page-link").ClickAsync();

        await FillBoundFieldAsync(page.Locator("#FirstName"), firstName);
        await FillBoundFieldAsync(page.Locator("#LastName"), lastName);
        await FillDateFieldAsync(page.Locator("#DateOfBirth"), dateOfBirth);
        await FillBoundFieldAsync(page.Locator("#Email"), email);
        await SubmitCreatePatientFormAsync(page);

        await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Modifica" }).ClickAsync();
        await page.WaitForURLAsync("**/patient/*/edit/");

        await page.Locator("#Title").SelectOptionAsync(new SelectOptionValue { Label = "Dottoressa" });
        await FillBoundFieldAsync(page.Locator("#PlaceOfBirth"), placeOfBirth);
        await page.Locator("input[name='Gender']").Nth(1).CheckAsync();
        await FillBoundFieldAsync(page.Locator("input[type='tel']"), phone);
        await FillBoundFieldAsync(page.Locator("input[type='email']"), email);
        await FillBoundFieldAsync(page.Locator("#Notes"), notes);
        await SubmitEditPatientFormAsync(page);

        await Expect(page.GetByText(placeOfBirth)).ToBeVisibleAsync();
        await Expect(page.GetByText(notes)).ToBeVisibleAsync();
        await Expect(page.GetByTestId("patient-age")).ToHaveTextAsync($"{CalculateAge(new DateTime(1985, 5, 12))} anni");
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task DeletePatient_ShouldRemovePatientFromIndex()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"DelNome{seed}";
        var lastName = $"DelCognome{seed}";
        var email = $"{seed}.del@example.test";

        await CreatePatientAsync(page, firstName, lastName, email, "1982-02-02");

        await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Rimuovi" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Paziente");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rimuovi" }).ClickAsync();

        // Wait for the app-side navigation after SaveAsync completes.
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Pazienti");
        await Expect(page.Locator("#search")).ToBeVisibleAsync();

        await FillLiveFieldAsync(page.Locator("#search"), firstName);
        await Expect(page.GetByText("Nessun paziente trovato.")).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task DeletePatient_FromIndexAction_ShouldRemovePatient()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"IdxDelNome{seed}";
        var lastName = $"IdxDelCognome{seed}";
        var email = $"{seed}.idxdel@example.test";

        await CreatePatientAsync(page, firstName, lastName, email, "1984-04-04");

        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);
        await FillLiveFieldAsync(page.Locator("#search"), firstName);
        await page.Locator(".patient-row-actions .btn-delete-soft").First.ClickAsync();

        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Paziente");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rimuovi" }).ClickAsync();

        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Pazienti");
        await FillLiveFieldAsync(page.Locator("#search"), firstName);
        await Expect(page.GetByText("Nessun paziente trovato.")).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task DeletePatient_Cancel_ShouldReturnToPatientPage()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"CancelNome{seed}";
        var lastName = $"CancelCognome{seed}";
        var email = $"{seed}.cancel@example.test";

        await CreatePatientAsync(page, firstName, lastName, email, "1983-03-03");

        await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Rimuovi" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Paziente");
        await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Annulla" }).ClickAsync();

        await Expect(page.GetByText(firstName)).ToBeVisibleAsync();
        await Expect(page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Modifica" })).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task DeletePatient_WithTherapies_ShouldRemovePatientFromIndex()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"TherDelNome{seed}";
        var lastName = $"TherDelCognome{seed}";
        var email = $"{seed}.therdel@example.test";
        var therapyDescription = $"Therapy before delete {seed}";

        var patientId = await CreatePatientAsync(page, firstName, lastName, email, "1981-01-01");
        await CreateTherapyAsync(page, patientId, therapyDescription);
        await Expect(page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Scarica ultima terapia" }))
            .ToBeVisibleAsync();
        await Expect(page.Locator("table tbody tr")).ToHaveCountAsync(1);

        await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Rimuovi" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Paziente");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rimuovi" }).ClickAsync();

        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Pazienti");
        await FillLiveFieldAsync(page.Locator("#search"), firstName);
        await Expect(page.GetByText("Nessun paziente trovato.")).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task DeletePatient_WithMultipleTherapies_ShouldRemovePatientFromIndex()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..8];
        var firstName = $"TherMultiNome{seed}";
        var lastName = $"TherMultiCognome{seed}";
        var email = $"{seed}.thermulti@example.test";
        var therapyDescription1 = $"Therapy before delete A {seed}";
        var therapyDescription2 = $"Therapy before delete B {seed}";

        var patientId = await CreatePatientAsync(page, firstName, lastName, email, "1981-01-01");
        await CreateTherapyAsync(page, patientId, therapyDescription1);
        await CreateTherapyAsync(page, patientId, therapyDescription2);

        await Expect(page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Scarica ultima terapia" }))
            .ToBeVisibleAsync();
        await Expect(page.Locator("table tbody tr")).ToHaveCountAsync(2);

        await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "Rimuovi" }).ClickAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Rimuovi Paziente");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rimuovi" }).ClickAsync();

        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Pazienti");
        await FillLiveFieldAsync(page.Locator("#search"), firstName);
        await Expect(page.GetByText("Nessun paziente trovato.")).ToBeVisibleAsync();
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
}
