namespace IndQuestResults.Tests.Unit.Performance;

public class TimedResultToStringBranchTests
{
    [Fact]
    public void TimedResult_Generic_ToString_ShowsSuccessAndFailure()
    {
        var success = ResultTiming.Timed(() => Result<int>.Success(1));
        var failure = ResultTiming.Timed(() => Result<int>.WithFailure("x"));

        success.ToString().ShouldContain("Success");
        failure.ToString().ShouldContain("Failure");
    }

    [Fact]
    public void TimedResult_NonGeneric_ToString_ShowsSuccessAndFailure()
    {
        var success = ResultTiming.Timed(() => Result.Success());
        var failure = ResultTiming.Timed(() => Result.WithFailure("x"));

        success.ToString().ShouldContain("Success");
        failure.ToString().ShouldContain("Failure");
    }
}




