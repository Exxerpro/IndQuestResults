namespace IndQuestResults.Tests.Unit.Reactive;

/// <summary>
/// Tests for the ResultBridgeObserver which converts observable values and errors
/// into Result instances for downstream processing.
/// </summary>
public class ResultBridgeObserverAdditionalTests
{
    [Fact]
    public void OnNext_ForwardsSuccess_ToHandler()
    {
        var results = new List<Result<int>>();
        var obs = new IndQuestResults.Reactive.ResultBridgeObserver<int>(r => results.Add(r), onCompleted: null);

        obs.OnNext(5);

        results.Count.ShouldBe(1);
        results[0].IsSuccess.ShouldBeTrue();
        results[0].Value.ShouldBe(5);
    }

    [Fact]
    public void OnError_ForwardsFailure_WithMessage()
    {
        var results = new List<Result<int>>();
        var obs = new IndQuestResults.Reactive.ResultBridgeObserver<int>(r => results.Add(r), onCompleted: null);

        obs.OnError(new InvalidOperationException("boom"));

        results.Count.ShouldBe(1);
        results[0].IsFailure.ShouldBeTrue();
        results[0].Error.ShouldContain("Observable error: boom");
    }

#pragma warning disable xUnit1004 // documented known-failing aspirational contract, kept visible as a skip
    [Fact(Skip = "Aspirational contract never implemented: ResultBridgeObserver.OnNext does not catch handler exceptions, and the test's own always-throwing handler makes its results assertion unsatisfiable. Known-failing on Kat3 baseline (IndQuestFailingTests). Revisit with a Reactive exception-preservation design.")]
#pragma warning restore xUnit1004
    public void OnNext_ExceptionInHandler_PreservesException()
    {
        // Arrange
        var exception = new InvalidOperationException("Handler exception");
        var results = new List<Result<int>>();
        var obs = new IndQuestResults.Reactive.ResultBridgeObserver<int>(
            r => { throw exception; },
            onCompleted: null);

        // Act
        obs.OnNext(5);

        // Assert - Exception should be caught and preserved
        results.Count.ShouldBe(1);
        results[0].IsFailure.ShouldBeTrue();
        results[0].Exception.ShouldNotBeNull();
        results[0].Exception.ShouldBe(exception);
        results[0].IsFaulted.ShouldBeTrue();
        results[0].Error.ShouldContain("Result handler error");
    }

    [Fact]
    public void OnError_PreservesException()
    {
        // Arrange
        var exception = new InvalidOperationException("Observable error");
        var results = new List<Result<int>>();
        var obs = new IndQuestResults.Reactive.ResultBridgeObserver<int>(r => results.Add(r), onCompleted: null);

        // Act
        obs.OnError(exception);

        // Assert
        results.Count.ShouldBe(1);
        results[0].IsFailure.ShouldBeTrue();
        results[0].Exception.ShouldNotBeNull();
        results[0].Exception.ShouldBe(exception);
        results[0].IsFaulted.ShouldBeTrue();
        results[0].Error.ShouldContain("Observable error: Observable error");
    }
}


