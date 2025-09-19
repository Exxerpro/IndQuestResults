using IndQuestResults;
using IndQuestResults.Operations;

namespace IndQuestResults.Tests.Unit.Operations;

public class ResultErrorExtensionsTests
{
    [Fact]
    public void MapError_OnFailure_TransformsErrors()
    {
        var r = Result.WithFailure(new[] { "a", "b" });
        var mapped = r.MapError(errs => errs.Select(e => e.ToUpperInvariant()));

        mapped.IsFailure.ShouldBeTrue();
        mapped.Errors.ShouldContain("A");
        mapped.Errors.ShouldContain("B");
    }

    [Fact]
    public void MapError_OnSuccess_NoChange()
    {
        var r = Result.Success();
        var mapped = r.MapError(errs => new[] { "X" });
        mapped.ShouldBeSameAs(r);
        mapped.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Recover_WithErrors_RecoversGeneric()
    {
        var r = Result<int>.WithFailure(new[] { "boom" });
        var recovered = r.Recover(errs => Result<int>.Success(5));
        recovered.IsSuccess.ShouldBeTrue();
        recovered.Value.ShouldBe(5);
    }

    [Fact]
    public void TapError_InvokesOnFailure_GenericAndNonGeneric()
    {
        var ng = Result.WithFailure("e1");
        var g = Result<string>.WithFailure("e2");
        var c1 = 0;
        var c2 = 0;
        ng.TapError(_ => c1++);
        g.TapError(_ => c2++);
        c1.ShouldBe(1);
        c2.ShouldBe(1);
    }
}

