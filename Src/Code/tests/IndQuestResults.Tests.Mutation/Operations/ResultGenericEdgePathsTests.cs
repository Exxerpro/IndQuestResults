namespace IndQuestResults.Tests.Mutation.Operations;

public class ResultGenericEdgePathsTests
{
    [Fact]
    public void WithFailure_ArrayOverload_NullOrEmpty_Defaults()
    {
        IEnumerable<string>? nullSeq = null;
        var r1 = Result<string>.WithFailure(nullSeq!, value: default);
        r1.IsFailure.ShouldBeTrue();
        r1.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);

        var r2 = Result<string>.WithFailure((IEnumerable<string>)Array.Empty<string>(), value: default);
        r2.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);

        var r3 = Result<string>.WithFailure(new[] { "e" });
        r3.Errors.ShouldContain("e");
    }

    [Fact]
    public void WithFailure_ValueAndErrorsParameterOrder_BothOverloadsCovered()
    {
        // IEnumerable overload
        var r1 = Result<int>.WithFailure((IEnumerable<string>)new[] { "e1" }, 1);
        r1.IsFailure.ShouldBeTrue();
        r1.Value.ShouldBe(1);
        r1.Errors.ShouldContain("e1");

        // Swapped overload
        var r2 = Result<int>.WithFailure(2, (IEnumerable<string>)new[] { "e2" });
        r2.IsFailure.ShouldBeTrue();
        r2.Value.ShouldBe(2);
        r2.Errors.ShouldContain("e2");
    }

    [Fact]
    public void OnFailure_WithNullErrors_UsesDefault()
    {
        var constructed = new Result<string>(isSuccess: false, errors: (List<string>?)null, value: "v");
        List<string> captured = new();
        constructed.OnFailure(errs => captured.AddRange(errs));
        captured.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    [Fact]
    public void Match_Failure_WithNullErrors_FallsBackToDefault()
    {
        var constructed = new Result<string>(isSuccess: false, errors: (IEnumerable<string>?)null, value: null);
        var matched = constructed.Match(s => s ?? "", errs => string.Join(";", errs));
        matched.IsSuccess.ShouldBeTrue();
        matched.Value!.ShouldContain(ResultConstants.DefaultErrorMessage);
    }
}
