using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace DottoressaIorio.E2E;

[Collection(PlaywrightCollection.Name)]
public sealed class SmokeTests(PlaywrightFixture fixture)
{
    [Fact]
    [Trait("Category", "E2E")]
    public async Task OpenApp_Login_NavigateToPatients_ShouldLoadPatientsPage()
    {
        await using var context = await fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync("/guide", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await PerformLoginIfNeededAsync(page, fixture.Settings);

        await page.GetByTestId("patients-nav-link").ClickAsync();

        await Expect(page.GetByTestId("patients-page")).ToBeVisibleAsync();
        await Expect(page.GetByTestId("page-heading")).ToHaveTextAsync("Pazienti");
    }

    private static async Task PerformLoginIfNeededAsync(IPage page, E2eSettings settings)
    {
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

        await Expect(page.GetByTestId("user-greeting")).ToBeVisibleAsync();
    }
}
