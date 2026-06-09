namespace IndQuestResults.Tests.Mutation.Reactive;

public class ReplayResultSubjectAdditionalTests
{
    [Fact]
    public void NewSubscriber_ReplaysBufferedResults_BeforeLive()
    {
        var subject = new ReplayResultSubject<int>(bufferSize: 2);

        subject.OnNext(Result<int>.Success(1));
        subject.OnNext(Result<int>.WithFailure("E1"));
        subject.OnNext(Result<int>.Success(2));

        var received = new List<string>();
        subject.Subscribe(
            onSuccess: v => received.Add($"S:{v}"),
            onFailure: errs => received.Add($"F:{string.Join(";", errs)}"));

        // Should replay last 2 buffered entries: E1 (failure), 2 (success)
        received.ShouldBe(new[] { "F:E1", "S:2" });

        subject.OnNext(Result<int>.Success(3));
        received.ShouldBe(new[] { "F:E1", "S:2", "S:3" });
    }
}


