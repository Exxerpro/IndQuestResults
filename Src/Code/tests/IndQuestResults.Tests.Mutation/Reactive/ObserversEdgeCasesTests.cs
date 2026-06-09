namespace IndQuestResults.Tests.Mutation.Reactive;

public class ObserversEdgeCasesTests
{
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

    [Fact]
    public void SelectResult_SelectorThrows_ProducesFailure()
    {
        var src = new TestObservable<int>();
        var results = new List<Result<string>>();

        using var sub = src.SelectResult<int, string>(
            selector: _ => throw new InvalidOperationException("bad-selector"),
            onNext: r => results.Add(r));

        src.OnNext(1);

        results.Count.ShouldBe(1);
        results[0].IsFailure.ShouldBeTrue();
        results[0].Error!.ShouldContain("SelectResult error:");
        results[0].Error!.ShouldContain("bad-selector");
    }

    [Fact]
    public void ReplayBridgeObserver_OnError_ForwardsFailureIntoSubject()
    {
        var src = new TestObservable<int>();
        var bridge = src.CreateReplayBridge(bufferSize: 4);

        // Push error through source
        src.OnError(new InvalidOperationException("boom"));

        var failures = new List<string>();
        using var sub = bridge.Subscribe(onSuccess: _ => { }, onFailure: e => failures.AddRange(e));

        failures.ShouldNotBeEmpty();
        failures.First().ShouldContain("Observable error:");
        failures.First().ShouldContain("boom");
    }

    [Fact]
    public void ReplayResultSubject_ReplaysFailures_And_RespectsBuffer()
    {
        var subject = new ReplayResultSubject<int>(bufferSize: 2);

        subject.OnNext(Result<int>.WithFailure("e1"));
        subject.OnNext(Result<int>.WithFailure("e2"));
        subject.OnNext(Result<int>.WithFailure("e3"));

        var receivedErrors = new List<string>();
        using var sub = subject.Subscribe(onSuccess: _ => { }, onFailure: e => receivedErrors.AddRange(e));

        // Buffer size 2 → last two failures are replayed
        receivedErrors.ShouldNotBeEmpty();
        receivedErrors.Any(x => x.Contains("e2")).ShouldBeTrue();
        receivedErrors.Any(x => x.Contains("e3")).ShouldBeTrue();
    }
}
