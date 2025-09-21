using IndQuestResults.Performance;

namespace IndQuestResults.Tests.Unit.Performance;

public class ResultTimingCancellationTests
{
    [Fact]
    public async Task TimedAsync_CancelledToken_ReturnsCancelledResult()
    {
        using var cts = new CancellationTokenSource();
        var task = ResultTiming.TimedAsync(
            async () =>
            {
                await Task.Delay(1000, cts.Token);
                return Result<int>.Success(1);
            },
            cts.Token);
        cts.Cancel();
        var timed = await task;
        timed.IsFailure.ShouldBeTrue();
        timed.Result.IsCancelled().ShouldBeTrue();
    }

    [Fact]
    public void Timed_Exception_IsCapturedAsFailure()
    {
        var timed = ResultTiming.Timed<int>(() => throw new InvalidOperationException("oops"));
        timed.IsFailure.ShouldBeTrue();
        timed.Result.Error.ShouldContain("Operation failed");
        timed.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(0);
    }
}

