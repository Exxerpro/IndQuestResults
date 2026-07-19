namespace IndQuestResults.Tests.Mutation.Operations;

/// <summary>
/// Tests for the pipeline members restored for API parity with the shipped 1.4.1 package:
/// <c>Result.Success&lt;T&gt;</c>, sync/Task <c>Then</c> overloads, <c>Ensure</c> on <c>Task&lt;Result&lt;T&gt;&gt;</c>,
/// <c>ToResult</c>, Task <c>RequireValue</c>, and chainable <c>ValidateNotNull</c>.
/// </summary>
public class ResultPipelineParityTests
{
    private sealed record Request(int MachineId, string? BarCode = null);

    // --- Result.Success<T> (chain-starting factory) ---

    [Fact]
    public void ResultSuccessGeneric_CreatesSuccessCarryingValue()
    {
        var result = Result.Success(new Request(7));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new Request(7));
    }

    // --- Then (sync source) ---

    [Fact]
    public void Then_Bind_Success_Chains()
    {
        var output = Result.Success(new Request(7)).Then(r => Result<int>.Success(r.MachineId));

        output.IsSuccess.ShouldBeTrue();
        output.Value.ShouldBe(7);
    }

    [Fact]
    public void Then_Bind_Failure_Propagates_BindNotInvoked()
    {
        var invocations = 0;

        var output = Result<Request>.WithFailure(new[] { "boom" })
            .Then(r => { invocations++; return Result<int>.Success(r.MachineId); });

        invocations.ShouldBe(0);
        output.IsFailure.ShouldBeTrue();
        output.Errors.ShouldBe(new[] { "boom" });
    }

    [Fact]
    public void Then_Map_Success_Projects()
    {
        var output = Result.Success(new Request(7)).Then(r => r.MachineId * 2);

        output.Value.ShouldBe(14);
    }

    // --- Then (Task source) ---

    [Fact]
    public async Task Then_TaskSource_AsyncBind_Chains()
    {
        var output = await Task.FromResult(Result.Success(new Request(7)))
            .Then(r => Task.FromResult(Result<int>.Success(r.MachineId)));

        output.Value.ShouldBe(7);
    }

    [Fact]
    public async Task Then_TaskSource_SyncBind_Chains()
    {
        var output = await Task.FromResult(Result.Success(new Request(7)))
            .Then(r => Result<int>.Success(r.MachineId));

        output.Value.ShouldBe(7);
    }

    [Fact]
    public async Task Then_TaskSource_Map_Projects()
    {
        var output = await Task.FromResult(Result.Success(new Request(7)))
            .Then(r => r.MachineId + 1);

        output.Value.ShouldBe(8);
    }

    [Fact]
    public async Task Then_TaskSource_Failure_PropagatesErrors()
    {
        var output = await Task.FromResult(Result<Request>.WithFailure(new[] { "boom" }))
            .Then(r => Task.FromResult(Result<int>.Success(r.MachineId)));

        output.IsFailure.ShouldBeTrue();
        output.Errors.ShouldBe(new[] { "boom" });
    }

    // --- Ensure (Task source) ---

    [Fact]
    public async Task Ensure_TaskSource_PredicateHolds_PassesThrough()
    {
        var output = await Task.FromResult(Result.Success(new Request(7)))
            .Ensure(r => r.MachineId > 0, "invalid machine");

        output.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Ensure_TaskSource_PredicateFails_FailsWithMessage()
    {
        var output = await Task.FromResult(Result.Success(new Request(0)))
            .Ensure(r => r.MachineId > 0, "invalid machine");

        output.IsFailure.ShouldBeTrue();
        output.Errors.ShouldContain("invalid machine");
    }

    // --- ToResult ---

    [Fact]
    public async Task ToResult_NullableTask_Null_FailsWithMessage()
    {
        var output = await Task.FromResult<string?>(null).ToResult("not found");

        output.IsFailure.ShouldBeTrue();
        output.Errors.ShouldBe(new[] { "not found" });
    }

    [Fact]
    public async Task ToResult_NullableTask_Value_Succeeds()
    {
        var output = await Task.FromResult<string?>("x").ToResult("not found");

        output.Value.ShouldBe("x");
    }

    [Fact]
    public async Task ToResult_ResultTask_FailurePropagatesFirst_ThenNullBecomesMessage()
    {
        var failed = await Task.FromResult(Result<string>.WithFailure(new[] { "boom" })).ToResult("not found");
        failed.Errors.ShouldBe(new[] { "boom" });

        var nullSuccess = await Task.FromResult(new Result<string>(true, (IEnumerable<string>?)null, null)).ToResult("not found");
        nullSuccess.Errors.ShouldBe(new[] { "not found" });

        var ok = await Task.FromResult(Result<string>.Success("x")).ToResult("not found");
        ok.Value.ShouldBe("x");
    }

    // --- RequireValue (Task source) ---

    [Fact]
    public async Task RequireValue_TaskSource_PrecedenceFailureThenNullThenSuccess()
    {
        var failed = await Task.FromResult(Result<string?>.WithFailure(new[] { "boom" })).RequireValue("missing");
        failed.Errors.ShouldBe(new[] { "boom" });

        var nullSuccess = await Task.FromResult(new Result<string?>(true, (IEnumerable<string>?)null, null)).RequireValue("missing");
        nullSuccess.Errors.ShouldBe(new[] { "missing" });

        var ok = await Task.FromResult(Result<string?>.Success("x")).RequireValue("missing");
        ok.Value.ShouldBe("x");
    }

    // --- ValidateNotNull (chainable selector overload) ---

    [Fact]
    public async Task ValidateNotNull_Selector_NonNullComponent_ContinuesWithOriginal()
    {
        var input = Result.Success(new Request(7, "BC-1"));

        var output = await input.ValidateNotNull(r => (r!.BarCode, nameof(Request.BarCode)));

        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateNotNull_Selector_NullComponent_FailsWithParameterName()
    {
        var output = await Result.Success(new Request(7, null))
            .ValidateNotNull(r => (r!.BarCode, nameof(Request.BarCode)));

        output.IsFailure.ShouldBeTrue();
        output.Error.ShouldNotBeNull();
        output.Error.ShouldContain(nameof(Request.BarCode));
    }

    [Fact]
    public async Task ValidateNotNull_Selector_EmptyParameterName_Fails()
    {
        var output = await Result.Success(new Request(7, "BC-1"))
            .ValidateNotNull(r => (r!.BarCode, string.Empty));

        output.IsFailure.ShouldBeTrue();
        output.Errors.ShouldContain("Parameter name cannot be null or empty.");
    }

    [Fact]
    public async Task ValidateNotNull_Selector_UpstreamFailure_ShortCircuits_SelectorNotInvoked()
    {
        var invocations = 0;
        var input = Result<Request>.WithFailure(new[] { "boom" });

        var output = await input.ValidateNotNull(r => { invocations++; return (r, "r"); });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    // --- The consumer's exemplar chain shape compiles and behaves ---

    [Fact]
    public async Task ExemplarChain_ValidateEnsureThenAsync_ComposesEndToEnd()
    {
        var output = await Result.Success(new Request(7, "BC-1"))
            .ValidateNotNull(r => (r, nameof(Request)))
            .Ensure(r => r.MachineId > 0, "Machine number invalid")
            .Then(r => Task.FromResult(Result<string>.Success($"machine:{r.MachineId}")))
            .Then(s => s.ToUpperInvariant());

        output.IsSuccess.ShouldBeTrue();
        output.Value.ShouldBe("MACHINE:7");
    }
}
