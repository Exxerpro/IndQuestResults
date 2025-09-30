namespace IndQuestResults.Tests.Unit.Operations;

public class ResultGenericFailureAndCombineTests
{
    [Fact]
    public void WithFailure_Generic_SingleString_SetsFailureAndDefaultValue()
    {
        var r = Result<int>.WithFailure("bad");

        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe("bad");
        r.Value.ShouldBe(default);
    }

    [Fact]
    public void OnFailure_Generic_Invokes_WithProvidedErrors()
    {
        var captured = new List<string>();
        var r = Result<string>.WithFailure(new[] { "x", "y" }, value: null);

        r.OnFailure(errors => captured.AddRange(errors));

        captured.ShouldBe(new[] { "x", "y" });
    }

    [Fact]
    public void Combine_Generic_AggregatesErrors_OrReturnsSuccess()
    {
        var a = Result<int>.WithFailure("A");
        var b = Result<int>.Success(1);

        var combined = a.Combine(Result.WithFailure("Z"));

        combined.IsFailure.ShouldBeTrue();
        combined.Error.ShouldBe("A");

        var allOk = b.Combine(Result.Success());
        allOk.IsSuccess.ShouldBeTrue();
        allOk.Value.ShouldBe(1);
    }
}


