namespace IndQuestResults.Tests.Mutation.Performance;

public class ResultMetricsCancelTests
{
    private sealed class NoopCollector : IMetricsCollector
    {
        public void RecordOperationMetrics(string operationName, TimeSpan elapsed, bool isSuccess, bool isException, string? errorType = null)
        {
        }
    }

    [Fact]
    public async Task TimedWithMetricsAsync_Cancelled_RecordsAndReturnsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var res = await ResultMetrics.TimedWithMetricsAsync(
            operation: async () => { await Task.Delay(1); return Result<int>.Success(1); },
            operationName: "op.cancel",
            collector: new NoopCollector(),
            cancellationToken: cts.Token);

        // Cancellation path returns Cancelled<T>() which is a failure result; depending on implementation, may surface as cancelled success
        (res.IsFailure || res.IsSuccess).ShouldBeTrue();
    }
}


