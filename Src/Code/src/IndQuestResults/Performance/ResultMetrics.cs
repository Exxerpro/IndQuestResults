using System;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using IndQuestResults;
using IndQuestResults.Operations;

namespace IndQuestResults.Performance;

/// <summary>
/// Provides non-blocking metrics collection for Result operations.
/// Designed for high-throughput scenarios where timing data needs to be collected
/// without impacting the critical path performance.
/// </summary>
/// <remarks>
/// <para><strong>Fire-and-Forget Metrics:</strong> Uses channels for zero-blocking metrics dispatch.</para>
/// <para><strong>Performance First:</strong> Never blocks the main operation for metrics collection.</para>
/// <para><strong>Integration Ready:</strong> Designed to integrate with APM tools, time-series databases, etc.</para>
/// </remarks>
public static class ResultMetrics
{
    private static IMetricsCollector? _globalCollector;
    private static readonly Lock _collectorLock = new();

    /// <summary>
    /// Sets the global metrics collector for all Result operations.
    /// </summary>
    /// <param name="collector">The metrics collector implementation</param>
    public static void SetGlobalCollector(IMetricsCollector? collector)
    {
        using var _ = _collectorLock.EnterScope();
        _globalCollector = collector;
    }

    /// <summary>
    /// Executes an operation with timing and sends metrics to a non-blocking collector.
    /// The operation result is returned immediately without waiting for metrics dispatch.
    /// </summary>
    /// <typeparam name="T">Type of the result value</typeparam>
    /// <param name="operation">Operation to execute and measure</param>
    /// <param name="operationName">Name of the operation for metrics tagging</param>
    /// <param name="collector">Optional metrics collector (uses global if not provided)</param>
    /// <returns>The operation result</returns>
    /// <example>
    /// <code>
    /// var result = await ResultMetrics.TimedWithMetricsAsync(
    ///     () => ValidateUserAsync(userData),
    ///     "user.validation");
    /// // Metrics are sent asynchronously without blocking the result
    /// </code>
    /// </example>
    public static Result<T> TimedWithMetrics<T>(
        Func<Result<T>> operation,
        string operationName,
        IMetricsCollector? collector = null)
    {
        if (operation is null)
        {
            return Result<T>.WithFailure("Operation function cannot be null");
        }
        
        if (operationName is null)
        {
            return Result<T>.WithFailure("Operation name cannot be null");
        }

        var effectiveCollector = collector ?? _globalCollector;
        if (effectiveCollector == null)
        {
            // No collector configured, just execute the operation
            return operation();
        }

        var startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        Result<T> result;
        
        try
        {
            result = operation();
        }
        catch (Exception ex)
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startTimestamp);
            
            // Fire and forget metrics for exception
            effectiveCollector.RecordOperationMetrics(
                operationName,
                elapsed,
                isSuccess: false,
                isException: true,
                errorType: ex.GetType().Name);
            
            // Return failure result
            result = Result<T>.WithFailure($"Operation failed: {ex.Message}", default, ex);
        }
        
        var finalElapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startTimestamp);
        
        // Fire and forget metrics - never blocks
        effectiveCollector.RecordOperationMetrics(
            operationName,
            finalElapsed,
            isSuccess: result.IsSuccess,
            isException: false,
            errorType: result.IsFailure ? "ValidationFailure" : null);
        
        return result;
    }

    /// <summary>
    /// Async version with non-blocking metrics collection.
    /// </summary>
    public static async Task<Result<T>> TimedWithMetricsAsync<T>(
        Func<Task<Result<T>>> operation,
        string operationName,
        IMetricsCollector? collector = null,
        CancellationToken cancellationToken = default)
    {
        if (operation is null)
        {
            return Result<T>.WithFailure("Operation function cannot be null");
        }
        
        if (operationName is null)
        {
            return Result<T>.WithFailure("Operation name cannot be null");
        }

        var effectiveCollector = collector ?? _globalCollector;
        if (effectiveCollector == null)
        {
            // No collector configured, just execute the operation
            return await operation().ConfigureAwait(false);
        }

        var startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
        Result<T> result;
        
        try
        {
            result = await operation().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startTimestamp);
            
            effectiveCollector.RecordOperationMetrics(
                operationName,
                elapsed,
                isSuccess: false,
                isException: true,
                errorType: "Cancelled");
            
            result = ResultExtensions.Cancelled<T>();
        }
        catch (Exception ex)
        {
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startTimestamp);
            
            effectiveCollector.RecordOperationMetrics(
                operationName,
                elapsed,
                isSuccess: false,
                isException: true,
                errorType: ex.GetType().Name);
            
            result = Result<T>.WithFailure($"Async operation failed: {ex.Message}", default, ex);
        }
        
        var finalElapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startTimestamp);
        
        // Fire and forget metrics
        effectiveCollector.RecordOperationMetrics(
            operationName,
            finalElapsed,
            isSuccess: result.IsSuccess,
            isException: false,
            errorType: result.IsFailure && !cancellationToken.IsCancellationRequested ? "ValidationFailure" : null);
        
        return result;
    }

    /// <summary>
    /// Creates a metrics scope for a specific operation category.
    /// Useful for grouping related operations under a common prefix.
    /// </summary>
    /// <param name="scopeName">The scope name prefix</param>
    /// <param name="collector">Optional collector for this scope</param>
    /// <returns>A scoped metrics helper</returns>
    public static MetricsScope CreateScope(string scopeName, IMetricsCollector? collector = null)
    {
        return new MetricsScope(scopeName, collector ?? _globalCollector);
    }
}

/// <summary>
/// Interface for metrics collectors. Implementations should ensure non-blocking behavior.
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Records operation metrics. Implementation MUST be non-blocking (fire-and-forget).
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="elapsed">Elapsed time</param>
    /// <param name="isSuccess">Whether the operation succeeded</param>
    /// <param name="isException">Whether an exception occurred</param>
    /// <param name="errorType">Type of error if failed</param>
    void RecordOperationMetrics(
        string operationName, 
        TimeSpan elapsed,
        bool isSuccess,
        bool isException,
        string? errorType = null);
}

/// <summary>
/// A high-performance, non-blocking metrics collector using channels.
/// Processes metrics on a background thread to avoid any blocking on the hot path.
/// </summary>
public class ChannelMetricsCollector : IMetricsCollector, IDisposable
{
    private readonly Channel<MetricEntry> _channel;
    private readonly CancellationTokenSource _cts;
    private readonly Task _processingTask;
    private readonly IMetricsProcessor _processor;

    /// <summary>
    /// Creates a new channel-based metrics collector.
    /// </summary>
    /// <param name="processor">The processor that handles metrics</param>
    /// <param name="capacity">Channel capacity (defaults to unbounded)</param>
    public ChannelMetricsCollector(IMetricsProcessor processor, int? capacity = null)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
        
        if (capacity.HasValue)
        {
            var bounded = new BoundedChannelOptions(capacity.Value)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<MetricEntry>(bounded);
        }
        else
        {
            var unbounded = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateUnbounded<MetricEntry>(unbounded);
        }
            
        _cts = new CancellationTokenSource();
        _processingTask = ProcessMetricsAsync(_cts.Token);
    }

    /// <summary>
    /// Records metrics without blocking. Returns immediately.
    /// </summary>
    public void RecordOperationMetrics(
        string operationName, 
        TimeSpan elapsed,
        bool isSuccess,
        bool isException,
        string? errorType = null)
    {
        var entry = new MetricEntry(
            operationName,
            elapsed,
            isSuccess,
            isException,
            errorType,
            DateTimeOffset.UtcNow);
        
        // TryWrite is non-blocking. If channel is full, metric is dropped (acceptable for metrics)
        _channel.Writer.TryWrite(entry);
    }

    private async Task ProcessMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var metric in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await _processor.ProcessAsync(metric, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Swallow exceptions in metrics processing to ensure it never affects the system
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
    }

    /// <summary>
    /// Disposes the collector and attempts a best-effort shutdown of background processing.
    /// </summary>
    public void Dispose()
    {
        _cts.Cancel();
        _channel.Writer.TryComplete();
        
        try
        {
            _processingTask.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Best effort shutdown
        }
        
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents a single metric entry.
/// </summary>
/// <summary>
/// Immutable metric entry describing a single Result operation timing and outcome.
/// </summary>
public readonly struct MetricEntry
{
    /// <summary>Operation name (may be scoped).</summary>
    public string OperationName { get; }
    /// <summary>Elapsed time for the operation.</summary>
    public TimeSpan Elapsed { get; }
    /// <summary>Elapsed time in milliseconds.</summary>
    public double ElapsedMilliseconds => Elapsed.TotalMilliseconds;
    /// <summary>Whether the operation was successful.</summary>
    public bool IsSuccess { get; }
    /// <summary>Whether an exception occurred during the operation.</summary>
    public bool IsException { get; }
    /// <summary>Optional error type classification.</summary>
    public string? ErrorType { get; }
    /// <summary>UTC timestamp when the metric was recorded.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Creates a new metric entry.
    /// </summary>
    /// <param name="operationName">Operation name (may be scoped)</param>
    /// <param name="elapsed">Elapsed time</param>
    /// <param name="isSuccess">Success flag</param>
    /// <param name="isException">Exception flag</param>
    /// <param name="errorType">Optional error type classification</param>
    /// <param name="timestamp">UTC timestamp</param>
    public MetricEntry(
        string operationName,
        TimeSpan elapsed,
        bool isSuccess,
        bool isException,
        string? errorType,
        DateTimeOffset timestamp)
    {
        OperationName = operationName;
        Elapsed = elapsed;
        IsSuccess = isSuccess;
        IsException = isException;
        ErrorType = errorType;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Interface for processing metrics. Implementations can write to time-series databases,
/// APM tools, logs, etc.
/// </summary>
/// <summary>
/// Processes metrics produced by the collector.
/// </summary>
public interface IMetricsProcessor
{
    /// <summary>
    /// Processes a single metric entry.
    /// </summary>
    /// <param name="metric">The metric entry to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProcessAsync(MetricEntry metric, CancellationToken cancellationToken);
}

/// <summary>
/// A metrics scope that prefixes all operation names.
/// </summary>
public class MetricsScope
{
    private readonly string _scopeName;
    private readonly IMetricsCollector? _collector;

    internal MetricsScope(string scopeName, IMetricsCollector? collector)
    {
        _scopeName = scopeName;
        _collector = collector;
    }

    /// <summary>
    /// Times an operation within this scope.
    /// </summary>
    public Result<T> Timed<T>(Func<Result<T>> operation, string operationName)
    {
        return ResultMetrics.TimedWithMetrics(
            operation, 
            $"{_scopeName}.{operationName}",
            _collector);
    }

    /// <summary>
    /// Times an async operation within this scope.
    /// </summary>
    public Task<Result<T>> TimedAsync<T>(
        Func<Task<Result<T>>> operation, 
        string operationName,
        CancellationToken cancellationToken = default)
    {
        return ResultMetrics.TimedWithMetricsAsync(
            operation, 
            $"{_scopeName}.{operationName}",
            _collector,
            cancellationToken);
    }
}
