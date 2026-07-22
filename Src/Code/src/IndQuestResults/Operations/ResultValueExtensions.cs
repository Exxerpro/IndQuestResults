using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IndQuestResults.Operations;

/// <summary>
/// Value-side fluent helpers for Result{T} and small control-flow helpers for Result.
/// </summary>
public static class ResultValueExtensions
{
    /// <summary>
    /// Gets the value for a successful result or the provided default when failed or value is null.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="defaultValue">The fallback value when not successful.</param>
    /// <returns>The successful value or the provided default.</returns>
    public static T ValueOr<T>(this Result<T> result, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess && result.Value is not null ? result.Value : defaultValue;
    }

    /// <summary>
    /// Gets the value for a successful result or computes a default when failed or value is null.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="defaultFactory">Factory to compute a fallback value.</param>
    /// <returns>The successful value or the computed default.</returns>
    public static T ValueOr<T>(this Result<T> result, Func<T> defaultFactory)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(defaultFactory);
        return result.IsSuccess && result.Value is not null ? result.Value : defaultFactory();
    }

    /// <summary>
    /// Returns the same result if successful, or the provided fallback when failed.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="fallback">The fallback Result when failed.</param>
    /// <returns>The original Result or the fallback.</returns>
    public static Result<T> OrElse<T>(this Result<T> result, Result<T> fallback)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (result is null) { return Result<T>.WithFailure("Result cannot be null"); }
        if (fallback is null) { return Result<T>.WithFailure("Fallback result cannot be null"); }
#pragma warning restore IDE0046
        
        return result.IsFailure ? fallback : result;
    }

    /// <summary>
    /// Returns the same result if successful, or computes a fallback when failed.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="fallbackFactory">Factory to produce a fallback Result.</param>
    /// <returns>The original Result or the computed fallback.</returns>
    public static Result<T> OrElse<T>(this Result<T> result, Func<Result<T>> fallbackFactory)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (result is null) { return Result<T>.WithFailure("Result cannot be null"); }
        if (fallbackFactory is null) { return Result<T>.WithFailure("Fallback factory function cannot be null"); }
#pragma warning restore IDE0046
        
        return result.IsFailure ? fallbackFactory() : result;
    }

    /// <summary>
    /// Async: gets the value or default after awaiting.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task producing a Result.</param>
    /// <param name="defaultValue">The fallback value when failed.</param>
    /// <returns>A task returning the value or default.</returns>
    public static async Task<T> ValueOrAsync<T>(this Task<Result<T>> resultTask, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null ? result.Value : defaultValue;
    }

    /// <summary>
    /// Async: returns the same result or fallback after awaiting.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The task producing a Result.</param>
    /// <param name="fallbackFactory">Async factory to produce a fallback Result.</param>
    /// <returns>A task returning the original or fallback Result.</returns>
    public static async Task<Result<T>> OrElseAsync<T>(this Task<Result<T>> resultTask, Func<Task<Result<T>>> fallbackFactory)
    {
        if (resultTask is null)
        {
            return Result<T>.WithFailure("Result task cannot be null");
        }
        
        if (fallbackFactory is null)
        {
            return Result<T>.WithFailure("Fallback factory function cannot be null");
        }
        
        var result = await resultTask.ConfigureAwait(false);
        return result.IsFailure ? await fallbackFactory().ConfigureAwait(false) : result;
    }

    /// <summary>
    /// Returns a plain value selected from success or failure branch.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TOut">The return value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="onSuccess">Function for the success branch.</param>
    /// <param name="onFailure">Function for the failure branch.</param>
    /// <returns>The value produced by the matching branch.</returns>
    public static TOut MatchValue<T, TOut>(this Result<T> result, Func<T, TOut> onSuccess, Func<IEnumerable<string>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        if (!result.IsRecoverable)
        {
            var errs = result.Errors ?? [ResultConstants.DefaultErrorMessage];
            return onFailure(errs);
        }
        return onSuccess(result.Value!);
    }

    /// <summary>
    /// Value-aware variant of <see cref="MatchValue{T, TOut}(Result{T}, Func{T, TOut}, Func{IEnumerable{string}, TOut})"/>
    /// whose failure branch also receives the (possibly null) carried failure value. Routing is preserved from the
    /// sibling overload: it branches on <c>IsRecoverable</c> (not <c>IsSuccess</c>), so the failure branch fires only
    /// when the result is not recoverable.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TOut">The return value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="onSuccess">Function for the success branch.</param>
    /// <param name="onFailure">Function for the failure branch, receiving the errors and the carried (possibly null) value.</param>
    /// <returns>The value produced by the matching branch.</returns>
    public static TOut MatchValue<T, TOut>(this Result<T> result, Func<T, TOut> onSuccess, Func<IEnumerable<string>, T?, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        if (!result.IsRecoverable)
        {
            var errs = result.Errors ?? [ResultConstants.DefaultErrorMessage];
            return onFailure(errs, result.Value);
        }
        return onSuccess(result.Value!);
    }

    /// <summary>
    /// Non-generic variant for convenience.
    /// </summary>
    /// <typeparam name="TOut">The return value type.</typeparam>
    /// <param name="result">The input non-generic Result.</param>
    /// <param name="onSuccess">Function for the success branch.</param>
    /// <param name="onFailure">Function for the failure branch.</param>
    /// <returns>The value produced by the matching branch.</returns>
    public static TOut MatchValue<TOut>(this Result result, Func<TOut> onSuccess, Func<IEnumerable<string>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return result.IsSuccess ? onSuccess() : onFailure(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
    }

    /// <summary>
    /// Executes side-effect regardless of success or failure and returns the same result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="action">Action to execute on both branches.</param>
    /// <returns>The original Result.</returns>
    public static Result<T> OnBoth<T>(this Result<T> result, Action action)
    {
        if (result is null)
        {
            return Result<T>.WithFailure("Result cannot be null");
        }
        
        if (action is null)
        {
            return Result<T>.WithFailure("Action cannot be null");
        }
        
        action();
        return result;
    }

    /// <summary>
    /// Executes side-effects for success and failure (branch-aware), returns the same result.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="onSuccess">Action to execute when successful.</param>
    /// <param name="onFailure">Action to execute when failed.</param>
    /// <returns>The original Result.</returns>
    public static Result<T> OnBoth<T>(this Result<T> result, Action<T> onSuccess, Action<IEnumerable<string>> onFailure)
    {
        if (result is null)
        {
            return Result<T>.WithFailure("Result cannot be null");
        }
        
        if (onSuccess is null)
        {
            return Result<T>.WithFailure("OnSuccess action cannot be null");
        }
        
        if (onFailure is null)
        {
            return Result<T>.WithFailure("OnFailure action cannot be null");
        }
        
        if (result.IsRecoverable)
        {
            onSuccess(result.Value!);
        }
        else
        {
            onFailure(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
        }
        return result;
    }

    /// <summary>
    /// Non-generic branch-aware side-effect.
    /// </summary>
    /// <param name="result">The input non-generic Result.</param>
    /// <param name="onSuccess">Action to execute when successful.</param>
    /// <param name="onFailure">Action to execute when failed.</param>
    /// <returns>The original Result.</returns>
    public static Result OnBoth(this Result result, Action onSuccess, Action<IEnumerable<string>> onFailure)
    {
        if (result is null)
        {
            return Result.WithFailure("Result cannot be null");
        }
        
        if (onSuccess is null)
        {
            return Result.WithFailure("OnSuccess action cannot be null");
        }
        
        if (onFailure is null)
        {
            return Result.WithFailure("OnFailure action cannot be null");
        }
        
        if (result.IsSuccess)
        {
            onSuccess();
        }
        else
        {
            onFailure(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
        }

        return result;
    }

    /// <summary>
    /// Alias for OnBoth(Action) to mirror semantics of finally.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="action">Action to execute on both branches.</param>
    /// <returns>The original Result.</returns>
    public static Result<T> Finally<T>(this Result<T> result, Action action)
    {
        return result.OnBoth(action);
    }

    /// <summary>
    /// Maps both branches into a new Result value, preserving Result wrapper.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TOut">The mapped value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="onSuccess">Mapping for the success branch.</param>
    /// <param name="onFailure">Mapping for the failure branch.</param>
    /// <returns>The mapped Result.</returns>
    public static Result<TOut> MapBoth<T, TOut>(this Result<T> result, Func<T, TOut> onSuccess, Func<IEnumerable<string>, TOut> onFailure)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (result is null) { return Result<TOut>.WithFailure("Result cannot be null"); }
        if (onSuccess is null) { return Result<TOut>.WithFailure("OnSuccess function cannot be null"); }
        if (onFailure is null) { return Result<TOut>.WithFailure("OnFailure function cannot be null"); }
#pragma warning restore IDE0046
        
        return result.Match(onSuccess, onFailure);
    }
}
