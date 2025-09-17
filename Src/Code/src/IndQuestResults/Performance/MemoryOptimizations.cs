namespace IndQuestResults.Performance;

/// <summary>
/// Memory optimization utilities for Result types to minimize allocations and improve performance.
/// Provides strategies for efficient object pooling, caching, and memory-conscious operations.
/// </summary>
public static class MemoryOptimizations
{
    /// <summary>
    /// Pre-allocated empty string array to avoid repeated allocations.
    /// </summary>
    public static readonly string[] EmptyStringArray = Array.Empty<string>();

    /// <summary>
    /// Cache of commonly used single-item string arrays to reduce allocations.
    /// </summary>
    private static readonly ConcurrentCache<string, string[]> SingleItemArrayCache = new(maxSize: 100);

    /// <summary>
    /// Cache of commonly used error message combinations.
    /// </summary>
    private static readonly ConcurrentCache<string, string> FormattedErrorCache = new(maxSize: 500);

    /// <summary>
    /// Gets a cached single-item string array or creates one if not cached.
    /// Reduces allocations for frequently used single error messages.
    /// </summary>
    /// <param name="item">The string item to wrap in an array.</param>
    /// <returns>A cached or new single-item string array.</returns>
    public static string[] GetSingleItemArray(string item)
    {
        if (string.IsNullOrEmpty(item))
            return EmptyStringArray;

        return SingleItemArrayCache.GetOrAdd(item, static key => new[] { key });
    }

    /// <summary>
    /// Gets a cached formatted error string or creates one if not cached.
    /// Optimizes repeated formatting of the same error message patterns.
    /// </summary>
    /// <param name="template">The template string with placeholders.</param>
    /// <param name="args">The arguments to format into the template.</param>
    /// <returns>A cached or newly formatted string.</returns>
    public static string GetFormattedError(string template, params object[] args)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        if (args == null || args.Length == 0)
            return template;

        // Create cache key from template and args
        var cacheKey = $"{template}|{string.Join("|", args.Select(a => a?.ToString() ?? "null"))}";
        
        return FormattedErrorCache.GetOrAdd(cacheKey, _ => string.Format(template, args), args);
    }

    /// <summary>
    /// Efficiently converts an enumerable to an array with minimal allocations.
    /// Uses collection count when available to pre-size the array.
    /// </summary>
    /// <typeparam name="T">The type of items in the enumerable.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <returns>An array containing the enumerable items.</returns>
    public static T[] ToArrayOptimized<T>(this IEnumerable<T> source)
    {
        if (source is null)
            return Array.Empty<T>();

        // Fast path: already an array
        if (source is T[] array)
            return array;

        // Fast path: collection with known count
        if (source is ICollection<T> collection)
        {
            if (collection.Count == 0)
                return Array.Empty<T>();

            var result = new T[collection.Count];
            collection.CopyTo(result, 0);
            return result;
        }

        // Fallback: use List<T> and convert
        return source.ToArray();
    }

    /// <summary>
    /// Efficiently combines multiple arrays with minimal allocations.
    /// Pre-calculates total size to avoid List resizing.
    /// </summary>
    /// <typeparam name="T">The type of items in the arrays.</typeparam>
    /// <param name="arrays">The arrays to combine.</param>
    /// <returns>A single array containing all items.</returns>
    public static T[] CombineArrays<T>(params T[][] arrays)
    {
        if (arrays is null || arrays.Length == 0)
            return Array.Empty<T>();

        // Filter out null arrays and calculate total length
        var validArrays = arrays.Where(a => a is not null && a.Length > 0).ToArray();
        if (validArrays.Length == 0)
            return Array.Empty<T>();

        if (validArrays.Length == 1)
            return validArrays[0];

        var totalLength = validArrays.Sum(a => a.Length);
        var result = new T[totalLength];
        var position = 0;

        foreach (var array in validArrays)
        {
            Array.Copy(array, 0, result, position, array.Length);
            position += array.Length;
        }

        return result;
    }

    /// <summary>
    /// Clears internal caches to free memory when needed.
    /// Useful for long-running applications that may accumulate cached data.
    /// </summary>
    public static void ClearCaches()
    {
        SingleItemArrayCache.Clear();
        FormattedErrorCache.Clear();
    }

    /// <summary>
    /// Gets current cache statistics for monitoring memory usage.
    /// </summary>
    /// <returns>A dictionary containing cache statistics.</returns>
    public static Dictionary<string, object> GetCacheStatistics()
    {
        return new Dictionary<string, object>
        {
            ["SingleItemArrayCache.Count"] = SingleItemArrayCache.Count,
            ["SingleItemArrayCache.MaxSize"] = SingleItemArrayCache.MaxSize,
            ["FormattedErrorCache.Count"] = FormattedErrorCache.Count,
            ["FormattedErrorCache.MaxSize"] = FormattedErrorCache.MaxSize
        };
    }
}

/// <summary>
/// A simple thread-safe LRU cache implementation for memory optimization.
/// </summary>
/// <typeparam name="TKey">The type of the cache key.</typeparam>
/// <typeparam name="TValue">The type of the cached value.</typeparam>
internal sealed class ConcurrentCache<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, CacheItem> _cache = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly int _maxSize;
    private long _accessCounter = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentCache{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="maxSize">The maximum number of items to cache.</param>
    public ConcurrentCache(int maxSize)
    {
        _maxSize = maxSize;
    }

    /// <summary>
    /// Gets the current number of cached items.
    /// </summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _cache.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    /// <summary>
    /// Gets the maximum cache size.
    /// </summary>
    public int MaxSize => _maxSize;

    /// <summary>
    /// Gets a cached value or adds a new one using the provided factory.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="valueFactory">The factory to create the value if not cached.</param>
    /// <param name="factoryArgs">Optional arguments for the factory.</param>
    /// <returns>The cached or newly created value.</returns>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory, params object[] factoryArgs)
    {
        // Try to get existing value
        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var existingItem))
            {
                existingItem.LastAccessed = Interlocked.Increment(ref _accessCounter);
                return existingItem.Value;
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }

        // Create new value outside of lock
        var newValue = valueFactory(key);

        // Add to cache
        _lock.EnterWriteLock();
        try
        {
            // Double-check in case another thread added it
            if (_cache.TryGetValue(key, out var existingItem))
            {
                existingItem.LastAccessed = Interlocked.Increment(ref _accessCounter);
                return existingItem.Value;
            }

            // Evict least recently used items if at capacity
            if (_cache.Count >= _maxSize)
            {
                EvictLeastRecentlyUsed();
            }

            var newItem = new CacheItem(newValue, Interlocked.Increment(ref _accessCounter));
            _cache[key] = newItem;
            return newValue;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears all cached items.
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Clear();
            _accessCounter = 0;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Evicts the least recently used items to make room for new ones.
    /// </summary>
    private void EvictLeastRecentlyUsed()
    {
        var itemsToRemove = _cache.Count - _maxSize + 1;
        if (itemsToRemove <= 0) return;

        var lruItems = _cache
            .OrderBy(kvp => kvp.Value.LastAccessed)
            .Take(itemsToRemove)
            .Select(kvp => kvp.Key)
            .ToArray();

        foreach (var key in lruItems)
        {
            _cache.Remove(key);
        }
    }

    /// <summary>
    /// Represents a cached item with access tracking.
    /// </summary>
    private sealed class CacheItem
    {
        public CacheItem(TValue value, long lastAccessed)
        {
            Value = value;
            LastAccessed = lastAccessed;
        }

        public TValue Value { get; }
        public long LastAccessed { get; set; }
    }
}