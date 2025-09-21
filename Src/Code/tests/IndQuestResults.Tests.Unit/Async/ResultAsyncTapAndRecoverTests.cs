namespace IndQuestResults.Tests.Unit.Async;

/// <summary>
/// Tests for async Tap and Recover helpers ensuring side effects, error preservation,
/// exception handling, and cancellation semantics.
/// </summary>
public class ResultAsyncTapAndRecoverTests
{
    /// <summary>
    /// Ensures TapAsync executes action on success and returns the original result.
    /// </summary>
    [Fact]
    public async Task TapAsync_OnSuccess_InvokesAction_AndReturnsOriginal()
    {
        var called = 0;
        var input = Task.FromResult(Result<int>.Success(3));
        var res = await input.TapAsync(async v => { await Task.Delay(1); called = v * 2; });

        called.ShouldBe(6);
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(3);
    }

    /// <summary>
    /// Ensures TapAsync does not execute action for failures and returns the failure.
    /// </summary>
    [Fact]
    public async Task TapAsync_OnFailure_DoesNotInvokeAction_ReturnsFailure()
    {
        var called = false;
        var input = Task.FromResult(Result<int>.WithFailure("x"));
        var res = await input.TapAsync(v => { called = true; return Task.CompletedTask; });

        called.ShouldBeFalse();
        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("x");
    }

    /// <summary>
    /// Ensures TapAsync returns a failure when the action throws.
    /// </summary>
    [Fact]
    public async Task TapAsync_ActionThrows_ReturnsFailure()
    {
        var input = Task.FromResult(Result<int>.Success(1));
        var res = await input.TapAsync<int>(async _ => { await Task.Delay(1); throw new InvalidOperationException("boom"); });

        res.IsFailure.ShouldBeTrue();
        res.Error!.ShouldContain("Async side effect failed");
        res.Error!.ShouldContain("boom");
    }

    /// <summary>
    /// Ensures TapAsync returns cancelled when the cancellation token is cancelled.
    /// </summary>
    [Fact]
    public async Task TapAsync_CancellationTokenCancelled_ReturnsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var input = Task.FromResult(Result<int>.Success(1));
        var res = await input.TapAsync(_ => Task.CompletedTask, cts.Token);

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Ensures RecoverAsync bypasses recovery when original result is successful.
    /// </summary>
    [Fact]
    public async Task RecoverAsync_OnSuccess_ReturnsOriginal_NoRecoveryCall()
    {
        var called = false;
        var input = Task.FromResult(Result<int>.Success(5));
        var res = await input.RecoverAsync(() => { called = true; return Task.FromResult(Result<int>.Success(0)); });

        called.ShouldBeFalse();
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(5);
    }

    /// <summary>
    /// Ensures RecoverAsync executes recovery on failure and returns recovered result.
    /// </summary>
    [Fact]
    public async Task RecoverAsync_OnFailure_CallsRecovery_AndReturnsRecovered()
    {
        var called = false;
        var input = Task.FromResult(Result<int>.WithFailure("fail"));
        var res = await input.RecoverAsync(() => { called = true; return Task.FromResult(Result<int>.Success(9)); });

        called.ShouldBeTrue();
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(9);
    }

    /// <summary>
    /// Ensures RecoverAsync returns failure when recovery throws an exception.
    /// </summary>
    [Fact]
    public async Task RecoverAsync_RecoveryThrows_ReturnsFailure()
    {
        var input = Task.FromResult(Result<int>.WithFailure("x"));
        var res = await input.RecoverAsync<int>(async () => { await Task.Delay(1); throw new InvalidOperationException("oops"); });

        res.IsFailure.ShouldBeTrue();
        res.Error!.ShouldContain("Async recovery failed");
        res.Error!.ShouldContain("oops");
    }

    /// <summary>
    /// Ensures RecoverAsync returns cancelled when token is cancelled.
    /// </summary>
    [Fact]
    public async Task RecoverAsync_CancellationTokenCancelled_ReturnsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var input = Task.FromResult(Result<int>.WithFailure("x"));
        var res = await input.RecoverAsync(() => Task.FromResult(Result<int>.Success(1)), cts.Token);

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Ensures RecoverAsync converts OperationCanceledException into a cancelled result.
    /// </summary>
    [Fact]
    public async Task RecoverAsync_RecoveryThrowsOperationCanceledException_ReturnsCancelled()
    {
        var input = Task.FromResult(Result<int>.WithFailure("x"));
        var res = await input.RecoverAsync<int>(async () => { await Task.Delay(1); throw new OperationCanceledException("stop"); });

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultErrors.OperationCancelled);
    }
}



