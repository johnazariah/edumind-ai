using System.Reflection;
using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using AcademicAssessment.Core.Models;

namespace AcademicAssessment.Web.Services;

/// <summary>
/// Base stub repository that returns empty results for all operations
/// Uses reflection to implement interface methods dynamically
/// </summary>
public abstract class StubRepositoryBase
{
    protected static Task<Result<T>> NotFound<T>(string entityName, object id)
    {
        return Task.FromResult(Result.Failure<T>(
            Error.NotFound(entityName, id)));
    }

    protected static Task<Result<IReadOnlyList<T>>> EmptyList<T>()
    {
        return Task.FromResult(Result.Success<IReadOnlyList<T>>(
            Array.Empty<T>()));
    }

    protected static Task<Result<T>> WriteNotSupported<T>(string operation = "write")
    {
        return Task.FromResult(Result.Failure<T>(
            Error.Validation($"Stub repository does not support {operation} operations")));
    }

    protected static Task<Result<Unit>> UnitWriteNotSupported(string operation = "write")
    {
        return Task.FromResult(Result.Failure<Unit>(
            Error.Validation($"Stub repository does not support {operation} operations")));
    }

    protected static Task<Result<bool>> FalseResult()
    {
        return Task.FromResult(Result.Success(false));
    }

    protected static Task<Result<int>> ZeroCount()
    {
        return Task.FromResult(Result.Success(0));
    }

    protected static Task<Result<double>> ZeroDouble()
    {
        return Task.FromResult(Result.Success(0.0));
    }

    protected static Task<Result<TimeSpan>> ZeroTimeSpan()
    {
        return Task.FromResult(Result.Success(TimeSpan.Zero));
    }
}

/// <summary>
/// Stub implementation that returns empty results for all repository methods
/// </summary>
public class UniversalStubRepository<TEntity, TKey> : StubRepositoryBase, IRepository<TEntity, TKey>
    where TEntity : class
{
    private readonly string _entityName = typeof(TEntity).Name;

    public virtual Task<Result<TEntity>> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => NotFound<TEntity>(_entityName, id!);

    public virtual Task<Result<IReadOnlyList<TEntity>>> GetAllAsync(CancellationToken cancellationToken = default)
        => EmptyList<TEntity>();

    public virtual Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => WriteNotSupported<TEntity>();

    public virtual Task<Result<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        => WriteNotSupported<TEntity>();

    public virtual Task<Result<Unit>> DeleteAsync(TKey id, CancellationToken cancellationToken = default)
        => UnitWriteNotSupported();

    public virtual Task<Result<bool>> ExistsAsync(TKey id, CancellationToken cancellationToken = default)
        => FalseResult();

    public virtual Task<Result<int>> CountAsync(CancellationToken cancellationToken = default)
        => ZeroCount();
}
