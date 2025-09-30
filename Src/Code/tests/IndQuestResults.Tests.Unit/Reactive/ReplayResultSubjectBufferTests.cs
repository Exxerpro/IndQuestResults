namespace IndQuestResults.Tests.Unit.Reactive;

public class ReplayResultSubjectBufferTests
{
    [Fact]
    public void ReplayBuffer_RollsOver_AndReplaysLastN()
    {
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var bridge = new PrivateReplayBridge<int>(bufferSize: 2);

        // Feed some results
        bridge.OnNext(Result<int>.Success(1));
        bridge.OnNext(Result<int>.Success(2));
        bridge.OnNext(Result<int>.Success(3)); // should evict 1

        var replayed = new List<int>();
        using var sub = bridge.Subscribe(s => replayed.Add(s));

        replayed.ShouldBe(new[] { 2, 3 });
    }

    [Fact]
    public void ReplaySubject_MultipleSubscribers_Work()
    {
        var bridge = new PrivateReplayBridge<int>(bufferSize: 1);
        bridge.OnNext(Result<int>.Success(5));

        var s1 = new List<int>();
        var s2 = new List<int>();
        using var d1 = bridge.Subscribe(v => s1.Add(v));
        using var d2 = bridge.Subscribe(v => s2.Add(v));

        bridge.OnNext(Result<int>.Success(6));

        s1.ShouldBe(new[] { 5, 6 });
        s2.ShouldBe(new[] { 5, 6 });
    }

    // Expose internal ReplayResultSubject<T> via a minimal wrapper for tests
    private sealed class PrivateReplayBridge<T> : IResultSubject<T>
    {
        private readonly IResultSubject<T> _inner;
        public PrivateReplayBridge(int bufferSize) => _inner = new PrivateFactory<T>().CreateReplay(bufferSize);
        public int SubscriberCount => (_inner as dynamic).SubscriberCount;
        public bool IsCompleted => (_inner as dynamic).IsCompleted;
        public void OnNext(Result<T> result) => _inner.OnNext(result);
        public void OnError(Exception error) => _inner.OnError(error);
        public void OnCompleted() => _inner.OnCompleted();
        public IDisposable Subscribe(Action<T> onSuccess, Action<IEnumerable<string>>? onFailure = null, Action? onCompleted = null) => _inner.Subscribe(onSuccess, onFailure, onCompleted);
        public IDisposable Subscribe(Action<Result<T>> onResult, Action? onCompleted = null) => _inner.Subscribe(onResult, onCompleted);
        public void Dispose() => (_inner as IDisposable).Dispose();
    }

    private sealed class PrivateFactory<T>
    {
        public IResultSubject<T> CreateReplay(int buffer) => CreateReplayBridgeInternal(buffer);

        private static IResultSubject<T> CreateReplayBridgeInternal(int bufferSize)
        {
            // Leverage public API by piping through an observable
            var obs = new LocalObservable<T>();
            return IndQuestResults.Reactive.ResultObservableBridge.CreateReplayBridge(obs, bufferSize);
        }

        private sealed class LocalObservable<TObs> : IObservable<TObs>
        {
            public IDisposable Subscribe(IObserver<TObs> observer) => new Dummy();
            private sealed class Dummy : IDisposable { public void Dispose() { } }
        }
    }
}

