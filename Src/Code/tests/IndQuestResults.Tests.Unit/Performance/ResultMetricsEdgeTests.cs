namespace IndQuestResults.Tests.Unit.Performance;

public class ResultMetricsEdgeTests
{
    private sealed class NoopCollector : IMetricsCollector
    {
        public void RecordOperationMetrics(string operationName, TimeSpan elapsed, bool isSuccess, bool isException, string? errorType = null)
        {
        }
    }

    [Fact]
    public void TimedWithMetrics_NoCollector_Configured_ReturnsOperationResult()
    {
        // No global collector set and no explicit collector passed
        var res = ResultMetrics.TimedWithMetrics(() => Result<string>.Success("ok"), "op");
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe("ok");
    }

    [Fact]
    public void TimedWithMetrics_Exception_ReturnsFailure_AndDoesNotThrow()
    {
        var res = ResultMetrics.TimedWithMetrics<string>(() => throw new InvalidOperationException("x"), "op", new NoopCollector());
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldContain("Operation failed");
    }
}


