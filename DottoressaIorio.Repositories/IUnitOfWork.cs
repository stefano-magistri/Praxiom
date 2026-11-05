namespace DottoressaIorio.Repositories;

/// <summary>
/// Provides a shared save and transaction boundary for repository operations.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes in the current unit of work.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the provided action inside a database transaction and saves once before commit.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
}
