namespace AcademicAssessment.Core.Common;

/// <summary>
/// Railway-oriented programming result type for functional error handling.
/// Represents either a successful computation with a value, or a failure with an error.
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public abstract record Result<T>
{
    /// <summary>
    /// Represents a successful computation with a value
    /// </summary>
    public sealed record Success(T Value) : Result<T>;

    /// <summary>
    /// Represents a failed computation with an error
    /// </summary>
    public sealed record Failure(Error Error) : Result<T>;

    /// <summary>
    /// Gets whether this result represents a success
    /// </summary>
    public bool IsSuccess => this is Success;

    /// <summary>
    /// Gets whether this result represents a failure
    /// </summary>
    public bool IsFailure => this is Failure;

    /// <summary>
    /// Implicitly converts a value to a successful result
    /// </summary>
    public static implicit operator Result<T>(T value) => new Success(value);

    /// <summary>
    /// Implicitly converts an error to a failed result
    /// </summary>
    public static implicit operator Result<T>(Error error) => new Failure(error);

    private Result() { } // Prevent external inheritance
}

/// <summary>
/// Represents an error with a code, message, and optional exception
/// </summary>
public sealed record Error(
    string Code,
    string Message,
    Exception? Exception = null)
{
    /// <summary>
    /// Creates an error from an exception
    /// </summary>
    public static Error FromException(Exception exception, string? code = null) =>
        new(code ?? "UNHANDLED_EXCEPTION", exception.Message, exception);

    /// <summary>
    /// Creates a validation error
    /// </summary>
    public static Error Validation(string message) =>
        new("VALIDATION_ERROR", message);

    /// <summary>
    /// Creates a not found error
    /// </summary>
    public static Error NotFound(string entityName, object id) =>
        new("NOT_FOUND", $"{entityName} with id '{id}' was not found");

    /// <summary>
    /// Creates an unauthorized error
    /// </summary>
    public static Error Unauthorized(string message = "Unauthorized access") =>
        new("UNAUTHORIZED", message);

    /// <summary>
    /// Creates a forbidden error
    /// </summary>
    public static Error Forbidden(string message = "Access forbidden") =>
        new("FORBIDDEN", message);

    /// <summary>
    /// Creates a conflict error
    /// </summary>
    public static Error Conflict(string message) =>
        new("CONFLICT", message);
}

/// <summary>
/// Unit type for representing void/no value in a functional way
/// </summary>
public sealed record Unit
{
    private Unit() { }

    /// <summary>
    /// The singleton instance of Unit
    /// </summary>
    public static readonly Unit Value = new();

    /// <summary>
    /// Implicitly converts Unit to Result&lt;Unit&gt; for convenience
    /// </summary>
    public static implicit operator Result<Unit>(Unit _) => new Result<Unit>.Success(Value);
}

/// <summary>
/// Extension methods for Result type to enable functional composition
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Maps a function over a successful result, or propagates the failure
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> mapper) =>
        result switch
        {
            Result<TIn>.Success(var value) => mapper(value),
            Result<TIn>.Failure(var error) => error,
            _ => throw new InvalidOperationException("Invalid result state")
        };

    /// <summary>
    /// Asynchronously maps a function over a successful result, or propagates the failure
    /// </summary>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> mapper) =>
        result switch
        {
            Result<TIn>.Success(var value) => await mapper(value),
            Result<TIn>.Failure(var error) => error,
            _ => throw new InvalidOperationException("Invalid result state")
        };

    /// <summary>
    /// Asynchronously maps a function over a successful result task, or propagates the failure
    /// </summary>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<TOut>> mapper)
    {
        var result = await resultTask;
        return await result.MapAsync(mapper);
    }

    /// <summary>
    /// Binds a function that returns a Result over a successful result (flatMap/chain)
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Result<TOut>> binder) =>
        result switch
        {
            Result<TIn>.Success(var value) => binder(value),
            Result<TIn>.Failure(var error) => error,
            _ => throw new InvalidOperationException("Invalid result state")
        };

    /// <summary>
    /// Asynchronously binds a function that returns a Result over a successful result
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> binder) =>
        result switch
        {
            Result<TIn>.Success(var value) => await binder(value),
            Result<TIn>.Failure(var error) => error,
            _ => throw new InvalidOperationException("Invalid result state")
        };

    /// <summary>
    /// Asynchronously binds a function over a successful result task
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<Result<TOut>>> binder)
    {
        var result = await resultTask;
        return await result.BindAsync(binder);
    }

    /// <summary>
    /// Pattern matches on the result, executing the appropriate function
    /// </summary>
    public static TOut Match<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> onSuccess,
        Func<Error, TOut> onFailure) =>
        result switch
        {
            Result<TIn>.Success(var value) => onSuccess(value),
            Result<TIn>.Failure(var error) => onFailure(error),
            _ => throw new InvalidOperationException("Invalid result state")
        };

    /// <summary>
    /// Asynchronously pattern matches on the result
    /// </summary>
    public static async Task<TOut> MatchAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> onSuccess,
        Func<Error, Task<TOut>> onFailure) =>
        result switch
        {
            Result<TIn>.Success(var value) => await onSuccess(value),
            Result<TIn>.Failure(var error) => await onFailure(error),
            _ => throw new InvalidOperationException("Invalid result state")
        };

    /// <summary>
    /// Executes an action on success, useful for side effects
    /// </summary>
    public static Result<T> Tap<T>(
        this Result<T> result,
        Action<T> action)
    {
        if (result is Result<T>.Success(var value))
        {
            action(value);
        }
        return result;
    }

    /// <summary>
    /// Asynchronously executes an action on success
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(
        this Result<T> result,
        Func<T, Task> action)
    {
        if (result is Result<T>.Success(var value))
        {
            await action(value);
        }
        return result;
    }

    /// <summary>
    /// Executes an action on failure, useful for logging
    /// </summary>
    public static Result<T> TapError<T>(
        this Result<T> result,
        Action<Error> action)
    {
        if (result is Result<T>.Failure(var error))
        {
            action(error);
        }
        return result;
    }

    /// <summary>
    /// Returns the value if success, or a default value if failure
    /// </summary>
    public static T GetValueOrDefault<T>(
        this Result<T> result,
        T defaultValue = default!) =>
        result switch
        {
            Result<T>.Success(var value) => value,
            _ => defaultValue
        };

    /// <summary>
    /// Returns the value if success, or throws the exception if failure
    /// </summary>
    public static T GetValueOrThrow<T>(this Result<T> result) =>
        result switch
        {
            Result<T>.Success(var value) => value,
            Result<T>.Failure(var error) => throw error.Exception
                ?? new InvalidOperationException(error.Message),
            _ => throw new InvalidOperationException("Invalid result state")
        };

    /// <summary>
    /// Combines multiple results into a single result containing a list
    /// Fails if any result fails
    /// </summary>
    public static Result<IReadOnlyList<T>> Sequence<T>(
        this IEnumerable<Result<T>> results)
    {
        var list = new List<T>();
        foreach (var result in results)
        {
            if (result is Result<T>.Success(var value))
            {
                list.Add(value);
            }
            else if (result is Result<T>.Failure(var error))
            {
                return error;
            }
        }
        return list.AsReadOnly();
    }

    /// <summary>
    /// Asynchronously combines multiple result tasks into a single result
    /// </summary>
    public static async Task<Result<IReadOnlyList<T>>> SequenceAsync<T>(
        this IEnumerable<Task<Result<T>>> resultTasks)
    {
        var results = await Task.WhenAll(resultTasks);
        return results.Sequence();
    }

    /// <summary>
    /// Ensures a condition is met, returning an error if not
    /// </summary>
    public static Result<T> Ensure<T>(
        this Result<T> result,
        Func<T, bool> predicate,
        Error error) =>
        result.Bind(value =>
            predicate(value) ? result : error);

    /// <summary>
    /// Ensures a condition is met asynchronously
    /// </summary>
    public static Task<Result<T>> EnsureAsync<T>(
        this Result<T> result,
        Func<T, Task<bool>> predicate,
        Error error) =>
        result.BindAsync(async value =>
            await predicate(value) ? result : error);
}
