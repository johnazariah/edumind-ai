using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AcademicAssessment.Infrastructure.Repositories;

/// <summary>
/// Base repository implementation with functional error handling
/// Provides common CRUD operations for all entities
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TId">The ID type</typeparam>
public abstract class RepositoryBase<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class
{
    protected readonly AcademicContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected RepositoryBase(AcademicContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    /// <summary>
    /// Gets the ID from an entity
    /// Override in derived classes to specify how to extract ID
    /// </summary>
    protected abstract Guid GetEntityId(TEntity entity);

    public virtual async Task<Result<TEntity>> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await DbSet.FindAsync([id], cancellationToken);

            return entity is not null
                ? new Result<TEntity>.Success(entity)
                : Error.NotFound(typeof(TEntity).Name, id!);
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    public virtual async Task<Result<IReadOnlyList<TEntity>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await DbSet.ToListAsync(cancellationToken);
            return new Result<IReadOnlyList<TEntity>>.Success(entities.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    public virtual async Task<Result<TEntity>> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await DbSet.AddAsync(entity, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
            return new Result<TEntity>.Success(entity);
        }
        catch (DbUpdateException ex)
        {
            return Error.Conflict($"Failed to add {typeof(TEntity).Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    public virtual async Task<Result<TEntity>> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            DbSet.Update(entity);
            await Context.SaveChangesAsync(cancellationToken);
            return new Result<TEntity>.Success(entity);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Error.Conflict($"Concurrency conflict updating {typeof(TEntity).Name}: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    public virtual async Task<Result<Unit>> DeleteAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await DbSet.FindAsync([id], cancellationToken);
            if (entity is null)
            {
                return Error.NotFound(typeof(TEntity).Name, id!);
            }

            DbSet.Remove(entity);
            await Context.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    public virtual async Task<Result<bool>> ExistsAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await DbSet.FindAsync([id], cancellationToken) is not null;
            return new Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    public virtual async Task<Result<int>> CountAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var count = await DbSet.CountAsync(cancellationToken);
            return new Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    /// <summary>
    /// Executes a query and wraps exceptions in Result
    /// </summary>
    protected async Task<Result<T>> ExecuteQueryAsync<T>(
        Func<Task<T>> query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await query();
            return new Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    /// <summary>
    /// Finds a single entity matching a predicate
    /// </summary>
    protected async Task<Result<TEntity>> FindSingleAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = queryBuilder(DbSet);
            var entity = await query.SingleOrDefaultAsync(cancellationToken);

            return entity is not null
                ? new Result<TEntity>.Success(entity)
                : Error.NotFound(typeof(TEntity).Name, "matching criteria");
        }
        catch (InvalidOperationException)
        {
            return Error.Conflict($"Multiple {typeof(TEntity).Name} entities found");
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }

    /// <summary>
    /// Finds multiple entities matching criteria
    /// </summary>
    protected async Task<Result<IReadOnlyList<TEntity>>> FindManyAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = queryBuilder(DbSet);
            var entities = await query.ToListAsync(cancellationToken);
            return new Result<IReadOnlyList<TEntity>>.Success(entities.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Error.FromException(ex);
        }
    }
}
