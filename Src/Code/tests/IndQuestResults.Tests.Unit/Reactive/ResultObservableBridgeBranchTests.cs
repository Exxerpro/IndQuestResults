namespace IndQuestResults.Tests.Unit.Reactive;

public class ResultObservableBridgeBranchTests
{
    private sealed class TestObservable<T> : IObservable<T>
    {
        private readonly Action<IObserver<T>> _subscribe;
        public TestObservable(Action<IObserver<T>> subscribe) => _subscribe = subscribe;
        public IDisposable Subscribe(IObserver<T> observer) { _subscribe(observer); return new Dummy(); }
        private sealed class Dummy : IDisposable { public void Dispose() { } }
    }

    [Fact]
    public void WhereSuccess_EmitsOnlySuccess()
    {
        var values = new List<int>();
        var obs = new TestObservable<int>(o =>
        {
            o.OnNext(1);
            o.OnNext(2);
            o.OnCompleted();
        });

        obs.WhereSuccess(
            selector: i => i % 2 == 0 ? Result<int>.Success(i) : Result<int>.WithFailure("odd"),
            onSuccess: values.Add,
            onCompleted: null);

        values.ShouldBe(new[] { 2 });
    }

    [Fact]
    public void HandleResults_RoutesSuccessAndFailure()
    {
        var successes = new List<int>();
        var failures = new List<string>();
        var obs = new TestObservable<int>(o =>
        {
            o.OnNext(1); // failure
            o.OnNext(2); // success
            o.OnCompleted();
        });

        obs.HandleResults(
            selector: i => i % 2 == 0 ? Result<int>.Success(i) : Result<int>.WithFailure("bad"),
            onSuccess: successes.Add,
            onFailure: errs => failures.AddRange(errs),
            onCompleted: null);

        successes.ShouldBe(new[] { 2 });
        failures.ShouldBe(new[] { "bad" });
    }

    [Fact]
    public void CreateReplayBridge_InvalidBuffer_Throws()
    {
        var obs = new TestObservable<int>(_ => { });
        Should.Throw<ArgumentException>(() => obs.CreateReplayBridge(bufferSize: 0));
        Should.Throw<ArgumentException>(() => obs.CreateReplayBridge(bufferSize: -1));
    }
}




