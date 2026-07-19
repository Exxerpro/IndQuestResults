namespace IndQuestResults.Tests.Mutation.Operations;

/// <summary>
/// Tests for <see cref="ResultStepExtensions.ElseWithValue{T}(Result{T}, Func{IEnumerable{string}, T})"/>:
/// chainable failure-value projection. Only a value-LESS failure invokes the projection; everything else
/// passes through as the same instance.
/// </summary>
public class ResultElseWithValueTests
{
    private sealed record Dto(string Code);

    [Fact]
    public void ElseWithValue_ValueLessFailure_AttachesProjectedValue_PreservingErrorsAndException()
    {
        var exception = new InvalidOperationException("inner");
        var input = Result<Dto>.WithFailure(new[] { "e1", "e2" }, value: null, exception: exception);

        var output = input.ElseWithValue(errors =>
        {
            errors.ShouldBe(new[] { "e1", "e2" });
            return new Dto("diag");
        });

        output.IsFailure.ShouldBeTrue();
        output.Value.ShouldBe(new Dto("diag"));
        output.Errors.ShouldBe(new[] { "e1", "e2" });
        output.Exception.ShouldBe(exception);
    }

    [Fact]
    public void ElseWithValue_ValueCarryingFailure_SameInstance_ProjectionNotInvoked()
    {
        var input = Result<Dto>.WithFailure(new[] { "e1" }, new Dto("already"));
        var invocations = 0;

        var output = input.ElseWithValue(_ => { invocations++; return new Dto("new"); });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
        output.Value.ShouldBe(new Dto("already"));
    }

    [Fact]
    public void ElseWithValue_NullProjection_LeavesFailureValueLess_SameInstance()
    {
        var input = Result<Dto>.WithFailure(new[] { "e1" });

        var output = input.ElseWithValue(_ => null);

        ReferenceEquals(output, input).ShouldBeTrue();
        output.Value.ShouldBeNull();
        output.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ElseWithValue_Success_SameInstance_ProjectionNotInvoked()
    {
        var input = Result<Dto>.Success(new Dto("ok"));
        var invocations = 0;

        var output = input.ElseWithValue(_ => { invocations++; return new Dto("new"); });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public void ElseWithValue_SuccessWithNullValue_SameInstance_ProjectionNotInvoked()
    {
        var input = new Result<Dto>(true, (IEnumerable<string>?)null, null);
        var invocations = 0;

        var output = input.ElseWithValue(_ => { invocations++; return new Dto("new"); });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
        output.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void ElseWithValue_EmptyErrorsFailure_SubstitutedDefaultErrorPreservedVerbatim()
    {
        var input = Result<Dto>.WithFailure(Array.Empty<string>());

        var output = input.ElseWithValue(errors =>
        {
            errors.ShouldBe(new[] { "Operation failed to execute successfully" });
            return new Dto("diag");
        });

        output.Errors.ShouldBe(new[] { "Operation failed to execute successfully" });
        output.Value.ShouldBe(new Dto("diag"));
    }

    [Fact]
    public void ElseWithValue_NullArguments_Throw()
    {
        var input = Result<Dto>.WithFailure(new[] { "e1" });

        Should.Throw<ArgumentNullException>(() => ((Result<Dto>)null!).ElseWithValue(_ => new Dto("x")));
        Should.Throw<ArgumentNullException>(() => input.ElseWithValue(null!));
    }

    // --- async-source overload ---

    [Fact]
    public async Task ElseWithValue_AsyncSource_ValueLessFailure_AttachesProjectedValue()
    {
        var output = await Task.FromResult(Result<Dto>.WithFailure(new[] { "e1" }))
            .ElseWithValue(_ => new Dto("diag"));

        output.IsFailure.ShouldBeTrue();
        output.Value.ShouldBe(new Dto("diag"));
        output.Errors.ShouldBe(new[] { "e1" });
    }

    [Fact]
    public async Task ElseWithValue_AsyncSource_Success_SameInstance()
    {
        var input = Result<Dto>.Success(new Dto("ok"));

        var output = await Task.FromResult(input).ElseWithValue(_ => new Dto("new"));

        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public async Task ElseWithValue_AsyncSource_NullArguments_Throw()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            ((Task<Result<Dto>>)null!).ElseWithValue(_ => new Dto("x")));
        await Should.ThrowAsync<ArgumentNullException>(
            Task.FromResult(Result<Dto>.WithFailure(new[] { "e1" })).ElseWithValue(null!));
    }

    [Fact]
    public async Task ElseWithValue_AfterThenStepChain_ConditionalDiagnostics_OffLeavesValueLess()
    {
        // Mirrors the consumer pattern: diagnostics flag OFF -> projection returns null -> failure stays value-less.
        var flagOn = false;

        var output = await Task.FromResult(Result<Dto>.Success(new Dto("start")))
            .ThenStep(_ => Result<Dto>.WithFailure(new[] { "step failed" }))
            .ElseWithValue(errors => flagOn ? new Dto("diag") : null);

        output.IsFailure.ShouldBeTrue();
        output.Value.ShouldBeNull();
    }
}
