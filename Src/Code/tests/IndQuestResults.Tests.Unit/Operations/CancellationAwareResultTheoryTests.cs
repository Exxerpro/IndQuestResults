namespace IndQuestResults.Tests.Unit.Operations;

public class CancellationAwareResultTheoryTests
{
    // wrapperType: 0=generic(T), 1=result(T), 2=non-generic
    // behavior: 0=success, 1=throw-cancel, 2=throw-other
    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(0, 1, false)]
    [InlineData(0, 2, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 1, false)]
    [InlineData(1, 2, false)]
    [InlineData(2, 0, false)]
    [InlineData(2, 1, false)]
    [InlineData(2, 2, false)]
    [InlineData(0, 0, true)]  // pre-cancel
    public async Task Wrap_Wrappers_Matrix(int wrapperType, int behavior, bool preCancelled)
    {
        using var cts = new CancellationTokenSource();
        if (preCancelled) cts.Cancel();

        switch (wrapperType)
        {
            case 0:
            {
                var res = await CancellationAwareResult.WrapCancellationAware<int>(ct => ExecuteBehaviorAsync(behavior, ct), cts.Token);
                AssertOutcome(res.IsSuccess, res.Error, behavior, preCancelled);
                break;
            }
            case 1:
            {
                var res = await CancellationAwareResult.WrapResultOperation<int>(ct => ExecuteBehaviorResultAsync(behavior, ct), cts.Token);
                AssertOutcome(res.IsSuccess, res.Error, behavior, preCancelled);
                break;
            }
            default:
            {
                var res = await CancellationAwareResult.WrapCancellationAware(ct => ExecuteBehaviorVoidAsync(behavior, ct), cts.Token);
                AssertOutcome(res.IsSuccess, res.Error, behavior, preCancelled);
                break;
            }
        }
    }

    private static async Task<int> ExecuteBehaviorAsync(int behavior, CancellationToken ct)
    {
        await Task.Delay(1, CancellationToken.None);
        return behavior switch
        {
            1 => throw new OperationCanceledException(),
            2 => throw new InvalidOperationException("x"),
            _ => 42
        };
    }

    private static async Task<Result<int>> ExecuteBehaviorResultAsync(int behavior, CancellationToken ct)
    {
        await Task.Delay(1, CancellationToken.None);
        return behavior switch
        {
            1 => throw new OperationCanceledException(),
            2 => throw new InvalidOperationException("x"),
            _ => Result<int>.Success(7)
        };
    }

    private static async Task ExecuteBehaviorVoidAsync(int behavior, CancellationToken ct)
    {
        await Task.Delay(1, CancellationToken.None);
        switch (behavior)
        {
            case 1: throw new OperationCanceledException();
            case 2: throw new InvalidOperationException("x");
            default: return;
        }
    }

    private static void AssertOutcome(bool isSuccess, string? error, int behavior, bool preCancelled)
    {
        if (preCancelled)
        {
            isSuccess.ShouldBeFalse();
            error.ShouldBe(ResultErrors.OperationCancelled);
            return;
        }

        if (behavior == 0)
        {
            isSuccess.ShouldBeTrue();
        }
        else if (behavior == 1)
        {
            isSuccess.ShouldBeFalse();
            error.ShouldBe(ResultErrors.OperationCancelled);
        }
        else
        {
            isSuccess.ShouldBeFalse();
            error.ShouldNotBeNull();
            error!.ShouldContain("Operation failed:");
        }
    }
}
