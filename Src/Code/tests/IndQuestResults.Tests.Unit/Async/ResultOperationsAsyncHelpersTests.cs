namespace IndQuestResults.Tests.Unit.Async;

public class ResultOperationsAsyncHelpersTests
{
    [Fact]
    public async Task ThenAsync_Chains_OnSuccess_Propagates_OnFailure()
    {
        var ok = Task.FromResult(Result<int>.Success(2));
        var chained = await ok.ThenAsync(v => Task.FromResult(Result<string>.Success($"{v}!")));
        chained.IsSuccess.ShouldBeTrue();
        chained.Value.ShouldBe("2!");

        var fail = Task.FromResult(Result<int>.WithFailure("e"));
        var chainedFail = await fail.ThenAsync(v => Task.FromResult(Result<string>.Success($"{v}!")));
        chainedFail.IsFailure.ShouldBeTrue();
        chainedFail.Errors.ShouldContain("e");
    }

    [Fact]
    public async Task ThenMap_Maps_OnSuccess_Propagates_OnFailure()
    {
        var ok = Task.FromResult(Result<int>.Success(3));
        var mapped = await ok.ThenMap(v => v * 2);
        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe(6);

        var fail = Task.FromResult(Result<int>.WithFailure("x"));
        var mappedFail = await fail.ThenMap(v => v * 2);
        mappedFail.IsFailure.ShouldBeTrue();
        mappedFail.Errors.ShouldContain("x");
    }

    [Fact]
    public async Task ThenTap_SideEffect_OnSuccess_Only()
    {
        var called = 0;
        var ok = Task.FromResult(Result<int>.Success(4));
        var res = await ok.ThenTap(async v => { await Task.Delay(1); called = v; });
        called.ShouldBe(4);
        res.IsSuccess.ShouldBeTrue();

        called = 0;
        var fail = Task.FromResult(Result<int>.WithFailure("e"));
        var resFail = await fail.ThenTap(_ => { called = 99; return Task.CompletedTask; });
        called.ShouldBe(0);
        resFail.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ThenDo_SynchronousSideEffect_OnSuccess_Only()
    {
        var called = 0;
        var ok = Task.FromResult(Result<int>.Success(5));
        var res = await ok.ThenDo(v => called = v * 2);
        called.ShouldBe(10);
        res.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ThenValidate_And_ThenValidateAsync_Behavior()
    {
        var ok = Task.FromResult(Result<string>.Success("abc"));
        var validated = await ok.ThenValidate(_ => Result.Success());
        validated.IsSuccess.ShouldBeTrue();

        var failed = await ok.ThenValidate(_ => Result.WithFailure("bad"));
        failed.IsFailure.ShouldBeTrue();
        failed.Errors.ShouldContain("bad");

        var validatedAsync = await ok.ThenValidateAsync(_ => Task.FromResult(Result.Success()));
        validatedAsync.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ThenEnsure_ProducesFailure_WhenPredicateFalse()
    {
        var ok = Task.FromResult(Result<int>.Success(10));
        var ensured = await ok.ThenEnsure(v => v > 100, "too small");
        ensured.IsFailure.ShouldBeTrue();
        ensured.Errors.ShouldContain("too small");
    }

    [Fact]
    public async Task ThenSwitch_SelectsBranch_OnCondition()
    {
        var ok = Task.FromResult(Result<int>.Success(2));
        var switched = await ok.ThenSwitch(v => v % 2 == 0,
            onTrue: v => Task.FromResult(Result<int>.Success(v + 10)),
            onFalse: v => Task.FromResult(Result<int>.Success(v + 1)));
        switched.IsSuccess.ShouldBeTrue();
        switched.Value.ShouldBe(12);
    }

    [Fact]
    public async Task ThenRecover_ErrorAware_CallsOnlyOnFailure()
    {
        var ok = Task.FromResult(Result<int>.Success(1));
        var okRes = await ok.ThenRecover(_ => Task.FromResult(Result<int>.Success(9)));
        okRes.IsSuccess.ShouldBeTrue();
        okRes.Value.ShouldBe(1);

        var fail = Task.FromResult(Result<int>.WithFailure("e1"));
        var rec = await fail.ThenRecover(errs => Task.FromResult(Result<int>.Success(errs.Count())));
        rec.IsSuccess.ShouldBeTrue();
        rec.Value.ShouldBe(1);
    }

    [Fact]
    public async Task ThenLogErrors_Logs_OnFailure_Only()
    {
        var logged = new List<string>();
        var ok = Task.FromResult(Result<int>.Success(1));
        _ = await ok.ThenLogErrors(errs => logged.Add(string.Join(",", errs)));
        logged.ShouldBeEmpty();

        var fail = Task.FromResult(Result<int>.WithFailure(new[] { "e1", "e2" }));
        _ = await fail.ThenLogErrors(errs => logged.Add(string.Join(",", errs)));
        logged.ShouldHaveSingleItem();
        logged[0].ShouldBe("e1,e2");
    }

    [Fact]
    public async Task DefaultIfFailure_ReturnsDefault_OnFailureOrNull()
    {
        var okNull = Task.FromResult(new Result<string?>(true, errors: null, value: null));
        var v1 = await okNull.DefaultIfFailure("x");
        v1.ShouldBe("x");

        var fail = Task.FromResult(Result<string>.WithFailure("e"));
        var v2 = await fail.DefaultIfFailure("y");
        v2.ShouldBe("y");
    }

    [Fact]
    public async Task CombineAsync_Tuples_Success_And_Failure()
    {
        var r1 = Task.FromResult(Result<int>.Success(1));
        var r2 = Task.FromResult(Result<string>.Success("a"));
        var both = await r1.CombineAsync(r2);
        both.IsSuccess.ShouldBeTrue();
        both.Value.ShouldBe((1, "a"));

        var r3 = Task.FromResult(Result<int>.WithFailure("e"));
        var bothFail = await r3.CombineAsync(r2);
        bothFail.IsFailure.ShouldBeTrue();
        bothFail.Errors.ShouldContain("e");

        var r4 = Task.FromResult(Result<double>.Success(1.5));
        var triple = await r1.CombineAsync(r2, r4);
        triple.IsSuccess.ShouldBeTrue();
        triple.Value.ShouldBe((1, "a", 1.5));
    }

    [Fact]
    public async Task WhenAllAsync_AggregatesFailures_OrExecutesOnSuccess()
    {
        var ok = new[]
        {
            Task.FromResult(Result<int>.Success(1)),
            Task.FromResult(Result<int>.Success(2))
        };
        var called = false;
        var okRes = await ResultExtensions.WhenAllAsync(ok, async values => { _ = values.Sum(); await Task.Delay(1); called = true; });
        okRes.IsSuccess.ShouldBeTrue();
        called.ShouldBeTrue();

        var mix = new[]
        {
            Task.FromResult(Result<int>.Success(1)),
            Task.FromResult(Result<int>.WithFailure("e"))
        };
        var mixRes = await ResultExtensions.WhenAllAsync(mix, _ => Task.CompletedTask);
        mixRes.IsFailure.ShouldBeTrue();
        mixRes.Errors.ShouldContain("e");
    }

    [Fact]
    public async Task ThenAsyncCancellable_RespectsCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ok = Task.FromResult(Result<int>.Success(1));
        var res = await ok.ThenAsyncCancellable((v, ct) => Task.FromResult(Result<string>.Success(v.ToString())), cts.Token);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultErrors.OperationCancelled);
    }
}

