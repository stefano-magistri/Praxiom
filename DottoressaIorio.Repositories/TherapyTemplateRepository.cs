using DottoressaIorio.Data;
using DottoressaIorio.Models;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Repositories;

/// <summary>
///     Repository class for managing therapy templates with specialized querying capabilities.
/// </summary>
public class TherapyTemplateRepository : GenericRepository<TherapyTemplate>
{
    private readonly ApplicationDbContext context;

    /// <summary>
    ///     Initializes a new instance of the TherapyTemplateRepository class.
    /// </summary>
    /// <param name="context">The database context to use for operations.</param>
    public TherapyTemplateRepository(ApplicationDbContext context) : base(context)
    {
        this.context = context;
    }

    /// <summary>
    ///     Retrieves all non-deleted therapy templates ordered by title.
    /// </summary>
    /// <returns>A list of all active therapy templates in alphabetical order by title.</returns>
    public async Task<IList<TherapyTemplate>> GetAllOrderedAsync()
    {
        return await context.TherapyTemplates
            .OrderBy(x => x.Title)
            .ToListAsync();
    }

    /// <summary>
    ///     Checks if a therapy template with the specified title already exists.
    /// </summary>
    /// <param name="title">The title to check for existence.</param>
    /// <returns>True if a template with the specified title exists, false otherwise.</returns>
    public async Task<bool> TitleExistsAsync(string title)
    {
        var normalizedTitle = NormalizeTitle(title);
        return await context.TherapyTemplates
            .AnyAsync(x => x.Title.ToLower() == normalizedTitle);
    }

    public Task<int> CountAsync()
    {
        return context.TherapyTemplates.CountAsync();
    }

    private static string NormalizeTitle(string? title)
    {
        return (title ?? string.Empty).Trim().ToLower();
    }
}
