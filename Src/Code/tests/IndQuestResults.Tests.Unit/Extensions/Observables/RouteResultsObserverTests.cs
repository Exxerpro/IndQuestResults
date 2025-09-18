using IndQuestResults.Extensions.Observables;
using IndQuestResults.Operations;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace IndQuestResults.Tests.Unit.Extensions.Observables;

/// <summary>
/// Tests for RouteResults functionality and the RouteResultsObserver class.
/// This tests the routing of Results to separate success and failure streams.
/// </summary>
public class RouteResultsObserverTests
{
    #region Test Helper Classes

    private sealed class TestObservable<T> : IObservable<T>
    {
        private readonly List<IObserver<T>> _observers = [];

        public IDisposable Subscribe(IObserver<T> observer)
        {
            _observers.Add(observer);
            return new Subscription(() => _observers.Remove(observer));
        }

        public void OnNext(T value)
        {
            foreach (var o in _observers.ToArray())
            {
                o.OnNext(value);
            }
        }

        public void OnError(Exception ex)
        {
            foreach (var o in _observers.ToArray())
            {
                o.OnError(ex);
            }
        }

        public void OnCompleted()
        {
            foreach (var o in _observers.ToArray())
            {
                o.OnCompleted();
            }
        }
    }

    #endregion

    #region RouteResults Method Tests

    [Fact]
    public void RouteResults_SuccessfulResult_RoutesToSuccessSubject()
    {
        // Arrange
        var sourceObservable = new TestObservable<Result<int>>();
        var successSubject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failureSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        var receivedSuccesses = new List<int>();
        var receivedFailures = new List<string>();

        successSubject.Subscribe(onSuccess: receivedSuccesses.Add);
        failureSubject.Subscribe(onSuccess: receivedFailures.Add);

        // Act
        using var subscription = sourceObservable.RouteResults(successSubject, failureSubject);
        sourceObservable.OnNext(Result<int>.Success(42));

        // Assert
        receivedSuccesses.ShouldBe([42]);
        receivedFailures.ShouldBeEmpty();
    }

    [Fact]
    public void RouteResults_FailedResult_RoutesToFailureSubject()
    {
        // Arrange
        var sourceObservable = new TestObservable<Result<int>>();
        var successSubject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failureSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        var receivedSuccesses = new List<int>();
        var receivedFailures = new List<string>();

        successSubject.Subscribe(onSuccess: receivedSuccesses.Add);
        failureSubject.Subscribe(onSuccess: receivedFailures.Add);

        // Act
        using var subscription = sourceObservable.RouteResults(successSubject, failureSubject);
        sourceObservable.OnNext(Result<int>.WithFailure("Operation failed"));

        // Assert
        receivedSuccesses.ShouldBeEmpty();
        receivedFailures.ShouldBe(["Operation failed"]);
    }

    [Fact]
    public void RouteResults_MultipleErrors_CombinesErrorMessages()
    {
        // Arrange
        var sourceObservable = new TestObservable<Result<int>>();
        var successSubject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failureSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        var receivedSuccesses = new List<int>();
        var receivedFailures = new List<string>();

        successSubject.Subscribe(onSuccess: receivedSuccesses.Add);
        failureSubject.Subscribe(onSuccess: receivedFailures.Add);

        // Act
        using var subscription = sourceObservable.RouteResults(successSubject, failureSubject);
        sourceObservable.OnNext(Result<int>.WithFailure(new[] { "Error 1", "Error 2", "Error 3" }));

        // Assert
        receivedSuccesses.ShouldBeEmpty();
        receivedFailures.ShouldBe(["Error 1, Error 2, Error 3"]);
    }

    [Fact]
    public void RouteResults_MixedResults_RoutesAppropriately()
    {
        // Arrange
        var sourceObservable = new TestObservable<Result<int>>();
        var successSubject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failureSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        var receivedSuccesses = new List<int>();
        var receivedFailures = new List<string>();

        successSubject.Subscribe(onSuccess: receivedSuccesses.Add);
        failureSubject.Subscribe(onSuccess: receivedFailures.Add);

        // Act
        using var subscription = sourceObservable.RouteResults(successSubject, failureSubject);
        sourceObservable.OnNext(Result<int>.Success(1));
        sourceObservable.OnNext(Result<int>.WithFailure("Error 1"));
        sourceObservable.OnNext(Result<int>.Success(2));
        sourceObservable.OnNext(Result<int>.WithFailure("Error 2"));

        // Assert
        receivedSuccesses.ShouldBe([1, 2]);
        receivedFailures.ShouldBe(["Error 1", "Error 2"]);
    }

    [Fact]
    public void RouteResults_StreamError_RoutesToFailureSubject()
    {
        // Arrange
        var sourceObservable = new TestObservable<Result<int>>();
        var successSubject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failureSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        var receivedSuccesses = new List<int>();
        var receivedFailures = new List<string>();

        successSubject.Subscribe(onSuccess: receivedSuccesses.Add);
        failureSubject.Subscribe(onSuccess: receivedFailures.Add);

        // Act
        using var subscription = sourceObservable.RouteResults(successSubject, failureSubject);
        sourceObservable.OnError(new InvalidOperationException("Stream failed"));

        // Assert
        receivedSuccesses.ShouldBeEmpty();
        receivedFailures.ShouldBe(["Stream error: Stream failed"]);
    }

    [Fact]
    public void RouteResults_StreamCompleted_CompletesSubjects()
    {
        // Arrange
        var sourceObservable = new TestObservable<Result<int>>();
        var successSubject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failureSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        // Act
        using var subscription = sourceObservable.RouteResults(successSubject, failureSubject);
        sourceObservable.OnCompleted();

        // Assert
        successSubject.IsCompleted.ShouldBeTrue();
        failureSubject.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public void RouteResults_EmptyErrors_UsesDefaultMessage()
    {
        // Arrange
        var sourceObservable = new TestObservable<Result<int>>();
        var successSubject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failureSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        var receivedFailures = new List<string>();
        failureSubject.Subscribe(onSuccess: receivedFailures.Add);

        // Act - Create a failure result and test default message handling
        using var subscription = sourceObservable.RouteResults(successSubject, failureSubject);
        
        // Create a result with an empty error message (which still contains the message)
        var failureResult = Result<int>.WithFailure("Empty message");
        sourceObservable.OnNext(failureResult);

        // Assert - Should receive the error message
        receivedFailures.ShouldNotBeEmpty();
        receivedFailures.First().ShouldBe("Empty message");
    }

    #endregion

    #region Real-world Scenarios

    [Fact]
    public void RouteResults_GatewayScenario_PartitionsDataFlow()
    {
        // Arrange - Simulate a gateway that processes orders
        var orderStream = new TestObservable<Result<Order>>();
        var validOrdersSubject = ResultSubscriptionsCore.CreateResultSubject<Order>();
        var invalidOrdersSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        var validOrders = new List<Order>();
        var invalidOrderReasons = new List<string>();

        validOrdersSubject.Subscribe(onSuccess: validOrders.Add);
        invalidOrdersSubject.Subscribe(onSuccess: invalidOrderReasons.Add);

        // Act
        using var subscription = orderStream.RouteResults(validOrdersSubject, invalidOrdersSubject);

        // Simulate processing various orders
        orderStream.OnNext(Result<Order>.Success(new Order(1, "Valid Order")));
        orderStream.OnNext(Result<Order>.WithFailure("Invalid payment method"));
        orderStream.OnNext(Result<Order>.Success(new Order(2, "Another Valid Order")));
        orderStream.OnNext(Result<Order>.WithFailure(new[] { "Missing customer ID", "Invalid shipping address" }));

        // Assert
        validOrders.Count().ShouldBe(2);
        validOrders.First().Id.ShouldBe(1);
        validOrders.Last().Id.ShouldBe(2);

        invalidOrderReasons.Count().ShouldBe(2);
        invalidOrderReasons.First().ShouldBe("Invalid payment method");
        invalidOrderReasons.Last().ShouldBe("Missing customer ID, Invalid shipping address");
    }

    [Fact]
    public void RouteResults_HighThroughputScenario_HandlesLargeVolume()
    {
        // Arrange
        var sourceObservable = new TestObservable<Result<int>>();
        var successSubject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failureSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        var receivedSuccesses = new List<int>();
        var receivedFailures = new List<string>();

        successSubject.Subscribe(onSuccess: receivedSuccesses.Add);
        failureSubject.Subscribe(onSuccess: receivedFailures.Add);

        // Act
        using var subscription = sourceObservable.RouteResults(successSubject, failureSubject);

        // Simulate high throughput with mixed results
        for (int i = 0; i < 1000; i++)
        {
            if (i % 3 == 0)
            {
                sourceObservable.OnNext(Result<int>.WithFailure($"Error {i}"));
            }
            else
            {
                sourceObservable.OnNext(Result<int>.Success(i));
            }
        }

        // Assert
        receivedSuccesses.Count().ShouldBe(666); // Items 1,2,4,5,7,8... (excludes 0,3,6,9...)
        receivedFailures.Count().ShouldBe(334); // Items 0,3,6,9... (i % 3 == 0)
        
        // Verify ordering is preserved
        receivedSuccesses.First().ShouldBe(1);
        receivedSuccesses.Skip(1).First().ShouldBe(2);
        
        receivedFailures.First().ShouldBe("Error 0");
        receivedFailures.Skip(1).First().ShouldBe("Error 3");
    }

    [Fact]
    public void RouteResults_MultiplePipelines_IndependentOperation()
    {
        // Arrange - Multiple independent routing pipelines
        var orderStream = new TestObservable<Result<Order>>();
        var userStream = new TestObservable<Result<User>>();

        // Order pipeline
        var validOrdersSubject = ResultSubscriptionsCore.CreateResultSubject<Order>();
        var invalidOrdersSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        // User pipeline
        var validUsersSubject = ResultSubscriptionsCore.CreateResultSubject<User>();
        var invalidUsersSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        var validOrders = new List<Order>();
        var validUsers = new List<User>();
        var orderErrors = new List<string>();
        var userErrors = new List<string>();

        validOrdersSubject.Subscribe(onSuccess: validOrders.Add);
        invalidOrdersSubject.Subscribe(onSuccess: orderErrors.Add);
        validUsersSubject.Subscribe(onSuccess: validUsers.Add);
        invalidUsersSubject.Subscribe(onSuccess: userErrors.Add);

        // Act
        using var orderSubscription = orderStream.RouteResults(validOrdersSubject, invalidOrdersSubject);
        using var userSubscription = userStream.RouteResults(validUsersSubject, invalidUsersSubject);

        orderStream.OnNext(Result<Order>.Success(new Order(1, "Order 1")));
        userStream.OnNext(Result<User>.WithFailure("Invalid user"));
        orderStream.OnNext(Result<Order>.WithFailure("Invalid order"));
        userStream.OnNext(Result<User>.Success(new User("John", "john@example.com")));

        // Assert
        validOrders.Count().ShouldBe(1);
        validUsers.Count().ShouldBe(1);
        orderErrors.Count().ShouldBe(1);
        userErrors.Count().ShouldBe(1);

        validOrders.First().Name.ShouldBe("Order 1");
        validUsers.First().Name.ShouldBe("John");
        orderErrors.First().ShouldBe("Invalid order");
        userErrors.First().ShouldBe("Invalid user");
    }

    #endregion

    #region Null Argument Tests

    [Fact]
    public void RouteResults_NullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var successSubject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failureSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            ((IObservable<Result<int>>)null!).RouteResults(successSubject, failureSubject));
    }

    [Fact]
    public void RouteResults_NullSuccessSubject_ThrowsArgumentNullException()
    {
        // Arrange
        var sourceObservable = new TestObservable<Result<int>>();
        var failureSubject = ResultSubscriptionsCore.CreateResultSubject<string>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            sourceObservable.RouteResults(null!, failureSubject));
    }

    [Fact]
    public void RouteResults_NullFailureSubject_ThrowsArgumentNullException()
    {
        // Arrange
        var sourceObservable = new TestObservable<Result<int>>();
        var successSubject = ResultSubscriptionsCore.CreateResultSubject<int>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            sourceObservable.RouteResults(successSubject, null!));
    }

    #endregion

    #region Helper Classes

    private record Order(int Id, string Name);
    private record User(string Name, string Email);

    #endregion
}




