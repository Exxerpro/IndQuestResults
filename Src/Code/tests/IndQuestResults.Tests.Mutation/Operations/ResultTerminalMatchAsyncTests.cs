namespace IndQuestResults.Tests.Mutation.Operations;

/// <summary>
/// Tests for <see cref="ResultStepExtensions.MatchAsync{T, TOut}(Task{Result{T}}, Func{T, TOut}, Func{IEnumerable{string}, T, TOut})"/>.
/// Routing is on IsSuccess (success branch can never observe null), and the failure branch receives the carried failure value.
/// </summary>
public class ResultTerminalMatchAsyncTests
{
    private sealed record State(string Name, string? FailureDto = null);

    [Fact]
    public async Task MatchAsync_Success_RoutesToOnSuccess_WithValue()
    {
        var successCalls = 0;
        var failureCalls = 0;

        var output = await Task.FromResult(Result<State>.Success(new State("a")))
            .MatchAsync(
                s => { successCalls++; return $"ok:{s.Name}"; },
                (errors, s) => { failureCalls++; return "failed"; });

        output.ShouldBe("ok:a");
        successCalls.ShouldBe(1);
        failureCalls.ShouldBe(0);
    }

    [Fact]
    public async Task MatchAsync_Failure_RoutesToOnFailure_WithExactErrorsAndCarriedValue()
    {
        var carried = new State("carried", FailureDto: "dto");
        var input = Result<State>.WithFailure(new[] { "e1", "e2" }, carried);

        var output = await Task.FromResult(input).MatchAsync(
            s => "ok",
            (errors, s) =>
            {
                errors.ShouldBe(new[] { "e1", "e2" });
                s.ShouldBe(carried);
                return $"failed:{s!.FailureDto}";
            });

        output.ShouldBe("failed:dto");
    }

    [Fact]
    public async Task MatchAsync_ValueLessFailure_PassesNullValue()
    {
        var output = await Task.FromResult(Result<State>.WithFailure(new[] { "boom" }))
            .MatchAsync(
                s => "ok",
                (errors, s) =>
                {
                    s.ShouldBeNull();
                    return "failed";
                });

        output.ShouldBe("failed");
    }

    [Fact]
    public async Task MatchAsync_SuccessWithNullValue_RoutesToOnFailure_WithEmptyErrorsAndNullValue()
    {
        var successCalls = 0;

        var output = await Task.FromResult(new Result<State>(true, (IEnumerable<string>?)null, null))
            .MatchAsync(
                s => { successCalls++; return "ok"; },
                (errors, s) =>
                {
                    errors.ShouldBeEmpty();
                    s.ShouldBeNull();
                    return "null-success";
                });

        output.ShouldBe("null-success");
        successCalls.ShouldBe(0);
    }

    [Fact]
    public async Task MatchAsync_BranchException_Propagates()
    {
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            Task.FromResult(Result<State>.Success(new State("a")))
                .MatchAsync<State, string>(
                    _ => throw new InvalidOperationException("raw"),
                    (errors, s) => "failed"));

        thrown.Message.ShouldBe("raw");
    }

    [Fact]
    public async Task MatchAsync_NullArguments_Throw()
    {
        var task = Task.FromResult(Result<State>.Success(new State("a")));

        await Should.ThrowAsync<ArgumentNullException>(
            ((Task<Result<State>>)null!).MatchAsync(s => "ok", (errors, s) => "failed"));
        await Should.ThrowAsync<ArgumentNullException>(
            task.MatchAsync((Func<State, string>)null!, (errors, s) => "failed"));
        await Should.ThrowAsync<ArgumentNullException>(
            task.MatchAsync(s => "ok", (Func<IEnumerable<string>, State?, string>)null!));
    }

    // --- async-branch overload ---

    [Fact]
    public async Task MatchAsync_AsyncBranches_Success_AwaitsOnSuccess()
    {
        var failureCalls = 0;

        var output = await Task.FromResult(Result<State>.Success(new State("a")))
            .MatchAsync(
                async s => { await Task.Yield(); return $"ok:{s.Name}"; },
                (errors, s) => { failureCalls++; return Task.FromResult("failed"); });

        output.ShouldBe("ok:a");
        failureCalls.ShouldBe(0);
    }

    [Fact]
    public async Task MatchAsync_AsyncBranches_Failure_AwaitsOnFailure_WithCarriedValue()
    {
        var carried = new State("carried", FailureDto: "dto");

        var output = await Task.FromResult(Result<State>.WithFailure(new[] { "e1" }, carried))
            .MatchAsync(
                s => Task.FromResult("ok"),
                async (errors, s) => { await Task.Yield(); return $"failed:{s!.FailureDto}"; });

        output.ShouldBe("failed:dto");
    }

    [Fact]
    public async Task MatchAsync_AsyncBranches_SuccessWithNullValue_RoutesToOnFailure()
    {
        var output = await Task.FromResult(new Result<State>(true, (IEnumerable<string>?)null, null))
            .MatchAsync(
                s => Task.FromResult("ok"),
                (errors, s) => Task.FromResult("null-success"));

        output.ShouldBe("null-success");
    }

    [Fact]
    public async Task MatchAsync_AsyncBranches_BranchException_Propagates()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            Task.FromResult(Result<State>.WithFailure(new[] { "boom" }))
                .MatchAsync<State, string>(
                    s => Task.FromResult("ok"),
                    (errors, s) => throw new InvalidOperationException("raw")));
    }
}
