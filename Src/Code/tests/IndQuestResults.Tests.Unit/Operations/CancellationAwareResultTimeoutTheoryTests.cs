using IndQuestResults.Operations;

namespace IndQuestResults.Tests.Unit.Operations;

public class CancellationAwareResultTimeoutTheoryTests
{
    // opDelayMs, timeoutMs, externalCancelMs (-1 = no external cancel), expect: 0=success,1=timeout,2=cancel
    public static TheoryData<int,int,int,int> WrapWithTimeout_Cases() => new()
    {
        { 20, 300, -1, 0 },  // completes before timeout (extra margin to avoid flakiness)
        { 200, 50, -1, 1 },  // times out before op completes
        { 200, 500, 10, 2 }, // external cancel wins
        { 500, 50, 10, 2 },  // external cancel vs timeout -> cancel (ensures cancel precedes timeout)
    };

    [Theory]
    [MemberData(nameof(WrapWithTimeout_Cases))]
    public async Task WrapWithTimeout_Matrix(int opDelayMs, int timeoutMs, int externalCancelMs, int expect)
    {
        using var cts = new CancellationTokenSource();
        if (externalCancelMs >= 0)
        {
            cts.CancelAfter(externalCancelMs);
        }

        var res = await CancellationAwareResult.WrapWithTimeout(
            async ct => { await Task.Delay(opDelayMs, ct); return 123; },
            TimeSpan.FromMilliseconds(timeoutMs),
            cts.Token);

        if (expect == 0)
        {
            res.IsSuccess.ShouldBeTrue();
            res.Value.ShouldBe(123);
        }
        else if (expect == 1)
        {
            res.IsFailure.ShouldBeTrue();
            res.Error.ShouldBe(ResultErrors.OperationTimedOut);
        }
        else
        {
            res.IsFailure.ShouldBeTrue();
            res.Error.ShouldBe(ResultErrors.OperationCancelled);
        }
    }
}
