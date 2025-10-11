using AcademicAssessment.Core.Common;

namespace AcademicAssessment.Core.Interfaces;

/// <summary>
/// Generic repository interface for data access with functional error handling
/// All operations return Result&lt;T&gt; for railway-oriented programming
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TId">The ID type (typically Guid)</typeparam>
public interface IRepository<TEntity, TId> where TEntity : class
{
    /// <summary>
    /// Gets an entity by ID
    /// </summary>
    Task<Result<TEntity>> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities (with tenant filtering applied)
    /// </summary>
    Task<Result<IReadOnlyList<TEntity>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity
    /// </summary>
    Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    Task<Result<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by ID
    /// </summary>
    Task<Result<Unit>> DeleteAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an entity exists by ID
    /// </summary>
    Task<Result<bool>> ExistsAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of entities (with tenant filtering applied)
    /// </summary>
    Task<Result<int>> CountAsync(CancellationToken cancellationToken = default);
}
