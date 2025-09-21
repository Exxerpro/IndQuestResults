using IndQuestResults;
using IndQuestResults.Operations;

namespace IndQuestResults.Tests.Unit.Operations;

public class ResultGenericNullabilityEdgeTests
{
    [Fact]
    public void Ensure_WhenSuccessWithNullValue_ReturnsConditionEvaluationWithNullValue()
    {
        Result<string> r = Result<string>.Success(null!);

        var after = r.Ensure(_ => false, "should-not-be-used");

        after.IsFailure.ShouldBeTrue();
        after.Error.ShouldBe(ResultConstants.ConditionEvaluationWithNullValue);
    }

    [Fact]
    public void Match_OnFailure_ReturnsSuccessWrappedFailureBranchValue()
    {
        var fail = Result<int>.WithFailure("err");

        var matched = fail.Match(
            onSuccess: v => (v + 1).ToString(),
            onFailure: errs => string.Join(";", errs));

        matched.IsSuccess.ShouldBeTrue();
        matched.Value.ShouldBe("err");
    }
}


