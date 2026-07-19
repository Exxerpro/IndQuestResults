namespace IndQuestResults.Operations;

/// <summary>
/// Same-type railway steps that preserve value-carrying failures, plus terminals that expose them.
/// </summary>
/// <remarks>
/// Every type-transitioning combinator (<c>Bind</c>, <c>Map</c>, <c>Then</c>, <c>ThenAsync</c>) rebuilds a failure as
/// <c>Result&lt;TOut&gt;.WithFailure(Errors)</c>, dropping the failure's carried <c>Value</c> and <c>Exception</c>.
/// Pipelines that attach a projected value to a failure mid-chain (for example a diagnostic response DTO)
/// need same-type steps whose short-circuit returns the <b>original instance</b>, and a terminal that can read
/// the carried failure value. These combinators fill that gap. Design contract:
/// <list type="bullet">
/// <item><description>No <c>catch</c> blocks — exceptions and cancellation propagate to the caller.</description></item>
/// <item><description>No cancellation-token pre-checks — pass tokens to steps via closure; use
/// <see cref="ResultExtensions.ThenAsyncCancellable{TIn, TOut}"/> when a pre-check is wanted.</description></item>
/// <item><description>Short-circuits return the same instance, so <c>Value</c>, <c>Exception</c>, warnings and
/// metadata on failures survive the rest of the chain.</description></item>
/// </list>
/// </remarks>
public static class ResultStepExtensions
{
    /// <summary>
    /// Applies a same-type step when the result is a success with a non-null value; any other state
    /// (failure — even value-carrying — or success-with-null) short-circuits by returning the original
    /// result instance unchanged.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The source result.</param>
    /// <param name="next">The step to run on the current value.</param>
    /// <returns>The step's result, or the original instance when short-circuiting.</returns>
    public static Result<T> ThenStep<T>(this Result<T> result, Func<T, Result<T>> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return result.IsSuccess && result.Value is not null
            ? next(result.Value)
            : result;
    }

    /// <summary>
    /// Async-source overload of <see cref="ThenStep{T}(Result{T}, Func{T, Result{T}})"/> with a synchronous step;
    /// a failure or success-with-null short-circuits unchanged, preserving any failure value.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="next">The step to run on the current value.</param>
    /// <returns>A task containing the step's result, or the original instance when short-circuiting.</returns>
    public static async Task<Result<T>> ThenStep<T>(this Task<Result<T>> resultTask, Func<T, Result<T>> next)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(next);

        var result = await resultTask.ConfigureAwait(false);
        return result.ThenStep(next);
    }

    /// <summary>
    /// Async-source overload of <see cref="ThenStep{T}(Result{T}, Func{T, Result{T}})"/> with an asynchronous step;
    /// exceptions and cancellation propagate to the caller (no wrapping), matching handler-level try/catch semantics.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="next">The asynchronous step to run on the current value.</param>
    /// <returns>A task containing the step's result, or the original instance when short-circuiting.</returns>
    public static async Task<Result<T>> ThenStep<T>(this Task<Result<T>> resultTask, Func<T, Task<Result<T>>> next)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(next);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null
            ? await next(result.Value).ConfigureAwait(false)
            : result;
    }

    /// <summary>
    /// Terminally projects an async result: <paramref name="onSuccess"/> when the result is a success with a
    /// non-null value; otherwise <paramref name="onFailure"/> receives the errors and the (possibly null) carried
    /// failure value — a success-with-null routes to <paramref name="onFailure"/> with its (typically empty)
    /// errors and a null value.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ResultValueExtensions.MatchValue{T, TOut}"/>, routing is on <c>IsSuccess</c> rather than
    /// <c>IsRecoverable</c>, so the success branch can never observe a null value.
    /// </remarks>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <typeparam name="TOut">The projected type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="onSuccess">Projection for a success with a non-null value.</param>
    /// <param name="onFailure">Projection receiving the errors and the carried failure value, when any.</param>
    /// <returns>A task containing the projected value.</returns>
    public static async Task<TOut> MatchAsync<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, TOut> onSuccess,
        Func<IEnumerable<string>, T?, TOut> onFailure)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null
            ? onSuccess(result.Value)
            : onFailure(result.Errors, result.Value);
    }

    /// <summary>
    /// Async-branch overload of <see cref="MatchAsync{T, TOut}(Task{Result{T}}, Func{T, TOut}, Func{IEnumerable{string}, T, TOut})"/>
    /// for terminals whose branches perform awaited work (for example failure audits).
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <typeparam name="TOut">The projected type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="onSuccess">Asynchronous projection for a success with a non-null value.</param>
    /// <param name="onFailure">Asynchronous projection receiving the errors and the carried failure value, when any.</param>
    /// <returns>A task containing the projected value.</returns>
    public static async Task<TOut> MatchAsync<T, TOut>(
        this Task<Result<T>> resultTask,
        Func<T, Task<TOut>> onSuccess,
        Func<IEnumerable<string>, T?, Task<TOut>> onFailure)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        var result = await resultTask.ConfigureAwait(false);
        return result.IsSuccess && result.Value is not null
            ? await onSuccess(result.Value).ConfigureAwait(false)
            : await onFailure(result.Errors, result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// On a failure that carries no value, attaches <paramref name="fallbackValue"/>(errors) (when non-null)
    /// while preserving the errors and exception; value-carrying failures, successes and success-with-null
    /// pass through as the same instance.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="result">The source result.</param>
    /// <param name="fallbackValue">Projects a failure value from the errors; returning null leaves the failure value-less.</param>
    /// <returns>The failure rebuilt with the projected value, or the original instance.</returns>
    public static Result<T> ElseWithValue<T>(this Result<T> result, Func<IEnumerable<string>, T?> fallbackValue)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(fallbackValue);

        if (!result.IsFailure || result.Value is not null)
        {
            return result;
        }

        var value = fallbackValue(result.Errors);
        return value is null
            ? result
            : Result<T>.WithFailure(result.Errors, value, result.Exception);
    }

    /// <summary>
    /// Async-source overload of <see cref="ElseWithValue{T}(Result{T}, Func{IEnumerable{string}, T})"/>.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="resultTask">The previous result task.</param>
    /// <param name="fallbackValue">Projects a failure value from the errors; returning null leaves the failure value-less.</param>
    /// <returns>A task containing the failure rebuilt with the projected value, or the original instance.</returns>
    public static async Task<Result<T>> ElseWithValue<T>(this Task<Result<T>> resultTask, Func<IEnumerable<string>, T?> fallbackValue)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(fallbackValue);

        var result = await resultTask.ConfigureAwait(false);
        return result.ElseWithValue(fallbackValue);
    }
}
