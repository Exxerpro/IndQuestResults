namespace IndQuestResults.Tests.Mutation.Reactive;

public class ReplayResultSubjectMoreTests
{
    [Fact]
    public void Subscribe_ReplaysFailures_ThenSuccesses_InOrder()
    {
        var subject = new ReplayResultSubject<string>(bufferSize: 3);
        subject.OnNext(Result<string>.WithFailure("E1"));
        subject.OnNext(Result<string>.WithFailure("E2"));
        subject.OnNext(Result<string>.Success("A"));

        var received = new List<string>();
        subject.Subscribe(
            onSuccess: v => received.Add($"S:{v}"),
            onFailure: errs => received.Add($"F:{string.Join(",", errs)}"));

        received.ShouldBe(new[] { "F:E1", "F:E2", "S:A" });
    }
}


