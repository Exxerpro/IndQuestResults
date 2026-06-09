using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IndQuestResults;
using IndQuestResults.Operations;

namespace IndQuestResults.Reactive;

/// <summary>
/// Provides bridge extensions between IObservable and Result patterns for gateway scenarios.
/// Enables seamless integration of existing IObservable infrastructure with Result-based error handling.
/// </summary>
/// <remarks>
/// <para><strong>Bridge Patterns:</strong></para>
/// <list type="bullet">
/// <item><strong>ToResultStream:</strong> Convert IObservable&lt;T&gt; to Result&lt;T&gt; stream</item>
/// <item><strong>SelectResult:</strong> Transform observable values through Result operations</item>
/// <item><strong>WhereSuccess:</strong> Filter to only successful Results</item>
/// <item><strong>HandleErrors:</strong> Convert exceptions to Result failures</item>
/// <item><strong>CollectResults:</strong> Aggregate Results from observables</item>
/// </list>
/// 
/// <para><strong>Gateway Integration:</strong> Designed for gateway scenarios where IObservables are the primary data source.</para>
/// 
/// <para><strong>Error Resilience:</strong> Converts observable errors to Result failures without terminating streams.</para>
/// </remarks>
public static class ResultObservableBridge
{
    /// <summary>
    /// Extension method that converts values from an IObservable&lt;T&gt; stream into Result&lt;T&gt; values.
    /// Exceptions in the stream are converted to Result failures rather than terminating the stream.
    /// </summary>
    /// <typeparam name="T">Type of the observable values</typeparam>
    /// <param name="source">Source observable</param>
    /// <param name="onNext">Handler that receives Result&lt;T&gt; values</param>
    /// <param name="onCompleted">Optional completion handler</param>
    /// <returns>Subscription handle</returns>
    /// <example>
    /// <code>
    /// // Gateway observable to Result stream
    /// var subscription = gatewayObservable.ToResultStream(
    ///     onNext: result => {
    ///         if (result.IsSuccess)
    ///             ProcessGatewayData(result.Value);
    ///         else
    ///             LogGatewayError(result.Errors);
    ///     }
    /// );
    /// </code>
    /// </example>
    public static IDisposable ToResultStream<T>(
        this IObservable<T> source,
        Action<Result<T>> onNext,
        Action? onCompleted = null)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (source is null || onNext is null) { return NoOpDisposable.Instance; }
#pragma warning restore IDE0046

        // Create a bridge observer that converts IObservable values to Result values
        var observer = new ResultBridgeObserver<T>(onNext, onCompleted);
        return source.Subscribe(observer);
    }

    /// <summary>
    /// Transforms observable values through a Result-returning function.
    /// Useful for validation or transformation operations in gateway pipelines.
    /// </summary>
    /// <typeparam name="TSource">Type of source values</typeparam>
    /// <typeparam name="TResult">Type of result values</typeparam>
    /// <param name="source">Source observable</param>
    /// <param name="selector">Function that transforms values to Results</param>
    /// <param name="onNext">Handler for transformed Results</param>
    /// <param name="onCompleted">Optional completion handler</param>
    /// <returns>Subscription handle</returns>
    /// <example>
    /// <code>
    /// // Validate gateway data stream
    /// gatewayDataStream.SelectResult(
    ///     selector: data => ValidateGatewayData(data),
    ///     onNext: result => ProcessValidatedData(result)
    /// );
    /// </code>
    /// </example>
    public static IDisposable SelectResult<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource, Result<TResult>> selector,
        Action<Result<TResult>> onNext,
        Action? onCompleted = null)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (source is null || selector is null || onNext is null) { return NoOpDisposable.Instance; }
#pragma warning restore IDE0046

        var observer = new SelectResultObserver<TSource, TResult>(selector, onNext, onCompleted);
        return source.Subscribe(observer);
    }

    /// <summary>
    /// Filters an observable stream to only emit values that result in success.
    /// Failed Results are filtered out (not converted to errors).
    /// </summary>
    /// <typeparam name="TSource">Type of source values</typeparam>
    /// <typeparam name="TResult">Type of result values</typeparam>
    /// <param name="source">Source observable</param>
    /// <param name="selector">Function that transforms values to Results</param>
    /// <param name="onSuccess">Handler for successful values only</param>
    /// <param name="onCompleted">Optional completion handler</param>
    /// <returns>Subscription handle</returns>
    /// <example>
    /// <code>
    /// // Process only valid gateway messages
    /// gatewayMessages.WhereSuccess(
    ///     selector: msg => ValidateMessage(msg),
    ///     onSuccess: validMsg => ProcessValidMessage(validMsg)
    /// );
    /// </code>
    /// </example>
    public static IDisposable WhereSuccess<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource, Result<TResult>> selector,
        Action<TResult> onSuccess,
        Action? onCompleted = null)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (source is null || selector is null || onSuccess is null) { return NoOpDisposable.Instance; }
#pragma warning restore IDE0046

        return source.SelectResult(
            selector,
            onNext: result =>
            {
                if (result.IsSuccess)
                {
                    onSuccess(result.Value!);
                }
            },
            onCompleted
        );
    }

    /// <summary>
    /// Handles both successes and failures from an observable stream separately.
    /// Provides partitioned handling of Results.
    /// </summary>
    /// <typeparam name="TSource">Type of source values</typeparam>
    /// <typeparam name="TResult">Type of result values</typeparam>
    /// <param name="source">Source observable</param>
    /// <param name="selector">Function that transforms values to Results</param>
    /// <param name="onSuccess">Handler for successful values</param>
    /// <param name="onFailure">Handler for failures</param>
    /// <param name="onCompleted">Optional completion handler</param>
    /// <returns>Subscription handle</returns>
    /// <example>
    /// <code>
    /// // Handle gateway results with separate paths
    /// gatewayStream.HandleResults(
    ///     selector: data => ProcessGatewayData(data),
    ///     onSuccess: result => UpdateDatabase(result),
    ///     onFailure: errors => LogErrors(errors)
    /// );
    /// </code>
    /// </example>
    public static IDisposable HandleResults<TSource, TResult>(
        this IObservable<TSource> source,
        Func<TSource, Result<TResult>> selector,
        Action<TResult> onSuccess,
        Action<IEnumerable<string>> onFailure,
        Action? onCompleted = null)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (source is null || selector is null || onSuccess is null || onFailure is null) { return NoOpDisposable.Instance; }
#pragma warning restore IDE0046

        return source.SelectResult(
            selector,
            onNext: result =>
            {
                if (result.IsSuccess)
                {
                    onSuccess(result.Value!);
                }
                else
                {
                    // Stryker disable once NullCoalescing: Errors are normalized by library
                    onFailure(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
                }
            },
            onCompleted
        );
    }

    /// <summary>
    /// Collects all Results from an observable stream with a timeout.
    /// Useful for batch processing gateway data.
    /// </summary>
    /// <typeparam name="T">Type of observable values</typeparam>
    /// <param name="source">Source observable</param>
    /// <param name="timeout">Maximum time to wait for completion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing all collected values or errors</returns>
    /// <example>
    /// <code>
    /// // Collect gateway batch with timeout
    /// var batchResult = await gatewayBatchStream.CollectResults(
    ///     timeout: TimeSpan.FromSeconds(30),
    ///     cancellationToken: ct
    /// );
    /// 
    /// if (batchResult.IsSuccess)
    ///     ProcessBatch(batchResult.Value);
    /// </code>
    /// </example>
    public static async Task<Result<IEnumerable<T>>> CollectResults<T>(
        this IObservable<T> source,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (source is null) { return Result<IEnumerable<T>>.WithFailure("Source observable cannot be null"); }
#pragma warning restore IDE0046

        var results = new List<T>();
        var errors = new List<string>();
        var tcs = new TaskCompletionSource<bool>();
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        var observer = new CollectionObserver<T>(results, errors, tcs);
        using var subscription = source.Subscribe(observer);

        try
        {
            await tcs.Task.WaitAsync(cts.Token);
            
            return errors.Count > 0
                ? Result<IEnumerable<T>>.WithFailure(errors)
                : Result<IEnumerable<T>>.Success(results);
        }
        catch (OperationCanceledException)
        {
            return cancellationToken.IsCancellationRequested
                ? ResultExtensions.Cancelled<IEnumerable<T>>()
                : Result<IEnumerable<T>>.WithFailure("Collection timed out");
        }
    }

    /// <summary>
    /// Transforms an observable of Results into separate success and failure streams.
    /// Useful for routing in gateway scenarios.
    /// </summary>
    /// <typeparam name="T">Type of Result values</typeparam>
    /// <param name="source">Source observable of Results</param>
    /// <param name="successSubject">Subject to receive successful values</param>
    /// <param name="failureSubject">Subject to receive failures</param>
    /// <returns>Subscription handle</returns>
    /// <example>
    /// <code>
    /// // Route gateway results to different processors
    /// var successSubject = ResultSubscriptionsCore.CreateResultSubject&lt;Order&gt;();
    /// var failureSubject = ResultSubscriptionsCore.CreateResultSubject&lt;string&gt;();
    /// 
    /// gatewayResultStream.RouteResults(successSubject, failureSubject);
    /// </code>
    /// </example>
    public static IDisposable RouteResults<T>(
        this IObservable<Result<T>> source,
        IResultSubject<T> successSubject,
        IResultSubject<string> failureSubject)
    {
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
        if (source is null || successSubject is null || failureSubject is null) { return NoOpDisposable.Instance; }
#pragma warning restore IDE0046

        var observer = new RouteResultsObserver<T>(successSubject, failureSubject);
        return source.Subscribe(observer);
    }

    /// <summary>
    /// Creates a replay bridge that caches Results for late subscribers.
    /// Useful for gateway scenarios where multiple consumers need the same data.
    /// </summary>
    /// <typeparam name="T">Type of values</typeparam>
    /// <param name="source">Source observable</param>
    /// <param name="bufferSize">Number of Results to cache</param>
    /// <returns>Result subject that replays cached values</returns>
    /// <example>
    /// <code>
    /// // Create replay bridge for gateway configuration
    /// var configBridge = gatewayConfigStream.CreateReplayBridge(bufferSize: 10);
    /// 
    /// // Multiple subscribers get the same cached configs
    /// configBridge.Subscribe(onSuccess: config => Service1.Configure(config));
    /// configBridge.Subscribe(onSuccess: config => Service2.Configure(config));
    /// </code>
    /// </example>
    public static IResultSubject<T> CreateReplayBridge<T>(
        this IObservable<T> source,
        int bufferSize = 1)
    {
        if (source is null)
        {
            // Return a subject that's already completed (no-op behavior)
            var nullSubject = ResultSubscriptionsCore.CreateResultSubject<T>();
            nullSubject.Dispose();
            return nullSubject;
        }
        
        if (bufferSize <= 0)
        {
            // Return a subject that's already completed (no-op behavior)
            var invalidSubject = ResultSubscriptionsCore.CreateResultSubject<T>();
            invalidSubject.Dispose();
            return invalidSubject;
        }

        var subject = new ReplayResultSubject<T>(bufferSize);

        var observer = new ReplayBridgeObserver<T>(subject);
        source.Subscribe(observer);

        return subject;
    }
}

/// <summary>
/// Result subject that replays cached Results to new subscribers.
/// </summary>
/// <typeparam name="T">Type of Result values</typeparam>
internal class ReplayResultSubject<T> : IResultSubject<T>
{
    private readonly ResultSubject<T> _innerSubject = new();
    private readonly Queue<Result<T>> _buffer = new();
    private readonly int _bufferSize;
    private readonly Lock _lock = new();

    public int SubscriberCount => _innerSubject.SubscriberCount;
    public bool IsCompleted => _innerSubject.IsCompleted;

    public ReplayResultSubject(int bufferSize)
    {
        _bufferSize = bufferSize > 0 ? bufferSize : throw new ArgumentException("Buffer size must be positive", nameof(bufferSize));
    }

    public void OnNext(Result<T> result)
    {
        {
            using var _ = _lock.EnterScope();
            _buffer.Enqueue(result);
            while (_buffer.Count > _bufferSize)
            {
                _buffer.Dequeue();
            }
        }

        _innerSubject.OnNext(result);
    }

    public void OnError(Exception error)
    {
        _innerSubject.OnError(error);
    }

    public void OnCompleted()
    {
        _innerSubject.OnCompleted();
    }

    public IDisposable Subscribe(Action<T> onSuccess, Action<IEnumerable<string>>? onFailure = null, Action? onCompleted = null)
    {
        // Replay buffered values to new subscriber
        {
            using var _ = _lock.EnterScope();
            foreach (var result in _buffer)
            {
                if (result.IsSuccess)
                {
                    onSuccess(result.Value!);
                }
                else
                {
                    onFailure?.Invoke(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
                }
            }
        }

        return _innerSubject.Subscribe(onSuccess, onFailure, onCompleted);
    }

    public IDisposable Subscribe(Action<Result<T>> onResult, Action? onCompleted = null)
    {
        // Replay buffered values to new subscriber
        {
            using var _ = _lock.EnterScope();
            foreach (var result in _buffer)
            {
                onResult(result);
            }
        }

        return _innerSubject.Subscribe(onResult, onCompleted);
    }

    public void Dispose()
    {
        _innerSubject.Dispose();
    }
}

/// <summary>
/// Observer that bridges IObservable values to Result values.
/// </summary>
/// <typeparam name="T">Type of observable values</typeparam>
internal class ResultBridgeObserver<T> : IObserver<T>
{
    private readonly Action<Result<T>> _onNext;
    private readonly Action? _onCompleted;

    public ResultBridgeObserver(Action<Result<T>> onNext, Action? onCompleted)
    {
        _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
        _onCompleted = onCompleted;
    }

    public void OnNext(T value)
    {
        try
        {
            _onNext(Result<T>.Success(value));
        }
        catch (Exception ex)
        {
            _onNext(Result<T>.WithFailure($"Result handler error: {ex.Message}", default, ex));
        }
    }

    public void OnError(Exception error)
    {
        _onNext(Result<T>.WithFailure($"Observable error: {error.Message}", default, error));
    }

    public void OnCompleted()
    {
        _onCompleted?.Invoke();
    }
}

/// <summary>
/// Observer that transforms observable values through a selector function.
/// </summary>
/// <typeparam name="TSource">Type of source values</typeparam>
/// <typeparam name="TResult">Type of result values</typeparam>
internal class SelectResultObserver<TSource, TResult> : IObserver<TSource>
{
    private readonly Func<TSource, Result<TResult>> _selector;
    private readonly Action<Result<TResult>> _onNext;
    private readonly Action? _onCompleted;

    public SelectResultObserver(
        Func<TSource, Result<TResult>> selector,
        Action<Result<TResult>> onNext,
        Action? onCompleted)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _onNext = onNext ?? throw new ArgumentNullException(nameof(onNext));
        _onCompleted = onCompleted;
    }

    public void OnNext(TSource value)
    {
        try
        {
            var result = _selector(value);
            _onNext(result);
        }
        catch (Exception ex)
        {
            _onNext(Result<TResult>.WithFailure($"SelectResult error: {ex.Message}", default, ex));
        }
    }

    public void OnError(Exception error)
    {
        _onNext(Result<TResult>.WithFailure($"Observable error: {error.Message}", default, error));
    }

    public void OnCompleted()
    {
        _onCompleted?.Invoke();
    }
}

/// <summary>
/// Observer that routes Results to success and failure subjects.
/// </summary>
/// <typeparam name="T">Type of Result values</typeparam>
internal class RouteResultsObserver<T> : IObserver<Result<T>>
{
    private readonly IResultSubject<T> _successSubject;
    private readonly IResultSubject<string> _failureSubject;

    public RouteResultsObserver(IResultSubject<T> successSubject, IResultSubject<string> failureSubject)
    {
        _successSubject = successSubject ?? throw new ArgumentNullException(nameof(successSubject));
        _failureSubject = failureSubject ?? throw new ArgumentNullException(nameof(failureSubject));
    }

    public void OnNext(Result<T> result)
    {
        if (result.IsSuccess)
        {
            _successSubject.OnNext(Result<T>.Success(result.Value!));
        }
        else
        {
            var errorMessage = string.Join(", ", result.Errors ?? [ResultConstants.DefaultErrorMessage]);
            _failureSubject.OnNext(Result<string>.Success(errorMessage));
        }
    }

    public void OnError(Exception error)
    {
        var errorMessage = $"Stream error: {error.Message}";
        _failureSubject.OnNext(Result<string>.Success(errorMessage));
    }

    public void OnCompleted()
    {
        _successSubject.OnCompleted();
        _failureSubject.OnCompleted();
    }
}

/// <summary>
/// Observer that bridges IObservable values to a Result subject.
/// </summary>
/// <typeparam name="T">Type of observable values</typeparam>
internal class ReplayBridgeObserver<T> : IObserver<T>
{
    private readonly IResultSubject<T> _subject;

    public ReplayBridgeObserver(IResultSubject<T> subject)
    {
        _subject = subject ?? throw new ArgumentNullException(nameof(subject));
    }

    public void OnNext(T value)
    {
        _subject.OnNext(Result<T>.Success(value));
    }

    public void OnError(Exception error)
    {
        _subject.OnNext(Result<T>.WithFailure($"Observable error: {error.Message}", default, error));
    }

    public void OnCompleted()
    {
        _subject.OnCompleted();
    }
}

/// <summary>
/// Observer that collects values from an observable stream.
/// </summary>
/// <typeparam name="T">Type of observable values</typeparam>
internal class CollectionObserver<T> : IObserver<T>
{
    private readonly List<T> _results;
    private readonly List<string> _errors;
    private readonly TaskCompletionSource<bool> _tcs;

    public CollectionObserver(List<T> results, List<string> errors, TaskCompletionSource<bool> tcs)
    {
        _results = results ?? throw new ArgumentNullException(nameof(results));
        _errors = errors ?? throw new ArgumentNullException(nameof(errors));
        _tcs = tcs ?? throw new ArgumentNullException(nameof(tcs));
    }

    public void OnNext(T value)
    {
        _results.Add(value);
    }

    public void OnError(Exception error)
    {
        _errors.Add($"Collection error: {error.Message}");
        _tcs.TrySetResult(false);
    }

    public void OnCompleted()
    {
        _tcs.TrySetResult(true);
    }
}
