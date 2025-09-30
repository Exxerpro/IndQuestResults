namespace IndQuestResults.Tests.Unit.Reactive;

public class ReactiveCancellationAndBridgeTests
{
    private sealed class TestObservable<T> : IObservable<T>
    {
        private readonly IEnumerable<object> _events; // T values or Exception
        public TestObservable(IEnumerable<object> events) => _events = events;
        public IDisposable Subscribe(IObserver<T> observer)
        {
            foreach (var e in _events)
            {
                if (e is Exception ex) observer.OnError(ex);
                else observer.OnNext((T)e);
            }
            observer.OnCompleted();
            return new Dummy();
        }
        private sealed class Dummy : IDisposable { public void Dispose() { } }
    }

    [Fact]
    public void ToResultStream_Converts_OnError_ToFailureAndCompletes()
    {
        var src = new TestObservable<int>(new object[] { 1, new InvalidOperationException("boom"), 2 });
        var results = new List<Result<int>>();
        var completed = false;

        src.ToResultStream(results.Add, () => completed = true);

        results.Count.ShouldBe(3);
        results[0].IsSuccess.ShouldBeTrue();
        results[1].IsFailure.ShouldBeTrue();
        results[1].Error.ShouldContain("Observable error");
        results[2].IsSuccess.ShouldBeTrue();
        completed.ShouldBeTrue();
    }

    [Fact]
    public void SelectResult_SelectorException_ProducesFailure()
    {
        var src = new TestObservable<int>(new object[] { 1 });
        var results = new List<Result<string>>();
        src.SelectResult<int, string>(
            _ => throw new InvalidOperationException("bad"),
            results.Add);
        results.Single().IsFailure.ShouldBeTrue();
        results.Single().Error.ShouldContain("SelectResult error");
    }

    [Fact]
    public void CreateReplayBridge_ReplaysBufferedResultsToNewSubscriber()
    {
        var src = new TestObservable<int>(new object[] { 1, 2, 3 });
        var bridge = src.CreateReplayBridge(bufferSize: 2);

        // Push some events through the bridge subject
        bridge.OnNext(Result<int>.Success(10));
        bridge.OnNext(Result<int>.WithFailure("err"));

        var successes = new List<int>();
        var failures = new List<string>();
        using var sub = bridge.Subscribe(s => successes.Add(s), f => failures.AddRange(f));

        successes.ShouldContain(10);
        failures.ShouldContain("err");
    }
}

