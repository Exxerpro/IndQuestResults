namespace IndQuestResults.Tests.Unit.Reactive;

public class ReplayBridgeObserverBranchTests
{
    [Fact]
    public void Ctor_NullSubject_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new ReplayBridgeObserver<int>(null!));
    }

    [Fact]
    public void OnNext_ForwardsSuccessToSubject()
    {
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var received = new List<int>();
        subject.Subscribe(received.Add);

        var observer = new ReplayBridgeObserver<int>(subject);
        observer.OnNext(7);

        received.ShouldBe(new[] { 7 });
    }

    [Fact]
    public void OnError_ForwardsFailureMessageToSubject()
    {
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var errors = new List<string>();
        subject.Subscribe(onSuccess: _ => { }, onFailure: errs => errors.AddRange(errs));

        var observer = new ReplayBridgeObserver<int>(subject);
        observer.OnError(new InvalidOperationException("Boom"));

        errors.ShouldHaveSingleItem();
        errors[0].ShouldContain("Observable error: Boom");
    }

    [Fact]
    public void OnCompleted_CompletesSubject()
    {
        var subject = ResultSubscriptionsCore.CreateResultSubject<int>();
        var completed = false;
        subject.Subscribe(_ => { }, onCompleted: () => completed = true);

        var observer = new ReplayBridgeObserver<int>(subject);
        observer.OnCompleted();

        completed.ShouldBeTrue();
    }
}


