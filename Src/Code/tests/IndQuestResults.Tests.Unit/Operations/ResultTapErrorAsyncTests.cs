namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Tests for <see cref="ResultErrorExtensions.TapErrorAsync{T}(Task{Result{T}}, Func{IEnumerable{string}, Task})"/>:
/// async failure-side tap that returns the original instance and swallows nothing.
/// </summary>
public class ResultTapErrorAsyncTests
{
    private sealed record Dto(string Code);

    [Fact]
    public async Task TapErrorAsync_Failure_InvokesAction_WithErrors_ReturnsSameInstance()
    {
        var input = Result<Dto>.WithFailure(new[] { "e1", "e2" }, new Dto("carried"));
        IEnumerable<string>? seen = null;

        var output = await Task.FromResult(input).TapErrorAsync(errors =>
        {
            seen = errors;
            return Task.CompletedTask;
        });

        seen.ShouldBe(new[] { "e1", "e2" });
        ReferenceEquals(output, input).ShouldBeTrue();
        output.Value.ShouldBe(new Dto("carried"));
    }

    [Fact]
    public async Task TapErrorAsync_Success_ActionNotInvoked()
    {
        var input = Result<Dto>.Success(new Dto("ok"));
        var invocations = 0;

        var output = await Task.FromResult(input).TapErrorAsync(_ => { invocations++; return Task.CompletedTask; });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public async Task TapErrorAsync_SuccessWithNullValue_ActionNotInvoked()
    {
        // Tri-state: success-with-null has IsFailure == false, so the tap must not fire.
        var input = new Result<Dto>(true, (IEnumerable<string>?)null, null);
        var invocations = 0;

        var output = await Task.FromResult(input).TapErrorAsync(_ => { invocations++; return Task.CompletedTask; });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public async Task TapErrorAsync_AwaitsActionCompletion_BeforeReturning()
    {
        var completed = false;

        _ = await Task.FromResult(Result<Dto>.WithFailure(new[] { "e1" })).TapErrorAsync(async _ =>
        {
            await Task.Delay(30);
            completed = true;
        });

        completed.ShouldBeTrue();
    }

    [Fact]
    public async Task TapErrorAsync_ActionException_Propagates_NotSwallowed()
    {
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            Task.FromResult(Result<Dto>.WithFailure(new[] { "e1" }))
                .TapErrorAsync(_ => throw new InvalidOperationException("audit failed")));

        thrown.Message.ShouldBe("audit failed");
    }

    [Fact]
    public async Task TapErrorAsync_NullArguments_Throw()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            ((Task<Result<Dto>>)null!).TapErrorAsync(_ => Task.CompletedTask));
        await Should.ThrowAsync<ArgumentNullException>(
            Task.FromResult(Result<Dto>.WithFailure(new[] { "e1" })).TapErrorAsync(null!));
    }

    [Fact]
    public async Task TapErrorAsync_ThenElseWithValue_AuditRunsBeforeDtoAttach_OrderPreserved()
    {
        // Mirrors the consumer failure path: audit (TapErrorAsync) then DTO attach (ElseWithValue).
        var order = new List<string>();

        var output = await Task.FromResult(Result<Dto>.WithFailure(new[] { "boom" }))
            .TapErrorAsync(_ => { order.Add("audit"); return Task.CompletedTask; })
            .ElseWithValue(_ => { order.Add("attach"); return new Dto("diag"); });

        order.ShouldBe(new[] { "audit", "attach" });
        output.Value.ShouldBe(new Dto("diag"));
        output.IsFailure.ShouldBeTrue();
    }
}
