namespace IndQuestResults.Tests.Mutation.Operations;

/// <summary>
/// Tests for <see cref="ResultStepExtensions.ThenStep{T}(Result{T}, Func{T, Result{T}})"/> and its async-source overloads.
/// The load-bearing contract: short-circuits return the ORIGINAL instance (value-carrying failures survive).
/// </summary>
public class ResultStepExtensionsTests
{
    private sealed record State(string Name, string? FailureDto = null);

    // --- sync source ---

    [Fact]
    public void ThenStep_Success_InvokesNext_AndReturnsItsResult()
    {
        var input = Result<State>.Success(new State("a"));
        var next = Result<State>.Success(new State("b"));
        var invocations = 0;

        var output = input.ThenStep(s => { invocations++; s.Name.ShouldBe("a"); return next; });

        invocations.ShouldBe(1);
        ReferenceEquals(output, next).ShouldBeTrue();
    }

    [Fact]
    public void ThenStep_Failure_ShortCircuits_SameInstance_NextNotInvoked()
    {
        var input = Result<State>.WithFailure(new[] { "boom" });
        var invocations = 0;

        var output = input.ThenStep(s => { invocations++; return Result<State>.Success(s); });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public void ThenStep_ValueCarryingFailure_PreservesValueErrorsAndException()
    {
        var exception = new InvalidOperationException("inner");
        var carried = new State("carried", FailureDto: "dto");
        var input = Result<State>.WithFailure(new[] { "e1", "e2" }, carried, exception);

        var output = input.ThenStep(s => Result<State>.Success(s));

        ReferenceEquals(output, input).ShouldBeTrue();
        output.Value.ShouldBe(carried);
        output.Errors.ShouldBe(new[] { "e1", "e2" });
        output.Exception.ShouldBe(exception);
    }

    [Fact]
    public void ThenStep_SuccessWithNullValue_PassesThroughSameInstance_StaysTriState()
    {
        var input = new Result<State>(true, (IEnumerable<string>?)null, null);
        var invocations = 0;

        var output = input.ThenStep(s => { invocations++; return Result<State>.Success(s); });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
        output.IsSuccess.ShouldBeFalse();
        output.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void ThenStep_StepReturningValueCarryingFailure_IsReturnedAsIs()
    {
        var input = Result<State>.Success(new State("a"));
        var failure = Result<State>.WithFailure(new[] { "step failed" }, new State("a", FailureDto: "dto"));

        var output = input.ThenStep(_ => failure);

        ReferenceEquals(output, failure).ShouldBeTrue();
    }

    [Fact]
    public void ThenStep_ExceptionFromNext_Propagates_NoWrapping()
    {
        var input = Result<State>.Success(new State("a"));

        Should.Throw<InvalidOperationException>(() => input.ThenStep<State>(_ => throw new InvalidOperationException("raw")))
            .Message.ShouldBe("raw");
    }

    [Fact]
    public void ThenStep_NullArguments_Throw()
    {
        var input = Result<State>.Success(new State("a"));

        Should.Throw<ArgumentNullException>(() => ((Result<State>)null!).ThenStep(s => Result<State>.Success(s)));
        Should.Throw<ArgumentNullException>(() => input.ThenStep((Func<State, Result<State>>)null!));
    }

    // --- async source, sync step ---

    [Fact]
    public async Task ThenStep_AsyncSource_SyncStep_Success_InvokesNext()
    {
        var next = Result<State>.Success(new State("b"));
        var invocations = 0;

        var output = await Task.FromResult(Result<State>.Success(new State("a")))
            .ThenStep(s => { invocations++; return next; });

        invocations.ShouldBe(1);
        ReferenceEquals(output, next).ShouldBeTrue();
    }

    [Fact]
    public async Task ThenStep_AsyncSource_SyncStep_ValueCarryingFailure_SameInstance()
    {
        var input = Result<State>.WithFailure(new[] { "boom" }, new State("carried"));
        var invocations = 0;

        var output = await Task.FromResult(input).ThenStep(s => { invocations++; return Result<State>.Success(s); });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    // --- async source, async step ---

    [Fact]
    public async Task ThenStep_AsyncSource_AsyncStep_Success_InvokesNext()
    {
        var next = Result<State>.Success(new State("b"));
        var invocations = 0;

        var output = await Task.FromResult(Result<State>.Success(new State("a")))
            .ThenStep(s => { invocations++; return Task.FromResult(next); });

        invocations.ShouldBe(1);
        ReferenceEquals(output, next).ShouldBeTrue();
    }

    [Fact]
    public async Task ThenStep_AsyncSource_AsyncStep_ValueCarryingFailure_SameInstance_NextNotInvoked()
    {
        var exception = new InvalidOperationException("inner");
        var input = Result<State>.WithFailure(new[] { "e1" }, new State("carried", FailureDto: "dto"), exception);
        var invocations = 0;

        var output = await Task.FromResult(input).ThenStep(s => { invocations++; return Task.FromResult(Result<State>.Success(s)); });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
        output.Exception.ShouldBe(exception);
    }

    [Fact]
    public async Task ThenStep_AsyncSource_AsyncStep_SuccessWithNullValue_PassesThrough()
    {
        var input = new Result<State>(true, (IEnumerable<string>?)null, null);

        var output = await Task.FromResult(input).ThenStep(s => Task.FromResult(Result<State>.Success(s)));

        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public async Task ThenStep_AsyncSource_AsyncStep_ExceptionPropagates_NoAsyncBindWrapping()
    {
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            Task.FromResult(Result<State>.Success(new State("a")))
                .ThenStep(new Func<State, Task<Result<State>>>(_ => throw new InvalidOperationException("raw"))));

        thrown.Message.ShouldBe("raw");
        thrown.Message.ShouldNotContain("Async bind operation failed");
    }

    [Fact]
    public async Task ThenStep_AsyncSource_AsyncStep_CancellationPropagates()
    {
        await Should.ThrowAsync<OperationCanceledException>(
            Task.FromResult(Result<State>.Success(new State("a")))
                .ThenStep(new Func<State, Task<Result<State>>>(_ => throw new OperationCanceledException())));
    }

    [Fact]
    public async Task ThenStep_AsyncSource_NullArguments_Throw()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            ((Task<Result<State>>)null!).ThenStep(s => Result<State>.Success(s)));
        await Should.ThrowAsync<ArgumentNullException>(
            Task.FromResult(Result<State>.Success(new State("a"))).ThenStep((Func<State, Task<Result<State>>>)null!));
    }

    [Fact]
    public async Task ThenStep_Chain_FailureDtoAttachedMidChain_SurvivesToEnd()
    {
        var output = await Task.FromResult(Result<State>.Success(new State("start")))
            .ThenStep(s => Result<State>.WithFailure(new[] { "validation failed" }, s with { FailureDto = "dto" }))
            .ThenStep(s => Task.FromResult(Result<State>.Success(s with { Name = "never" })))
            .ThenStep(s => Result<State>.Success(s with { Name = "never2" }));

        output.IsFailure.ShouldBeTrue();
        output.Value.ShouldNotBeNull();
        output.Value.FailureDto.ShouldBe("dto");
        output.Errors.ShouldBe(new[] { "validation failed" });
    }
}
