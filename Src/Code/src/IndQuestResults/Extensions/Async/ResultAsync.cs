using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IndQuestResults.Operations;

namespace IndQuestResults.Extensions.Async;

/// <summary>
/// Provides async/await integration for Result types, enabling monadic operations in asynchronous contexts.
/// Implements async versions of Map, Bind, Tap, and other functional operations following the
/// Task&lt;Result&lt;T&gt;&gt; pattern common in functional programming languages.
/// </summary>
/// <remarks>
/// <para><strong>Async Patterns Supported:</strong></para>
/// <list type="bullet">
/// <item><strong>BindAsync:</strong> Async monadic bind for chaining async operations</item>
/// <item><strong>MapAsync:</strong> Async functor mapping for transformations</item>
/// <item><strong>TapAsync:</strong> Async side effects without changing the result</item>
/// <item><strong>RecoverAsync:</strong> Async error recovery patterns</item>
/// <item><strong>TraverseAsync:</strong> Async collection processing</item>
/// </list>
/// 
/// <para><strong>Cancellation Support:</strong> All async operations support CancellationToken for proper cancellation handling.</para>
/// 
/// <para><strong>Error Handling:</strong> Preserves Result semantics while properly handling async exceptions.</para>
/// </remarks>
public static class ResultAsync
{
    /// <summary>
    /// Asynchronously binds a Result-returning function to a Task&lt;Result&lt;T&gt;&gt;.
    /// If the original Result is successful, applies the async function; otherwise propagates the error.
    /// </summary>
    /// <typeparam name="TInput">Type of the input Result value</typeparam>
    /// <typeparam name="TOutput">Type of the output Result value</typeparam>
    /// <param name="resultTask">Task containing the input Result</param>
    /// <param name="func">Async function to apply if the input Result is successful</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task containing the bound Result</returns>
    /// <example>
    /// <code>
    /// Task&lt;Result&lt;User&gt;&gt; userTask = GetUserAsync(id);
    /// Task&lt;Result&lt;Profile&gt;&gt; profileTask = userTask.BindAsync(LoadProfileAsync);
    /// </code>
    /// </example>
    public static async Task<Result<TOutput>> BindAsync<TInput, TOutput>(
        this Task<Result<TInput>> resultTask,
        Func<TInput, Task<Result<TOutput>>> func,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(func);

        try
        {
            var result = await resultTask.ConfigureAwait(false);
            
            if (cancellationToken.IsCancellationRequested)
                return ResultExtensions.Cancelled<TOutput>();

            return result.IsSuccess
                ? await func(result.Value!).ConfigureAwait(false)
                : Result<TOutput>.WithFailure(result.Errors ?? new[] { ResultConstants.DefaultErrorMessage });
        }
        catch (OperationCanceledException)
        {
            return ResultExtensions.Cancelled<TOutput>();
        }
        catch (Exception ex)
        {
            return Result<TOutput>.WithFailure($"Async bind operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously maps a function over a Task&lt;Result&lt;T&gt;&gt;.
    /// If the Result is successful, applies the async function to transform the value.
    /// </summary>
    /// <typeparam name="TInput">Type of the input value</typeparam>
    /// <typeparam name="TOutput">Type of the output value</typeparam>
    /// <param name="resultTask">Task containing the input Result</param>
    /// <param name="func">Async function to apply for transformation</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task containing the mapped Result</returns>
    /// <example>
    /// <code>
    /// Task&lt;Result&lt;User&gt;&gt; userTask = GetUserAsync(id);
    /// Task&lt;Result&lt;UserDto&gt;&gt; dtoTask = userTask.MapAsync(user =&gt; user.ToDtoAsync());
    /// </code>
    /// </example>
    public static async Task<Result<TOutput>> MapAsync<TInput, TOutput>(
        this Task<Result<TInput>> resultTask,
        Func<TInput, Task<TOutput>> func,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(func);

        try
        {
            var result = await resultTask.ConfigureAwait(false);
            
            if (cancellationToken.IsCancellationRequested)
                return ResultExtensions.Cancelled<TOutput>();

            if (result.IsSuccess)
            {
                var transformedValue = await func(result.Value!).ConfigureAwait(false);
                return Result<TOutput>.Success(transformedValue);
            }

            return Result<TOutput>.WithFailure(result.Errors ?? new[] { ResultConstants.DefaultErrorMessage });
        }
        catch (OperationCanceledException)
        {
            return ResultExtensions.Cancelled<TOutput>();
        }
        catch (Exception ex)
        {
            return Result<TOutput>.WithFailure($"Async map operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously performs a side effect on a successful Result without changing the result.
    /// Useful for logging, caching, or other side effects in async pipelines.
    /// </summary>
    /// <typeparam name="T">Type of the Result value</typeparam>
    /// <param name="resultTask">Task containing the Result</param>
    /// <param name="action">Async action to perform on success</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task containing the original Result</returns>
    /// <example>
    /// <code>
    /// Task&lt;Result&lt;User&gt;&gt; userTask = GetUserAsync(id)
    ///     .TapAsync(user =&gt; LogUserAccessAsync(user.Id))
    ///     .TapAsync(user =&gt; CacheUserAsync(user));
    /// </code>
    /// </example>
    public static async Task<Result<T>> TapAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            var result = await resultTask.ConfigureAwait(false);
            
            if (cancellationToken.IsCancellationRequested)
                return ResultExtensions.Cancelled<T>();

            if (result.IsSuccess)
            {
                await action(result.Value!).ConfigureAwait(false);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            return ResultExtensions.Cancelled<T>();
        }
        catch (Exception ex)
        {
            // For side effects, we typically want to preserve the original result
            // but we could also choose to fail the entire operation
            return Result<T>.WithFailure($"Async side effect failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously recovers from a failed Result using an async recovery function.
    /// </summary>
    /// <typeparam name="T">Type of the Result value</typeparam>
    /// <param name="resultTask">Task containing the Result</param>
    /// <param name="recoveryFunc">Async function to provide recovery value</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task containing the recovered or original Result</returns>
    /// <example>
    /// <code>
    /// Task&lt;Result&lt;User&gt;&gt; userTask = GetUserAsync(id)
    ///     .RecoverAsync(() =&gt; GetDefaultUserAsync());
    /// </code>
    /// </example>
    public static async Task<Result<T>> RecoverAsync<T>(
        this Task<Result<T>> resultTask,
        Func<Task<Result<T>>> recoveryFunc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(recoveryFunc);

        try
        {
            var result = await resultTask.ConfigureAwait(false);
            
            if (cancellationToken.IsCancellationRequested)
                return ResultExtensions.Cancelled<T>();

            return result.IsSuccess
                ? result
                : await recoveryFunc().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return ResultExtensions.Cancelled<T>();
        }
        catch (Exception ex)
        {
            return Result<T>.WithFailure($"Async recovery failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously traverses a collection, applying an async function to each element.
    /// This is the async version of ResultCollections.Traverse.
    /// </summary>
    /// <typeparam name="TInput">Type of input values</typeparam>
    /// <typeparam name="TOutput">Type of output values</typeparam>
    /// <param name="inputs">Collection of input values</param>
    /// <param name="func">Async function to apply to each input</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task containing Result with all transformed values or accumulated errors</returns>
    /// <example>
    /// <code>
    /// var userIds = new[] { 1, 2, 3 };
    /// Task&lt;Result&lt;IEnumerable&lt;User&gt;&gt;&gt; usersTask = 
    ///     ResultAsync.TraverseAsync(userIds, LoadUserAsync);
    /// </code>
    /// </example>
    public static async Task<Result<IEnumerable<TOutput>>> TraverseAsync<TInput, TOutput>(
        IEnumerable<TInput> inputs,
        Func<TInput, Task<Result<TOutput>>> func,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(func);

        try
        {
            var tasks = inputs.Select(input => func(input));
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            
            if (cancellationToken.IsCancellationRequested)
                return ResultExtensions.Cancelled<IEnumerable<TOutput>>();

            return Collections.ResultCollections.Sequence(results);
        }
        catch (OperationCanceledException)
        {
            return ResultExtensions.Cancelled<IEnumerable<TOutput>>();
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<TOutput>>.WithFailure($"Async traverse failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Asynchronously traverses a collection with parallel execution and degree of parallelism control.
    /// </summary>
    /// <typeparam name="TInput">Type of input values</typeparam>
    /// <typeparam name="TOutput">Type of output values</typeparam>
    /// <param name="inputs">Collection of input values</param>
    /// <param name="func">Async function to apply to each input</param>
    /// <param name="maxDegreeOfParallelism">Maximum number of concurrent operations</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task containing Result with all transformed values or accumulated errors</returns>
    public static async Task<Result<IEnumerable<TOutput>>> TraverseParallelAsync<TInput, TOutput>(
        IEnumerable<TInput> inputs,
        Func<TInput, Task<Result<TOutput>>> func,
        int maxDegreeOfParallelism = 4,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(func);
        if (maxDegreeOfParallelism <= 0) throw new ArgumentException("Max degree of parallelism must be positive", nameof(maxDegreeOfParallelism));

        try
        {
            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = inputs.Select(async input =>
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    return await func(input).ConfigureAwait(false);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            
            if (cancellationToken.IsCancellationRequested)
                return ResultExtensions.Cancelled<IEnumerable<TOutput>>();

            return Collections.ResultCollections.Sequence(results);
        }
        catch (OperationCanceledException)
        {
            return ResultExtensions.Cancelled<IEnumerable<TOutput>>();
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<TOutput>>.WithFailure($"Async parallel traverse failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Sequences a collection of Task&lt;Result&lt;T&gt;&gt; into Task&lt;Result&lt;IEnumerable&lt;T&gt;&gt;&gt;.
    /// </summary>
    /// <typeparam name="T">Type of values in the Results</typeparam>
    /// <param name="resultTasks">Collection of async Results to sequence</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task containing Result with all values or accumulated errors</returns>
    public static async Task<Result<IEnumerable<T>>> SequenceAsync<T>(
        IEnumerable<Task<Result<T>>> resultTasks,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resultTasks);

        try
        {
            var results = await Task.WhenAll(resultTasks).ConfigureAwait(false);
            
            if (cancellationToken.IsCancellationRequested)
                return ResultExtensions.Cancelled<IEnumerable<T>>();

            return Collections.ResultCollections.Sequence(results);
        }
        catch (OperationCanceledException)
        {
            return ResultExtensions.Cancelled<IEnumerable<T>>();
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<T>>.WithFailure($"Async sequence failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Extension method for fluent async traversal of collections.
    /// </summary>
    /// <typeparam name="TInput">Type of input values</typeparam>
    /// <typeparam name="TOutput">Type of output values</typeparam>
    /// <param name="inputs">Collection of input values</param>
    /// <param name="func">Async function to apply to each input</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>Task containing Result with all transformed values or accumulated errors</returns>
    public static Task<Result<IEnumerable<TOutput>>> TraverseResultsAsync<TInput, TOutput>(
        this IEnumerable<TInput> inputs,
        Func<TInput, Task<Result<TOutput>>> func,
        CancellationToken cancellationToken = default)
    {
        return TraverseAsync(inputs, func, cancellationToken);
    }
}