namespace IndQuestResults.Tests.Mutation.Performance;

public class ChannelMetricsCollectorCapacityTests
{
    private sealed class CountingProcessor : IMetricsProcessor
    {
        public int Count => Volatile.Read(ref _count);
        private int _count;
        public Task ProcessAsync(MetricEntry metric, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _count);
            return Task.CompletedTask;
        }
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(4, 250)]
    public async Task BoundedCapacity_Overflow_DropsWithoutBlocking(int capacity, int produce)
    {
        var proc = new CountingProcessor();
        using var collector = new ChannelMetricsCollector(proc, capacity);

        // Fire a burst of metrics; TryWrite should avoid blocking and may drop
        for (int i = 0; i < produce; i++)
        {
            collector.RecordOperationMetrics("cap.test", TimeSpan.FromMilliseconds(i % 3), isSuccess: i % 2 == 0, isException: false);
        }

        // Allow background loop to process a bit
        await Task.Delay(50);
        // Push one more to ensure at least one processes even under heavy overflow
        collector.RecordOperationMetrics("cap.test", TimeSpan.Zero, true, false);
        await Task.Delay(100);

        // We shouldn't process more than produced, and often less due to drops
        proc.Count.ShouldBeLessThanOrEqualTo(produce + 1);
        proc.Count.ShouldBeGreaterThanOrEqualTo(1);
    }
}
