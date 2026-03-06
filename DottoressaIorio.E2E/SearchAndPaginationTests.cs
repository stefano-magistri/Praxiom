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

        for (var i = 1; i <= 11; i++)
            await CreatePatientAsync(
                page,
                $"{prefix}{i:D2}",
                "LN",
                $"{prefix.ToLowerInvariant()}{i:D2}@example.test",
                "1980-01-01");

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

        for (var i = 1; i <= 11; i++)
            await CreatePatientAsync(
                page,
                $"{prefix}{i:D2}",
                "LN",
                $"{prefix.ToLowerInvariant()}{i:D2}@example.test",
                "1980-01-01");

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

        for (var i = 1; i <= 11; i++)
            await CreateTherapyTemplateAsync(page, $"{prefix}-{i:D2}", $"Descrizione {prefix}-{i:D2}");

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
        var patientId = await CreatePatientAsync(
            page,
            $"THP{seed}",
            "LN",
            $"thp{seed}@example.test",
            "1984-04-04");

        var prefix = $"PAGTH-{seed}";
        for (var i = 1; i <= 9; i++) await CreateTherapyAsync(page, patientId, $"{prefix}-{i:D2}");

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
