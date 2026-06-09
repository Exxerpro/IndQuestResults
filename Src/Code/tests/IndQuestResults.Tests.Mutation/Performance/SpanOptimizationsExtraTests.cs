namespace IndQuestResults.Tests.Mutation.Performance;

public class SpanOptimizationsExtraTests
{
    [Fact]
    public void CombineCollections_And_IsSuitableForSpanOptimization()
    {
        var c1 = new List<string> { "a", "b" };
        var c2 = new List<string> { "c" };
        var combined = SpanOptimizations.CombineCollections(new[] { c1, c2 });
        combined.ShouldBe(["a", "b", "c"]);

        SpanOptimizations.IsSuitableForSpanOptimization(c1).ShouldBeTrue();
        SpanOptimizations.IsSuitableForSpanOptimization(Enumerable.Range(1, 100).Select(i => i.ToString()).ToList()).ShouldBeFalse();
    }
}

