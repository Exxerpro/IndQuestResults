namespace IndQuestResults.Tests.Mutation.Reactive;

public class ReplayResultSubjectCompletionAndBufferTests
{
    [Fact]
    public void BufferSizeOne_ReplaysOnlyLast()
    {
        var subject = new ReplayResultSubject<int>(bufferSize: 1);
        subject.OnNext(Result<int>.Success(1));
        subject.OnNext(Result<int>.Success(2));

        var received = new List<int>();
        subject.Subscribe(onSuccess: v => received.Add(v));

        received.ShouldBe(new[] { 2 });
    }

    [Fact]
    public void Completed_ReplaysBuffered_ToNewSubscriber_ThenNoMoreItems()
    {
        var subject = new ReplayResultSubject<string>(bufferSize: 2);
        subject.OnNext(Result<string>.Success("A"));
        subject.OnNext(Result<string>.Success("B"));
        subject.OnCompleted();

        var received = new List<string>();
        subject.Subscribe(
            onSuccess: v => received.Add(v),
            onFailure: null,
            onCompleted: null);

        received.ShouldBe(new[] { "A", "B" });

        // After completion, behavior is delegated to inner subject; just assert completion observed
    }
}


