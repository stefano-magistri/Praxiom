using DottoressaIorio.Data;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Repositories;

/// <summary>
/// Coordinates repository saves and transactions for a single EF Core context.
/// </summary>
public class EfUnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    /// <summary>
    /// Persists the current unit of work and detaches failed entries after update exceptions.
    /// </summary>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            DetachFailedEntries(ex);
            throw;
        }
    }

    /// <summary>
    /// Executes the supplied action within a transaction and saves the accumulated changes once.
    /// </summary>
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await action();
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            DetachFailedEntries(ex);
            throw;
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static void DetachFailedEntries(DbUpdateException ex)
    {
        // Detaching failed entries prevents one bad entity from poisoning later saves on the same DbContext.
        foreach (var entry in ex.Entries) entry.State = EntityState.Detached;
    }
}
