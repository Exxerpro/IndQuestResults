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
}


