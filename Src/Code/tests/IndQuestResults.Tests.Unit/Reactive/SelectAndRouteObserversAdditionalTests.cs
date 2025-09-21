using IndQuestResults;
using IndQuestResults.Reactive;

namespace IndQuestResults.Tests.Unit.Reactive;

public class SelectAndRouteObserversAdditionalTests
{
    [Fact]
    public void SelectResultObserver_TransformsValues_AndForwards()
    {
        var forwarded = new List<Result<string>>();
        var obs = new SelectResultObserver<int, string>(
            selector: i => i % 2 == 0 ? Result<string>.Success($"E{i}") : Result<string>.WithFailure("odd"),
            onNext: r => forwarded.Add(r),
            onCompleted: null);

        obs.OnNext(2);
        obs.OnNext(3);

        forwarded.Count.ShouldBe(2);
        forwarded[0].IsSuccess.ShouldBeTrue();
        forwarded[0].Value.ShouldBe("E2");
        forwarded[1].IsFailure.ShouldBeTrue();
        forwarded[1].Error.ShouldBe("odd");
    }

    [Fact]
    public void RouteResultsObserver_RoutesSuccessAndFailures()
    {
        var successSubject = new ResultSubject<int>();
        var failureSubject = new ResultSubject<string>();
        var routedSuccess = new List<int>();
        var routedFailure = new List<string>();

        successSubject.Subscribe(v => routedSuccess.Add(v));
        failureSubject.Subscribe(v => routedFailure.Add(v));

        var router = new RouteResultsObserver<int>(successSubject, failureSubject);

        router.OnNext(Result<int>.Success(7));
        router.OnNext(Result<int>.WithFailure("errA"));
        router.OnNext(Result<int>.WithFailure(new[] { "errB", "errC" }));

        routedSuccess.ShouldBe(new[] { 7 });
        routedFailure.ShouldBe(new[] { "errA", "errB, errC" });
    }
}


