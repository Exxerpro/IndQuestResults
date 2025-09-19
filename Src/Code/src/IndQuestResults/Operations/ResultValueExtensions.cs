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
    public static T ValueOr<T>(this Result<T> result, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess && result.Value is not null ? result.Value : defaultValue;
    }

    /// <summary>
    /// Gets the value for a successful result or computes a default when failed or value is null.
    /// </summary>
    public static T ValueOr<T>(this Result<T> result, Func<T> defaultFactory)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(defaultFactory);
        return result.IsSuccess && result.Value is not null ? result.Value : defaultFactory();
    }

    /// <summary>
    /// Returns the same result if successful, or the provided fallback when failed.
    /// </summary>
    public static Result<T> OrElse<T>(this Result<T> result, Result<T> fallback)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(fallback);
        return result.IsFailure ? fallback : result;
    }

    /// <summary>
    /// Returns the same result if successful, or computes a fallback when failed.
    /// </summary>
    public static Result<T> OrElse<T>(this Result<T> result, Func<Result<T>> fallbackFactory)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(fallbackFactory);
        return result.IsFailure ? fallbackFactory() : result;
    }

    /// <summary>
    /// Async: gets the value or default after awaiting.
    /// </summary>
    public static async Task<T> ValueOrAsync<T>(this Task<Result<T>> resultTask, T defaultValue)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null ? result.Value : defaultValue;
    }

    /// <summary>
    /// Async: returns the same result or fallback after awaiting.
    /// </summary>
    public static async Task<Result<T>> OrElseAsync<T>(this Task<Result<T>> resultTask, Func<Task<Result<T>>> fallbackFactory)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(fallbackFactory);
        var result = await resultTask.ConfigureAwait(false);
        return result.IsFailure ? await fallbackFactory().ConfigureAwait(false) : result;
    }

    /// <summary>
    /// Returns a plain value selected from success or failure branch.
    /// </summary>
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
    /// Non-generic variant for convenience.
    /// </summary>
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
    public static Result<T> OnBoth<T>(this Result<T> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);
        action();
        return result;
    }

    /// <summary>
    /// Executes side-effects for success and failure (branch-aware), returns the same result.
    /// </summary>
    public static Result<T> OnBoth<T>(this Result<T> result, Action<T> onSuccess, Action<IEnumerable<string>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
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
    public static Result OnBoth(this Result result, Action onSuccess, Action<IEnumerable<string>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
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
    public static Result<T> Finally<T>(this Result<T> result, Action action)
    {
        return result.OnBoth(action);
    }

    /// <summary>
    /// Maps both branches into a new Result value, preserving Result wrapper.
    /// </summary>
    public static Result<TOut> MapBoth<T, TOut>(this Result<T> result, Func<T, TOut> onSuccess, Func<IEnumerable<string>, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return result.Match(onSuccess, onFailure);
    }
}
