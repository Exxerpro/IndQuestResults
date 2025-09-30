namespace IndQuestResults.Tests.Unit.Reactive;

public class SelectAndResultObserverBranchTests
{
    [Fact]
    public void SelectResultObserver_OnError_EmitsObservableError()
    {
        var received = new List<Result<string>>();
        var observer = new SelectResultObserver<int, string>(i => Result<string>.Success(i.ToString()), received.Add, null);

        observer.OnError(new InvalidOperationException("broken"));

        received.ShouldHaveSingleItem();
        received[0].IsFailure.ShouldBeTrue();
        received[0].Error.ShouldContain("Observable error: broken");
    }

    [Fact]
    public void ResultObserver_OnResult_OnError_And_OnCompleted_Branches()
    {
        var results = new List<Result<int>>();
        var observer1 = new ResultObserver<int>(onResult: results.Add, onCompleted: null);

        observer1.OnError(new Exception("oops"));
        observer1.OnCompleted();

        results.ShouldHaveSingleItem();
        results[0].IsFailure.ShouldBeTrue();
        results[0].Error.ShouldContain("Stream error: oops");

        var successes = new List<int>();
        var failures = new List<string>();
        var completed = false;
        var observer2 = new ResultObserver<int>(onSuccess: successes.Add, onFailure: errs => failures.AddRange(errs), onCompleted: () => completed = true);

        observer2.OnNext(Result<int>.Success(1));
        observer2.OnError(new Exception("e2"));
        observer2.OnCompleted();

        successes.ShouldBe(new[] { 1 });
        failures.ShouldHaveSingleItem();
        failures[0].ShouldContain("Stream error: e2");
        completed.ShouldBeTrue();
    }
}




