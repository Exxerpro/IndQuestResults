namespace IndQuestResults.Tests.Mutation.Operations;

public class CancellationAwareResultTimeoutTheoryTests
{
    // opDelayMs, timeoutMs, externalCancelMs (-1 = no external cancel), expect: 0=success,1=timeout,2=cancel
    // Margins between competing events are deliberately large (>=500ms) so CI timer-scheduling
    // jitter cannot flip which event fires first. Each case still completes as soon as its winning
    // event fires (op done / timeout / cancel), so the suite stays fast despite the large delays.
    public static TheoryData<int,int,int,int> WrapWithTimeout_Cases() => new()
    {
        { 50, 1000, -1, 0 },    // op (50ms) completes well before timeout (1000ms) -> success
        { 1000, 100, -1, 1 },   // op (1000ms) far exceeds timeout (100ms) -> timeout
        { 1000, 1000, 50, 2 },  // external cancel (50ms) beats op and timeout (both 1000ms) -> cancel
        { 1000, 600, 50, 2 },   // external cancel (50ms) reliably precedes timeout (600ms) -> cancel
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
