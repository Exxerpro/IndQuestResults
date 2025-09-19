using IndQuestResults.Reactive;

namespace IndQuestResults.Tests.Unit.Reactive;

public class ReplaySubjectOnResultTests
{
    private sealed class TestObservable<T> : IObservable<T>
    {
        private readonly List<IObserver<T>> _observers = [];
        public IDisposable Subscribe(IObserver<T> observer)
        {
            _observers.Add(observer);
            return new Subscription(() => _observers.Remove(observer));
        }
        public void OnNext(T value) { foreach (var o in _observers.ToArray()) o.OnNext(value); }
        public void OnError(Exception ex) { foreach (var o in _observers.ToArray()) o.OnError(ex); }
        public void OnCompleted() { foreach (var o in _observers.ToArray()) o.OnCompleted(); }
        private sealed class Subscription : IDisposable { private readonly Action _d; public Subscription(Action d){_d=d;} public void Dispose()=>_d(); }
    }

    [Fact]
    public void OnResult_Subscription_Replays_Success_And_Failure()
    {
        var src = new TestObservable<int>();
        var bridge = src.CreateReplayBridge(bufferSize: 3);

        // push success and failure
        src.OnNext(1);
        src.OnError(new InvalidOperationException("bad"));

        var results = new List<Result<int>>();
        using var sub = bridge.Subscribe(onResult: r => results.Add(r), onCompleted: null);

        results.Count.ShouldBe(2);
        results[0].IsSuccess.ShouldBeTrue();
        results[1].IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void BufferOverflow_Replays_Only_Last_N()
    {
        var src = new TestObservable<int>();
        var bridge = src.CreateReplayBridge(bufferSize: 2);

        src.OnNext(1);
        src.OnNext(2);
        src.OnNext(3);
        src.OnNext(4);

        var received = new List<Result<int>>();
        using var sub = bridge.Subscribe(onResult: r => received.Add(r), onCompleted: null);

        received.Select(r => r.Value).ShouldBe([3, 4]);
    }
}

