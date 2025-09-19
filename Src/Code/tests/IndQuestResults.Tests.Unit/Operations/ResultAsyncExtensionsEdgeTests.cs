namespace IndQuestResults.Tests.Unit.Operations;

public class ResultAsyncExtensionsEdgeTests
{
    [Fact]
    public async Task ThenAsync_WhenNextThrows_PropagatesException()
    {
        var first = Task.FromResult(Result<int>.Success(1));
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await first.ThenAsync<int, int>(_ => throw new InvalidOperationException("boom")));
    }

    [Fact]
    public async Task ThenMap_Failure_DoesNotInvokeMapper()
    {
        var first = Task.FromResult(Result<string>.WithFailure("e"));
        var invoked = false;
        var res = await first.ThenMap(_ => { invoked = true; return 0; });
        invoked.ShouldBeFalse();
        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("e");
    }

    [Fact]
    public async Task ThenTap_ActionThrows_Propagates()
    {
        var first = Task.FromResult(Result<string>.Success("x"));
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await first.ThenTap(_ => throw new InvalidOperationException("tap")));
    }

    [Fact]
    public async Task ThenDo_ActionThrows_Propagates()
    {
        var first = Task.FromResult(Result<int>.Success(2));
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await first.ThenDo(_ => throw new InvalidOperationException("do")));
    }

    [Fact]
    public async Task ThenValidate_Success_ReturnsOriginal()
    {
        var res = await Task.FromResult(Result<int>.Success(5))
            .ThenValidate(_ => Result.Success());
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(5);
    }

    [Fact]
    public async Task ThenValidate_ValidatorThrows_Propagates()
    {
        var first = Task.FromResult(Result<int>.Success(1));
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await first.ThenValidate(_ => throw new InvalidOperationException("val")));
    }

    [Fact]
    public async Task ThenEnsure_True_ReturnsOriginal_AndFailureShortCircuits()
    {
        var success = await Task.FromResult(Result<string>.Success("ok")).ThenEnsure(_ => true, "err");
        success.IsSuccess.ShouldBeTrue();
        success.Value.ShouldBe("ok");

        var failure = await Task.FromResult(Result<string>.WithFailure("e")).ThenEnsure(_ => false, "x");
        failure.IsFailure.ShouldBeTrue();
        failure.Errors.ShouldContain("e");
    }

    [Fact]
    public async Task ThenSwitch_OnFalse_Path_And_OnFailure_NotInvoked()
    {
        var trueCalled = false; var falseCalled = false;
        var res = await Task.FromResult(Result<int>.Success(3))
            .ThenSwitch(v => v % 2 == 0,
                onTrue: v => { trueCalled = true; return Task.FromResult(Result<int>.Success(v)); },
                onFalse: v => { falseCalled = true; return Task.FromResult(Result<int>.WithFailure("odd")); });
        trueCalled.ShouldBeFalse();
        falseCalled.ShouldBeTrue();
        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("odd");

        trueCalled = false; falseCalled = false;
        var failed = await Task.FromResult(Result<int>.WithFailure("e"))
            .ThenSwitch(_ => true,
                onTrue: v => { trueCalled = true; return Task.FromResult(Result<int>.Success(v)); },
                onFalse: v => { falseCalled = true; return Task.FromResult(Result<int>.Success(v)); });
        failed.IsFailure.ShouldBeTrue();
        trueCalled.ShouldBeFalse();
        falseCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task ThenRecover_Success_DoesNotCallRecover()
    {
        var called = false;
        var res = await Task.FromResult(Result<int>.Success(1))
            .ThenRecover(_ => { called = true; return Task.FromResult(Result<int>.Success(2)); });
        called.ShouldBeFalse();
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(1);
    }

    [Fact]
    public async Task ThenLogErrors_Success_DoesNotLog()
    {
        var logged = new List<string>();
        var res = await Task.FromResult(Result<string>.Success("v"))
            .ThenLogErrors(errs => logged.AddRange(errs));
        res.IsSuccess.ShouldBeTrue();
        logged.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenAllAsync_WithFailure_Aggregates_And_SkipsOnSuccess()
    {
        var successCalled = false;
        var res = await ResultExtensions.WhenAllAsync(
            new[] { Task.FromResult(Result<int>.Success(1)), Task.FromResult(Result<int>.WithFailure("e")) },
            values => { successCalled = true; return Task.CompletedTask; });
        successCalled.ShouldBeFalse();
        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("e");
    }

    [Fact]
    public void ToFailureOf_OnSuccess_Throws()
    {
        var ok = Result<string>.Success("x");
        Should.Throw<InvalidOperationException>(() => ok.ToFailureOf<string, int>());
    }

    [Fact]
    public async Task ThenAsyncCancellable_Success_Path()
    {
        using var cts = new CancellationTokenSource();
        var res = await Task.FromResult(Result<int>.Success(3))
            .ThenAsyncCancellable<int, string>((v, ct) => Task.FromResult(Result<string>.Success(v.ToString())), cts.Token);
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe("3");
    }

    [Fact]
    public async Task DefaultIfFailure_Success_ReturnsValue()
    {
        var value = await Task.FromResult(Result<string>.Success("ok")).DefaultIfFailure("def");
        value.ShouldBe("ok");
    }
}

