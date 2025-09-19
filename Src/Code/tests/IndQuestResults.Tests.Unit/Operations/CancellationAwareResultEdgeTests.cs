using IndQuestResults.Operations;

namespace IndQuestResults.Tests.Unit.Operations;

public class CancellationAwareResultEdgeTests
{
    [Fact]
    public async Task WrapCancellationAware_Generic_NullOperation_ReturnsFailureMessage()
    {
        var res = await CancellationAwareResult.WrapCancellationAware<int>(null!, CancellationToken.None);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error!.ShouldContain("Operation was null");
    }

    [Fact]
    public async Task WrapResultOperation_NullOperation_ReturnsFailureMessage()
    {
        var res = await CancellationAwareResult.WrapResultOperation<int>(null!, CancellationToken.None);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error!.ShouldContain("Operation was null");
    }

    [Fact]
    public async Task WrapCancellationAware_NonGeneric_NullOperation_ReturnsFailureMessage()
    {
        var res = await CancellationAwareResult.WrapCancellationAware(null!, CancellationToken.None);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error!.ShouldContain("Operation was null");
    }

    [Fact]
    public async Task WrapWithTimeout_NullOperation_ReturnsFailureMessage()
    {
        var res = await CancellationAwareResult.WrapWithTimeout<int>(null!, TimeSpan.FromMilliseconds(10), CancellationToken.None);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error!.ShouldContain("Operation was null");
    }
}

