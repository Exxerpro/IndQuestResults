namespace IndQuestResults.Tests.Unit.Performance;

public class MemoryOptimizationsCacheEvictionTests
{
    [Fact]
    public void SingleItemArrayCache_Evicts_WhenOverCapacity()
    {
        MemoryOptimizations.ClearCaches();
        var before = MemoryOptimizations.GetCacheStatistics();
        int max = (int)before["SingleItemArrayCache.MaxSize"];

        for (int i = 0; i < max + 50; i++)
        {
            _ = MemoryOptimizations.GetSingleItemArray($"K{i}");
        }

        var after = MemoryOptimizations.GetCacheStatistics();
        ((int)after["SingleItemArrayCache.Count"]).ShouldBeLessThanOrEqualTo(max);
    }

    [Fact]
    public void FormattedErrorCache_Evicts_WhenOverCapacity()
    {
        MemoryOptimizations.ClearCaches();
        var before = MemoryOptimizations.GetCacheStatistics();
        int max = (int)before["FormattedErrorCache.MaxSize"];

        for (int i = 0; i < max + 150; i++)
        {
            _ = MemoryOptimizations.GetFormattedError("E{0}", i);
        }

        var after = MemoryOptimizations.GetCacheStatistics();
        ((int)after["FormattedErrorCache.Count"]).ShouldBeLessThanOrEqualTo(max);
    }
}

