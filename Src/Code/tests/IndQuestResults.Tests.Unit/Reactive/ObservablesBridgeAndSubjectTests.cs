namespace IndQuestResults.Tests.Unit.Reactive;

public class ObservablesBridgeAndSubjectTests
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

    private sealed class FlagDisposable : IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
    }

    [Fact]
    public void ResultSubject_Publish_Subscribe_Dispose_Basics()
    {
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var success = 0;
        var failures = new List<string>();

        subject.SubscriberCount.ShouldBe(0);
        using var sub = subject.Subscribe(onSuccess: v => success = v, onFailure: failures.AddRange);
        subject.SubscriberCount.ShouldBe(1);

        subject.OnNext(Result<int>.Success(42));
        success.ShouldBe(42);

        subject.OnNext(Result<int>.WithFailure("oops"));
        failures.ShouldContain("oops");

        sub.Dispose();
        subject.SubscriberCount.ShouldBe(0);

        subject.OnCompleted();
        subject.IsCompleted.ShouldBeTrue();

        // After completion, OnNext should be ignored
        success = 0;
        subject.OnNext(Result<int>.Success(7));
        success.ShouldBe(0);
    }

    [Fact]
    public void SubscriptionManager_Add_Remove_Clear_Dispose()
    {
        using var mgr = ResultSubscriptionsCore.CreateSubscriptionManager();
        mgr.Count.ShouldBe(0);

        var d1 = new FlagDisposable();
        var d2 = new FlagDisposable();
        mgr.Add(d1);
        mgr.Add(d2);
        mgr.Count.ShouldBe(2);

        mgr.Remove(d1).ShouldBeTrue();
        d1.Disposed.ShouldBeTrue();

        mgr.Clear();
        d2.Disposed.ShouldBeTrue();
        mgr.Count.ShouldBe(0);
    }

    [Fact]
    public void ToResultStream_Converts_Values_And_Errors()
    {
        var obs = new TestObservable<int>();
        var received = new List<Result<int>>();

        using var sub = obs.ToResultStream(onNext: r => received.Add(r));
        obs.OnNext(1);
        obs.OnError(new InvalidOperationException("bad"));
        obs.OnCompleted();

        received.Count.ShouldBe(2);
        received[0].IsSuccess.ShouldBeTrue();
        received[0].Value.ShouldBe(1);
        received[1].IsFailure.ShouldBeTrue();
        received[1].Error!.ShouldContain("Observable error:");
    }

    [Fact]
    public void SelectResult_And_WhereSuccess_Handle_Correctly()
    {
        var obs = new TestObservable<string>();
        var results = new List<Result<int>>();
        var successes = new List<int>();

        using var s1 = obs.SelectResult(
            selector: s => int.TryParse(s, out var v) ? Result<int>.Success(v) : Result<int>.WithFailure("parse"),
            onNext: results.Add);

        using var s2 = obs.WhereSuccess(
            selector: s => int.TryParse(s, out var v) ? Result<int>.Success(v) : Result<int>.WithFailure("parse"),
            onSuccess: successes.Add);

        obs.OnNext("10");
        obs.OnNext("x");
        obs.OnCompleted();

        results.Count.ShouldBe(2);
        results[0].IsSuccess.ShouldBeTrue();
        results[1].IsFailure.ShouldBeTrue();
        successes.ShouldBe([10]);
    }

    [Fact]
    public void HandleResults_Routes_Success_And_Failure()
    {
        var obs = new TestObservable<string>();
        var succ = new List<int>();
        var errs = new List<string[]>();

        using var sub = obs.HandleResults(
            selector: s => int.TryParse(s, out var v) ? Result<int>.Success(v) : Result<int>.WithFailure("bad"),
            onSuccess: succ.Add,
            onFailure: e => errs.Add(e.ToArray()));

        obs.OnNext("1");
        obs.OnNext("bad");
        obs.OnCompleted();

        succ.ShouldBe([1]);
        errs.Count.ShouldBe(1);
        errs[0].ShouldContain("bad");
    }

    [Fact]
    public async Task CollectResults_Success_Error_Timeout_Cancel()
    {
        var obs = new TestObservable<int>();

        // Success collection
        var tSuccess = obs.CollectResults(TimeSpan.FromMilliseconds(200));
        obs.OnNext(1);
        obs.OnNext(2);
        obs.OnCompleted();
        var success = await tSuccess;
        success.IsSuccess.ShouldBeTrue();
        success.Value!.ShouldBe([1, 2]);

        // Error collection
        var obs2 = new TestObservable<int>();
        var tError = obs2.CollectResults(TimeSpan.FromMilliseconds(200));
        obs2.OnError(new InvalidOperationException("oops"));
        var error = await tError;
        error.IsFailure.ShouldBeTrue();
        error.Error!.ShouldContain("Collection error:");

        // Timeout case (no completion)
        var obs3 = new TestObservable<int>();
        var timeoutRes = await obs3.CollectResults(TimeSpan.FromMilliseconds(50));
        timeoutRes.IsFailure.ShouldBeTrue();
        timeoutRes.Error.ShouldNotBeNull();
        timeoutRes.Error!.ShouldContain("timed", Case.Insensitive);

        // Cancellation case
        var obs4 = new TestObservable<int>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancelled = await obs4.CollectResults(TimeSpan.FromSeconds(5), cts.Token);
        cancelled.IsFailure.ShouldBeTrue();
        cancelled.Error.ShouldBe(ResultErrors.OperationCancelled);
    }

    [Fact]
    public void CreateReplayBridge_Replays_Buffer_To_Late_Subscribers()
    {
        var src = new TestObservable<int>();
        var bridge = src.CreateReplayBridge(bufferSize: 2);

        // push 3 values; buffer keeps last 2
        src.OnNext(1);
        src.OnNext(2);
        src.OnNext(3);

        var received1 = new List<int>();
        var received2 = new List<int>();
        using var s1 = bridge.Subscribe(onSuccess: received1.Add);
        using var s2 = bridge.Subscribe(onSuccess: received2.Add);

        received1.ShouldBe([2, 3]);
        received2.ShouldBe([2, 3]);

        // new value should propagate
        src.OnNext(4);
        received1.ShouldBe([2, 3, 4]);
        received2.ShouldBe([2, 3, 4]);

        src.OnCompleted();
    }

    [Fact]
    public void CreateReplayBridge_BufferSize_Zero_Throws()
    {
        var src = new TestObservable<int>();
        Should.Throw<ArgumentException>(() => src.CreateReplayBridge(bufferSize: 0));
    }

    [Fact]
    public void CreateReplayBridge_BufferSize_Negative_Throws()
    {
        var src = new TestObservable<int>();
        Should.Throw<ArgumentException>(() => src.CreateReplayBridge(bufferSize: -1));
    }
}







