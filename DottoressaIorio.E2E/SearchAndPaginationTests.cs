using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class SearchAndPaginationTests(PlaywrightFixture fixture) : E2eTestBase(fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task Patients_SearchAndPagination_ShouldWork()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..6];
        var prefix = $"PAGPAZ{seed}";

        await Fixture.SeedPatientsAsync(
            Enumerable.Range(1, 11).Select(i => (
                FirstName: $"{prefix}{i:D2}",
                LastName: "LN",
                Email: $"{prefix.ToLowerInvariant()}{i:D2}@example.test",
                DateOfBirth: new DateTime(1980, 1, 1))));

        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);
        await FillLiveFieldAsync(page.Locator("#search"), prefix);

        var pageTwoButton = page.Locator("//button[contains(@class,'page-link') and normalize-space()='2']").First;
        await Expect(pageTwoButton).ToBeVisibleAsync();
        await pageTwoButton.ClickAsync();

        await Expect(page.GetByText($"{prefix}11")).ToBeVisibleAsync();

        await FillLiveFieldAsync(page.Locator("#search"), $"{prefix}03");
        await Expect(page.GetByText($"{prefix}03")).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task Patients_PaginationPrevNext_ShouldMoveActivePage()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..6];
        var prefix = $"PAGNAV{seed}";

        await Fixture.SeedPatientsAsync(
            Enumerable.Range(1, 11).Select(i => (
                FirstName: $"{prefix}{i:D2}",
                LastName: "LN",
                Email: $"{prefix.ToLowerInvariant()}{i:D2}@example.test",
                DateOfBirth: new DateTime(1980, 1, 1))));

        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);
        await FillLiveFieldAsync(page.Locator("#search"), prefix);

        var pagination = page.Locator("nav[aria-label='Patients Pagination']").First;
        var prevButton = pagination.GetByTestId("pagination-prev");
        var nextButton = pagination.GetByTestId("pagination-next");

        await Expect(prevButton).ToBeDisabledAsync();
        await Expect(pagination.Locator("li.page-item.active .page-link")).ToHaveTextAsync("1");

        await nextButton.ClickAsync();
        await Expect(pagination.Locator("li.page-item.active .page-link")).ToHaveTextAsync("2");
        await Expect(page.GetByText($"{prefix}11")).ToBeVisibleAsync();

        await prevButton.ClickAsync();
        await Expect(pagination.Locator("li.page-item.active .page-link")).ToHaveTextAsync("1");
        await Expect(prevButton).ToBeDisabledAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task TherapyTemplates_SearchAndPagination_ShouldWork()
    {
        var setup = await NewAuthorizedPageAsync("/therapy-templates");
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..6];
        var prefix = $"PAGTPL-{seed}";
        var finalDescription = $"Descrizione {prefix}-11";
        var filteredDescription = $"Descrizione {prefix}-04";

        await Fixture.SeedTherapyTemplatesAsync(
            Enumerable.Range(1, 11)
                .Select(i => ($"{prefix}-{i:D2}", $"Descrizione {prefix}-{i:D2}")));

        await page.GotoAsync("/therapy-templates", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);
        await FillLiveFieldAsync(page.Locator("#templateSearch"), prefix);

        var pagination = page.Locator("nav[aria-label='Patients Pagination']").First;
        var pageTwoButton = pagination.Locator("//button[contains(@class,'page-link') and normalize-space()='2']").First;
        await Expect(pageTwoButton).ToBeVisibleAsync();
        await pageTwoButton.ClickAsync();
        await Expect(pagination.Locator("li.page-item.active .page-link")).ToHaveTextAsync("2");

        await Expect(page.Locator($"//tbody/tr[contains(., '{finalDescription}')]").First).ToBeVisibleAsync();

        await FillLiveFieldAsync(page.Locator("#templateSearch"), $"{prefix}-04");
        await Expect(page.Locator($"//tbody/tr[contains(., '{filteredDescription}')]").First).ToBeVisibleAsync();
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task PatientTherapies_SearchAndPagination_ShouldWork()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..6];
        var patientId = (await Fixture.SeedPatientsAsync([
            ($"THP{seed}", "LN", $"thp{seed}@example.test", new DateTime(1984, 4, 4))
        ])).Single();

        var prefix = $"PAGTH-{seed}";
        await Fixture.SeedTherapiesAsync(patientId, Enumerable.Range(1, 9).Select(i => $"{prefix}-{i:D2}"));

        await page.GotoAsync($"/patient/{patientId}/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);
        await FillLiveFieldAsync(page.Locator("#therapySearch"), prefix);

        var pageTwoButton = page.Locator("//button[contains(@class,'page-link') and normalize-space()='2']").First;
        await Expect(pageTwoButton).ToBeVisibleAsync();
        await pageTwoButton.ClickAsync();

        await Expect(page.GetByText($"{prefix}-01")).ToBeVisibleAsync();

        await FillLiveFieldAsync(page.Locator("#therapySearch"), $"{prefix}-09");
        await Expect(page.GetByText($"{prefix}-09")).ToBeVisibleAsync();
    }
}
