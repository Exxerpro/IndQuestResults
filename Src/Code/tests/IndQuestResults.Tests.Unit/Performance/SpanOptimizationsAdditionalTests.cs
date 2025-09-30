namespace IndQuestResults.Tests.Unit.Performance;

public class SpanOptimizationsAdditionalTests
{
    [Fact]
    public void FormatCollection_SmallArray_UsesSpanPath()
    {
        var s = SpanOptimizations.FormatCollection(new[] { "a", "b", "c" }, "WithFailure", ", ");
        // SpanOptimizations does not append ": " after prefix; it starts with prefix then items
        s.ShouldBe("WithFailurea, b, c");
    }

    [Fact]
    public void CombineCollections_Estimate_And_Deduplicate()
    {
        var combined = SpanOptimizations.CombineCollections(new[]
        {
            new[] { "a", "b" },
            new[] { "b", "c" }
        }, totalEstimatedCount: 4);

        combined.Length.ShouldBe(4);

        var unique = SpanOptimizations.DeduplicatePreserveOrder(combined);
        unique.ShouldBe(new[] { "a", "b", "c" });
    }
}


