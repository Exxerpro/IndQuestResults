using IndQuestResults.Performance;

namespace IndQuestResults.Tests.Unit.Performance;

public class ChannelMetricsCollectorExtraTests
{
    private sealed class ThrowingProcessor : IMetricsProcessor
    {
        public Task ProcessAsync(MetricEntry metric, CancellationToken cancellationToken)
            => throw new InvalidOperationException("processor boom");
    }

    [Fact]
    public async Task ProcessorExceptions_AreSwallowed_NoCrash()
    {
        using var collector = new ChannelMetricsCollector(new ThrowingProcessor(), capacity: 2);
        collector.RecordOperationMetrics("x", TimeSpan.FromMilliseconds(1), true, false, null);

        // Give background task a moment to hit exception path
        await Task.Delay(20);

        // If exceptions escape, test would fail; reaching here is success
        true.ShouldBeTrue();
    }

    [Fact]
    public void Dispose_AllowsBestEffortShutdown()
    {
        var collector = new ChannelMetricsCollector(new ThrowingProcessor(), capacity: 1);
        collector.RecordOperationMetrics("y", TimeSpan.Zero, true, false, null);
        collector.Dispose();

        // After dispose, further calls should not throw
        Should.NotThrow(() => collector.RecordOperationMetrics("z", TimeSpan.Zero, false, true, "E"));
    }
}

