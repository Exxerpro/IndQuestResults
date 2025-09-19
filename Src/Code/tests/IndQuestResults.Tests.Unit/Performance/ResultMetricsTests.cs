using IndQuestResults.Performance;

namespace IndQuestResults.Tests.Unit.Performance;

public class ResultMetricsTests
{
    private sealed class InMemoryProcessor : IMetricsProcessor
    {
        public List<MetricEntry> Entries { get; } = new();
        public Task ProcessAsync(MetricEntry metric, CancellationToken cancellationToken)
        {
            Entries.Add(metric);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void TimedWithMetrics_Success_And_Exception_RecordMetrics()
    {
        var proc = new InMemoryProcessor();
        using var collector = new ChannelMetricsCollector(proc, capacity: 8);

        // Success path
        var ok = ResultMetrics.TimedWithMetrics(
            () => Result<string>.Success("ok"),
            operationName: "spec.success",
            collector: collector);
        ok.IsSuccess.ShouldBeTrue();

        // Exception path
        var fail = ResultMetrics.TimedWithMetrics<string>(
            () => throw new InvalidOperationException("boom"),
            operationName: "spec.ex",
            collector: collector);
        fail.IsFailure.ShouldBeTrue();
        fail.Error!.ShouldContain("Operation failed:");

        // Give background processor a tick
        SpinWait.SpinUntil(() => proc.Entries.Count >= 2, 2000);

        proc.Entries.Any(e => e.OperationName == "spec.success" && e.IsSuccess && !e.IsException).ShouldBeTrue();
        proc.Entries.Any(e => e.OperationName == "spec.ex" && !e.IsSuccess && e.IsException && e.ErrorType == nameof(InvalidOperationException)).ShouldBeTrue();
    }

    [Fact]
    public async Task TimedWithMetricsAsync_Success_And_Cancel_RecordMetrics()
    {
        var proc = new InMemoryProcessor();
        using var collector = new ChannelMetricsCollector(proc);

        var ok = await ResultMetrics.TimedWithMetricsAsync(
            async () => { await Task.Delay(5); return Result<int>.Success(1); },
            operationName: "spec.async.success",
            collector: collector);
        ok.IsSuccess.ShouldBeTrue();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cancelled = await ResultMetrics.TimedWithMetricsAsync(
            async () => { await Task.Delay(50, cts.Token); return Result<int>.Success(2); },
            operationName: "spec.async.cancel",
            collector: collector,
            cancellationToken: cts.Token);
        cancelled.IsFailure.ShouldBeTrue();
        cancelled.Error.ShouldBe(ResultErrors.OperationCancelled);

        SpinWait.SpinUntil(() => proc.Entries.Count >= 2, 2000);

        proc.Entries.Any(e => e.OperationName == "spec.async.success" && e.IsSuccess && !e.IsException).ShouldBeTrue();
        proc.Entries.Any(e => e.OperationName == "spec.async.cancel" && !e.IsSuccess && e.IsException && e.ErrorType == "Cancelled").ShouldBeTrue();
    }

    [Fact]
    public void MetricsScope_Prefixes_Operation_Names()
    {
        var proc = new InMemoryProcessor();
        using var collector = new ChannelMetricsCollector(proc);
        var scope = ResultMetrics.CreateScope("scope", collector);

        var res = scope.Timed(() => Result<string>.Success("v"), "op");
        res.IsSuccess.ShouldBeTrue();

        SpinWait.SpinUntil(() => proc.Entries.Count >= 1, 200);
        proc.Entries[0].OperationName.ShouldBe("scope.op");
    }
}
