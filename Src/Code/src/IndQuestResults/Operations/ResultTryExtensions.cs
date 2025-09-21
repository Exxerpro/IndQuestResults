using System;
using System.Threading.Tasks;

namespace IndQuestResults.Operations;

/// <summary>
/// Helpers to capture exceptions and turn them into Result failures.
/// </summary>
public static class ResultTryExtensions
{
    /// <summary>
    /// Executes a function and returns Success(value) or Failure(mapped exception).
    /// </summary>
    /// <typeparam name="T">The return value type.</typeparam>
    /// <param name="func">Function that may throw.</param>
    /// <param name="mapError">Mapper from Exception to error string.</param>
    /// <returns>A successful Result or a failure with the mapped error.</returns>
    public static Result<T> Try<T>(Func<T> func, Func<Exception, string> mapError)
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(mapError);
        try
        {
            return Result<T>.Success(func());
        }
        catch (Exception ex)
        {
            return Result<T>.WithFailure(mapError(ex));
        }
    }

    /// <summary>
    /// Executes an async function and returns Success(value) or Failure(mapped exception).
    /// </summary>
    /// <typeparam name="T">The return value type.</typeparam>
    /// <param name="func">Async function that may throw.</param>
    /// <param name="mapError">Mapper from Exception to error string.</param>
    /// <returns>A task with successful Result or failure with the mapped error.</returns>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> func, Func<Exception, string> mapError)
    {
        ArgumentNullException.ThrowIfNull(func);
        ArgumentNullException.ThrowIfNull(mapError);
        try
        {
            var value = await func().ConfigureAwait(false);
            return Result<T>.Success(value);
        }
        catch (Exception ex)
        {
            return Result<T>.WithFailure(mapError(ex));
        }
    }

    /// <summary>
    /// Maps a Result using a function that may throw; failures are converted using mapError.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TOut">The mapped value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="map">Mapping function that may throw.</param>
    /// <param name="mapError">Mapper from Exception to error string.</param>
    /// <returns>A mapped Result or a failure with the mapped error.</returns>
    public static Result<TOut> MapTry<T, TOut>(this Result<T> result, Func<T, TOut> map, Func<Exception, string> mapError)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(mapError);
        if (!result.IsRecoverable)
        {
            return Result<TOut>.WithFailure(result.Errors);
        }
        try
        {
            return Result<TOut>.Success(map(result.Value!));
        }
        catch (Exception ex)
        {
            return Result<TOut>.WithFailure(mapError(ex));
        }
    }

    /// <summary>
    /// Binds a Result using a function that may throw; failures are converted using mapError.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TOut">The bound value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="bind">Binding function that may throw.</param>
    /// <param name="mapError">Mapper from Exception to error string.</param>
    /// <returns>The bound Result or a failure with the mapped error.</returns>
    public static Result<TOut> BindTry<T, TOut>(this Result<T> result, Func<T, Result<TOut>> bind, Func<Exception, string> mapError)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);
        if (!result.IsRecoverable)
        {
            return Result<TOut>.WithFailure(result.Errors);
        }
        try
        {
            return bind(result.Value!);
        }
        catch (Exception ex)
        {
            return Result<TOut>.WithFailure(mapError(ex));
        }
    }
}

