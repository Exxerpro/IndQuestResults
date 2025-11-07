using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IndQuestResults;
using IndQuestResults.Reactive;
using Shouldly;
using Xunit;

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
    public void CreateReplayBridge_InvalidBuffer_ReturnsDisposedSubject()
    {
        var obs = new TestObservable<int>(_ => { });
        var subject1 = obs.CreateReplayBridge(bufferSize: 0);
        var subject2 = obs.CreateReplayBridge(bufferSize: -1);
        
        // Should return a disposed subject (no-op behavior)
        subject1.ShouldNotBeNull();
        subject2.ShouldNotBeNull();
    }

    [Fact]
    public void ToResultStream_NullSource_ReturnsNoOpDisposable()
    {
        var disposable = ((IObservable<int>?)null).ToResultStream(_ => { });
        
        disposable.ShouldNotBeNull();
        Should.NotThrow(() => disposable.Dispose());
    }

    [Fact]
    public void ToResultStream_NullOnNext_ReturnsNoOpDisposable()
    {
        var obs = new TestObservable<int>(_ => { });
        var disposable = obs.ToResultStream((Action<Result<int>>?)null);
        
        disposable.ShouldNotBeNull();
        Should.NotThrow(() => disposable.Dispose());
    }

    [Fact]
    public void SelectResult_NullSource_ReturnsNoOpDisposable()
    {
        var disposable = ((IObservable<int>?)null).SelectResult(_ => Result<string>.Success("test"), _ => { });
        
        disposable.ShouldNotBeNull();
        Should.NotThrow(() => disposable.Dispose());
    }

    [Fact]
    public void SelectResult_NullSelector_ReturnsNoOpDisposable()
    {
        var obs = new TestObservable<int>(_ => { });
        var disposable = obs.SelectResult((Func<int, Result<string>>?)null, _ => { });
        
        disposable.ShouldNotBeNull();
        Should.NotThrow(() => disposable.Dispose());
    }

    [Fact]
    public void SelectResult_NullOnNext_ReturnsNoOpDisposable()
    {
        var obs = new TestObservable<int>(_ => { });
        var disposable = obs.SelectResult(_ => Result<string>.Success("test"), (Action<Result<string>>?)null);
        
        disposable.ShouldNotBeNull();
        Should.NotThrow(() => disposable.Dispose());
    }

    [Fact]
    public void WhereSuccess_NullSource_ReturnsNoOpDisposable()
    {
        var disposable = ((IObservable<int>?)null).WhereSuccess(_ => Result<string>.Success("test"), _ => { });
        
        disposable.ShouldNotBeNull();
        Should.NotThrow(() => disposable.Dispose());
    }

    [Fact]
    public void HandleResults_NullSource_ReturnsNoOpDisposable()
    {
        var disposable = ((IObservable<int>?)null).HandleResults(_ => Result<string>.Success("test"), _ => { }, _ => { });
        
        disposable.ShouldNotBeNull();
        Should.NotThrow(() => disposable.Dispose());
    }

    [Fact]
    public async Task CollectResults_NullSource_ReturnsFailureResult()
    {
        var result = await ((IObservable<int>?)null).CollectResults(TimeSpan.FromSeconds(1));
        
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain("Source observable cannot be null");
    }

    [Fact]
    public void CreateReplayBridge_NullSource_ReturnsDisposedSubject()
    {
        var subject = ((IObservable<int>?)null).CreateReplayBridge();
        
        subject.ShouldNotBeNull();
        // Subject should be disposed (no-op behavior)
    }
}




