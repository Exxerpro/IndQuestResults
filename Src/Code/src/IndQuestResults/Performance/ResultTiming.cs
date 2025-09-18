using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IndQuestResults;
using IndQuestResults.Operations;

namespace IndQuestResults.Performance;

/// <summary>
/// Provides timing extensions for Result operations to measure execution time and performance metrics.
/// Designed for enterprise scenarios where operation timing is critical for monitoring and observability.
/// </summary>
/// <remarks>
/// <para><strong>Performance Patterns:</strong></para>
/// <list type="bullet">
/// <item><strong>Timed:</strong> Measure operation execution time</item>
/// <item><strong>TimedWithCallback:</strong> Execute timing callback with metrics</item>
/// <item><strong>TimedAsync:</strong> Async operation timing with cancellation support</item>
/// <item><strong>TimedBatch:</strong> Batch operation timing with aggregate metrics</item>
/// </list>
/// 
/// <para><strong>Zero Allocation:</strong> Uses high-resolution Stopwatch with minimal overhead.</para>
/// 
/// <para><strong>Enterprise Observability:</strong> Designed for APM, logging, and monitoring integration.</para>
/// </remarks>
public static class ResultTiming
{
    /// <summary>
    /// Executes a function and measures its execution time, returning both the result and timing information.
    /// Uses zero-allocation timing with high-resolution timestamps.
    /// </summary>
    /// <typeparam name="T">Type of the result value</typeparam>
    /// <param name="operation">Operation to execute and time</param>
    /// <returns>Timed result containing both the operation result and execution metrics</returns>
    /// <example>
    /// <code>
    /// var timedResult = ResultTiming.Timed(() => ValidateUser(userData));
    /// 
    /// Log.Information("Validation finished {Success} in {ElapsedMs}ms", 
    ///     timedResult.Result.IsSuccess, 
    ///     timedResult.ElapsedMilliseconds);
    /// 
    /// return timedResult.Result;
    /// </code>
    /// </example>
    public static TimedResult<T> Timed<T>(Func<Result<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var result = operation();
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            return new TimedResult<T>(result, elapsed);
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var failureResult = Result<T>.WithFailure($"Operation failed: {ex.Message}");
            return new TimedResult<T>(failureResult, elapsed);
        }
    }

    /// <summary>
    /// Executes a non-generic Result operation and measures its execution time.
    /// Uses zero-allocation timing with high-resolution timestamps.
    /// </summary>
    /// <param name="operation">Operation to execute and time</param>
    /// <returns>Timed result containing both the operation result and execution metrics</returns>
    /// <example>
    /// <code>
    /// var timedResult = ResultTiming.Timed(() => ProcessData(data));
    /// 
    /// Log.Information("Processing finished {Success} in {ElapsedMs}ms", 
    ///     timedResult.Result.IsSuccess, 
    ///     timedResult.ElapsedMilliseconds);
    /// </code>
    /// </example>
    public static TimedResult Timed(Func<Result> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var result = operation();
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            return new TimedResult(result, elapsed);
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var failureResult = Result.WithFailure($"Operation failed: {ex.Message}");
            return new TimedResult(failureResult, elapsed);
        }
    }

    /// <summary>
    /// Executes an async operation and measures its execution time.
    /// Uses zero-allocation timing with high-resolution timestamps.
    /// </summary>
    /// <typeparam name="T">Type of the result value</typeparam>
    /// <param name="operation">Async operation to execute and time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Timed result containing both the operation result and execution metrics</returns>
    /// <example>
    /// <code>
    /// var timedResult = await ResultTiming.TimedAsync(
    ///     () => ValidateUserAsync(userData, ct), ct);
    /// 
    /// Log.Information("Async validation finished {Success} in {ElapsedMs}ms", 
    ///     timedResult.Result.IsSuccess, 
    ///     timedResult.ElapsedMilliseconds);
    /// </code>
    /// </example>
    public static async Task<TimedResult<T>> TimedAsync<T>(
        Func<Task<Result<T>>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var result = await operation().ConfigureAwait(false);
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            return new TimedResult<T>(result, elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var cancelledResult = ResultExtensions.Cancelled<T>();
            return new TimedResult<T>(cancelledResult, elapsed);
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var failureResult = Result<T>.WithFailure($"Async operation failed: {ex.Message}");
            return new TimedResult<T>(failureResult, elapsed);
        }
    }

    /// <summary>
    /// Executes an async non-generic Result operation and measures its execution time.
    /// Uses zero-allocation timing with high-resolution timestamps.
    /// </summary>
    /// <param name="operation">Async operation to execute and time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Timed result containing both the operation result and execution metrics</returns>
    public static async Task<TimedResult> TimedAsync(
        Func<Task<Result>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            var result = await operation().ConfigureAwait(false);
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            return new TimedResult(result, elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var cancelledResult = ResultExtensions.Cancelled();
            return new TimedResult(cancelledResult, elapsed);
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            var failureResult = Result.WithFailure($"Async operation failed: {ex.Message}");
            return new TimedResult(failureResult, elapsed);
        }
    }

    /// <summary>
    /// Executes an operation with timing and immediately calls a callback with the timing metrics.
    /// Useful for immediate logging or monitoring without creating intermediate objects.
    /// </summary>
    /// <typeparam name="T">Type of the result value</typeparam>
    /// <param name="operation">Operation to execute and time</param>
    /// <param name="onTimed">Callback to execute with timing information</param>
    /// <returns>The operation result</returns>
    /// <example>
    /// <code>
    /// var result = ResultTiming.TimedWithCallback(
    ///     () => ValidateUser(userData),
    ///     (result, elapsed) => Log.Information("Validation {Success} in {ElapsedMs}ms", 
    ///         result.IsSuccess, elapsed.TotalMilliseconds));
    /// </code>
    /// </example>
    public static Result<T> TimedWithCallback<T>(
        Func<Result<T>> operation,
        Action<Result<T>, TimeSpan> onTimed)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(onTimed);

        var timedResult = Timed(operation);
        
        try
        {
            onTimed(timedResult.Result, timedResult.Elapsed);
        }
        catch (Exception)
        {
            // Don't let callback exceptions affect the operation result
        }

        return timedResult.Result;
    }

    /// <summary>
    /// Executes a non-generic Result operation with timing and immediately calls a callback.
    /// </summary>
    /// <param name="operation">Operation to execute and time</param>
    /// <param name="onTimed">Callback to execute with timing information</param>
    /// <returns>The operation result</returns>
    public static Result TimedWithCallback(
        Func<Result> operation,
        Action<Result, TimeSpan> onTimed)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(onTimed);

        var timedResult = Timed(operation);
        
        try
        {
            onTimed(timedResult.Result, timedResult.Elapsed);
        }
        catch (Exception)
        {
            // Don't let callback exceptions affect the operation result
        }

        return timedResult.Result;
    }

    /// <summary>
    /// Executes an async operation with timing and immediately calls a callback.
    /// </summary>
    /// <typeparam name="T">Type of the result value</typeparam>
    /// <param name="operation">Async operation to execute and time</param>
    /// <param name="onTimed">Callback to execute with timing information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The operation result</returns>
    public static async Task<Result<T>> TimedWithCallbackAsync<T>(
        Func<Task<Result<T>>> operation,
        Action<Result<T>, TimeSpan> onTimed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(onTimed);

        var timedResult = await TimedAsync(operation, cancellationToken).ConfigureAwait(false);
        
        try
        {
            onTimed(timedResult.Result, timedResult.Elapsed);
        }
        catch (Exception)
        {
            // Don't let callback exceptions affect the operation result
        }

        return timedResult.Result;
    }
}

/// <summary>
/// Contains the result of a timed operation along with execution metrics.
/// </summary>
/// <typeparam name="T">Type of the result value</typeparam>
public readonly struct TimedResult<T>
{
    /// <summary>
    /// The result of the timed operation.
    /// </summary>
    public Result<T> Result { get; }

    /// <summary>
    /// The elapsed time for the operation.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// The elapsed time in milliseconds (convenience property).
    /// </summary>
    public double ElapsedMilliseconds => Elapsed.TotalMilliseconds;

    /// <summary>
    /// The elapsed time in microseconds (high-precision timing).
    /// </summary>
    public double ElapsedMicroseconds => Elapsed.TotalMicroseconds;

    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool IsSuccess => Result.IsSuccess;

    /// <summary>
    /// Whether the operation failed.
    /// </summary>
    public bool IsFailure => Result.IsFailure;

    /// <summary>
    /// Initializes a new timed result.
    /// </summary>
    /// <param name="result">The operation result</param>
    /// <param name="elapsed">The elapsed time</param>
    public TimedResult(Result<T> result, TimeSpan elapsed)
    {
        Result = result;
        Elapsed = elapsed;
    }

    /// <summary>
    /// Deconstructs the timed result into its components.
    /// </summary>
    /// <param name="result">The operation result</param>
    /// <param name="elapsed">The elapsed time</param>
    public void Deconstruct(out Result<T> result, out TimeSpan elapsed)
    {
        result = Result;
        elapsed = Elapsed;
    }

    /// <summary>
    /// Creates a string representation of the timed result.
    /// </summary>
    public override string ToString() =>
        $"TimedResult<{typeof(T).Name}>: {(IsSuccess ? "Success" : "Failure")} in {ElapsedMilliseconds:F2}ms";
}

/// <summary>
/// Contains the result of a timed non-generic operation along with execution metrics.
/// </summary>
public readonly struct TimedResult
{
    /// <summary>
    /// The result of the timed operation.
    /// </summary>
    public Result Result { get; }

    /// <summary>
    /// The elapsed time for the operation.
    /// </summary>
    public TimeSpan Elapsed { get; }

    /// <summary>
    /// The elapsed time in milliseconds (convenience property).
    /// </summary>
    public double ElapsedMilliseconds => Elapsed.TotalMilliseconds;

    /// <summary>
    /// The elapsed time in microseconds (high-precision timing).
    /// </summary>
    public double ElapsedMicroseconds => Elapsed.TotalMicroseconds;

    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool IsSuccess => Result.IsSuccess;

    /// <summary>
    /// Whether the operation failed.
    /// </summary>
    public bool IsFailure => Result.IsFailure;

    /// <summary>
    /// Initializes a new timed result.
    /// </summary>
    /// <param name="result">The operation result</param>
    /// <param name="elapsed">The elapsed time</param>
    public TimedResult(Result result, TimeSpan elapsed)
    {
        Result = result;
        Elapsed = elapsed;
    }

    /// <summary>
    /// Deconstructs the timed result into its components.
    /// </summary>
    /// <param name="result">The operation result</param>
    /// <param name="elapsed">The elapsed time</param>
    public void Deconstruct(out Result result, out TimeSpan elapsed)
    {
        result = Result;
        elapsed = Elapsed;
    }

    /// <summary>
    /// Creates a string representation of the timed result.
    /// </summary>
    public override string ToString() =>
        $"TimedResult: {(IsSuccess ? "Success" : "Failure")} in {ElapsedMilliseconds:F2}ms";
}