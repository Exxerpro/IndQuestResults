using IndQuestResults;
using IndQuestResults.Reactive;

namespace IndQuestResults.Tests.Unit.Reactive;

public class RouteResultsObserverBranchTests
{
    [Fact]
    public void OnNext_Success_RoutesToSuccessSubject()
    {
        var success = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failure = ResultSubscriptionsCore.CreateResultSubject<string>();
        var router = new RouteResultsObserver<int>(success, failure);

        var successes = new List<int>();
        success.Subscribe(successes.Add);

        router.OnNext(Result<int>.Success(10));

        successes.ShouldBe(new[] { 10 });
    }

    [Fact]
    public void OnNext_Failure_WithNullErrors_RoutesDefaultMessage()
    {
        var success = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failure = ResultSubscriptionsCore.CreateResultSubject<string>();
        var router = new RouteResultsObserver<int>(success, failure);

        var failures = new List<string>();
        failure.Subscribe(onSuccess: failures.Add);

        // Construct failure with null errors via generic constructor path
        var res = Result<int>.WithFailure(errors: null);
        router.OnNext(res);

        failures.ShouldHaveSingleItem();
        failures[0].ShouldBe(ResultConstants.DefaultErrorMessage);
    }

    [Fact]
    public void OnError_RoutesStreamErrorToFailureSubject()
    {
        var success = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failure = ResultSubscriptionsCore.CreateResultSubject<string>();
        var router = new RouteResultsObserver<int>(success, failure);

        var failures = new List<string>();
        failure.Subscribe(onSuccess: failures.Add);

        router.OnError(new Exception("E1"));

        failures.ShouldHaveSingleItem();
        failures[0].ShouldContain("Stream error: E1");
    }

    [Fact]
    public void OnCompleted_CompletesBothSubjects()
    {
        var success = ResultSubscriptionsCore.CreateResultSubject<int>();
        var failure = ResultSubscriptionsCore.CreateResultSubject<string>();
        var router = new RouteResultsObserver<int>(success, failure);

        var sCompleted = false;
        var fCompleted = false;
        success.Subscribe(_ => { }, onCompleted: () => sCompleted = true);
        failure.Subscribe(_ => { }, onCompleted: () => fCompleted = true);

        router.OnCompleted();

        sCompleted.ShouldBeTrue();
        fCompleted.ShouldBeTrue();
    }
}




