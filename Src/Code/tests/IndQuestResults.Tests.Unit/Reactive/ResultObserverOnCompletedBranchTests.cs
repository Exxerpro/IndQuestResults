namespace IndQuestResults.Tests.Unit.Reactive;

public class ResultObserverOnCompletedBranchTests
{
    [Fact]
    public void ResultObserver_OnCompleted_InvokesCallback()
    {
        var completed = false;
        var observer = new ResultObserver<int>(onSuccess: _ => { }, onFailure: null, onCompleted: () => completed = true);

        observer.OnCompleted();

        completed.ShouldBeTrue();
    }
}


