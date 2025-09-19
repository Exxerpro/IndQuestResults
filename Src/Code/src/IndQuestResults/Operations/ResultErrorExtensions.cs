using System;
using System.Collections.Generic;
using System.Linq;

namespace IndQuestResults.Operations;

/// <summary>
/// Error-side fluent helpers for Result and Result{T}.
/// </summary>
public static class ResultErrorExtensions
{
    /// <summary>
    /// Transforms the errors of a failed non-generic Result. Success returns the same instance.
    /// </summary>
    public static Result MapError(this Result result, Func<IEnumerable<string>, IEnumerable<string>> map)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);
        return result.IsFailure
            ? Result.WithFailure(map(result.Errors ?? [ResultConstants.DefaultErrorMessage]))
            : result;
    }

    /// <summary>
    /// Transforms the errors of a failed Result{T}. Success returns the same instance.
    /// </summary>
    public static Result<T> MapError<T>(this Result<T> result, Func<IEnumerable<string>, IEnumerable<string>> map)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(map);
        return result.IsFailure
            ? Result<T>.WithFailure(map(result.Errors ?? [ResultConstants.DefaultErrorMessage]), result.Value)
            : result;
    }

    /// <summary>
    /// Executes an action with the errors of a failed non-generic Result without changing it.
    /// </summary>
    public static Result TapError(this Result result, Action<IEnumerable<string>> action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);
        if (result.IsFailure)
        {
            action(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
        }
        return result;
    }

    /// <summary>
    /// Executes an action with the errors of a failed Result{T} without changing it.
    /// </summary>
    public static Result<T> TapError<T>(this Result<T> result, Action<IEnumerable<string>> action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);
        if (result.IsFailure)
        {
            action(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
        }
        return result;
    }

    /// <summary>
    /// Error-aware recovery for non-generic Result.
    /// </summary>
    public static Result Recover(this Result result, Func<IEnumerable<string>, Result> recover)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(recover);
        return result.IsFailure
            ? recover(result.Errors ?? [ResultConstants.DefaultErrorMessage])
            : result;
    }

    /// <summary>
    /// Error-aware recovery for Result{T}.
    /// </summary>
    public static Result<T> Recover<T>(this Result<T> result, Func<IEnumerable<string>, Result<T>> recover)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(recover);
        return result.IsFailure
            ? recover(result.Errors ?? [ResultConstants.DefaultErrorMessage])
            : result;
    }
}

