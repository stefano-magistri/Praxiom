using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

public abstract class E2eTestBase(PlaywrightFixture fixture)
{
    protected PlaywrightFixture Fixture { get; } = fixture;

    protected async Task<(IBrowserContext Context, IPage Page)> NewAuthorizedPageAsync(string path = "/")
    {
        var context = await Fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(path, new PageGotoOptions { WaitUntil = WaitUntilState.Load });
        await EnsureAuthorizedShellAsync(page);
        await WaitForInteractiveReadyAsync(page);
        return (context, page);
    }

    protected static async Task EnsureAuthorizedShellAsync(IPage page)
    {
        var notAuthorized = page.GetByText("Non sei autorizzato a visualizzare questo contenuto.");
        if (await notAuthorized.IsVisibleAsync())
            throw new InvalidOperationException(
                "App returned 'Not authorized'. For automated E2E, run the app with Authentication__UseNegotiate=false or configure CI authentication.");

        await Expect(page.GetByTestId("patients-nav-link")).ToBeVisibleAsync();
    }

    protected static async Task<int> CreatePatientAsync(IPage page, string firstName, string lastName, string email,
        string dateOfBirth)
    {
        await page.GotoAsync("/create-patient", new PageGotoOptions { WaitUntil = WaitUntilState.Load });
        await WaitForInteractiveReadyAsync(page);

        var createFormRows = page.Locator("form .row.g-3");
        await FillBoundFieldAsync(createFormRows.Nth(0).Locator("input.form-control"), firstName);
        await FillBoundFieldAsync(createFormRows.Nth(1).Locator("input.form-control"), lastName);
        await FillDateFieldAsync(createFormRows.Nth(2).Locator("input.form-control[type='date']"), dateOfBirth);
        await FillBoundFieldAsync(createFormRows.Nth(3).Locator("input.form-control[type='email']"), email);
        await SubmitCreatePatientFormAsync(page);

        var match = Regex.Match(page.Url, @"/patient/(\d+)/?");
        if (!match.Success) throw new InvalidOperationException($"Could not extract patient id from URL: {page.Url}");

        return int.Parse(match.Groups[1].Value);
    }

    protected static async Task CreateTherapyTemplateAsync(IPage page, string title, string description)
    {
        await page.GotoAsync("/create-therapy-template",
            new PageGotoOptions { WaitUntil = WaitUntilState.Load });
        await WaitForInteractiveReadyAsync(page);
        await FillBoundFieldAsync(page.Locator("#Title"), title);
        await FillBoundFieldAsync(page.Locator("#Description"), description);
        await SubmitTemplateFormAsync(page);
    }

    protected static async Task CreateTherapyAsync(IPage page, int patientId, string description)
    {
        await page.GotoAsync($"/patient/{patientId}/create-therapy",
            new PageGotoOptions { WaitUntil = WaitUntilState.Load });
        await WaitForInteractiveReadyAsync(page);
        await FillBoundFieldAsync(page.Locator("#Description"), description);
        await SubmitCreateTherapyFormAsync(page);
    }

    protected static async Task FillBoundFieldAsync(ILocator locator, string value)
    {
        await locator.FocusAsync();
        await locator.FillAsync(string.Empty);
        await locator.PressSequentiallyAsync(value);
        await locator.PressAsync("Tab");
        await Task.Delay(50);
    }

    protected static async Task FillDateFieldAsync(ILocator locator, string value)
    {
        await locator.FocusAsync();
        await locator.FillAsync(value);
        await locator.PressAsync("Tab");
        await Task.Delay(50);
    }

    protected static Task WaitForInteractiveReadyAsync(IPage page) => page.WaitForTimeoutAsync(750);

    protected static async Task SubmitTemplateFormAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();

        for (var i = 0; i < 80; i++)
        {
            if (page.Url.Contains("/therapy-templates", StringComparison.OrdinalIgnoreCase))
            {
                await WaitForInteractiveReadyAsync(page);
                return;
            }

            await page.WaitForTimeoutAsync(250);
        }

        var warnings = await page.Locator(".alert").AllInnerTextsAsync();
        var validation = await page.Locator(".validation-message").AllInnerTextsAsync();
        var details = string.Join(" | ", warnings.Concat(validation).Where(x => !string.IsNullOrWhiteSpace(x)));

        throw new TimeoutException(
            $"Template form did not navigate to /therapy-templates. Current URL: {page.Url}. Messages: {details}");
    }

    protected static async Task SubmitTemplateFormExpectDuplicateAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();

        var duplicateTitleWarning = page.GetByRole(AriaRole.Alert).Filter(new LocatorFilterOptions
        {
            HasText = "Il titolo"
        });

        await Expect(duplicateTitleWarning).ToBeVisibleAsync();
        await Expect(duplicateTitleWarning).ToContainTextAsync("esiste");

        if (page.Url.Contains("/therapy-templates", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Duplicate template should not navigate to /therapy-templates.");
    }

    protected static async Task SubmitCreatePatientFormAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();

        var duplicatePatientWarning = page.GetByRole(AriaRole.Alert).Filter(new LocatorFilterOptions
        {
            HasText = "Il paziente giá esiste"
        });

        for (var i = 0; i < 40; i++)
        {
            if (Regex.IsMatch(page.Url, @"/patient/\d+/?$"))
            {
                await WaitForInteractiveReadyAsync(page);
                return;
            }

            if (await duplicatePatientWarning.IsVisibleAsync())
            {
                await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();
                await page.WaitForURLAsync("**/patient/*/");
                await WaitForInteractiveReadyAsync(page);
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

    protected static async Task SubmitCreateTherapyFormAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();

        for (var i = 0; i < 40; i++)
        {
            if (Regex.IsMatch(page.Url, @"/patient/\d+/?$"))
            {
                await WaitForInteractiveReadyAsync(page);
                return;
            }

            await page.WaitForTimeoutAsync(250);
        }

        var validation = await page.Locator(".validation-message").AllInnerTextsAsync();
        var details = string.Join(" | ", validation.Where(x => !string.IsNullOrWhiteSpace(x)));

        throw new TimeoutException(
            $"Therapy form did not navigate to /patient/{{id}}/. Current URL: {page.Url}. Messages: {details}");
    }

    protected static async Task SubmitEditTherapyFormAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();

        for (var i = 0; i < 40; i++)
        {
            if (Regex.IsMatch(page.Url, @"/patient/\d+/?$"))
            {
                await WaitForInteractiveReadyAsync(page);
                return;
            }

            await page.WaitForTimeoutAsync(250);
        }

        var validation = await page.Locator(".validation-message").AllInnerTextsAsync();
        var details = string.Join(" | ", validation.Where(x => !string.IsNullOrWhiteSpace(x)));

        throw new TimeoutException(
            $"Edit therapy form did not navigate to /patient/{{id}}/. Current URL: {page.Url}. Messages: {details}");
    }

    protected static async Task SubmitEditPatientFormAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();

        for (var i = 0; i < 40; i++)
        {
            if (Regex.IsMatch(page.Url, @"/patient/\d+/?$"))
            {
                await WaitForInteractiveReadyAsync(page);
                return;
            }

            await page.WaitForTimeoutAsync(250);
        }

        var warnings = await page.Locator(".alert").AllInnerTextsAsync();
        var validation = await page.Locator(".validation-message").AllInnerTextsAsync();
        var details = string.Join(" | ", warnings.Concat(validation).Where(x => !string.IsNullOrWhiteSpace(x)));

        throw new TimeoutException(
            $"Edit patient form did not navigate to /patient/{{id}}/. Current URL: {page.Url}. Messages: {details}");
    }

    protected static async Task FilterTemplatesAsync(IPage page, string term)
    {
        await FillLiveFieldAsync(page.Locator("#templateSearch"), term);
    }

    protected static async Task FillLiveFieldAsync(ILocator locator, string value)
    {
        await locator.FocusAsync();
        await locator.FillAsync(value);
        await locator.DispatchEventAsync("input");
        await Task.Delay(250);
    }

    protected static string FormatTitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLower(CultureInfo.CurrentCulture));
    }
}
