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
    /// <param name="result">The input Result.</param>
    /// <param name="map">Function that transforms the error collection.</param>
    /// <returns>The transformed failed Result or the original Result when successful.</returns>
    public static Result MapError(this Result result, Func<IEnumerable<string>, IEnumerable<string>> map)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (result is null) { return Result.WithFailure("Result cannot be null"); }
        if (map is null) { return Result.WithFailure("Map function cannot be null"); }
#pragma warning restore IDE0046
        
        return result.IsFailure
            ? Result.WithFailure(map(result.Errors ?? [ResultConstants.DefaultErrorMessage]))
            : result;
    }

    /// <summary>
    /// Transforms the errors of a failed Result{T}. Success returns the same instance.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="map">Function that transforms the error collection.</param>
    /// <returns>The transformed failed Result or the original Result when successful.</returns>
    public static Result<T> MapError<T>(this Result<T> result, Func<IEnumerable<string>, IEnumerable<string>> map)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (result is null) { return Result<T>.WithFailure("Result cannot be null"); }
        if (map is null) { return Result<T>.WithFailure("Map function cannot be null"); }
#pragma warning restore IDE0046
        
        return result.IsFailure
            ? Result<T>.WithFailure(map(result.Errors ?? [ResultConstants.DefaultErrorMessage]), result.Value)
            : result;
    }

    /// <summary>
    /// Executes an action with the errors of a failed non-generic Result without changing it.
    /// </summary>
    /// <param name="result">The input Result.</param>
    /// <param name="action">Action to execute with the error collection.</param>
    /// <returns>The original Result instance.</returns>
    public static Result TapError(this Result result, Action<IEnumerable<string>> action)
    {
        if (result is null)
        {
            return Result.WithFailure("Result cannot be null");
        }
        
        if (action is null)
        {
            return Result.WithFailure("Action cannot be null");
        }
        
        if (result.IsFailure)
        {
            action(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
        }
        return result;
    }

    /// <summary>
    /// Executes an action with the errors of a failed Result{T} without changing it.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="action">Action to execute with the error collection.</param>
    /// <returns>The original Result instance.</returns>
    public static Result<T> TapError<T>(this Result<T> result, Action<IEnumerable<string>> action)
    {
        if (result is null)
        {
            return Result<T>.WithFailure("Result cannot be null");
        }
        
        if (action is null)
        {
            return Result<T>.WithFailure("Action cannot be null");
        }
        
        if (result.IsFailure)
        {
            action(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
        }
        return result;
    }

    /// <summary>
    /// Asynchronously executes a side effect with the errors of a failed Result{T} without changing it
    /// (async mirror of <see cref="TapError{T}(Result{T}, Action{IEnumerable{string}})"/>: fires only when the
    /// result is a failure) and returns the original result instance, so value-carrying failures survive.
    /// Exceptions from the action propagate to the caller; nothing is swallowed.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="action">Asynchronous action to execute with the error collection.</param>
    /// <returns>A task containing the original result instance.</returns>
    public static async Task<Result<T>> TapErrorAsync<T>(this Task<Result<T>> resultTask, Func<IEnumerable<string>, Task> action)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        var result = await resultTask.ConfigureAwait(false);
        if (result.IsFailure)
        {
            await action(result.Errors ?? [ResultConstants.DefaultErrorMessage]).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Error-aware recovery for non-generic Result.
    /// </summary>
    /// <param name="result">The input Result.</param>
    /// <param name="recover">Function that produces a recovery Result from the errors.</param>
    /// <returns>The recovered Result or the original when successful.</returns>
    public static Result Recover(this Result result, Func<IEnumerable<string>, Result> recover)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (result is null) { return Result.WithFailure("Result cannot be null"); }
        if (recover is null) { return Result.WithFailure("Recover function cannot be null"); }
#pragma warning restore IDE0046
        
        return result.IsFailure
            ? recover(result.Errors ?? [ResultConstants.DefaultErrorMessage])
            : result;
    }

    /// <summary>
    /// Error-aware recovery for Result{T}.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="recover">Function that produces a recovery Result from the errors.</param>
    /// <returns>The recovered Result or the original when successful.</returns>
    public static Result<T> Recover<T>(this Result<T> result, Func<IEnumerable<string>, Result<T>> recover)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (result is null) { return Result<T>.WithFailure("Result cannot be null"); }
        if (recover is null) { return Result<T>.WithFailure("Recover function cannot be null"); }
#pragma warning restore IDE0046
        
        return result.IsFailure
            ? recover(result.Errors ?? [ResultConstants.DefaultErrorMessage])
            : result;
    }
}

