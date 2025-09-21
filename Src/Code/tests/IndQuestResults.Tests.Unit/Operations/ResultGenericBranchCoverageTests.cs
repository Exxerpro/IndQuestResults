namespace IndQuestResults.Tests.Unit.Operations;

public class ResultGenericBranchCoverageTests
{
    [Fact]
    public void Tap_OnFailure_DoesNotInvokeAction()
    {
        var executed = false;
        var r = Result<string>.WithFailure("e");
        var r2 = r.Tap(_ => executed = true);
        executed.ShouldBeFalse();
        r2.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void OnSuccess_NullReferenceValue_InvokesAction()
    {
        // For reference types, OnSuccess executes even when Value is null
        var executed = false;
        var r = Result<string>.Success(null!);
        r.OnSuccess(_ => executed = true);
        executed.ShouldBeTrue();
    }

    [Fact]
    public void Combine_NoArgs_ReturnsThis()
    {
        var r = Result<int>.WithFailure("e");
        var combined = r.Combine();
        combined.ShouldBeSameAs(r);
        combined.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void WithFailure_ValueFirst_NullErrors_Defaults()
    {
        IEnumerable<string>? errs = null;
        var r = Result<int>.WithFailure(0, errs);
        r.IsFailure.ShouldBeTrue();
        r.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }
}

