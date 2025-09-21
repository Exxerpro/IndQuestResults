using IndQuestResults;
using IndQuestResults.Reactive;

namespace IndQuestResults.Tests.Unit.Reactive;

/// <summary>
/// Tests for reactive collection helpers that aggregate IObservable streams into Result values.
/// Covers success aggregation, error propagation, timeouts, and cancellation.
/// </summary>
public class CollectResultsBranchTests
{
    private sealed class TestObservable<T> : IObservable<T>
    {
        private readonly Action<IObserver<T>> _subscribe;
        public TestObservable(Action<IObserver<T>> subscribe) => _subscribe = subscribe;
        public IDisposable Subscribe(IObserver<T> observer) { _subscribe(observer); return new Dummy(); }
        private sealed class Dummy : IDisposable { public void Dispose() { } }
    }

    [Fact]
    public async Task CollectResults_Success_ReturnsAllValues()
    {
        var observable = new TestObservable<int>(obs => { obs.OnNext(1); obs.OnNext(2); obs.OnCompleted(); });
        var res = await observable.CollectResults(TimeSpan.FromMilliseconds(200));
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(new[] { 1, 2 });
    }

    [Fact]
    public async Task CollectResults_Error_ReturnsFailure()
    {
        var observable = new TestObservable<int>(obs => { obs.OnError(new InvalidOperationException("bad")); });
        var res = await observable.CollectResults(TimeSpan.FromMilliseconds(200));
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldContain("Collection error: bad");
    }

    [Fact]
    public async Task CollectResults_Timeout_ReturnsTimeoutFailure()
    {
        var observable = new TestObservable<int>(obs => { /* never completes */ });
        var res = await observable.CollectResults(TimeSpan.FromMilliseconds(50));
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe("Collection timed out");
    }

    [Fact]
    public async Task CollectResults_Cancelled_ReturnsCancelled()
    {
        var observable = new TestObservable<int>(obs => { /* never completes */ });
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(10);
        var res = await observable.CollectResults(TimeSpan.FromSeconds(5), cts.Token);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultErrors.OperationCancelled);
    }
}




