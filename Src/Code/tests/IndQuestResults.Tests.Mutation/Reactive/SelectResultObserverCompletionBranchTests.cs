namespace IndQuestResults.Tests.Mutation.Reactive;

public class SelectResultObserverCompletionBranchTests
{
    [Fact]
    public void SelectResultObserver_OnCompleted_InvokesCallback()
    {
        var completed = false;
        var observer = new SelectResultObserver<int, string>(i => Result<string>.Success(i.ToString()), _ => { }, () => completed = true);

        observer.OnCompleted();

        completed.ShouldBeTrue();
    }
}




