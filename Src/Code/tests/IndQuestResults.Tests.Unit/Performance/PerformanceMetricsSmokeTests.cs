using IndQuestResults.Performance;

namespace IndQuestResults.Tests.Unit.Performance;

public class PerformanceMetricsSmokeTests
{
    private sealed class TestProcessor : IMetricsProcessor
    {
        public List<MetricEntry> Entries { get; } = new();

        public Task ProcessAsync(MetricEntry metric, CancellationToken cancellationToken)
        {
            // Simulate lightweight processing
            Entries.Add(metric);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ChannelMetricsCollector_ProcessesEntries_FireAndForget()
    {
        var processor = new TestProcessor();
        using var collector = new ChannelMetricsCollector(processor, capacity: 4);

        collector.RecordOperationMetrics("op1", TimeSpan.FromMilliseconds(2), isSuccess: true, isException: false);
        collector.RecordOperationMetrics("op2", TimeSpan.FromMilliseconds(3), isSuccess: false, isException: true, errorType: "InvalidOperationException");

        // Give background loop a moment
        await Task.Delay(10);

        processor.Entries.Count.ShouldBeGreaterThanOrEqualTo(2);
        processor.Entries.Any(e => e.OperationName == "op1" && e.IsSuccess && !e.IsException).ShouldBeTrue();
        processor.Entries.Any(e => e.OperationName == "op2" && !e.IsSuccess && e.IsException && e.ErrorType!.Contains("InvalidOperation")).ShouldBeTrue();
    }

    [Fact]
    public void ResultMetrics_TimedWithMetrics_RecordsSuccessAndFailure()
    {
        var processor = new TestProcessor();
        using var collector = new ChannelMetricsCollector(processor);
        ResultMetrics.SetGlobalCollector(collector);

        // Success path
        var res1 = ResultMetrics.TimedWithMetrics(() => Result<string>.Success("ok"), "spec.success");
        res1.IsSuccess.ShouldBeTrue();

        // Failure path via exception
        var res2 = ResultMetrics.TimedWithMetrics<string>(() => throw new InvalidOperationException("boom"), "spec.exception");
        res2.IsFailure.ShouldBeTrue();
        res2.Error!.ShouldContain("Operation failed");

        // Failure path via Result failure
        var res3 = ResultMetrics.TimedWithMetrics(() => Result<string>.WithFailure("bad"), "spec.failure");
        res3.IsFailure.ShouldBeTrue();

        // Allow dispatch
        Thread.Sleep(10);

        processor.Entries.Any(e => e.OperationName == "spec.success" && e.IsSuccess).ShouldBeTrue();
        processor.Entries.Any(e => e.OperationName == "spec.exception" && !e.IsSuccess && e.IsException && e.ErrorType!.Contains("InvalidOperation")).ShouldBeTrue();
        processor.Entries.Any(e => e.OperationName == "spec.failure" && !e.IsSuccess && !e.IsException && e.ErrorType == "ValidationFailure").ShouldBeTrue();

        ResultMetrics.SetGlobalCollector(null);
    }

    [Fact]
    public async Task MetricsScope_PrefixesOperationNames_ForSyncAndAsync()
    {
        var processor = new TestProcessor();
        using var collector = new ChannelMetricsCollector(processor);
        var scope = ResultMetrics.CreateScope("scopeX", collector);

        var r1 = scope.Timed(() => Result<int>.Success(1), "op");
        r1.IsSuccess.ShouldBeTrue();

        var r2 = await scope.TimedAsync(async () => { await Task.Delay(1); return Result<int>.Success(2); }, "aop");
        r2.IsSuccess.ShouldBeTrue();

        await Task.Delay(10);

        processor.Entries.Any(e => e.OperationName == "scopeX.op").ShouldBeTrue();
        processor.Entries.Any(e => e.OperationName == "scopeX.aop").ShouldBeTrue();
    }

    [Fact]
    public void MetricEntry_CanBeConstructed_AndExposesProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var entry = new MetricEntry("m", TimeSpan.FromMilliseconds(12), true, false, null, now);

        entry.OperationName.ShouldBe("m");
        entry.ElapsedMilliseconds.ShouldBeGreaterThan(0);
        entry.IsSuccess.ShouldBeTrue();
        entry.IsException.ShouldBeFalse();
        entry.ErrorType.ShouldBeNull();
        entry.Timestamp.ShouldBeInRange(now.AddSeconds(-1), now.AddSeconds(1));
    }
}
