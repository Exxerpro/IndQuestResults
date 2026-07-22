namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Tests for the value-aware "symmetry" overloads that complete the value-carrying failure family:
/// the sync <see cref="Result{T}.Match{TOut}(Func{T, TOut}, Func{IEnumerable{string}, T, TOut})"/> terminal
/// (sync sibling of <see cref="ResultStepExtensions.MatchAsync{T, TOut}(Task{Result{T}}, Func{T, TOut}, Func{IEnumerable{string}, T, TOut})"/>),
/// the value-aware <see cref="ResultValueExtensions.MatchValue{T, TOut}(Result{T}, Func{T, TOut}, Func{IEnumerable{string}, T, TOut})"/>,
/// and the value-aware <see cref="ResultErrorExtensions.TapErrorAsync{T}(Result{T}, Func{IEnumerable{string}, T, Task})"/> pair.
/// </summary>
public class ResultValueAwareSymmetryTests
{
    private sealed record Dto(string Code);

    private static Result<Dto> Success() => Result<Dto>.Success(new Dto("ok"));
    private static Result<Dto> FailureWithValue() => Result<Dto>.WithFailure(new[] { "e1", "e2" }, new Dto("carried"));
    private static Result<Dto> FailureValueLess() => Result<Dto>.WithFailure(new[] { "e1", "e2" });
    private static Result<Dto> SuccessWithNull() => new(true, (IEnumerable<string>?)null, null);

    // ---------------------------------------------------------------------
    // 1) sync value-aware Match<TOut> (routes on IsSuccess, sibling of MatchAsync)
    // ---------------------------------------------------------------------

    [Fact]
    public void Match_ValueAware_Success_RoutesToOnSuccess()
    {
        var output = Success().Match(
            onSuccess: v => $"ok:{v.Code}",
            onFailure: (errors, value) => "should-not-run");

        output.ShouldBe("ok:ok");
    }

    [Fact]
    public void Match_ValueAware_FailureWithValue_RoutesToOnFailure_ReceivesCarriedValue()
    {
        Dto? received = new Dto("sentinel");
        IEnumerable<string>? receivedErrors = null;

        var output = FailureWithValue().Match(
            onSuccess: _ => "should-not-run",
            onFailure: (errors, value) =>
            {
                receivedErrors = errors;
                received = value;
                return "fail";
            });

        output.ShouldBe("fail");
        received.ShouldBe(new Dto("carried"));
        receivedErrors.ShouldBe(new[] { "e1", "e2" });
    }

    [Fact]
    public void Match_ValueAware_FailureValueLess_RoutesToOnFailure_ReceivesNullValue()
    {
        Dto? received = new Dto("sentinel");

        var output = FailureValueLess().Match(
            onSuccess: _ => "should-not-run",
            onFailure: (errors, value) =>
            {
                received = value;
                return "fail";
            });

        output.ShouldBe("fail");
        received.ShouldBeNull();
    }

    [Fact]
    public void Match_ValueAware_SuccessWithNull_RoutesToOnFailure_ParityWithMatchAsync()
    {
        // Match routes on IsSuccess (Value non-null) -> success-with-null goes to onFailure with a null value.
        Dto? received = new Dto("sentinel");

        var output = SuccessWithNull().Match(
            onSuccess: _ => "should-not-run",
            onFailure: (errors, value) =>
            {
                received = value;
                return "fail";
            });

        output.ShouldBe("fail");
        received.ShouldBeNull();
    }

    [Fact]
    public async Task Match_ValueAware_RoutingParityWithMatchAsync_AcrossStateMatrix()
    {
        foreach (var factory in new Func<Result<Dto>>[] { Success, FailureWithValue, FailureValueLess, SuccessWithNull })
        {
            var syncBranch = factory().Match(
                onSuccess: _ => "S",
                onFailure: (_, _) => "F");

            var asyncBranch = await Task.FromResult(factory()).MatchAsync(
                onSuccess: _ => "S",
                onFailure: (_, _) => "F");

            syncBranch.ShouldBe(asyncBranch);
        }
    }

    // ---------------------------------------------------------------------
    // 2) value-aware MatchValue<T, TOut> (routes on IsRecoverable, preserves family)
    // ---------------------------------------------------------------------

    [Fact]
    public void MatchValue_ValueAware_Success_RoutesToOnSuccess()
    {
        var output = Success().MatchValue(
            onSuccess: v => $"ok:{v.Code}",
            onFailure: (errors, value) => "should-not-run");

        output.ShouldBe("ok:ok");
    }

    [Fact]
    public void MatchValue_ValueAware_FailureWithValue_RoutesToOnFailure_ReceivesCarriedValue()
    {
        Dto? received = new Dto("sentinel");
        IEnumerable<string>? receivedErrors = null;

        var output = FailureWithValue().MatchValue(
            onSuccess: _ => "should-not-run",
            onFailure: (errors, value) =>
            {
                receivedErrors = errors;
                received = value;
                return "fail";
            });

        output.ShouldBe("fail");
        received.ShouldBe(new Dto("carried"));
        receivedErrors.ShouldBe(new[] { "e1", "e2" });
    }

    [Fact]
    public void MatchValue_ValueAware_FailureValueLess_RoutesToOnFailure_ReceivesNullValue()
    {
        Dto? received = new Dto("sentinel");

        var output = FailureValueLess().MatchValue(
            onSuccess: _ => "should-not-run",
            onFailure: (errors, value) =>
            {
                received = value;
                return "fail";
            });

        output.ShouldBe("fail");
        received.ShouldBeNull();
    }

    [Fact]
    public void MatchValue_ValueAware_SuccessWithNull_RoutesToOnSuccess_PreservesIsRecoverableFamily()
    {
        // MatchValue routes on IsRecoverable (NOT IsSuccess): success-with-null goes to onSuccess, unlike Match.
        var invokedSuccess = false;
        Dto? received = new Dto("sentinel");

        var output = SuccessWithNull().MatchValue(
            onSuccess: v => { invokedSuccess = true; received = v; return "success"; },
            onFailure: (_, _) => "fail");

        output.ShouldBe("success");
        invokedSuccess.ShouldBeTrue();
        received.ShouldBeNull();
    }

    [Fact]
    public void MatchValue_ValueAware_RoutingDivergesFromMatch_OnSuccessWithNull()
    {
        // Documents the deliberate difference between the two families on success-with-null.
        var matchBranch = SuccessWithNull().Match(onSuccess: _ => "S", onFailure: (_, _) => "F");
        var matchValueBranch = SuccessWithNull().MatchValue(onSuccess: _ => "S", onFailure: (_, _) => "F");

        matchBranch.ShouldBe("F");
        matchValueBranch.ShouldBe("S");
    }

    [Fact]
    public void MatchValue_ValueAware_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((Result<Dto>)null!).MatchValue(v => v.Code, (errors, value) => "fail"));
        Should.Throw<ArgumentNullException>(() =>
            Success().MatchValue((Func<Dto, string>)null!, (errors, value) => "fail"));
        Should.Throw<ArgumentNullException>(() =>
            Success().MatchValue(v => v.Code, (Func<IEnumerable<string>, Dto?, string>)null!));
    }

    // ---------------------------------------------------------------------
    // 3) value-aware TapErrorAsync (both overloads)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task TapErrorAsync_ValueAware_Sync_Failure_Fires_ReceivesErrorsAndCarriedValue_ReturnsOriginal()
    {
        var input = FailureWithValue();
        IEnumerable<string>? receivedErrors = null;
        Dto? received = new Dto("sentinel");

        var output = await input.TapErrorAsync((errors, value) =>
        {
            receivedErrors = errors;
            received = value;
            return Task.CompletedTask;
        });

        receivedErrors.ShouldBe(new[] { "e1", "e2" });
        received.ShouldBe(new Dto("carried"));
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public async Task TapErrorAsync_ValueAware_Sync_FailureValueLess_ReceivesNullValue()
    {
        Dto? received = new Dto("sentinel");

        await FailureValueLess().TapErrorAsync((errors, value) =>
        {
            received = value;
            return Task.CompletedTask;
        });

        received.ShouldBeNull();
    }

    [Fact]
    public async Task TapErrorAsync_ValueAware_Sync_Success_DoesNotFire_ReturnsOriginal()
    {
        var input = Success();
        var invocations = 0;

        var output = await input.TapErrorAsync((errors, value) => { invocations++; return Task.CompletedTask; });

        invocations.ShouldBe(0);
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public async Task TapErrorAsync_ValueAware_Sync_DoesNotSwallowExceptions()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            FailureWithValue().TapErrorAsync((errors, value) => throw new InvalidOperationException("boom")));
    }

    [Fact]
    public async Task TapErrorAsync_ValueAware_Sync_NullArguments_Throw()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            ((Result<Dto>)null!).TapErrorAsync((errors, value) => Task.CompletedTask));
        await Should.ThrowAsync<ArgumentNullException>(
            FailureWithValue().TapErrorAsync((Func<IEnumerable<string>, Dto?, Task>)null!));
    }

    [Fact]
    public async Task TapErrorAsync_ValueAware_AsyncSource_Failure_Fires_ReceivesCarriedValue_ReturnsOriginal()
    {
        var input = FailureWithValue();
        Dto? received = new Dto("sentinel");

        var output = await Task.FromResult(input).TapErrorAsync((errors, value) =>
        {
            received = value;
            return Task.CompletedTask;
        });

        received.ShouldBe(new Dto("carried"));
        ReferenceEquals(output, input).ShouldBeTrue();
    }

    [Fact]
    public async Task TapErrorAsync_ValueAware_AsyncSource_Success_DoesNotFire()
    {
        var invocations = 0;

        await Task.FromResult(Success()).TapErrorAsync((errors, value) => { invocations++; return Task.CompletedTask; });

        invocations.ShouldBe(0);
    }

    [Fact]
    public async Task TapErrorAsync_ValueAware_AsyncSource_DoesNotSwallowExceptions()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            Task.FromResult(FailureWithValue()).TapErrorAsync((errors, value) => throw new InvalidOperationException("boom")));
    }

    [Fact]
    public async Task TapErrorAsync_ValueAware_AsyncSource_NullArguments_Throw()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            ((Task<Result<Dto>>)null!).TapErrorAsync((errors, value) => Task.CompletedTask));
        await Should.ThrowAsync<ArgumentNullException>(
            Task.FromResult(FailureWithValue()).TapErrorAsync((Func<IEnumerable<string>, Dto?, Task>)null!));
    }

    [Fact]
    public async Task TapErrorAsync_ValueAware_AsyncSource_ChainedAfterStep_SeesShortCircuitedFailureValue()
    {
        // Upstream/short-circuit behavior: a mid-chain failure carrying a value flows into the tap unchanged.
        Dto? received = new Dto("sentinel");

        var output = await Task.FromResult(Result<Dto>.Success(new Dto("start")))
            .ThenStep(_ => Result<Dto>.WithFailure(new[] { "step failed" }, new Dto("diag")))
            .TapErrorAsync((errors, value) => { received = value; return Task.CompletedTask; });

        received.ShouldBe(new Dto("diag"));
        output.IsFailure.ShouldBeTrue();
        output.Value.ShouldBe(new Dto("diag"));
    }

    // ---------------------------------------------------------------------
    // Non-regression: existing arity-1 overloads still resolve unambiguously
    // ---------------------------------------------------------------------

    [Fact]
    public void Arity1_Match_StillResolves_ToOriginalOverload_ReturningResultOfTOut()
    {
        // arity-1 onFailure binds to the pre-existing Match<TOut> that wraps in Result<TOut>.
        Result<string> output = FailureValueLess().Match(
            onSuccess: v => v.Code,
            onFailure: errors => "fail");

        output.IsSuccess.ShouldBeTrue();
        output.Value.ShouldBe("fail");
    }

    [Fact]
    public void Arity1_MatchValue_StillResolves_ToOriginalOverload_ReturningPlainValue()
    {
        var output = FailureValueLess().MatchValue(
            onSuccess: v => v.Code,
            onFailure: errors => "fail");

        output.ShouldBe("fail");
    }

    [Fact]
    public async Task Arity1_TapErrorAsync_StillResolves_ToOriginalOverload()
    {
        IEnumerable<string>? receivedErrors = null;

        await Task.FromResult(FailureWithValue()).TapErrorAsync(errors =>
        {
            receivedErrors = errors;
            return Task.CompletedTask;
        });

        receivedErrors.ShouldBe(new[] { "e1", "e2" });
    }
}
