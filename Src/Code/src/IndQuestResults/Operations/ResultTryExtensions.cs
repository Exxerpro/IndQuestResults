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
        if (func is null)
        {
            return Result<T>.WithFailure("Function cannot be null");
        }
        
        if (mapError is null)
        {
            return Result<T>.WithFailure("Map error function cannot be null");
        }
        
        try
        {
            return Result<T>.Success(func());
        }
        catch (Exception ex)
        {
            return Result<T>.WithFailure(mapError(ex), default, ex);
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
        if (func is null)
        {
            return Result<T>.WithFailure("Function cannot be null");
        }
        
        if (mapError is null)
        {
            return Result<T>.WithFailure("Map error function cannot be null");
        }
        
        try
        {
            var value = await func().ConfigureAwait(false);
            return Result<T>.Success(value);
        }
        catch (Exception ex)
        {
            return Result<T>.WithFailure(mapError(ex), default, ex);
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
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (result is null) { return Result<TOut>.WithFailure("Result cannot be null"); }
        if (map is null) { return Result<TOut>.WithFailure("Map function cannot be null"); }
        if (mapError is null) { return Result<TOut>.WithFailure("Map error function cannot be null"); }
#pragma warning restore IDE0046
        
        return result.Bind(value =>
        {
            try
            {
                return Result<TOut>.Success(map(value));
            }
            catch (Exception ex)
            {
                return Result<TOut>.WithFailure(mapError(ex), default, ex);
            }
        });
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
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (result is null) { return Result<TOut>.WithFailure("Result cannot be null"); }
        if (bind is null) { return Result<TOut>.WithFailure("Bind function cannot be null"); }
        if (mapError is null) { return Result<TOut>.WithFailure("Map error function cannot be null"); }
#pragma warning restore IDE0046
        
        return result.Bind(value =>
        {
            try
            {
                return bind(value);
            }
            catch (Exception ex)
            {
                return Result<TOut>.WithFailure(mapError(ex), default, ex);
            }
        });
    }

    /// <summary>
    /// Executes an action and returns Success() or Failure(mapped exception).
    /// </summary>
    /// <param name="action">Action that may throw.</param>
    /// <param name="mapError">Mapper from Exception to error string.</param>
    /// <returns>A successful Result or a failure with the mapped error.</returns>
    public static Result Try(Action action, Func<Exception, string> mapError)
    {
        if (action is null)
        {
            return Result.WithFailure("Action cannot be null");
        }
        
        if (mapError is null)
        {
            return Result.WithFailure("Map error function cannot be null");
        }
        
        try
        {
            action();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.WithFailure(mapError(ex), ex);
        }
    }

    /// <summary>
    /// Executes an async action and returns Success() or Failure(mapped exception).
    /// </summary>
    /// <param name="action">Async action that may throw.</param>
    /// <param name="mapError">Mapper from Exception to error string.</param>
    /// <returns>A task with successful Result or failure with the mapped error.</returns>
    public static async Task<Result> TryAsync(Func<Task> action, Func<Exception, string> mapError)
    {
        if (action is null)
        {
            return Result.WithFailure("Action cannot be null");
        }
        
        if (mapError is null)
        {
            return Result.WithFailure("Map error function cannot be null");
        }
        
        try
        {
            await action().ConfigureAwait(false);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.WithFailure(mapError(ex), ex);
        }
    }
}

