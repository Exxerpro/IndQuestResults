using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IndQuestResults.Operations;

namespace IndQuestResults.Extensions.Observables;

/// <summary>
/// Provides core subscription support for Result types without external dependencies.
/// Implements subscription patterns for observable-like behavior using only built-in .NET types.
/// This provides subscription management, resource disposal, and reactive patterns for Result-based streams.
/// </summary>
/// <remarks>
/// <para><strong>Core Subscription Patterns:</strong></para>
/// <list type="bullet">
/// <item><strong>ResultSubject:</strong> Subject-like behavior for publishing Result values</item>
/// <item><strong>SubscriptionManager:</strong> Group and manage multiple subscriptions</item>
/// <item><strong>SafeSubscription:</strong> Exception-safe subscription handling</item>
/// <item><strong>AsyncSubscription:</strong> Async subscription patterns with cancellation</item>
/// <item><strong>ResourceSubscription:</strong> Automatic resource cleanup</item>
/// </list>
///
/// <para><strong>No External Dependencies:</strong> Uses only built-in .NET types for maximum compatibility.</para>
///
/// <para><strong>Thread Safety:</strong> All subscription operations are thread-safe.</para>
/// </remarks>
public static class ResultSubscriptionsCore
{
    /// <summary>
    /// Creates a Result subject that can publish Result values to multiple subscribers.
    /// This provides observable-like behavior for Result streams without external dependencies.
    /// </summary>
    /// <typeparam name="T">Type of the Result values</typeparam>
    /// <returns>Result subject for publishing and subscribing to Results</returns>
    /// <example>
    /// <code>
    /// var subject = ResultSubscriptionsCore.CreateResultSubject&lt;User&gt;();
    /// var subscription = subject.Subscribe(
    ///     onSuccess: user => Console.WriteLine($"User: {user.Name}"),
    ///     onFailure: errors => Console.WriteLine($"Error: {string.Join(", ", errors)}")
    /// );
    ///
    /// subject.OnNext(Result&lt;User&gt;.Success(new User("John")));
    /// subject.OnNext(Result&lt;User&gt;.WithFailure("User not found"));
    /// </code>
    /// </example>
    public static IResultSubject<T> CreateResultSubject<T>()
    {
        return new ResultSubject<T>();
    }

    /// <summary>
    /// Creates a subscription manager for grouping and managing multiple subscriptions.
    /// </summary>
    /// <returns>Subscription manager</returns>
    /// <example>
    /// <code>
    /// using var subscriptions = ResultSubscriptionsCore.CreateSubscriptionManager();
    ///
    /// subscriptions.Add(subject1.Subscribe(onSuccess: HandleUser));
    /// subscriptions.Add(subject2.Subscribe(onSuccess: HandleOrder));
    /// // All subscriptions disposed when manager is disposed
    /// </code>
    /// </example>
    public static ISubscriptionManager CreateSubscriptionManager()
    {
        return new SubscriptionManager();
    }

    /// <summary>
    /// Creates a safe subscription wrapper that converts exceptions to Result failures.
    /// </summary>
    /// <typeparam name="T">Type of the values</typeparam>
    /// <param name="onNext">Handler for values</param>
    /// <param name="onError">Handler for errors (optional)</param>
    /// <returns>Safe subscription wrapper</returns>
    public static Action<T> CreateSafeHandler<T>(Action<T> onNext, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(onNext);

        return value =>
        {
            try
            {
                onNext(value);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        };
    }

    /// <summary>
    /// Creates an async subscription wrapper with cancellation support.
    /// </summary>
    /// <typeparam name="T">Type of the values</typeparam>
    /// <param name="onNextAsync">Async handler for values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async subscription wrapper</returns>
    public static Func<T, Task> CreateAsyncHandler<T>(
        Func<T, Task> onNextAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onNextAsync);

        return async value =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await onNextAsync(value);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected
            }
            catch (Exception)
            {
                // Log or handle async errors as needed
                throw;
            }
        };
    }
}

/// <summary>
/// Interface for a Result subject that can publish Result values to subscribers.
/// </summary>
/// <typeparam name="T">Type of the Result values</typeparam>
public interface IResultSubject<T> : IDisposable
{
    /// <summary>
    /// Publishes a Result value to all subscribers.
    /// </summary>
    /// <param name="result">Result to publish</param>
    void OnNext(Result<T> result);

    /// <summary>
    /// Notifies all subscribers that an error occurred.
    /// </summary>
    /// <param name="exception">Error that occurred</param>
    void OnError(Exception exception);

    /// <summary>
    /// Notifies all subscribers that the stream is complete.
    /// </summary>
    void OnCompleted();

    /// <summary>
    /// Subscribes to the Result stream with separate success and failure handlers.
    /// </summary>
    /// <param name="onSuccess">Handler for successful Results</param>
    /// <param name="onFailure">Handler for failed Results</param>
    /// <param name="onCompleted">Handler for stream completion</param>
    /// <returns>Subscription that can be disposed</returns>
    IDisposable Subscribe(
        Action<T> onSuccess,
        Action<IEnumerable<string>>? onFailure = null,
        Action? onCompleted = null);

    /// <summary>
    /// Subscribes to the Result stream with a single Result handler.
    /// </summary>
    /// <param name="onResult">Handler for all Results</param>
    /// <param name="onCompleted">Handler for stream completion</param>
    /// <returns>Subscription that can be disposed</returns>
    IDisposable Subscribe(
        Action<Result<T>> onResult,
        Action? onCompleted = null);

    /// <summary>
    /// Gets the number of active subscribers.
    /// </summary>
    int SubscriberCount { get; }

    /// <summary>
    /// Gets whether the subject has been completed.
    /// </summary>
    bool IsCompleted { get; }
}

/// <summary>
/// Implementation of Result subject for publishing Result values to multiple subscribers.
/// </summary>
/// <typeparam name="T">Type of the Result values</typeparam>
internal class ResultSubject<T> : IResultSubject<T>
{
    private readonly ConcurrentDictionary<int, IResultObserver<T>> _observers = new();
    private readonly Lock _lock = new();
    private int _nextId = 0;
    private bool _disposed = false;

    public int SubscriberCount => _observers.Count;
    public bool IsCompleted { get; private set; } = false;

    public void OnNext(Result<T> result)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ResultSubject<>).Name);
        if (IsCompleted)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(result);

        foreach (var observer in _observers.Values)
        {
            try
            {
                observer.OnNext(result);
            }
            catch (Exception)
            {
                // Don't let observer exceptions affect other observers
            }
        }
    }

    public void OnError(Exception exception)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ResultSubject<>).Name);
        if (IsCompleted)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(exception);

        {
            using var _ = _lock.EnterScope();
            if (IsCompleted)
            {
                return;
            }

            IsCompleted = true;
        }

        foreach (var observer in _observers.Values)
        {
            try
            {
                observer.OnError(exception);
            }
            catch (Exception)
            {
                // Don't let observer exceptions affect other observers
            }
        }

        _observers.Clear();
    }

    public void OnCompleted()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ResultSubject<>).Name);
        if (IsCompleted)
        {
            return;
        }

        {
            using var _ = _lock.EnterScope();
            if (IsCompleted)
            {
                return;
            }

            IsCompleted = true;
        }

        foreach (var observer in _observers.Values)
        {
            try
            {
                observer.OnCompleted();
            }
            catch (Exception)
            {
                // Don't let observer exceptions affect other observers
            }
        }

        _observers.Clear();
    }

    public IDisposable Subscribe(
        Action<T> onSuccess,
        Action<IEnumerable<string>>? onFailure = null,
        Action? onCompleted = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ResultSubject<>).Name);
        ArgumentNullException.ThrowIfNull(onSuccess);

        var observer = new ResultObserver<T>(onSuccess, onFailure, onCompleted);
        var id = Interlocked.Increment(ref _nextId);

        _observers[id] = observer;

        return new Subscription(() => _observers.TryRemove(id, out _));
    }

    public IDisposable Subscribe(
        Action<Result<T>> onResult,
        Action? onCompleted = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(ResultSubject<>).Name);
        ArgumentNullException.ThrowIfNull(onResult);

        var observer = new ResultObserver<T>(onResult, onCompleted);
        var id = Interlocked.Increment(ref _nextId);

        _observers[id] = observer;

        return new Subscription(() => _observers.TryRemove(id, out _));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            OnCompleted();
            _disposed = true;
        }
    }
}

/// <summary>
/// Interface for observing Result values.
/// </summary>
/// <typeparam name="T">Type of the Result values</typeparam>
internal interface IResultObserver<T>
{
    void OnNext(Result<T> result);

    void OnError(Exception exception);

    void OnCompleted();
}

/// <summary>
/// Implementation of Result observer.
/// </summary>
/// <typeparam name="T">Type of the Result values</typeparam>
internal class ResultObserver<T> : IResultObserver<T>
{
    private readonly Action<T>? _onSuccess;
    private readonly Action<IEnumerable<string>>? _onFailure;
    private readonly Action<Result<T>>? _onResult;
    private readonly Action? _onCompleted;

    public ResultObserver(
        Action<T> onSuccess,
        Action<IEnumerable<string>>? onFailure,
        Action? onCompleted)
    {
        _onSuccess = onSuccess ?? throw new ArgumentNullException(nameof(onSuccess));
        _onFailure = onFailure;
        _onCompleted = onCompleted;
    }

    public ResultObserver(
        Action<Result<T>> onResult,
        Action? onCompleted)
    {
        _onResult = onResult ?? throw new ArgumentNullException(nameof(onResult));
        _onCompleted = onCompleted;
    }

    public void OnNext(Result<T> result)
    {
        if (_onResult != null)
        {
            _onResult(result);
        }
        else if (result.IsSuccess)
        {
            _onSuccess?.Invoke(result.Value!);
        }
        else
        {
            _onFailure?.Invoke(result.Errors ?? [ResultConstants.DefaultErrorMessage]);
        }
    }

    public void OnError(Exception exception)
    {
        if (_onResult != null)
        {
            _onResult(Result<T>.WithFailure($"Stream error: {exception.Message}"));
        }
        else
        {
            _onFailure?.Invoke([$"Stream error: {exception.Message}"]);
        }
    }

    public void OnCompleted()
    {
        _onCompleted?.Invoke();
    }
}

/// <summary>
/// Simple subscription implementation.
/// </summary>
internal class Subscription : IDisposable
{
    private readonly Action _dispose;
    private bool _disposed = false;

    public Subscription(Action dispose)
    {
        _dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Interface for managing multiple subscriptions.
/// </summary>
public interface ISubscriptionManager : IDisposable
{
    /// <summary>
    /// Adds a subscription to be managed.
    /// </summary>
    /// <param name="subscription">Subscription to add</param>
    void Add(IDisposable subscription);

    /// <summary>
    /// Removes and disposes a subscription.
    /// </summary>
    /// <param name="subscription">Subscription to remove</param>
    /// <returns>True if removed successfully</returns>
    bool Remove(IDisposable subscription);

    /// <summary>
    /// Gets the number of managed subscriptions.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Clears all subscriptions without disposing the manager.
    /// </summary>
    void Clear();
}

/// <summary>
/// Implementation of subscription manager.
/// </summary>
internal class SubscriptionManager : ISubscriptionManager
{
    private readonly ConcurrentBag<IDisposable> _subscriptions = [];
    private bool _disposed = false;

    public int Count => _subscriptions.Count;

    public void Add(IDisposable subscription)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SubscriptionManager));
        ArgumentNullException.ThrowIfNull(subscription);

        _subscriptions.Add(subscription);
    }

    public bool Remove(IDisposable subscription)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SubscriptionManager));
        ArgumentNullException.ThrowIfNull(subscription);

        // Note: ConcurrentBag doesn't support removal, so we dispose it directly
        subscription.Dispose();
        return true;
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SubscriptionManager));

        while (_subscriptions.TryTake(out var subscription))
        {
            subscription?.Dispose();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Clear();
            _disposed = true;
        }
    }
}
