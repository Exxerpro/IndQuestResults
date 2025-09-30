namespace IndQuestResults.Tests.Unit.Operations;

public class ResultValueExtensionsTests
{
    [Fact]
    public void ValueOr_ReturnsDefault_OnFailureOrNull()
    {
        var fail = Result<string>.WithFailure("err");
        fail.ValueOr("x").ShouldBe("x");

        var okNull = new Result<string?>(true, errors: null, value: null);
        okNull.ValueOr("y").ShouldBe("y");
    }

    [Fact]
    public void OrElse_ReturnsFallback_OnFailure()
    {
        var primary = Result<int>.WithFailure("e");
        var alt = Result<int>.Success(7);
        var r = primary.OrElse(alt);
        r.IsSuccess.ShouldBeTrue();
        r.Value.ShouldBe(7);
    }

    [Fact]
    public void MatchValue_ReturnsPlainValues()
    {
        var ok = Result<int>.Success(3);
        var val1 = ok.MatchValue(v => v * 2, _ => -1);
        val1.ShouldBe(6);

        var err = Result<int>.WithFailure("x");
        var val2 = err.MatchValue(v => v * 2, errs => -2);
        val2.ShouldBe(-2);
    }

    [Fact]
    public void OnBoth_BranchAware_InvokesCorrectAction()
    {
        var ok = Result<string>.Success("v");
        var calledOk = false;
        var calledErr = false;
        ok.OnBoth(_ => calledOk = true, _ => calledErr = true);
        calledOk.ShouldBeTrue();
        calledErr.ShouldBeFalse();

        calledOk = false;
        calledErr = false;
        var err = Result<string>.WithFailure("e");
        err.OnBoth(_ => calledOk = true, _ => calledErr = true);
        calledOk.ShouldBeFalse();
        calledErr.ShouldBeTrue();
    }
}

