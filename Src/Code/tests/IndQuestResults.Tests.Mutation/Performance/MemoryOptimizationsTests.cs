namespace IndQuestResults.Tests.Mutation.Performance;

public class MemoryOptimizationsTests
{
    [Fact]
    public void GetSingleItemArray_CachesByItem()
    {
        var a1 = MemoryOptimizations.GetSingleItemArray("x");
        var a2 = MemoryOptimizations.GetSingleItemArray("x");
        ReferenceEquals(a1, a2).ShouldBeTrue();
        MemoryOptimizations.GetSingleItemArray("").ShouldBeSameAs(MemoryOptimizations.EmptyStringArray);
    }

    [Fact]
    public void GetFormattedError_CachesFormattedString()
    {
        var f1 = MemoryOptimizations.GetFormattedError("E {0}", 1);
        var f2 = MemoryOptimizations.GetFormattedError("E {0}", 1);
        f1.ShouldBe("E 1");
        f1.ShouldBeSameAs(f2);
        MemoryOptimizations.GetFormattedError(null!).ShouldBe(string.Empty);
        MemoryOptimizations.GetFormattedError("plain").ShouldBe("plain");
    }

    [Fact]
    public void ToArrayOptimized_HandlesArrayCollectionAndNull()
    {
        int[] arr = [1,2];
        MemoryOptimizations.ToArrayOptimized(arr).ShouldBeSameAs(arr);
        MemoryOptimizations.ToArrayOptimized((IEnumerable<int>)new List<int>{3,4}).ShouldBe(new[]{3,4});
        MemoryOptimizations.ToArrayOptimized<int>(null!).ShouldBeEmpty();
    }

    [Fact]
    public void CombineArrays_JoinsAndSkipsNulls()
    {
        var combined = MemoryOptimizations.CombineArrays(new[]{1,2}, null!, Array.Empty<int>(), new[]{3});
        combined.ShouldBe(new[]{1,2,3});
        MemoryOptimizations.CombineArrays<int>(null!).ShouldBeEmpty();
    }

    [Fact]
    public void ConcurrentCache_EvictsLeastRecentlyUsed()
    {
        var cache = new ConcurrentCache<string, string>(maxSize: 2);
        cache.GetOrAdd("a", k => k);
        cache.GetOrAdd("b", k => k);
        cache.GetOrAdd("a", k => k); // access a to keep it fresh
        cache.GetOrAdd("c", k => k); // triggers eviction of least-recently-used (b)
        cache.Count.ShouldBe(2);
        cache.GetOrAdd("b", k => k).ShouldBe("b"); // re-adds b
        cache.Clear();
        cache.Count.ShouldBe(0);
    }
}

