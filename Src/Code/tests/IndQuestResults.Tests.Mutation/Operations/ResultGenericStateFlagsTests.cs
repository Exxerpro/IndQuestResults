namespace IndQuestResults.Tests.Mutation.Operations;

public class ResultGenericStateFlagsTests
{
    [Fact]
    public void Success_NullableReference_IsSuccessMayBeNullTrue_ValueNull()
    {
        Result<string> r = Result<string>.Success(null!);
        r.IsRecoverable.ShouldBeTrue();
        r.IsSuccessMayBeNull.ShouldBeTrue();
        r.IsSuccessValueNull.ShouldBeTrue();
        r.IsSuccessNotNull.ShouldBeFalse();
        r.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void Success_NonNull_IsSuccessAllTrue()
    {
        var r = Result<string>.Success("v");
        r.IsRecoverable.ShouldBeTrue();
        r.IsSuccessMayBeNull.ShouldBeTrue();
        r.IsSuccessNotNull.ShouldBeTrue();
        r.IsSuccess.ShouldBeTrue();
        r.IsSuccessValueNull.ShouldBeFalse();
    }

    [Fact]
    public void WithFailure_EmptyCollections_NormalizedToDefault()
    {
        IEnumerable<string> empty = Array.Empty<string>();
        var r1 = Result<int>.WithFailure(empty, 0);
        r1.IsFailure.ShouldBeTrue();
        r1.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);

        var r2 = Result<int>.WithFailure(Enumerable.Empty<string>(), 1);
        r2.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);

        var r3 = Result<int>.WithFailure(empty, 2);
        r3.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    [Fact]
    public void Error_Property_ReturnsFirstNonEmpty()
    {
        var r = Result<int>.WithFailure(new[] { "", "e2", "e3" }, 0);
        r.Error.ShouldBe("e2");
    }

    [Fact]
    public void ConfidenceClamp_NaN_ClampedToZero()
    {
        var r = Result<double>.WithWarnings(["w"], 1.0, double.NaN, double.NaN);
        r.Confidence.ShouldBe(0.0);
        r.MissingDataRatio.ShouldBe(0.0);
    }
}
