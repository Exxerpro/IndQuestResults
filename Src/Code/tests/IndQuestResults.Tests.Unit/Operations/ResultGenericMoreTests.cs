namespace IndQuestResults.Tests.Unit.Operations;

public class ResultGenericMoreTests
{
    [Fact]
    public void Recover_OnSuccess_ReturnsOriginal_OnFailure_UsesRecovery()
    {
        var succ = Result<int>.Success(10);
        var rec1 = succ.Recover(() => Result<int>.Success(99));
        rec1.IsSuccess.ShouldBeTrue();
        rec1.Value.ShouldBe(10);

        var fail = Result<int>.WithFailure("x");
        var rec2 = fail.Recover(() => Result<int>.Success(77));
        rec2.IsSuccess.ShouldBeTrue();
        rec2.Value.ShouldBe(77);
    }

    [Fact]
    public void RecoverWith_CompatibleConversion_Succeeds_Incompatible_Fails()
    {
        var succ = Result<int>.Success(5);
        var toObject = succ.RecoverWith<object>(() => Result<object>.WithFailure("n/a"));
        toObject.IsSuccess.ShouldBeTrue();
        toObject.Value.ShouldBe(5); // boxed

        var incompatible = succ.RecoverWith<string>(() => Result<string>.WithFailure("n/a"));
        incompatible.IsFailure.ShouldBeTrue();
        incompatible.Error!.ShouldContain("RecoverWith");
    }

    [Fact]
    public void Match_Failure_UsesDefaultMessage_WhenErrorsEmpty()
    {
        var failure = Result<int>.WithFailure([]);
        var matched = failure.Match(
            onSuccess: v => v.ToString(),
            onFailure: errs => string.Join(",", errs));
        // Implementation returns Success of onFailure result; we assert behavior
        matched.IsSuccess.ShouldBeTrue();
        matched.Value!.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    [Fact]
    public void CombineErrorsTOut_NullOrEmpty_ReturnsDefaultMessage()
    {
        var res1 = Result<int>.CombineErrors<string>(null, null);
        res1.IsFailure.ShouldBeTrue();
        res1.Error.ShouldBe(ResultConstants.NoErrorsFoundMessage);

        var res2 = Result<int>.CombineErrors<string>([], []);
        res2.IsFailure.ShouldBeTrue();
        res2.Error.ShouldBe(ResultConstants.NoErrorsFoundMessage);
    }
}



