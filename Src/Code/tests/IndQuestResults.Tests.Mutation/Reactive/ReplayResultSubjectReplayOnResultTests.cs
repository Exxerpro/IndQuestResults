namespace IndQuestResults.Tests.Mutation.Reactive;

public class ReplayResultSubjectReplayOnResultTests
{
    [Fact]
    public void Subscribe_OnResult_ReplaysBuffered_SuccessAndFailure()
    {
        var subject = new ReplayResultSubject<int>(bufferSize: 2);
        subject.OnNext(Result<int>.Success(1));
        subject.OnNext(Result<int>.WithFailure("E"));

        var received = new List<Result<int>>();
        subject.Subscribe(onResult: received.Add, onCompleted: null);

        received.Count.ShouldBe(2);
        received[0].IsSuccess.ShouldBeTrue();
        received[1].IsFailure.ShouldBeTrue();
        received[1].Error.ShouldBe("E");
    }
}




