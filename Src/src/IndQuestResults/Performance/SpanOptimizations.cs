namespace IndQuestResults.Performance;

/// <summary>
/// High-performance utilities using Span&lt;T&gt; for memory-efficient operations.
/// Provides optimized string formatting and collection handling for Result types.
/// </summary>
public static class SpanOptimizations
{
    /// <summary>
    /// Maximum size for stackalloc operations to prevent stack overflow.
    /// </summary>
    public const int MaxStackAllocSize = 512;

    /// <summary>
    /// Maximum number of items suitable for Span&lt;T&gt; optimization.
    /// </summary>
    public const int MaxSpanOptimizationItems = 16;

    /// <summary>
    /// Formats a collection of strings using Span&lt;T&gt; optimizations when beneficial.
    /// Falls back to StringBuilder for larger collections.
    /// </summary>
    /// <param name="items">The collection of items to format.</param>
    /// <param name="prefix">The prefix string to prepend.</param>
    /// <param name="separator">The separator between items.</param>
    /// <returns>A formatted string representation.</returns>
    public static string FormatCollection(IEnumerable<string> items, string prefix = "", string separator = ", ")
    {
        if (items is null)
            return prefix;

        // Fast path for arrays with known count
        if (items is string[] itemArray && itemArray.Length <= MaxSpanOptimizationItems)
        {
            return FormatArraySpan(itemArray.AsSpan(), prefix, separator);
        }

        // Fast path for small collections
        if (items is ICollection<string> collection && collection.Count <= MaxSpanOptimizationItems)
        {
            var collectionArray = new string[collection.Count];
            var index = 0;
            foreach (var item in collection)
            {
                collectionArray[index++] = item;
            }
            return FormatArraySpan(collectionArray.AsSpan(), prefix, separator);
        }

        // Fallback to StringBuilder for large collections
        return FormatCollectionFallback(items, prefix, separator);
    }

    /// <summary>
    /// High-performance formatting using Span&lt;string&gt; for small collections.
    /// Uses stackalloc char buffer to minimize heap allocations.
    /// </summary>
    /// <param name="itemSpan">The span of items to format.</param>
    /// <param name="prefix">The prefix string to prepend.</param>
    /// <param name="separator">The separator between items.</param>
    /// <returns>A formatted string.</returns>
    private static string FormatArraySpan(ReadOnlySpan<string> itemSpan, string prefix, string separator)
    {
        if (itemSpan.IsEmpty)
            return prefix;

        // Estimate capacity: prefix + items + separators
        var estimatedLength = prefix.Length;
        foreach (var item in itemSpan)
        {
            estimatedLength += (item?.Length ?? 0) + separator.Length;
        }

        // Subtract one separator length (no separator after last item)
        if (itemSpan.Length > 0)
            estimatedLength -= separator.Length;

        // Use stackalloc for small strings
        if (estimatedLength <= MaxStackAllocSize)
        {
            Span<char> buffer = stackalloc char[estimatedLength];
            return BuildStringInSpan(buffer, itemSpan, prefix, separator);
        }
        else
        {
            return FormatCollectionFallback(itemSpan.ToArray(), prefix, separator);
        }
    }

    /// <summary>
    /// Builds the formatted string directly in a Span&lt;char&gt; buffer for maximum efficiency.
    /// </summary>
    /// <param name="buffer">The character buffer to write to.</param>
    /// <param name="itemSpan">The span of items to format.</param>
    /// <param name="prefix">The prefix string to prepend.</param>
    /// <param name="separator">The separator between items.</param>
    /// <returns>The formatted string.</returns>
    private static string BuildStringInSpan(Span<char> buffer, ReadOnlySpan<string> itemSpan, string prefix, string separator)
    {
        var position = 0;

        // Write prefix
        if (!string.IsNullOrEmpty(prefix))
        {
            prefix.AsSpan().CopyTo(buffer[position..]);
            position += prefix.Length;
        }

        // Write items with separators
        for (var i = 0; i < itemSpan.Length; i++)
        {
            if (i > 0 && !string.IsNullOrEmpty(separator))
            {
                separator.AsSpan().CopyTo(buffer[position..]);
                position += separator.Length;
            }

            var item = itemSpan[i] ?? string.Empty;
            if (!string.IsNullOrEmpty(item))
            {
                item.AsSpan().CopyTo(buffer[position..]);
                position += item.Length;
            }
        }

        return new string(buffer[..position]);
    }

    /// <summary>
    /// StringBuilder fallback for large collections or when Span optimization isn't beneficial.
    /// </summary>
    /// <param name="items">The items to format.</param>
    /// <param name="prefix">The prefix string to prepend.</param>
    /// <param name="separator">The separator between items.</param>
    /// <returns>A formatted string.</returns>
    private static string FormatCollectionFallback(IEnumerable<string> items, string prefix, string separator)
    {
        var builder = new System.Text.StringBuilder(prefix);
        var isFirst = true;

        foreach (var item in items)
        {
            if (!isFirst && !string.IsNullOrEmpty(separator))
                builder.Append(separator);

            if (!string.IsNullOrEmpty(item))
                builder.Append(item);

            isFirst = false;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Efficiently combines multiple string collections with minimal allocations.
    /// </summary>
    /// <param name="collections">The collections to combine.</param>
    /// <param name="totalEstimatedCount">Optional hint for total item count to optimize allocation.</param>
    /// <returns>A combined array of strings.</returns>
    public static string[] CombineCollections(IEnumerable<IEnumerable<string>> collections, int? totalEstimatedCount = null)
    {
        if (collections is null)
            return Array.Empty<string>();

        var collectionList = collections.ToList();
        if (collectionList.Count == 0)
            return Array.Empty<string>();

        // Use provided estimate or calculate
        var estimatedCount = totalEstimatedCount ?? EstimateTotalCount(collectionList);
        var result = new List<string>(estimatedCount);

        foreach (var collection in collectionList)
        {
            if (collection is not null)
            {
                result.AddRange(collection);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Estimates the total count of items across multiple collections.
    /// </summary>
    /// <param name="collections">The collections to count.</param>
    /// <returns>The estimated total count.</returns>
    private static int EstimateTotalCount(IEnumerable<IEnumerable<string>> collections)
    {
        var totalCount = 0;
        foreach (var collection in collections)
        {
            if (collection is ICollection<string> knownCollection)
            {
                totalCount += knownCollection.Count;
            }
            else if (collection is not null)
            {
                // Fallback: count enumerable (expensive but accurate)
                totalCount += collection.Count();
            }
        }
        return totalCount;
    }

    /// <summary>
    /// Determines if a collection is suitable for Span&lt;T&gt; optimization.
    /// </summary>
    /// <param name="collection">The collection to check.</param>
    /// <returns>True if the collection benefits from Span optimization.</returns>
    public static bool IsSuitableForSpanOptimization(IEnumerable<string> collection)
    {
        return collection is ICollection<string> knownCollection && 
               knownCollection.Count > 0 && 
               knownCollection.Count <= MaxSpanOptimizationItems;
    }

    /// <summary>
    /// Efficiently deduplicates a collection of strings while preserving order.
    /// Uses Span&lt;T&gt; for small collections to reduce allocations.
    /// </summary>
    /// <param name="items">The items to deduplicate.</param>
    /// <returns>An array of unique strings in original order.</returns>
    public static string[] DeduplicatePreserveOrder(IEnumerable<string> items)
    {
        if (items is null)
            return Array.Empty<string>();

        if (items is ICollection<string> collection && collection.Count <= MaxSpanOptimizationItems)
        {
            // Small collection: use array-based deduplication
            var itemArray = collection.ToArray();
            return DeduplicateSmallArray(itemArray);
        }

        // Large collection: use HashSet for efficiency
        var seen = new HashSet<string>();
        var result = new List<string>();

        foreach (var item in items)
        {
            if (item is not null && seen.Add(item))
            {
                result.Add(item);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Deduplicates a small array using direct comparison to avoid HashSet overhead.
    /// </summary>
    /// <param name="items">The small array to deduplicate.</param>
    /// <returns>An array of unique strings.</returns>
    private static string[] DeduplicateSmallArray(string[] items)
    {
        if (items.Length <= 1)
            return items;

        var result = new List<string>(items.Length);
        
        foreach (var item in items)
        {
            if (item is not null && !result.Contains(item))
            {
                result.Add(item);
            }
        }

        return result.ToArray();
    }
}