namespace IndQuestResults.Tests.Unit.Operations;

public class CancellationAwareResultExceptionTests
{
    [Fact]
    public async Task WrapCancellationAware_Generic_OperationThrows_ReturnsFailure()
    {
        var res = await CancellationAwareResult.WrapCancellationAware<int>(async _ =>
        {
            await Task.Delay(1, CancellationToken.None);
            throw new InvalidOperationException("oops");
        });
        res.IsFailure.ShouldBeTrue();
        res.Error!.ShouldContain("Operation failed:");
    }

    [Fact]
    public async Task WrapResultOperation_OperationThrows_ReturnsFailure()
    {
        var res = await CancellationAwareResult.WrapResultOperation<int>(async _ =>
        {
            await Task.Delay(1, CancellationToken.None);
            throw new InvalidOperationException("boom");
        });
        res.IsFailure.ShouldBeTrue();
        res.Error!.ShouldContain("Operation failed:");
    }

    [Fact]
    public async Task WrapCancellationAware_NonGeneric_Throws_ReturnsFailure()
    {
        var res = await CancellationAwareResult.WrapCancellationAware(async _ =>
        {
            await Task.Delay(1, CancellationToken.None);
            throw new InvalidOperationException("err");
        });
        res.IsFailure.ShouldBeTrue();
        res.Error!.ShouldContain("Operation failed:");
    }
}
