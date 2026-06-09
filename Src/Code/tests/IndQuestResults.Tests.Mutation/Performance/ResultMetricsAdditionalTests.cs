namespace IndQuestResults.Tests.Mutation.Performance;

public class ResultMetricsAdditionalTests
{
    private sealed class TestCollector : IMetricsCollector
    {
        public List<MetricEntry> Entries { get; } = new();

        public void RecordOperationMetrics(string operationName, TimeSpan elapsed, bool isSuccess, bool isException, string? errorType = null)
        {
            Entries.Add(new MetricEntry(operationName, elapsed, isSuccess, isException, errorType, DateTimeOffset.UtcNow));
        }
    }

    [Fact]
    public void TimedWithMetrics_Success_RecordsSuccess()
    {
        var collector = new TestCollector();

        var res = ResultMetrics.TimedWithMetrics<int>(
            () => Result<int>.Success(42),
            "op.success",
            collector);

        res.IsSuccess.ShouldBeTrue();
        collector.Entries.Count.ShouldBe(1);
        collector.Entries[0].IsSuccess.ShouldBeTrue();
        collector.Entries[0].IsException.ShouldBeFalse();
        collector.Entries[0].ErrorType.ShouldBeNull();
    }

    [Fact]
    public void TimedWithMetrics_Exception_RecordsExceptionAndFailure()
    {
        var collector = new TestCollector();

        var res = ResultMetrics.TimedWithMetrics<int>(
            () => throw new InvalidOperationException("boom"),
            "op.exception",
            collector);

        res.IsFailure.ShouldBeTrue();
        collector.Entries.Count.ShouldBe(2); // one for exception path, one final record
        collector.Entries[0].IsException.ShouldBeTrue();
        collector.Entries[0].ErrorType.ShouldBe("InvalidOperationException");
        collector.Entries[1].IsSuccess.ShouldBeFalse();
    }
}


