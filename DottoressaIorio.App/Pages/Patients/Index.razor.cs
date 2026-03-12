using DottoressaIorio.Models;
using DottoressaIorio.Repositories;
using DottoressaIorio.Services.Licensing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Globalization;

namespace DottoressaIorio.App.Pages.Patients;

/// <summary>
///     Base component for the Patients index page. Handles patient search, pagination,
///     and navigation to edit/delete views.
/// </summary>
public class PatientsBase : ComponentBase
{
    /// <summary>
    ///     Number of patients displayed per page.
    /// </summary>
    protected readonly int PageSize = 10; // Number of patients per page

    /// <summary>
    ///     Cached list of all patients loaded from the repository.
    /// </summary>
    private IList<Patient> patients;

    /// <summary>
    ///     Repository used to query patient data.
    /// </summary>
    [Inject]
    public PatientRepository Repository { get; set; }

    /// <summary>
    ///     Navigation manager used to navigate between pages.
    /// </summary>
    [Inject]
    public NavigationManager NavigationManager { get; set; }

    [Inject]
    public ILicenseService LicenseService { get; set; }

    protected string CreationInlineMessage { get; set; } = string.Empty;

    /// <summary>
    ///     The subset of patients displayed on the current page after filtering and pagination.
    /// </summary>
    protected IList<Patient> DisplayedPatients { get; set; }

    /// <summary>
    ///     The list of patients matching the current search term.
    /// </summary>
    protected IList<Patient> FilteredPatients { get; set; }

    /// <summary>
    ///     The current page number (1-based).
    /// </summary>
    protected int CurrentPage { get; set; } = 1;

    /// <summary>
    ///     The total number of pages based on <see cref="FilteredPatients" /> and <see cref="PageSize" />.
    /// </summary>
    protected int TotalPages => (int)Math.Ceiling((double)FilteredPatients.Count / PageSize);

    /// <summary>
    ///     The current search term used to filter patients.
    /// </summary>
    protected string SearchTerm { get; set; } = "";

    /// <summary>
    ///     Initializes the component by loading patients and computing the initial view.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task OnInitializedAsync()
    {
        await LicenseService.InitializeAsync();
        CreationInlineMessage = await LicenseService.GetPatientCreationBlockMessageAsync() ?? string.Empty;
        patients = await Repository.GetAllOrderedAsync();
        await UpdateDisplayedPatients();
    }

    /// <summary>
    ///     Updates the <see cref="DisplayedPatients" /> collection by applying the current
    ///     search filter and pagination.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task UpdateDisplayedPatients()
    {
        await FilterPatientsBySearchTermAsync();
        var startIndex = (CurrentPage - 1) * PageSize;
        DisplayedPatients = FilteredPatients.Skip(startIndex).Take(PageSize).ToList();
    }

    /// <summary>
    ///     Filters patients using the current <see cref="SearchTerm" />.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task FilterPatientsBySearchTermAsync()
    {
        FilteredPatients = await Repository.SearchPatientsAsync(SearchTerm);
    }

    /// <summary>
    ///     Handles changes to the search input and refreshes the displayed patients.
    /// </summary>
    /// <param name="e">The change event args containing the new search term.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task SearchPatients(ChangeEventArgs e)
    {
        CurrentPage = 1;
        SearchTerm = e.Value?.ToString() ?? string.Empty;
        StateHasChanged();
        await UpdateDisplayedPatients();
    }

    /// <summary>
    ///     Handles page selection from the pagination component.
    /// </summary>
    /// <param name="page">The page number to navigate to (1-based).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task HandleSelectedPage(int page)
    {
        CurrentPage = page;
        await UpdateDisplayedPatients();
    }

    /// <summary>
    ///     Navigates to the edit page for the patient with the specified identifier.
    /// </summary>
    /// <param name="id">The patient identifier.</param>
    protected void NavigateToEditPatient(int id)
    {
        NavigationManager.NavigateTo($"/patient/{id}/edit/");
    }

    /// <summary>
    ///     Navigates to the detail page for the patient with the specified identifier.
    /// </summary>
    /// <param name="id">The patient identifier.</param>
    protected void NavigateToPatient(int id)
    {
        NavigationManager.NavigateTo($"/patient/{id}/");
    }

    /// <summary>
    ///     Navigates to the delete confirmation page for the patient with the specified identifier.
    /// </summary>
    /// <param name="id">The patient identifier.</param>
    protected void NavigateToDeletePatient(int id)
    {
        NavigationManager.NavigateTo($"/patient/{id}/delete");
    }

    /// <summary>
    ///     Supports keyboard activation for clickable patient rows.
    /// </summary>
    /// <param name="args">The keyboard event data.</param>
    /// <param name="id">The patient identifier.</param>
    protected void HandlePatientRowKeyDown(KeyboardEventArgs args, int id)
    {
        if (args.Key is "Enter" or " ")
        {
            NavigateToPatient(id);
        }
    }

    /// <summary>
    ///     Navigates to the previous page if not already on the first page.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task PreviousPage()
    {
        if (CurrentPage != 1)
        {
            CurrentPage--;
            await UpdateDisplayedPatients();
        }
    }

    /// <summary>
    ///     Navigates to the next page if not already on the last page.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task NextPage()
    {
        if (CurrentPage != TotalPages)
        {
            CurrentPage++;
            await UpdateDisplayedPatients();
        }
    }

    protected static string FormatPatientDisplayName(string? firstName, string? lastName)
    {
        return PatientNameFormatter.FormatFullName(firstName, lastName);
    }

    protected static DateTime? GetLastVisitDate(Patient patient)
    {
        return patient.Therapies?.Count > 0
            ? patient.Therapies.Max(therapy => therapy.CreatedDate)
            : null;
    }

    protected static int? GetPatientAge(DateTime? dateOfBirth)
    {
        if (!dateOfBirth.HasValue)
        {
            return null;
        }

        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    protected static string FormatTitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
            value.ToLower(CultureInfo.CurrentCulture));
    }

    protected string GetCsvExportUrl()
    {
        var encodedSearch = Uri.EscapeDataString(SearchTerm ?? string.Empty);
        return $"/patients/export-csv?search={encodedSearch}";
    }
}

