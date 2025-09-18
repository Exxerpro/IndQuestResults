using IndQuestResults.Extensions.Observables;
using IndQuestResults.Operations;
using Shouldly;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IndQuestResults.Tests.Unit.Extensions.Observables;

/// <summary>
/// Tests for ResultSubscriptionsCore functionality covering factory methods,
/// subscription management, thread safety, and resource cleanup.
/// </summary>
public class ResultSubscriptionsCoreTests
{
    #region CreateResultSubject Tests

    [Fact]
    public void CreateResultSubject_ReturnsWorkingSubject()
    {
        // Act
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();

        // Assert
        subject.ShouldNotBeNull();
        subject.SubscriberCount.ShouldBe(0);
        subject.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public void CreateResultSubject_CanPublishAndSubscribe()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<string>();
        var receivedValues = new List<string>();
        var receivedErrors = new List<string>();

        using var subscription = subject.Subscribe(
            onSuccess: receivedValues.Add,
            onFailure: errors => receivedErrors.AddRange(errors)
        );

        // Act
        subject.OnNext(Result<string>.Success("Hello"));
        subject.OnNext(Result<string>.WithFailure("Error"));

        // Assert
        receivedValues.ShouldBe(["Hello"]);
        receivedErrors.ShouldBe(["Error"]);
    }

    #endregion

    #region CreateSubscriptionManager Tests

    [Fact]
    public void CreateSubscriptionManager_ReturnsWorkingManager()
    {
        // Act
        using var manager = ResultSubscriptionsCore.CreateSubscriptionManager();

        // Assert
        manager.ShouldNotBeNull();
        manager.Count.ShouldBe(0);
    }

    [Fact]
    public void CreateSubscriptionManager_CanManageSubscriptions()
    {
        // Arrange
        using var manager = ResultSubscriptionsCore.CreateSubscriptionManager();
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();

        var subscription1 = subject.Subscribe(onSuccess: _ => { });
        var subscription2 = subject.Subscribe(onSuccess: _ => { });

        // Act
        manager.Add(subscription1);
        manager.Add(subscription2);

        // Assert
        manager.Count.ShouldBe(2);
    }

    [Fact]
    public void CreateSubscriptionManager_RemoveDisposesSubscription()
    {
        // Arrange
        using var manager = ResultSubscriptionsCore.CreateSubscriptionManager();
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var subscription = subject.Subscribe(onSuccess: _ => { });

        manager.Add(subscription);
        var initialCount = subject.SubscriberCount;

        // Act
        var removed = manager.Remove(subscription);

        // Assert
        removed.ShouldBeTrue();
        subject.SubscriberCount.ShouldBeLessThan(initialCount);
    }

    [Fact]
    public void CreateSubscriptionManager_ClearDisposesAllSubscriptions()
    {
        // Arrange
        using var manager = ResultSubscriptionsCore.CreateSubscriptionManager();
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();

        manager.Add(subject.Subscribe(onSuccess: _ => { }));
        manager.Add(subject.Subscribe(onSuccess: _ => { }));
        var initialCount = subject.SubscriberCount;

        // Act
        manager.Clear();

        // Assert
        manager.Count.ShouldBe(0);
        subject.SubscriberCount.ShouldBeLessThan(initialCount);
    }

    [Fact]
    public void CreateSubscriptionManager_DisposesCleansUpAll()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var manager = ResultSubscriptionsCore.CreateSubscriptionManager();

        manager.Add(subject.Subscribe(onSuccess: _ => { }));
        manager.Add(subject.Subscribe(onSuccess: _ => { }));
        var initialCount = subject.SubscriberCount;

        // Act
        manager.Dispose();

        // Assert
        subject.SubscriberCount.ShouldBeLessThan(initialCount);
    }

    #endregion

    #region CreateSafeHandler Tests

    [Fact]
    public void CreateSafeHandler_NullOnNext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ResultSubscriptionsCore.CreateSafeHandler<int>(null!));
    }

    [Fact]
    public void CreateSafeHandler_NormalExecution_CallsOnNext()
    {
        // Arrange
        var receivedValues = new List<int>();
        var handler = ResultSubscriptionsCore.CreateSafeHandler<int>(
            onNext: receivedValues.Add
        );

        // Act
        handler(42);
        handler(100);

        // Assert
        receivedValues.ShouldBe([42, 100]);
    }

    [Fact]
    public void CreateSafeHandler_OnNextThrows_CallsOnError()
    {
        // Arrange
        var caughtExceptions = new List<Exception>();
        var handler = ResultSubscriptionsCore.CreateSafeHandler<int>(
            onNext: _ => throw new InvalidOperationException("Test error"),
            onError: caughtExceptions.Add
        );

        // Act
        handler(42);

        // Assert
        caughtExceptions.Count().ShouldBe(1);
        caughtExceptions.First().ShouldBeOfType<InvalidOperationException>();
        caughtExceptions.First().Message.ShouldBe("Test error");
    }

    [Fact]
    public void CreateSafeHandler_OnNextThrowsNoErrorHandler_DoesNotPropagate()
    {
        // Arrange
        var handler = ResultSubscriptionsCore.CreateSafeHandler<int>(
            onNext: _ => throw new InvalidOperationException("Test error")
        );

        // Act & Assert - Should not throw
        Should.NotThrow(() => handler(42));
    }

    #endregion

    #region CreateAsyncHandler Tests

    [Fact]
    public void CreateAsyncHandler_NullOnNextAsync_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ResultSubscriptionsCore.CreateAsyncHandler<int>(null!));
    }

    [Fact]
    public async Task CreateAsyncHandler_NormalExecution_CallsOnNextAsync()
    {
        // Arrange
        var receivedValues = new List<int>();
        var handler = ResultSubscriptionsCore.CreateAsyncHandler<int>(
            onNextAsync: async value =>
            {
                await Task.Delay(1);
                receivedValues.Add(value);
            }
        );

        // Act
        await handler(42);
        await handler(100);

        // Assert
        receivedValues.ShouldBe([42, 100]);
    }

    [Fact]
    public async Task CreateAsyncHandler_CancellationRequested_ReturnsEarly()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var called = false;
        var handler = ResultSubscriptionsCore.CreateAsyncHandler<int>(
            onNextAsync: async _ =>
            {
                await Task.Delay(100);
                called = true;
            },
            cancellationToken: cts.Token
        );

        // Act
        await handler(42);

        // Assert
        called.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateAsyncHandler_OperationCanceledException_DoesNotPropagate()
    {
        // Arrange
        var handler = ResultSubscriptionsCore.CreateAsyncHandler<int>(
            onNextAsync: _ => throw new OperationCanceledException("Cancelled")
        );

        // Act & Assert - Should not throw
        await Should.NotThrowAsync(async () => await handler(42));
    }

    [Fact]
    public async Task CreateAsyncHandler_OtherException_Propagates()
    {
        // Arrange
        var handler = ResultSubscriptionsCore.CreateAsyncHandler<int>(
            onNextAsync: _ => throw new InvalidOperationException("Test error")
        );

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await handler(42));
    }

    #endregion

    #region ResultSubject Lifecycle Tests

    [Fact]
    public void ResultSubject_InitialState_IsCorrect()
    {
        // Arrange & Act
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();

        // Assert
        subject.SubscriberCount.ShouldBe(0);
        subject.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public void ResultSubject_Subscribe_IncreasesSubscriberCount()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();

        // Act
        using var subscription1 = subject.Subscribe(onSuccess: _ => { });
        using var subscription2 = subject.Subscribe(onSuccess: _ => { });

        // Assert
        subject.SubscriberCount.ShouldBe(2);
    }

    [Fact]
    public void ResultSubject_SubscribeDispose_DecreasesSubscriberCount()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var subscription = subject.Subscribe(onSuccess: _ => { });

        // Act
        subscription.Dispose();

        // Assert
        subject.SubscriberCount.ShouldBe(0);
    }

    [Fact]
    public void ResultSubject_OnCompleted_SetsCompletedState()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var completed = false;

        using var subscription = subject.Subscribe(
            onSuccess: _ => { },
            onCompleted: () => completed = true
        );

        // Act
        subject.OnCompleted();

        // Assert
        subject.IsCompleted.ShouldBeTrue();
        completed.ShouldBeTrue();
        subject.SubscriberCount.ShouldBe(0); // Observers cleared after completion
    }

    [Fact]
    public void ResultSubject_OnError_SetsCompletedState()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var receivedErrors = new List<string>();

        using var subscription = subject.Subscribe(
            onSuccess: _ => { },
            onFailure: receivedErrors.AddRange
        );

        // Act
        subject.OnError(new InvalidOperationException("Test error"));

        // Assert
        subject.IsCompleted.ShouldBeTrue();
        receivedErrors.ShouldContain("Stream error: Test error");
        subject.SubscriberCount.ShouldBe(0); // Observers cleared after error
    }

    [Fact]
    public void ResultSubject_OnNextAfterCompleted_IsIgnored()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var receivedValues = new List<int>();

        using var subscription = subject.Subscribe(onSuccess: receivedValues.Add);
        subject.OnCompleted();

        // Act
        subject.OnNext(Result<int>.Success(42));

        // Assert
        receivedValues.ShouldBeEmpty();
    }

    [Fact]
    public void ResultSubject_OnErrorAfterCompleted_IsIgnored()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var receivedErrors = new List<string>();

        using var subscription = subject.Subscribe(
            onSuccess: _ => { },
            onFailure: receivedErrors.AddRange
        );
        subject.OnCompleted();

        // Act
        subject.OnError(new InvalidOperationException("Should be ignored"));

        // Assert
        receivedErrors.ShouldBeEmpty();
    }

    [Fact]
    public void ResultSubject_Dispose_CallsOnCompleted()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var completed = false;

        using var subscription = subject.Subscribe(
            onSuccess: _ => { },
            onCompleted: () => completed = true
        );

        // Act
        subject.Dispose();

        // Assert
        subject.IsCompleted.ShouldBeTrue();
        completed.ShouldBeTrue();
    }

    #endregion

    #region Exception Safety Tests

    [Fact]
    public void ResultSubject_ObserverExceptionOnNext_DoesNotAffectOtherObservers()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var goodObserverCalled = false;

        using var subscription1 = subject.Subscribe(onSuccess: _ => throw new InvalidOperationException("Bad observer"));
        using var subscription2 = subject.Subscribe(onSuccess: _ => goodObserverCalled = true);

        // Act
        subject.OnNext(Result<int>.Success(42));

        // Assert
        goodObserverCalled.ShouldBeTrue();
    }

    [Fact]
    public void ResultSubject_ObserverExceptionOnError_DoesNotAffectOtherObservers()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var goodObserverCalled = false;

        using var subscription1 = subject.Subscribe(
            onSuccess: _ => { },
            onFailure: _ => throw new InvalidOperationException("Bad observer")
        );
        using var subscription2 = subject.Subscribe(
            onSuccess: _ => { },
            onFailure: _ => goodObserverCalled = true
        );

        // Act
        subject.OnError(new InvalidOperationException("Test error"));

        // Assert
        goodObserverCalled.ShouldBeTrue();
    }

    [Fact]
    public void ResultSubject_ObserverExceptionOnCompleted_DoesNotAffectOtherObservers()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var goodObserverCalled = false;

        using var subscription1 = subject.Subscribe(
            onSuccess: _ => { },
            onCompleted: () => throw new InvalidOperationException("Bad observer")
        );
        using var subscription2 = subject.Subscribe(
            onSuccess: _ => { },
            onCompleted: () => goodObserverCalled = true
        );

        // Act
        subject.OnCompleted();

        // Assert
        goodObserverCalled.ShouldBeTrue();
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task ResultSubject_ConcurrentSubscribeUnsubscribe_IsThreadSafe()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var subscriptions = new ConcurrentBag<IDisposable>();
        var tasks = new List<Task>();

        // Act - Concurrent subscribe and unsubscribe
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var subscription = subject.Subscribe(onSuccess: _ => { });
                subscriptions.Add(subscription);
                
                // Randomly dispose some subscriptions
                if (Random.Shared.Next(2) == 0)
                {
                    subscription.Dispose();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should not crash or corrupt state
        subject.SubscriberCount.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ResultSubject_ConcurrentPublishing_IsThreadSafe()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var receivedValues = new ConcurrentBag<int>();

        using var subscription = subject.Subscribe(onSuccess: receivedValues.Add);

        var tasks = new List<Task>();

        // Act - Concurrent publishing
        for (int i = 0; i < 100; i++)
        {
            var value = i;
            tasks.Add(Task.Run(() => subject.OnNext(Result<int>.Success(value))));
        }

        await Task.WhenAll(tasks);

        // Assert
        receivedValues.Count().ShouldBe(100);
        receivedValues.Distinct().Count().ShouldBe(100); // All unique values received
    }

    [Fact]
    public async Task ResultSubject_ConcurrentCompletionAndPublishing_IsThreadSafe()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var receivedValues = new ConcurrentBag<int>();

        using var subscription = subject.Subscribe(onSuccess: receivedValues.Add);

        var tasks = new List<Task>();

        // Act - Concurrent publishing and completion
        for (int i = 0; i < 50; i++)
        {
            var value = i;
            tasks.Add(Task.Run(() => subject.OnNext(Result<int>.Success(value))));
        }

        tasks.Add(Task.Run(() => subject.OnCompleted()));

        await Task.WhenAll(tasks);

        // Assert - Should complete without issues
        subject.IsCompleted.ShouldBeTrue();
    }

    #endregion

    #region ResultObserver Subscribe Patterns Tests

    [Fact]
    public void ResultSubject_SubscribeWithResultHandler_ReceivesAllResults()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var receivedResults = new List<Result<int>>();

        using var subscription = subject.Subscribe(onResult: receivedResults.Add);

        // Act
        subject.OnNext(Result<int>.Success(42));
        subject.OnNext(Result<int>.WithFailure("Error"));

        // Assert
        receivedResults.Count().ShouldBe(2);
        receivedResults[0].IsSuccess.ShouldBeTrue();
        receivedResults[0].Value.ShouldBe(42);
        receivedResults[1].IsFailure.ShouldBeTrue();
        receivedResults[1].Error.ShouldBe("Error");
    }

    [Fact]
    public void ResultSubject_SubscribeWithSeparateHandlers_RoutesCorrectly()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<string>();
        var successValues = new List<string>();
        var failureErrors = new List<string>();

        using var subscription = subject.Subscribe(
            onSuccess: successValues.Add,
            onFailure: errors => failureErrors.AddRange(errors)
        );

        // Act
        subject.OnNext(Result<string>.Success("Success"));
        subject.OnNext(Result<string>.WithFailure(new[] { "Error1", "Error2" }));

        // Assert
        successValues.ShouldBe(["Success"]);
        failureErrors.ShouldBe(["Error1", "Error2"]);
    }

    [Fact]
    public void ResultSubject_SubscribeWithNullFailureHandler_HandlesSuccessOnly()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var receivedValues = new List<int>();

        using var subscription = subject.Subscribe(
            onSuccess: receivedValues.Add,
            onFailure: null // Null failure handler
        );

        // Act
        subject.OnNext(Result<int>.Success(42));
        subject.OnNext(Result<int>.WithFailure("Error"));

        // Assert
        receivedValues.ShouldBe([42]);
        // Failure should be handled gracefully with null handler
    }

    #endregion

    #region Error Handling Edge Cases

    [Fact]
    public void ResultSubject_OnNextWithNullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => subject.OnNext(null!));
    }

    [Fact]
    public void ResultSubject_OnErrorWithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => subject.OnError(null!));
    }

    [Fact]
    public void ResultSubject_SubscribeWithNullOnSuccess_ThrowsArgumentNullException()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => subject.Subscribe(onSuccess: null!));
    }

    [Fact]
    public void ResultSubject_SubscribeWithNullOnResult_ThrowsArgumentNullException()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => subject.Subscribe(onResult: null!));
    }

    [Fact]
    public void ResultSubject_OperationsAfterDispose_ThrowObjectDisposedException()
    {
        // Arrange
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        subject.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => subject.OnNext(Result<int>.Success(42)));
        Should.Throw<ObjectDisposedException>(() => subject.OnError(new Exception()));
        Should.Throw<ObjectDisposedException>(() => subject.OnCompleted());
        Should.Throw<ObjectDisposedException>(() => subject.Subscribe(onSuccess: _ => { }));
    }

    [Fact]
    public void SubscriptionManager_OperationsAfterDispose_ThrowObjectDisposedException()
    {
        // Arrange
        var manager = ResultSubscriptionsCore.CreateSubscriptionManager();
        var subscription = ResultSubscriptionsCore.CreateResultSubject<int>().Subscribe(onSuccess: _ => { });
        manager.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => manager.Add(subscription));
        Should.Throw<ObjectDisposedException>(() => manager.Remove(subscription));
        Should.Throw<ObjectDisposedException>(() => manager.Clear());
    }

    [Fact]
    public void SubscriptionManager_NullArguments_ThrowArgumentNullException()
    {
        // Arrange
        using var manager = ResultSubscriptionsCore.CreateSubscriptionManager();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => manager.Add(null!));
        Should.Throw<ArgumentNullException>(() => manager.Remove(null!));
    }

    #endregion
}




