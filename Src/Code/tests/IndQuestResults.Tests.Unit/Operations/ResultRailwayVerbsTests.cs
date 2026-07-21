namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Tests for the ADR 0004 async railway verbs: <c>Ensure</c>, <c>Then</c> (map/bind, sync and async),
/// and <c>ToResult</c> (nullable-async adapters). Verifies behavior, overload resolution
/// (map vs bind by delegate return shape), railway flattening (no nested results), short-circuiting,
/// and the documented null/failure precedence for <c>ToResult</c>.
/// </summary>
public class ResultRailwayVerbsTests
{
    // -----------------------------------------------------------------------
    // Ensure (async alias over ThenEnsure)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Ensure_PredicatePasses_ReturnsUnchangedSuccess()
    {
        var res = await Task.FromResult(Result<int>.Success(42))
            .Ensure(v => v > 0, "must be positive");

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(42);
    }

    [Fact]
    public async Task Ensure_PredicateFails_ReturnsFailureWithMessage()
    {
        var res = await Task.FromResult(Result<int>.Success(-1))
            .Ensure(v => v > 0, "must be positive");

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe("must be positive");
    }

    [Fact]
    public async Task Ensure_UpstreamFailure_ShortCircuitsAndPreservesErrors()
    {
        var predicateInvoked = false;
        var res = await Task.FromResult(Result<int>.WithFailure("upstream"))
            .Ensure(v => { predicateInvoked = true; return true; }, "must be positive");

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("upstream");
        predicateInvoked.ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // Then — sync receiver Result<TIn>
    // -----------------------------------------------------------------------

    [Fact]
    public void Then_Sync_Map_WrapsValue()
    {
        // Delegate returns a plain value => map overload selected.
        Result<int> res = Result<string>.Success("abc").Then(s => s.Length);

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(3);
    }

    [Fact]
    public void Then_Sync_Bind_FlattensResult_NoNesting()
    {
        // Delegate returns Result<int> => bind overload selected (more specific, preferred).
        // The explicit Result<int> target type compile-asserts there is NO nested Result<Result<int>>.
        Result<int> res = Result<string>.Success("abc").Then(s => Result<int>.Success(s.Length));

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(3);
        res.Value.ShouldBeOfType<int>();
    }

    [Fact]
    public void Then_Sync_Bind_PropagatesInnerFailure()
    {
        Result<int> res = Result<string>.Success("abc").Then(_ => Result<int>.WithFailure("inner"));

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("inner");
    }

    [Fact]
    public void Then_Sync_Map_UpstreamFailure_ShortCircuits()
    {
        var invoked = false;
        Result<int> res = Result<string>.WithFailure("upstream").Then(s => { invoked = true; return s.Length; });

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("upstream");
        invoked.ShouldBeFalse();
    }

    [Fact]
    public void Then_Sync_Bind_UpstreamFailure_ShortCircuits()
    {
        var invoked = false;
        Result<int> res = Result<string>.WithFailure("upstream")
            .Then(s => { invoked = true; return Result<int>.Success(s.Length); });

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("upstream");
        invoked.ShouldBeFalse();
    }

    // -----------------------------------------------------------------------
    // Then — async receiver Task<Result<TIn>>
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Then_Async_Map_WrapsValue()
    {
        Result<int> res = await Task.FromResult(Result<string>.Success("hello")).Then(s => s.Length);

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(5);
    }

    [Fact]
    public async Task Then_Async_SyncResultBind_FlattensResult_NoNesting()
    {
        // Delegate returns Result<int> (not a Task) => sync-Result bind overload selected and flattened.
        Result<int> res = await Task.FromResult(Result<string>.Success("hello"))
            .Then(s => Result<int>.Success(s.Length));

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(5);
        res.Value.ShouldBeOfType<int>();
    }

    [Fact]
    public async Task Then_Async_AsyncResultBind_FlattensResult_NoNesting()
    {
        // Delegate returns Task<Result<int>> => async-bind overload selected and flattened.
        Result<int> res = await Task.FromResult(Result<string>.Success("hello"))
            .Then(s => Task.FromResult(Result<int>.Success(s.Length)));

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(5);
        res.Value.ShouldBeOfType<int>();
    }

    [Fact]
    public async Task Then_Async_Map_UpstreamFailure_ShortCircuits()
    {
        var invoked = false;
        Result<int> res = await Task.FromResult(Result<string>.WithFailure("upstream"))
            .Then(s => { invoked = true; return s.Length; });

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("upstream");
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task Then_Async_SyncResultBind_PropagatesInnerFailure()
    {
        Result<int> res = await Task.FromResult(Result<string>.Success("hi"))
            .Then(_ => Result<int>.WithFailure("inner"));

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("inner");
    }

    [Fact]
    public async Task Then_Async_AsyncResultBind_PropagatesInnerFailure()
    {
        Result<int> res = await Task.FromResult(Result<string>.Success("hi"))
            .Then(_ => Task.FromResult(Result<int>.WithFailure("inner")));

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("inner");
    }

    // -----------------------------------------------------------------------
    // Overload-resolution lock tests: a value-returning delegate selects map;
    // a Result-returning delegate selects bind. These lock the polymorphism decision.
    // -----------------------------------------------------------------------

    /// <summary>Method group returning a plain value (DTO-style projection) must bind as a map.</summary>
    private static int ProjectLength(string s) => s.Length;

    /// <summary>Method group returning a Result must bind as a flatten.</summary>
    private static Result<int> WrapLength(string s) => Result<int>.Success(s.Length);

    [Fact]
    public void OverloadResolution_Sync_ValueReturningMethodGroup_SelectsMap()
    {
        // If map were not selected, TOut would be inferred differently; the explicit
        // Result<int> target type proves a single (un-nested) Result<int> is produced.
        Result<int> res = Result<string>.Success("abcd").Then(ProjectLength);

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(4);
    }

    [Fact]
    public void OverloadResolution_Sync_ResultReturningMethodGroup_SelectsBind()
    {
        // Bind (preferred, more-specific) selected: result is a flattened Result<int>, not Result<Result<int>>.
        Result<int> res = Result<string>.Success("abcd").Then(WrapLength);

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(4);
    }

    [Fact]
    public async Task OverloadResolution_Async_ValueReturningMethodGroup_SelectsMap()
    {
        Result<int> res = await Task.FromResult(Result<string>.Success("abcd")).Then(ProjectLength);

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(4);
    }

    [Fact]
    public async Task OverloadResolution_Async_ResultReturningMethodGroup_SelectsBind()
    {
        Result<int> res = await Task.FromResult(Result<string>.Success("abcd")).Then(WrapLength);

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(4);
    }

    // -----------------------------------------------------------------------
    // ToResult — Task<T?> receiver
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ToResult_NullableTask_NonNull_ReturnsSuccess()
    {
        Task<string?> task = Task.FromResult<string?>("value");

        var res = await task.ToResult("not found");

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe("value");
    }

    [Fact]
    public async Task ToResult_NullableTask_Null_ReturnsFailureWithMessage()
    {
        Task<string?> task = Task.FromResult<string?>(null);

        var res = await task.ToResult("not found");

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe("not found");
    }

    // -----------------------------------------------------------------------
    // ToResult — Task<Result<T>> receiver (failure precedence)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ToResult_ResultTask_ExistingFailure_PropagatesPreservingErrors()
    {
        var task = Task.FromResult(Result<string>.WithFailure(new[] { "domain-error-1", "domain-error-2" }));

        var res = await task.ToResult("not found");

        res.IsFailure.ShouldBeTrue();
        // Existing failure wins over the generic "not found" message; all errors preserved.
        res.Errors.ShouldContain("domain-error-1");
        res.Errors.ShouldContain("domain-error-2");
        res.Errors.ShouldNotContain("not found");
    }

    [Fact]
    public async Task ToResult_ResultTask_SuccessButNull_ReturnsFailureWithMessage()
    {
        // A success carrying a null value becomes failure(errorMessage).
        var task = Task.FromResult(new Result<string>(true, errors: null, value: null));

        var res = await task.ToResult("not found");

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe("not found");
    }

    [Fact]
    public async Task ToResult_ResultTask_SuccessNonNull_ReturnsSuccess()
    {
        var task = Task.FromResult(Result<string>.Success("value"));

        var res = await task.ToResult("not found");

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe("value");
    }

    // -----------------------------------------------------------------------
    // Result.Success<T>(T) — chain-starting factory on the non-generic Result.
    // Delegates to Result<T>.Success(T); see ADR 0004 (4th additive method).
    // -----------------------------------------------------------------------

    [Fact]
    public void Success_Generic_ReturnsSuccessCarryingValue()
    {
        // The non-generic Result exposes a one-arg generic factory that produces Result<T>.
        Result<int> res = Result.Success(42);

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(42);
    }

    [Fact]
    public void Success_Generic_ReferenceType_ReturnsSuccessCarryingValue()
    {
        Result<string> res = Result.Success("payload");

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe("payload");
    }

    [Fact]
    public void Success_Generic_ComposesAsChainStart()
    {
        // The whole point: Result.Success(request) STARTS a railway chain that
        // the consumer (IndTrace.Application) continues with ValidateNotNull/Then/etc.
        var request = "request";

        Result<int> res = Result.Success(request)
            .ValidateNotNull(nameof(request))
            .Then(r => r.Length);

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(request.Length);
    }

    // -----------------------------------------------------------------------
    // ValidateNotNull<T>(selector) — chainable, value-preserving selector overload.
    // Restores the IndTrace chain-start: Result.Success(cmd).ValidateNotNull(c => (c, nameof(cmd))).
    // See ADR 0004. Complements (does not replace) the non-chainable params overload.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ValidateNotNullSelector_SuccessNonNull_PassesThroughUnchanged()
    {
        var cmd = "command";
        Result<string> source = Result.Success(cmd);

        Result<string> res = await source.ValidateNotNull(c => (c, nameof(cmd)));

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(cmd);
        // The original value is preserved exactly (same reference for a class).
        res.Value.ShouldBeSameAs(cmd);
    }

    [Fact]
    public async Task ValidateNotNullSelector_SuccessNullProjectedValue_ReturnsFailureCarryingParameterName()
    {
        // Success carrying a non-null value, but the selector projects a null inner value.
        Result<string> source = Result.Success("command");

        Result<string> res = await source.ValidateNotNull(_ => ((object?)null, "cmd"));

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldContain("cmd");
    }

    [Fact]
    public async Task ValidateNotNullSelector_UpstreamFailure_ShortCircuitsAndPreservesErrors()
    {
        var selectorInvoked = false;
        Result<string> source = Result<string>.WithFailure("upstream");

        Result<string> res = await source.ValidateNotNull(c =>
        {
            selectorInvoked = true;
            return (c, nameof(c));
        });

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("upstream");
        selectorInvoked.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ValidateNotNullSelector_EmptyOrNullParameterName_ReturnsCannotBeNullOrEmptyFailure(string? parameterName)
    {
        Result<string> source = Result.Success("command");

        Result<string> res = await source.ValidateNotNull(c => ((object?)c, parameterName!));

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe("Parameter name cannot be null or empty.");
    }

    [Fact]
    public async Task ValidateNotNullSelector_ChainsIntoTaskReceiverVerb_Succeeds()
    {
        // Regression: the selector overload returns Task<Result<T>> so the chain-start flows
        // directly into a Task-receiver verb (ThenMap) without breaking. This is the motivating fix.
        var x = 42;

        Result<string> res = await Result.Success(x)
            .ValidateNotNull(v => (v, "x"))
            .ThenMap(v => v.ToString());

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe("42");
    }

    // -----------------------------------------------------------------------
    // RequireValue — Task<Result<T?>> receiver (nullable-value adapter, fail-on-null).
    // Adapts what IndTrace repositories return (Result<T?> from FirstOrDefaultAsync /
    // GetByIdAsync) into a non-null Result<T>. Distinctly named (NOT a ToResult overload)
    // because nullable reference annotations are erased in metadata (CS0111). See ADR 0004.
    // -----------------------------------------------------------------------

    /// <summary>A small reference type standing in for an IndTrace repository entity.</summary>
    private sealed class Foo
    {
        public override string ToString() => "foo";
    }

    [Fact]
    public async Task RequireValue_SuccessNonNull_ReturnsSuccessCarryingSameValue()
    {
        var foo = new Foo();
        var task = Task.FromResult(Result<Foo?>.Success(foo));

        Result<Foo> res = await task.RequireValue("not found");

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBeSameAs(foo);
    }

    [Fact]
    public async Task RequireValue_SuccessNull_ReturnsFailureWithMessage()
    {
        // A success carrying a null reference becomes failure(errorMessage).
        var task = Task.FromResult(Result<Foo?>.Success(null));

        Result<Foo> res = await task.RequireValue("not found");

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe("not found");
    }

    [Fact]
    public async Task RequireValue_UpstreamFailure_PropagatesErrorsAndIgnoresMessage()
    {
        var task = Task.FromResult(Result<Foo?>.WithFailure(new[] { "domain-error-1", "domain-error-2" }));

        Result<Foo> res = await task.RequireValue("not found");

        res.IsFailure.ShouldBeTrue();
        // Existing failure wins over the generic message; all upstream errors preserved, message NOT used.
        res.Errors.ShouldContain("domain-error-1");
        res.Errors.ShouldContain("domain-error-2");
        res.Errors.ShouldNotContain("not found");
    }

    [Fact]
    public async Task RequireValue_ChainsIntoTaskReceiverVerb_Succeeds()
    {
        // Bridging proof (the IndTrace usage shape): RequireValue adapts Result<Foo?> into a
        // non-null Result<Foo> Task that flows directly into the async, Task-receiver verbs (ThenMap).
        var foo = new Foo();

        Result<string> res = await Task.FromResult(Result<Foo?>.Success(foo))
            .RequireValue("x")
            .ThenMap(f => f.ToString());

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe("foo");
    }
}
