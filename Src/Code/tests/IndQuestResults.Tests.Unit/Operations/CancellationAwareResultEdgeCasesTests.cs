namespace IndQuestResults.Tests.Unit.Operations;

public class CancellationAwareResultEdgeCasesTests
{
    [Fact]
    public async Task WrapCancellationAware_NullOperation_ReturnsFailure()
    {
        var res = await CancellationAwareResult.WrapCancellationAware<int>(null!);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldContain("Operation was null");
    }

    [Fact]
    public async Task WrapResultOperation_NullOperation_ReturnsFailure()
    {
        var res = await CancellationAwareResult.WrapResultOperation<int>(null!);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldContain("Operation was null");
    }

    [Fact]
    public async Task WrapCancellationAware_EarlyCancelledToken_ReturnsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var res = await CancellationAwareResult.WrapCancellationAware<int>(_ => Task.FromResult(1), cts.Token);
        res.IsFailure.ShouldBeTrue();
        res.IsCancelled().ShouldBeTrue();
    }

    [Fact]
    public async Task WrapWithTimeout_TimeoutVsExternalCancellation_AreDistinguished()
    {
        // Timeout case — op (1000ms) far exceeds timeout (100ms) so the timeout reliably fires
        // first even under CI load (900ms margin; small margins here flaked when the op completed
        // before a jittered short-timeout timer).
        var timeoutRes = await CancellationAwareResult.WrapWithTimeout(async token =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1000), token);
            return 1;
        }, TimeSpan.FromMilliseconds(100));
        timeoutRes.IsFailure.ShouldBeTrue();
        timeoutRes.Error.ShouldBe(ResultErrors.OperationTimedOut);

        // External cancellation case
        using var cts = new CancellationTokenSource();
        var externalCancelTask = CancellationAwareResult.WrapWithTimeout(async token =>
        {
            await Task.Delay(1000, token);
            return 2;
        }, TimeSpan.FromSeconds(5), cts.Token);
        cts.Cancel();
        var externalRes = await externalCancelTask;
        externalRes.IsFailure.ShouldBeTrue();
        externalRes.IsCancelled().ShouldBeTrue();
    }
}
