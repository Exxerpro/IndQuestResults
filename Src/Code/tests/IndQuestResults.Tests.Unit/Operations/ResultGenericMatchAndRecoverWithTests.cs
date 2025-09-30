namespace IndQuestResults.Tests.Unit.Operations;

public class ResultGenericMatchAndRecoverWithTests
{
    [Fact]
    public void Match_Success_NullValue_ForNullableType_InvokesOnSuccess()
    {
        Result<string?> r = Result<string?>.Success(null);
        var matched = r.Match(
            onSuccess: s => s ?? "NULL",
            onFailure: _ => "FAIL");
        matched.IsSuccess.ShouldBeTrue();
        matched.Value.ShouldBe("NULL");
    }

    [Fact]
    public void RecoverWith_IncompatibleType_ReturnsFailureWithMessage()
    {
        Result<int> r = Result<int>.Success(5);
        var recovered = r.RecoverWith<DateTime>(() => Result<DateTime>.Success(DateTime.UtcNow));
        recovered.IsFailure.ShouldBeTrue();
        recovered.Error.ShouldContain("Cannot convert");
    }
}


