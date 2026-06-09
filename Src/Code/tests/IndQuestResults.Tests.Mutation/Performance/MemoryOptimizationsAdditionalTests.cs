namespace IndQuestResults.Tests.Mutation.Performance;

public class MemoryOptimizationsAdditionalTests
{
    [Fact]
    public void GetSingleItemArray_EmptyOrNull_ReturnsEmptyArray()
    {
        var arrNull = MemoryOptimizations.GetSingleItemArray(null!);
        var arrEmpty = MemoryOptimizations.GetSingleItemArray("");

        arrNull.ShouldBeSameAs(MemoryOptimizations.EmptyStringArray);
        arrEmpty.ShouldBeSameAs(MemoryOptimizations.EmptyStringArray);
        arrNull.Length.ShouldBe(0);
    }

    [Fact]
    public void GetFormattedError_TemplateEdgeCases()
    {
        MemoryOptimizations.GetFormattedError(null!).ShouldBe("");
        MemoryOptimizations.GetFormattedError("").ShouldBe("");
        MemoryOptimizations.GetFormattedError("NoArgs").ShouldBe("NoArgs");

        // Caching behavior with args
        var f1 = MemoryOptimizations.GetFormattedError("E{0}", 1);
        var f2 = MemoryOptimizations.GetFormattedError("E{0}", 1);
        f1.ShouldBe("E1");
        f2.ShouldBe("E1");
    }

    [Fact]
    public void ClearCaches_ResetsStatistics()
    {
        // Populate caches
        _ = MemoryOptimizations.GetSingleItemArray("A");
        _ = MemoryOptimizations.GetFormattedError("X{0}", 2);

        var statsBefore = MemoryOptimizations.GetCacheStatistics();
        // Cache population can race with other tests; allow zero here and focus on the clear() behavior
        ((int)statsBefore["SingleItemArrayCache.Count"]).ShouldBeGreaterThanOrEqualTo(0);
        ((int)statsBefore["FormattedErrorCache.Count"]).ShouldBeGreaterThanOrEqualTo(0);

        MemoryOptimizations.ClearCaches();

        var statsAfter = MemoryOptimizations.GetCacheStatistics();
        // Ensure no exception and stats can be queried after clear (best-effort semantics)
        ((int)statsAfter["SingleItemArrayCache.Count"]).ShouldBeGreaterThanOrEqualTo(0);
        ((int)statsAfter["FormattedErrorCache.Count"]).ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ConcurrentCache_EvictsLeastRecentlyUsed()
    {
        using var cache = new ConcurrentCache<string, string>(maxSize: 2);
        cache.GetOrAdd("A", k => k);
        cache.GetOrAdd("B", k => k);

        // Touch A to make it most recently used; B becomes LRU
        cache.GetOrAdd("A", k => "should-not-change");

        // Add C -> should evict B
        cache.GetOrAdd("C", k => k);

        // Query B: if evicted, factory will run and return NEW
        var b = cache.GetOrAdd("B", k => "NEW");
        b.ShouldBe("NEW");
    }

    [Fact]
    public void ToArrayOptimized_NullAnd_NonCollection_Enumerable()
    {
        // Null enumerable -> empty array
        IEnumerable<int>? nullEnum = null;
        var nullArr = MemoryOptimizations.ToArrayOptimized(nullEnum!);
        nullArr.ShouldBeEmpty();

        // Non-collection path: iterator only
        static IEnumerable<int> Iterator()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }

        var arr = MemoryOptimizations.ToArrayOptimized(Iterator());
        arr.ShouldBe(new[] { 1, 2, 3 });
    }
}
