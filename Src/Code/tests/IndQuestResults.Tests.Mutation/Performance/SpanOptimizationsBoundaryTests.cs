namespace IndQuestResults.Tests.Mutation.Performance;

public class SpanOptimizationsBoundaryTests
{
    [Fact]
    public void FormatCollection_EstimatedLength_JustOverStackAlloc_UsesFallback()
    {
        var veryLong = new string('x', SpanOptimizations.MaxStackAllocSize);
        var s = SpanOptimizations.FormatCollection([veryLong], prefix: "P:", separator: ", ");
        s.ShouldStartWith("P:");
        s.Length.ShouldBeGreaterThan(SpanOptimizations.MaxStackAllocSize);
    }

    [Fact]
    public void FormatCollection_NullItems_And_EmptySeparator()
    {
        var s = SpanOptimizations.FormatCollection(["a", null!, "b"], prefix: "", separator: "");
        s.ShouldBe("ab");
    }

    [Fact]
    public void DeduplicatePreserveOrder_SmallAndLarge()
    {
        var small = new[] { "x", "y", "x", null!, "y", "z" };
        SpanOptimizations.DeduplicatePreserveOrder(small).ShouldBe(["x", "y", "z"]);

        var large = Enumerable.Range(0, 50).Select(i => i % 10 == 0 ? "d" : i.ToString());
        var unique = SpanOptimizations.DeduplicatePreserveOrder(large);
        unique.First().ShouldBe("d");
        unique.Length.ShouldBeGreaterThan(1);
    }
}

