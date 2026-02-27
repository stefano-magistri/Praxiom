using DottoressaIorio.Data;
using DottoressaIorio.Models.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DottoressaIorio.Repositories;

/// <summary>
///     A generic repository implementation that provides common CRUD operations for entities.
/// </summary>
/// <typeparam name="T">The type of entity this repository handles. Must implement IDataHandler.</typeparam>
public class GenericRepository<T> where T : class, IDataHandler
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    ///     Initializes a new instance of the GenericRepository class.
    /// </summary>
    /// <param name="context">The database context to use for operations.</param>
    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    ///     Retrieves an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <returns>The entity if found, null otherwise.</returns>
    public async Task<T?> GetByIdAsync(int id)
    {
        var entity = await _context.Set<T>().FindAsync(id);

        if (entity is null) return null;

        _context.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    /// <summary>
    ///     Retrieves all entities of type T from the database.
    /// </summary>
    /// <returns>A list of all entities.</returns>
    public async Task<IList<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    /// <summary>
    ///     Adds a new entity to the database.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    public Task AddAsync(T entity)
    {
        entity.CreatedDate = DateTime.Now;
        _context.Set<T>().Add(entity);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Updates an existing entity in the database.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public Task UpdateAsync(T entity)
    {
        entity.EditDate = DateTime.Now;
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Soft deletes an entity by marking it as deleted.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    public Task DeleteAsync(T entity)
    {
        var entityType = _context.Model.FindEntityType(typeof(T))
                         ?? throw new InvalidOperationException($"Entity type {typeof(T).Name} is not mapped.");
        var primaryKey = entityType.FindPrimaryKey()
                         ?? throw new InvalidOperationException($"Entity type {typeof(T).Name} has no primary key.");
        var keyValues = GetKeyValues(entity, primaryKey.Properties);

        var target = FindTrackedEntityByKey(primaryKey.Properties, keyValues)
                     ?? CreateKeyOnlyEntity(primaryKey.Properties, keyValues);

        target.Deleted = true;
        target.EditDate = DateTime.Now;

        var entry = _context.Entry(target);
        if (entry.State == EntityState.Detached) _context.Set<T>().Attach(target);

        _context.Entry(target).Property(x => x.Deleted).IsModified = true;
        _context.Entry(target).Property(x => x.EditDate).IsModified = true;

        return Task.CompletedTask;
    }

    private object?[] GetKeyValues(T entity, IReadOnlyList<IProperty> keyProperties)
    {
        var sourceEntry = _context.Entry(entity);
        return keyProperties.Select(p => sourceEntry.Property(p.Name).CurrentValue).ToArray();
    }

    private T? FindTrackedEntityByKey(IReadOnlyList<IProperty> keyProperties, object?[] keyValues)
    {
        foreach (var trackedEntry in _context.ChangeTracker.Entries<T>())
        {
            var allMatch = true;
            for (var i = 0; i < keyProperties.Count; i++)
            {
                var trackedValue = trackedEntry.Property(keyProperties[i].Name).CurrentValue;
                if (!Equals(trackedValue, keyValues[i]))
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch) return trackedEntry.Entity;
        }

        return null;
    }

    private static T CreateKeyOnlyEntity(IReadOnlyList<IProperty> keyProperties, object?[] keyValues)
    {
        var instance = Activator.CreateInstance<T>()
                       ?? throw new InvalidOperationException($"Could not create instance of {typeof(T).Name}.");

        for (var i = 0; i < keyProperties.Count; i++)
        {
            var keyProperty = typeof(T).GetProperty(keyProperties[i].Name)
                              ?? throw new InvalidOperationException(
                                  $"Key property '{keyProperties[i].Name}' was not found on {typeof(T).Name}.");
            keyProperty.SetValue(instance, keyValues[i]);
        }

        return instance;
    }
}