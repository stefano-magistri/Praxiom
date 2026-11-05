using System.Text;
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
        await Expect(page.Locator("#templateSearch")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("create-therapy-template-page-link")).ToBeVisibleAsync();
        await Expect(page.Locator("table tbody tr").First).ToBeVisibleAsync();
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

    [Fact]
    [Trait("Category", "E2E")]
    public async Task Patients_ExportCsv_ShouldRespectActiveFilters()
    {
        var setup = await NewAuthorizedPageAsync();
        await using var context = setup.Context;
        var page = setup.Page;

        var seed = Guid.NewGuid().ToString("N")[..6];
        var filterToken = $"CSVMATCH{seed}";
        var matchingPrefix = $"{filterToken}A";
        var matchingNoTherapyPrefix = $"{filterToken}B";
        var otherPrefix = $"CSVNO{seed}";

        var patientIds = await Fixture.SeedPatientsAsync([
            ($"{matchingPrefix}", "Rossi", $"{matchingPrefix.ToLowerInvariant()}@example.test", new DateTime(1980, 1, 1)),
            ($"{matchingNoTherapyPrefix}", "Verdi", $"{matchingNoTherapyPrefix.ToLowerInvariant()}@example.test", new DateTime(1982, 2, 2)),
            ($"{otherPrefix}", "Bianchi", $"{otherPrefix.ToLowerInvariant()}@example.test", new DateTime(1981, 1, 1))
        ]);

        await Fixture.SeedTherapiesAsync(patientIds[0], ["Terapia filtro, \"confermata\""]);
        await Fixture.SeedTherapiesAsync(patientIds[2], ["Terapia esclusa"]);

        await page.GotoAsync("/", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await WaitForInteractiveReadyAsync(page);
        await FillLiveFieldAsync(page.Locator("#search"), filterToken);
        await Expect(page.GetByText(matchingPrefix)).ToBeVisibleAsync();
        await Expect(page.GetByText(matchingNoTherapyPrefix)).ToBeVisibleAsync();

        var download = await page.RunAndWaitForDownloadAsync(async () =>
            await page.GetByTestId("export-csv-link").ClickAsync());

        Assert.Equal($"pazienti-filtrati-{DateTime.Today:yyyy-MM-dd}.csv", download.SuggestedFilename);

        var downloadPath = await download.PathAsync();
        Assert.NotNull(downloadPath);

        var content = await File.ReadAllTextAsync(downloadPath!, Encoding.UTF8);

        Assert.Contains($"Export pazienti e terapie - {DateTime.Today:dd/MM/yyyy}", content);
        Assert.Contains(matchingPrefix, content);
        Assert.Contains(matchingNoTherapyPrefix, content);
        Assert.DoesNotContain(otherPrefix, content);
        Assert.Contains("\"Terapia filtro, \"\"confermata\"\"\"", content);
    }
}
