using DottoressaIorio.Data;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Repositories;

public class EfUnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
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
        foreach (var entry in ex.Entries)
        {
            entry.State = EntityState.Detached;
        }
    }
}
