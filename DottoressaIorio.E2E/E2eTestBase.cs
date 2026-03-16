using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

/// <summary>
/// Provides common Playwright helpers for authenticated end-to-end scenarios.
/// </summary>
public abstract class E2eTestBase(PlaywrightFixture fixture)
{
    protected PlaywrightFixture Fixture { get; } = fixture;

    /// <summary>
    /// Creates a browser page and waits for the authenticated shell to become interactive.
    /// </summary>
    protected async Task<(IBrowserContext Context, IPage Page)> NewAuthorizedPageAsync(string path = "/")
    {
        var context = await Fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(path, new PageGotoOptions { WaitUntil = WaitUntilState.Load });
        await EnsureAuthorizedShellAsync(page);
        await WaitForInteractiveReadyAsync(page);
        return (context, page);
    }

    /// <summary>
    /// Verifies that the current page is not showing the authorization failure shell.
    /// </summary>
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
        var firstNameInput = page.Locator("#FirstName");
        var lastNameInput = page.Locator("#LastName");
        var dateOfBirthInput = page.Locator("#DateOfBirth");
        var emailInput = page.Locator("#Email");

        await Expect(firstNameInput).ToBeVisibleAsync();
        await FillBoundFieldAsync(firstNameInput, firstName);
        await FillBoundFieldAsync(lastNameInput, lastName);
        await FillDateFieldAsync(dateOfBirthInput, dateOfBirth);
        await FillBoundFieldAsync(emailInput, email);
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
        var titleInput = page.Locator("#Title");
        await Expect(titleInput).ToBeVisibleAsync();
        await FillBoundFieldAsync(titleInput, title);
        await FillTemplateDescriptionAsync(page, description);
        await SubmitTemplateFormAsync(page);
    }

    protected static async Task CreateTherapyAsync(IPage page, int patientId, string description)
    {
        await page.GotoAsync($"/patient/{patientId}/create-therapy",
            new PageGotoOptions { WaitUntil = WaitUntilState.Load });
        await WaitForInteractiveReadyAsync(page);
        await FillTherapyDescriptionAsync(page, description);
        await SubmitCreateTherapyFormAsync(page);
    }

    protected static async Task FillTherapyDescriptionAsync(IPage page, string value)
    {
        var editor = page.GetByTestId("therapy-description-editor");
        if (await editor.CountAsync() > 0 && await editor.First.IsVisibleAsync())
        {
            await FillRichTextEditorAsync(page, editor.First, value);
            return;
        }

        await FillBoundFieldAsync(page.Locator("#Description"), value);
    }

    protected static async Task FillTemplateDescriptionAsync(IPage page, string value)
    {
        var editor = page.GetByTestId("template-description-editor");
        if (await editor.CountAsync() > 0 && await editor.First.IsVisibleAsync())
        {
            await FillRichTextEditorAsync(page, editor.First, value);
            return;
        }

        await FillBoundFieldAsync(page.Locator("#Description"), value);
    }

    private static async Task FillRichTextEditorAsync(IPage page, ILocator editor, string value)
    {
        await editor.ClickAsync();
        await page.Keyboard.PressAsync("Control+A");
        await page.Keyboard.PressAsync("Backspace");
        if (!string.IsNullOrEmpty(value))
        {
            await page.Keyboard.InsertTextAsync(value);
        }

        await page.Keyboard.PressAsync("Tab");
        await Task.Delay(50);
    }

    protected static async Task FillBoundFieldAsync(ILocator locator, string value)
    {
        await Expect(locator).ToBeVisibleAsync();
        await locator.FocusAsync();
        await locator.FillAsync(string.Empty);
        await locator.PressSequentiallyAsync(value);
        if (!string.Equals(await locator.InputValueAsync(), value, StringComparison.Ordinal))
        {
            await locator.FillAsync(value);
        }
        if (!string.Equals(await locator.InputValueAsync(), value, StringComparison.Ordinal))
        {
            await locator.EvaluateAsync(
                "(element, nextValue) => { element.value = nextValue; element.dispatchEvent(new Event('input', { bubbles: true })); element.dispatchEvent(new Event('change', { bubbles: true })); }",
                value);
        }
        await locator.PressAsync("Tab");
        await Task.Delay(50);
    }

    protected static async Task FillDateFieldAsync(ILocator locator, string value)
    {
        await Expect(locator).ToBeVisibleAsync();
        await locator.FocusAsync();
        await locator.FillAsync(value);
        if (!string.Equals(await locator.InputValueAsync(), value, StringComparison.Ordinal))
        {
            await locator.FillAsync(value);
        }
        if (!string.Equals(await locator.InputValueAsync(), value, StringComparison.Ordinal))
        {
            await locator.EvaluateAsync(
                "(element, nextValue) => { element.value = nextValue; element.dispatchEvent(new Event('input', { bubbles: true })); element.dispatchEvent(new Event('change', { bubbles: true })); }",
                value);
        }
        await locator.PressAsync("Tab");
        await Task.Delay(50);
    }

    protected static async Task WaitForInteractiveReadyAsync(IPage page)
    {
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.WaitForTimeoutAsync(150);
    }

    protected static async Task SubmitTemplateFormAsync(IPage page)
    {
        try
        {
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();
            await page.WaitForURLAsync("**/therapy-templates", new PageWaitForURLOptions { Timeout = 5000 });
            await WaitForInteractiveReadyAsync(page);
        }
        catch (TimeoutException)
        {
            var warnings = await page.Locator(".alert").AllInnerTextsAsync();
            var validation = await page.Locator(".validation-message").AllInnerTextsAsync();
            var details = string.Join(" | ", warnings.Concat(validation).Where(x => !string.IsNullOrWhiteSpace(x)));

            throw new TimeoutException(
                $"Template form did not navigate to /therapy-templates. Current URL: {page.Url}. Messages: {details}");
        }
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

        try
        {
            await page.WaitForURLAsync(new Regex(@".*/patient/\d+/?$"), new PageWaitForURLOptions { Timeout = 5000 });
            await WaitForInteractiveReadyAsync(page);
            return;
        }
        catch (TimeoutException)
        {
            if (await duplicatePatientWarning.IsVisibleAsync())
            {
                await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();
                await page.WaitForURLAsync(new Regex(@".*/patient/\d+/?$"), new PageWaitForURLOptions { Timeout = 5000 });
                await WaitForInteractiveReadyAsync(page);
                return;
            }
        }

        var warnings = await page.Locator(".alert").AllInnerTextsAsync();
        var validation = await page.Locator(".validation-message").AllInnerTextsAsync();
        var details = string.Join(" | ", warnings.Concat(validation).Where(x => !string.IsNullOrWhiteSpace(x)));

        throw new TimeoutException(
            $"Patient form did not navigate to /patient/{{id}}/. Current URL: {page.Url}. Messages: {details}");
    }

    protected static async Task SubmitCreateTherapyFormAsync(IPage page)
    {
        try
        {
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();
            await page.WaitForURLAsync(new Regex(@".*/patient/\d+/?$"), new PageWaitForURLOptions { Timeout = 5000 });
            await WaitForInteractiveReadyAsync(page);
        }
        catch (TimeoutException)
        {
            var validation = await page.Locator(".validation-message").AllInnerTextsAsync();
            var details = string.Join(" | ", validation.Where(x => !string.IsNullOrWhiteSpace(x)));

            throw new TimeoutException(
                $"Therapy form did not navigate to /patient/{{id}}/. Current URL: {page.Url}. Messages: {details}");
        }
    }

    protected static async Task SubmitEditTherapyFormAsync(IPage page)
    {
        try
        {
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();
            await page.WaitForURLAsync(new Regex(@".*/patient/\d+/?$"), new PageWaitForURLOptions { Timeout = 5000 });
            await WaitForInteractiveReadyAsync(page);
        }
        catch (TimeoutException)
        {
            var validation = await page.Locator(".validation-message").AllInnerTextsAsync();
            var details = string.Join(" | ", validation.Where(x => !string.IsNullOrWhiteSpace(x)));

            throw new TimeoutException(
                $"Edit therapy form did not navigate to /patient/{{id}}/. Current URL: {page.Url}. Messages: {details}");
        }
    }

    protected static Task<string[]> GetTherapyFormActionLabelsAsync(IPage page)
    {
        return page.GetByTestId("therapy-form-actions").Locator("a.btn, button.btn").EvaluateAllAsync<string[]>(
            "nodes => nodes.map(node => (node.textContent || '').trim()).filter(Boolean)");
    }

    protected static async Task<IPage> ClickPrintExpectPopupAsync(IPage page)
    {
        var popupTask = page.WaitForPopupAsync();
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Stampa" }).ClickAsync();
        return await popupTask;
    }

    protected static async Task ClickPrintExpectNoPopupAsync(IPage page)
    {
        try
        {
            var popupTask = page.WaitForPopupAsync(new PageWaitForPopupOptions { Timeout = 1500 });
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Stampa" }).ClickAsync();
            var popup = await popupTask;
            await popup.CloseAsync();
            throw new InvalidOperationException("Unexpected print popup opened.");
        }
        catch (TimeoutException)
        {
        }
    }

    protected static async Task SubmitEditPatientFormAsync(IPage page)
    {
        try
        {
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Salva" }).ClickAsync();
            await page.WaitForURLAsync(new Regex(@".*/patient/\d+/?$"), new PageWaitForURLOptions { Timeout = 5000 });
            await WaitForInteractiveReadyAsync(page);
        }
        catch (TimeoutException)
        {
            var warnings = await page.Locator(".alert").AllInnerTextsAsync();
            var validation = await page.Locator(".validation-message").AllInnerTextsAsync();
            var details = string.Join(" | ", warnings.Concat(validation).Where(x => !string.IsNullOrWhiteSpace(x)));

            throw new TimeoutException(
                $"Edit patient form did not navigate to /patient/{{id}}/. Current URL: {page.Url}. Messages: {details}");
        }
    }

    protected static async Task FilterTemplatesAsync(IPage page, string term)
    {
        await FillLiveFieldAsync(page.Locator("#templateSearch"), term);
    }

    protected static async Task FillLiveFieldAsync(ILocator locator, string value)
    {
        await locator.FocusAsync();
        await locator.FillAsync(value);
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
