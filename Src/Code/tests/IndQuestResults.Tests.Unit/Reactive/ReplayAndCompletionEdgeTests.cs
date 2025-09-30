namespace IndQuestResults.Tests.Unit.Reactive;

public class ReplayAndCompletionEdgeTests
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
    public void CreateReplayBridge_Replays_Failure_To_Late_Subscribers()
    {
        var src = new TestObservable<int>();
        var bridge = src.CreateReplayBridge(bufferSize: 2);

        // Push a failure by raising observable error
        src.OnError(new InvalidOperationException("bad"));

        List<string>? failure1 = null;
        using var s = bridge.Subscribe(onSuccess: _ => { }, onFailure: e => failure1 = e.ToList());

        failure1.ShouldNotBeNull();
        failure1!.First().ShouldContain("Observable error:");
    }

    [Fact]
    public void CreateReplayBridge_OnCompleted_Notifies_All_Subscribers()
    {
        var src = new TestObservable<int>();
        var bridge = src.CreateReplayBridge(bufferSize: 1);

        var completed1 = false; var completed2 = false;
        using var a = bridge.Subscribe(onSuccess: _ => { }, onFailure: null, onCompleted: () => completed1 = true);
        using var b = bridge.Subscribe(onSuccess: _ => { }, onFailure: null, onCompleted: () => completed2 = true);

        src.OnCompleted();

        completed1.ShouldBeTrue();
        completed2.ShouldBeTrue();
        bridge.IsCompleted.ShouldBeTrue();
    }
}

