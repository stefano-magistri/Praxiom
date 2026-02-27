using DottoressaIorio.Data;
using DottoressaIorio.Models;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Repositories;

/// <summary>
/// Repository class for managing therapy records associated with specific patients.
/// </summary>
public class PatientTherapyRepository : GenericRepository<Therapy>
{
    private readonly ApplicationDbContext context;

    /// <summary>
    /// Initializes a new instance of the PatientTherapyRepository class.
    /// </summary>
    /// <param name="context">The database context to use for operations.</param>
    public PatientTherapyRepository(ApplicationDbContext context) : base(context)
    {
        this.context = context;
    }

    /// <summary>
    /// Retrieves all non-deleted therapies for a specific patient, ordered by creation date.
    /// </summary>
    /// <param name="patientId">The unique identifier of the patient.</param>
    /// <returns>A list of therapies associated with the specified patient.</returns>
    public async Task<IList<Therapy>> GetAllAsync(int patientId)
    {
        return await context.Therapies
            .Where(t => t.PatientId == patientId)
            .OrderBy(t => t.CreatedDate)
            .ToListAsync();
    }

    /// <summary>
    /// Backward-compatible alias for <see cref="GetAllAsync(int)"/>.
    /// </summary>
    [Obsolete("Use GetAllAsync(int patientId) instead.")]
    public Task<IList<Therapy>> GetAllsync(int patientId)
    {
        return GetAllAsync(patientId);
    }
}
