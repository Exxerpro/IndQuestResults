namespace IndQuestResults.Tests.Unit.Operations;

public class ResultErrorCombinationEdgeTests
{
    [Fact]
    public void CombineErrors_BothNull_UsesDefaultMessage()
    {
        var combined = Result<int>.CombineErrors<int>(null, null, value: 0);
        combined.IsFailure.ShouldBeTrue();
        combined.Errors.Single().ShouldBe(ResultConstants.NoErrorsFoundMessage);
    }

    [Fact]
    public void CombineErrors_LargeLists_AccumulatesAll()
    {
        var primary = Enumerable.Range(0, 500).Select(i => $"p{i}");
        var secondary = Enumerable.Range(0, 500).Select(i => $"s{i}");
        var combined = Result<string>.CombineErrors<string>(primary, secondary, value: null);
        combined.IsFailure.ShouldBeTrue();
        combined.Errors.Count().ShouldBe(1000);
        combined.Errors.ShouldContain("p0");
        combined.Errors.ShouldContain("s499");
    }
}

