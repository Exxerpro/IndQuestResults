using IndQuestResults.Operations;

namespace IndQuestResults.Operations;

/// <summary>
/// Utility class for wrapping async operations to handle cancellation in a functional way.
/// Converts OperationCanceledException to Result&lt;T&gt; failures, maintaining functional purity.
/// </summary>
public static class CancellationAwareResult
{
    /// <summary>
    /// Wraps an async operation to handle cancellation functionally without throwing exceptions.
    /// </summary>
    /// <typeparam name="T">The type of the operation result.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>A Result&lt;T&gt; representing success, failure, or cancellation.</returns>
    public static async Task<Result<T>> WrapCancellationAware<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        // Validate arguments
        if (operation is null)
            return Result<T>.WithFailure($"Operation was null name of {nameof(operation)} type Typeof {typeof(Func<CancellationToken, Task<T>>)}");

        // Early cancellation check
        if (cancellationToken.IsCancellationRequested)
            return ResultExtensions.Cancelled<T>();

        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);
            return Result<T>.Success(result);
        }
        catch (OperationCanceledException)
        {
            return ResultExtensions.Cancelled<T>();
        }
        catch (Exception ex)
        {
            return Result<T>.WithFailure($"Operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Wraps an async operation returning Result&lt;T&gt; to handle cancellation functionally.
    /// </summary>
    /// <typeparam name="T">The type of the operation result.</typeparam>
    /// <param name="operation">The async operation that already returns Result&lt;T&gt;.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>A Result&lt;T&gt; representing the operation outcome or cancellation.</returns>
    public static async Task<Result<T>> WrapResultOperation<T>(
        Func<CancellationToken, Task<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        // Validate arguments
        if (operation is null)
            return Result<T>.WithFailure($"Operation was null name of {nameof(operation)} type Typeof {typeof(Func<CancellationToken, Task<Result<T>>>)}");

        // Early cancellation check
        if (cancellationToken.IsCancellationRequested)
            return ResultExtensions.Cancelled<T>();

        try
        {
            return await operation(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return ResultExtensions.Cancelled<T>();
        }
        catch (Exception ex)
        {
            return Result<T>.WithFailure($"Operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Wraps a non-generic async operation to handle cancellation functionally.
    /// </summary>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token to monitor.</param>
    /// <returns>A Result representing success, failure, or cancellation.</returns>
    public static async Task<Result> WrapCancellationAware(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        // Validate arguments
        if (operation is null)
            return Result.WithFailure($"Operation was null name of {nameof(operation)} type Typeof {typeof(Func<CancellationToken, Task>)}");

        // Early cancellation check
        if (cancellationToken.IsCancellationRequested)
            return ResultExtensions.Cancelled();

        try
        {
            await operation(cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException)
        {
            return ResultExtensions.Cancelled();
        }
        catch (Exception ex)
        {
            return Result.WithFailure($"Operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates a timeout-aware cancellation token and wraps the operation.
    /// </summary>
    /// <typeparam name="T">The type of the operation result.</typeparam>
    /// <param name="operation">The async operation to execute.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="cancellationToken">Additional cancellation token to combine with timeout.</param>
    /// <returns>A Result&lt;T&gt; representing the operation outcome, timeout, or cancellation.</returns>
    public static async Task<Result<T>> WrapWithTimeout<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        // Validate arguments
        if (operation is null)
            return Result<T>.WithFailure($"Operation was null name of {nameof(operation)} type Typeof {typeof(Func<CancellationToken, Task<T>>)}");

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        // Early cancellation check
        if (cancellationToken.IsCancellationRequested)
            return ResultExtensions.Cancelled<T>();

        try
        {
            var result = await operation(combinedCts.Token).ConfigureAwait(false);
            return Result<T>.Success(result);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Timeout was triggered, not external cancellation
            return Result<T>.WithFailure(ResultErrors.OperationTimedOut);
        }
        catch (OperationCanceledException)
        {
            // External cancellation was triggered
            return ResultExtensions.Cancelled<T>();
        }
        catch (Exception ex)
        {
            return Result<T>.WithFailure($"Operation failed: {ex.Message}");
        }
    }
}