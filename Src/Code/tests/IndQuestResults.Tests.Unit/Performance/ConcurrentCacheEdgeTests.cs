using IndQuestResults.Performance;

namespace IndQuestResults.Tests.Unit.Performance;

public class ConcurrentCacheEdgeTests
{
    [Fact]
    public void LruEviction_Respects_AccessOrder()
    {
        using var cache = new ConcurrentCache<string, string>(maxSize: 3);
        cache.GetOrAdd("A", k => k);
        cache.GetOrAdd("B", k => k);
        cache.GetOrAdd("C", k => k);

        // Access A and C, B becomes LRU
        cache.GetOrAdd("A", k => "x");
        cache.GetOrAdd("C", k => "y");

        // Insert D -> should evict B
        cache.GetOrAdd("D", k => k);

        // B should be re-created
        var b = cache.GetOrAdd("B", k => "NEW");
        b.ShouldBe("NEW");
        cache.Count.ShouldBeLessThanOrEqualTo(cache.MaxSize);
    }
}

