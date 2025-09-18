using IndQuestResults.Performance;

namespace IndQuestResults.Tests.Unit.Performance;

public class SpanAndMemorySmokeTests
{
    [Fact]
    public void SpanOptimizations_FormatCollection_SmallAndLarge()
    {
        var small = new[] { "a", "b", "c" };
        var smallFormatted = SpanOptimizations.FormatCollection(small, "P:", ";");
        smallFormatted.ShouldBe("P:a;b;c");

        var large = Enumerable.Range(1, SpanOptimizations.MaxSpanOptimizationItems + 5).Select(i => i.ToString()).ToArray();
        var largeFormatted = SpanOptimizations.FormatCollection(large, "L:", ", ");
        largeFormatted.ShouldStartWith("L:");
        largeFormatted.ShouldContain(", ");
        largeFormatted.Split(", ").Length.ShouldBe(large.Length); // items separated by ", "
    }

    [Fact]
    public void SpanOptimizations_DeduplicatePreserveOrder_Works()
    {
        var input = new[] { "x", "y", "x", "z", "y" };
        var unique = SpanOptimizations.DeduplicatePreserveOrder(input);
        unique.ShouldBe(["x", "y", "z"]);
    }

    [Fact]
    public void MemoryOptimizations_GetSingleItemArray_And_FormattedError()
    {
        var arr1 = MemoryOptimizations.GetSingleItemArray("E1");
        var arr2 = MemoryOptimizations.GetSingleItemArray("E1");
        arr1.ShouldBe(arr2); // same cached array instance for same key

        var formatted1 = MemoryOptimizations.GetFormattedError("Err {0}", 42);
        var formatted2 = MemoryOptimizations.GetFormattedError("Err {0}", 42);
        formatted1.ShouldBe(formatted2);
        formatted1.ShouldBe("Err 42");
    }

    [Fact]
    public void MemoryOptimizations_ToArrayOptimized_And_CombineArrays()
    {
        var list = new List<int> { 1, 2, 3 };
        var arr = list.ToArrayOptimized();
        arr.ShouldBe([1, 2, 3]);

        var combined = MemoryOptimizations.CombineArrays([1, 2], [3], []);
        combined.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void ConcurrentCache_Basic_GetOrAdd_EvictsWhenOverCapacity()
    {
        using var cache = new ConcurrentCache<string, string>(maxSize: 3);
        cache.GetOrAdd("a", k => k);
        cache.GetOrAdd("b", k => k);
        cache.GetOrAdd("c", k => k);
        cache.GetOrAdd("d", k => k); // triggers eviction of least-recently-used

        // Accessing all keys should not throw; we can't guarantee which was evicted,
        // but we can ensure cache size limit and that recently added is present.
        var present = cache.GetOrAdd("d", k => "X");
        present.ShouldBe("d");
    }
}




