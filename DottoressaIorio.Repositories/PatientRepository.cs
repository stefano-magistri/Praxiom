using DottoressaIorio.Data;
using DottoressaIorio.Models;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Repositories;

/// <summary>
///     Repository class for managing patient data with specialized querying capabilities.
/// </summary>
public class PatientRepository : GenericRepository<Patient>
{
    private readonly ApplicationDbContext context;

    /// <summary>
    ///     Initializes a new instance of the PatientRepository class.
    /// </summary>
    /// <param name="context">The database context to use for operations.</param>
    public PatientRepository(ApplicationDbContext context) : base(context)
    {
        this.context = context;
    }

    /// <summary>
    ///     Creates a base query for patients that excludes deleted records and orders by last name and first name.
    /// </summary>
    /// <returns>An IQueryable of patients with the specified filters and ordering.</returns>
    private IQueryable<Patient> BaseQuery()
    {
        return context.Patients
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName);
    }

    /// <summary>
    ///     Retrieves all non-deleted patients ordered by last name and first name.
    /// </summary>
    /// <returns>A list of all active patients in alphabetical order.</returns>
    public async Task<IList<Patient>> GetAllOrderedAsync()
    {
        return await BaseQuery().ToListAsync();
    }

    /// <summary>
    ///     Searches for patients based on a search term that can match first name, last name, or full name.
    /// </summary>
    /// <param name="searchTerm">The term to search for in patient names.</param>
    /// <returns>A list of patients matching the search criteria.</returns>
    public async Task<IList<Patient>> SearchPatientsAsync(string searchTerm)
    {
        return await ApplySearch(BaseQuery(), searchTerm).ToListAsync();
    }

    /// <summary>
    ///     Searches for patients using the same filtering rules as the list view and includes therapies for export.
    /// </summary>
    /// <param name="searchTerm">The term to search for in patient names.</param>
    /// <returns>A list of matching patients with their therapies loaded.</returns>
    public async Task<IList<Patient>> SearchPatientsWithTherapiesAsync(string searchTerm)
    {
        return await ApplySearch(BaseQuery().Include(p => p.Therapies), searchTerm).ToListAsync();
    }

    /// <summary>
    ///     Checks if a patient with the same first name, last name, and date of birth already exists.
    /// </summary>
    /// <param name="patient">The patient to check for existence.</param>
    /// <returns>True if a matching patient exists, false otherwise.</returns>
    public async Task<bool> PatientExistsAsync(Patient patient)
    {
        var normalizedFirstName = NormalizeSearchToken(patient.FirstName);
        var normalizedLastName = NormalizeSearchToken(patient.LastName);
        var dateOfBirth = patient.DateOfBirth;

        return await context.Patients.AnyAsync(p =>
            p.FirstName.ToLower() == normalizedFirstName &&
            p.LastName.ToLower() == normalizedLastName &&
            p.DateOfBirth == dateOfBirth);
    }

    private static string NormalizeSearchToken(string? value)
    {
        return (value ?? string.Empty).Trim().ToLower();
    }

    private static IQueryable<Patient> ApplySearch(IQueryable<Patient> query, string? searchTerm)
    {
        var normalizedSearchTerm = NormalizeSearchToken(searchTerm);
        if (string.IsNullOrEmpty(normalizedSearchTerm))
        {
            return query;
        }

        var names = normalizedSearchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return names.Aggregate(query,
            (current, name) => current.Where(p =>
                p.FirstName.ToLower().Contains(name) || p.LastName.ToLower().Contains(name) ||
                (p.FirstName.ToLower() + " " + p.LastName.ToLower()).Contains(normalizedSearchTerm)));
    }
}
