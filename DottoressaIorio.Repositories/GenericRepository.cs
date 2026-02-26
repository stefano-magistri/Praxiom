using DottoressaIorio.Data;
using DottoressaIorio.Models.Abstract;
using Microsoft.EntityFrameworkCore;

namespace DottoressaIorio.Repositories;

/// <summary>
/// A generic repository implementation that provides common CRUD operations for entities.
/// </summary>
/// <typeparam name="T">The type of entity this repository handles. Must implement IDataHandler.</typeparam>
public class GenericRepository<T> where T : class, IDataHandler
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the GenericRepository class.
    /// </summary>
    /// <param name="context">The database context to use for operations.</param>
    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <returns>The entity if found, null otherwise.</returns>
    public async Task<T?> GetByIdAsync(int id)
    {
        var entity = await _context.Set<T>().FindAsync(id);

        if (entity is null)
            return null;

        _context.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    /// <summary>
    /// Retrieves all entities of type T from the database.
    /// </summary>
    /// <returns>A list of all entities.</returns>
    public async Task<IList<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    /// <summary>
    /// Adds a new entity to the database.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    public Task AddAsync(T entity)
    {
        entity.CreatedDate = DateTime.Now;
        _context.Set<T>().Add(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates an existing entity in the database.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public Task UpdateAsync(T entity)
    {
        entity.EditDate = DateTime.Now;
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Soft deletes an entity by marking it as deleted.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    public async Task DeleteAsync(T entity)
    {
        entity.Deleted = true;
        await UpdateAsync(entity);
    }
}
