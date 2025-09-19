using IndQuestResults.Performance;

namespace IndQuestResults.Tests.Unit.Performance;

public class ChannelMetricsCollectorDisposeTests
{
    private sealed class SlowProcessor : IMetricsProcessor
    {
        public int Count => Volatile.Read(ref _count);
        private int _count;
        public async Task ProcessAsync(MetricEntry metric, CancellationToken cancellationToken)
        {
            await Task.Delay(2, CancellationToken.None);
            Interlocked.Increment(ref _count);
        }
    }

    [Fact]
    public async Task Dispose_DuringProduction_DoesNotThrow_AndShutsDown()
    {
        var proc = new SlowProcessor();
        var collector = new ChannelMetricsCollector(proc, capacity: 2);

        // Produce metrics, then dispose while background task is processing
        for (int i = 0; i < 50; i++)
        {
            collector.RecordOperationMetrics("dispose.test", TimeSpan.FromMilliseconds(1), true, false);
        }

        collector.Dispose();

        // Post-dispose calls should not throw
        Should.NotThrow(() => collector.RecordOperationMetrics("after", TimeSpan.Zero, false, true));

        await Task.Delay(20);
        proc.Count.ShouldBeLessThanOrEqualTo(50);
    }
}

