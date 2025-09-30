namespace IndQuestResults.Tests.Unit.Operations;

public class ResultErrorExtensionsTests
{
    [Fact]
    public void MapError_NonGeneric_TransformsErrors_OnFailure()
    {
        var r = Result.WithFailure(new[] { "e1", "e2" });
        var transformed = r.MapError(errs => errs.Select(e => $"X:{e}"));
        transformed.IsFailure.ShouldBeTrue();
        transformed.Errors.ShouldBe(new[] { "X:e1", "X:e2" });
    }

    [Fact]
    public void MapError_NonGeneric_NoChange_OnSuccess()
    {
        var r = Result.Success();
        var transformed = r.MapError(errs => errs.Select(e => $"X:{e}"));
        transformed.ShouldBeSameAs(r);
        transformed.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void MapError_Generic_TransformsErrors_OnFailure()
    {
        var r = Result<int>.WithFailure(new[] { "e" }, 1);
        var transformed = r.MapError(errs => errs.Select(e => e + "!"));
        transformed.IsFailure.ShouldBeTrue();
        transformed.Errors.ShouldBe(new[] { "e!" });
        transformed.Value.ShouldBe(1);
    }

    [Fact]
    public void TapError_Invokes_OnFailure_Only()
    {
        var called = false;
        Result.WithFailure("e").TapError(_ => called = true);
        called.ShouldBeTrue();
        called = false;
        Result.Success().TapError(_ => called = true);
        called.ShouldBeFalse();
    }

    [Fact]
    public void Recover_ErrorAware_NonGeneric_UsesErrors()
    {
        var r = Result.WithFailure(new[] { "e1", "e2" });
        var recovered = r.Recover(errs =>
        {
            errs.ShouldBe(new[] { "e1", "e2" });
            return Result.Success();
        });
        recovered.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Recover_ErrorAware_Generic_UsesErrors()
    {
        var r = Result<string>.WithFailure(new[] { "e" }, value: "keep");
        var recovered = r.Recover(errs =>
        {
            errs.ShouldContain("e");
            return Result<string>.Success("ok");
        });
        recovered.IsSuccess.ShouldBeTrue();
        recovered.Value.ShouldBe("ok");
    }
}

