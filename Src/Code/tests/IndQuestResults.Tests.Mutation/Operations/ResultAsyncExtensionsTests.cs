namespace IndQuestResults.Tests.Mutation.Operations;

/// <summary>
/// Tests for async extension methods defined on Result tasks in ResultExtensions.
/// Covers chaining, mapping, side effects, validation, combining, switching,
/// recovery, logging, and defaulting behaviors.
/// </summary>
public class ResultAsyncExtensionsTests
{
    [Fact]
    public async Task ThenAsync_Success_ChainsNext()
    {
        var first = Task.FromResult(Result<string>.Success("ok"));
        var next = new Func<string, Task<Result<int>>>(s => Task.FromResult(Result<int>.Success(s.Length)));

        var res = await first.ThenAsync(next);

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(2);
    }

    [Fact]
    public async Task ThenAsync_Failure_PropagatesErrors()
    {
        var first = Task.FromResult(Result<string>.WithFailure("boom"));
        var res = await first.ThenAsync(_ => Task.FromResult(Result<int>.Success(1)));

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("boom");
    }

    [Fact]
    public async Task ThenMap_Success_MapsValue()
    {
        var first = Task.FromResult(Result<string>.Success("abc"));

        var res = await first.ThenMap(s => s.ToUpperInvariant());

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe("ABC");
    }

    [Fact]
    public async Task ThenMap_Failure_PropagatesErrors()
    {
        var first = Task.FromResult(Result<string>.WithFailure("x"));

        var res = await first.ThenMap(s => s);

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("x");
    }

    [Fact]
    public async Task ThenTap_Success_ExecutesSideEffect()
    {
        var called = false;
        var res = await Task.FromResult(Result<int>.Success(5))
            .ThenTap(async v => { await Task.Yield(); called = v == 5; });

        called.ShouldBeTrue();
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(5);
    }

    [Fact]
    public async Task ThenDo_Success_ExecutesSideEffect()
    {
        var called = false;
        var res = await Task.FromResult(Result<int>.Success(7))
            .ThenDo(v => called = v == 7);

        called.ShouldBeTrue();
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(7);
    }

    [Fact]
    public async Task ThenValidate_FailingValidator_ReturnsFailureWithOriginalValue()
    {
        var res = await Task.FromResult(Result<string>.Success("data"))
            .ThenValidate(v => Result.WithFailure($"bad:{v}"));

        res.IsFailure.ShouldBeTrue();
        res.Value.ShouldBe("data");
        res.Error.ShouldNotBeNull();
        res.Error!.ShouldContain("bad:data");
    }

    [Fact]
    public async Task ThenValidateAsync_FailingValidator_ReturnsFailure()
    {
        var res = await Task.FromResult(Result<int>.Success(10))
            .ThenValidateAsync(v => Task.FromResult(Result.WithFailure($"e:{v}")));

        res.IsFailure.ShouldBeTrue();
        res.Value.ShouldBe(10);
        res.Error.ShouldNotBeNull();
        res.Error!.ShouldContain("e:10");
    }

    [Fact]
    public async Task ThenEnsure_FalsePredicate_ReturnsFailure()
    {
        var res = await Task.FromResult(Result<int>.Success(1))
            .ThenEnsure(v => v > 10, "too small");

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe("too small");
    }

    [Fact]
    public async Task CombineAsync_TwoResults_AggregatesErrors()
    {
        var r1 = Task.FromResult(Result<int>.WithFailure("e1"));
        var r2 = Task.FromResult(Result<string>.WithFailure("e2"));

        var res = await r1.CombineAsync(r2);

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("e1");
        res.Errors.ShouldContain("e2");
    }

    [Fact]
    public async Task CombineAsync_ThreeResults_AllSuccess_ReturnsTuple()
    {
        var r1 = Task.FromResult(Result<int>.Success(1));
        var r2 = Task.FromResult(Result<string>.Success("x"));
        var r3 = Task.FromResult(Result<bool>.Success(true));

        var res = await r1.CombineAsync(r2, r3);

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe((1, "x", true));
    }

    [Fact]
    public async Task ThenSwitch_RoutesByCondition()
    {
        var calledTrue = false;
        var calledFalse = false;
        var res = await Task.FromResult(Result<int>.Success(2))
            .ThenSwitch(v => v % 2 == 0,
                onTrue: v => { calledTrue = true; return Task.FromResult(Result<int>.Success(v)); },
                onFalse: v => { calledFalse = true; return Task.FromResult(Result<int>.WithFailure("odd")); });

        calledTrue.ShouldBeTrue();
        calledFalse.ShouldBeFalse();
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(2);
    }

    [Fact]
    public async Task ThenRecover_OnFailure_InvokesRecovery()
    {
        var recovered = await Task.FromResult(Result<string>.WithFailure("e"))
            .ThenRecover(errs => Task.FromResult(Result<string>.Success(string.Join(";", errs))));

        recovered.IsSuccess.ShouldBeTrue();
        recovered.Value.ShouldBe("e");
    }

    [Fact]
    public async Task ThenLogErrors_OnFailure_LogsOnce()
    {
        var logged = new List<string>();
        var res = await Task.FromResult(Result<int>.WithFailure(new[] { "a", "b" }))
            .ThenLogErrors(errs => logged.AddRange(errs));

        res.IsFailure.ShouldBeTrue();
        logged.ShouldBe(["a", "b"]);
    }

    [Fact]
    public async Task WhenAllAsync_AllSuccess_ExecutesOnSuccess()
    {
        var called = false;
        var tasks = new[]
        {
            Task.FromResult(Result<int>.Success(1)),
            Task.FromResult(Result<int>.Success(2)),
            Task.FromResult(Result<int>.Success(3)),
        };

        var res = await ResultExtensions.WhenAllAsync(tasks, values => { called = values.Sum() == 6; return Task.CompletedTask; });

        called.ShouldBeTrue();
        res.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ToFailureOf_FailedSource_PreservesErrors()
    {
        var failed = Result<string>.WithFailure(new[] { "x", "y" });
        var converted = failed.ToFailureOf<string, int>();

        converted.IsFailure.ShouldBeTrue();
        converted.Errors.ShouldContain("x");
        converted.Errors.ShouldContain("y");
    }

    [Fact]
    public async Task ThenAsyncCancellable_Cancelled_EarlyReturnsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var res = await Task.FromResult(Result<int>.Success(1))
            .ThenAsyncCancellable<int, string>(
                (v, ct) => Task.FromResult(Result<string>.Success(v.ToString())),
                cts.Token);

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain(ResultErrors.OperationCancelled);
    }

    [Fact]
    public async Task DefaultIfFailure_OnFailure_ReturnsDefault()
    {
        var value = await Task.FromResult(Result<string>.WithFailure("e")).DefaultIfFailure("def");
        value.ShouldBe("def");
    }
}
