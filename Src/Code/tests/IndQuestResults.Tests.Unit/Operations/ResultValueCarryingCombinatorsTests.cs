namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Tests for the value-carrying failure combinators <c>EnsureOrFault</c> and the value-aware <c>TapError</c>
/// overload (ADR 0005). Load-bearing contracts: <c>EnsureOrFault</c>'s failure branch is caller-supplied and can
/// carry a diagnostic value; short-circuits (upstream failure, success-with-null) return the ORIGINAL instance;
/// value-aware <c>TapError</c> fires only on failure, exposes the carried value, returns the original instance, and
/// complements — without shadowing — the existing string-only <c>TapError</c> overload.
/// </summary>
public class ResultValueCarryingCombinatorsTests
{
    private sealed record State(string Name, string? Diagnostic = null);

    // -----------------------------------------------------------------------
    // EnsureOrFault — sync source
    // -----------------------------------------------------------------------

    [Fact]
    public void EnsureOrFault_PredicateTrue_ReturnsOriginalSuccess_OnFalseNotInvoked()
    {
        var input = Result<State>.Success(new State("ok"));
        var invocations = 0;

        var output = input.EnsureOrFault(s => s.Name == "ok", s => { invocations++; return Result<State>.WithFailure("bad", s); });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public void EnsureOrFault_PredicateFalse_ReturnsOnFalse_WithCarriedValue()
    {
        var value = new State("input");
        var input = Result<State>.Success(value);
        State? received = null;

        var output = input.EnsureOrFault(
            _ => false,
            s => { received = s; return Result<State>.WithFailure("validation failed", s with { Diagnostic = "dto" }); });

        received.ShouldBe(value);
        output.IsFailure.ShouldBeTrue();
        output.Errors.ShouldBe(new[] { "validation failed" });
        output.Value.ShouldNotBeNull();
        output.Value!.Diagnostic.ShouldBe("dto");
    }

    [Fact]
    public void EnsureOrFault_UpstreamFailure_ShortCircuits_SameInstance_PredicateNotInvoked()
    {
        var carried = new State("carried", Diagnostic: "kept");
        var exception = new InvalidOperationException("inner");
        var input = Result<State>.WithFailure(new[] { "e1", "e2" }, carried, exception);
        var predicateCalls = 0;

        var output = input.EnsureOrFault(_ => { predicateCalls++; return true; }, s => Result<State>.WithFailure("nope", s));

        predicateCalls.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
        output.Value.ShouldBe(carried);
        output.Errors.ShouldBe(new[] { "e1", "e2" });
        output.Exception.ShouldBe(exception);
    }

    [Fact]
    public void EnsureOrFault_SuccessWithNullValue_PassesThroughSameInstance()
    {
        var input = new Result<State>(true, (IEnumerable<string>?)null, null);
        var predicateCalls = 0;

        var output = input.EnsureOrFault(_ => { predicateCalls++; return true; }, s => Result<State>.WithFailure("nope", s));

        predicateCalls.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
        output.IsSuccess.ShouldBeFalse();
        output.IsFailure.ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // EnsureOrFault — async source
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EnsureOrFault_Async_PredicateTrue_ReturnsOriginalSuccess()
    {
        var input = Result<State>.Success(new State("ok"));

        var output = await Task.FromResult(input)
            .EnsureOrFault(s => s.Name == "ok", s => Result<State>.WithFailure("bad", s));

        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public async Task EnsureOrFault_Async_PredicateFalse_ReturnsOnFalse_WithCarriedValue()
    {
        var output = await Task.FromResult(Result<State>.Success(new State("input")))
            .EnsureOrFault(_ => false, s => Result<State>.WithFailure("validation failed", s with { Diagnostic = "dto" }));

        output.IsFailure.ShouldBeTrue();
        output.Value.ShouldNotBeNull();
        output.Value!.Diagnostic.ShouldBe("dto");
        output.Errors.ShouldBe(new[] { "validation failed" });
    }

    [Fact]
    public async Task EnsureOrFault_Async_UpstreamFailure_ShortCircuits_SameInstance()
    {
        var input = Result<State>.WithFailure(new[] { "boom" }, new State("carried"));
        var predicateCalls = 0;

        var output = await Task.FromResult(input)
            .EnsureOrFault(_ => { predicateCalls++; return true; }, s => Result<State>.WithFailure("nope", s));

        predicateCalls.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    // -----------------------------------------------------------------------
    // TapError (value-aware) — sync source
    // -----------------------------------------------------------------------

    [Fact]
    public void TapError_ValueAware_OnFailure_ReceivesErrorsAndCarriedValue_ReturnsSameInstance()
    {
        var carried = new State("carried", Diagnostic: "dto");
        var input = Result<State>.WithFailure(new[] { "e1", "e2" }, carried);
        IReadOnlyList<string>? seenErrors = null;
        State? seenValue = null;

        var output = input.TapError((errors, value) => { seenErrors = errors; seenValue = value; });

        seenErrors.ShouldBe(new[] { "e1", "e2" });
        seenValue.ShouldBe(carried);
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public void TapError_ValueAware_ValueLessFailure_ReceivesNullValue()
    {
        var input = Result<State>.WithFailure(new[] { "boom" });
        State? seenValue = new State("sentinel");
        var invoked = false;

        input.TapError((_, value) => { invoked = true; seenValue = value; });

        invoked.ShouldBeTrue();
        seenValue.ShouldBeNull();
    }

    [Fact]
    public void TapError_ValueAware_OnSuccess_DoesNotFire_ReturnsSameInstance()
    {
        var input = Result<State>.Success(new State("ok"));
        var invoked = false;

        var output = input.TapError((_, _) => invoked = true);

        invoked.ShouldBeFalse();
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public void TapError_ValueAware_ExceptionFromAction_Propagates_NotSwallowed()
    {
        var input = Result<State>.WithFailure(new[] { "boom" }, new State("carried"));

        Should.Throw<InvalidOperationException>(
                () => input.TapError((_, _) => throw new InvalidOperationException("raw")))
            .Message.ShouldBe("raw");
    }

    // -----------------------------------------------------------------------
    // TapError (value-aware) — async source
    // -----------------------------------------------------------------------

    [Fact]
    public async Task TapError_ValueAware_Async_OnFailure_ReceivesCarriedValue_ReturnsSameInstance()
    {
        var carried = new State("carried", Diagnostic: "dto");
        var input = Result<State>.WithFailure(new[] { "e1" }, carried);
        State? seenValue = null;

        var output = await Task.FromResult(input).TapError((_, value) => seenValue = value);

        seenValue.ShouldBe(carried);
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public async Task TapError_ValueAware_Async_OnSuccess_DoesNotFire()
    {
        var input = Result<State>.Success(new State("ok"));
        var invoked = false;

        var output = await Task.FromResult(input).TapError((_, _) => invoked = true);

        invoked.ShouldBeFalse();
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    // -----------------------------------------------------------------------
    // Overload resolution — the string-only TapError still binds unambiguously
    // -----------------------------------------------------------------------

    [Fact]
    public void TapError_StringOnlyOverload_StillResolves_ForSingleArgDelegate()
    {
        var input = Result<State>.WithFailure(new[] { "e1" }, new State("carried"));
        IEnumerable<string>? seenErrors = null;

        // A one-argument delegate must still bind to the existing string-only overload.
        var output = input.TapError(errors => seenErrors = errors);

        seenErrors.ShouldBe(new[] { "e1" });
        ReferenceEquals(output, input).ShouldBeTrue();
    }
}
