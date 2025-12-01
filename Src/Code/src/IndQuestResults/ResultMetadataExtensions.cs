namespace IndQuestResults;

/// <summary>
/// Extension methods for type-safe metadata access on Result and Result{T}.
/// Uses type names as dictionary keys for compile-time safety and refactoring support.
/// </summary>
/// <remarks>
/// <para><strong>Design Goals:</strong></para>
/// <list type="bullet">
/// <item>Type-safe metadata access - type IS the key</item>
/// <item>Support multiple metadata objects per Result</item>
/// <item>Graceful null handling (null Metadata dictionary, missing keys, type mismatches)</item>
/// <item>Zero allocations when metadata is not used</item>
/// <item>Generic constraint where TMetadata : class, new() ensures instantiability and reference type</item>
/// </list>
/// <para><strong>Usage:</strong></para>
/// <code>
/// // Set - type inferred from parameter
/// result.SetMetadata(new ExtractionMetadata { Confidence = 0.87 });
///
/// // Get - type specified explicitly
/// var metadata = result.GetMetadata&lt;ExtractionMetadata&gt;();
///
/// // TryGet - safe pattern
/// if (result.TryGetMetadata&lt;ExtractionMetadata&gt;(out var metadata))
/// {
///     Console.WriteLine($"Confidence: {metadata.Confidence}");
/// }
/// </code>
/// </remarks>
public static class ResultMetadataExtensions
{
    // ==================== Result (non-generic) ====================

    /// <summary>
    /// Sets a metadata value on a Result using the type as the key.
    /// Type name is used as dictionary key, ensuring type safety and refactoring support.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type (must be reference type with parameterless constructor).</typeparam>
    /// <param name="result">The Result to attach metadata to.</param>
    /// <param name="value">The metadata value to store.</param>
    /// <example>
    /// <code>
    /// var result = Result.Success();
    /// result.SetMetadata(new ProcessingContext { Duration = 100 });
    /// </code>
    /// </example>
    public static void SetMetadata<TMetadata>(this Result result, TMetadata value)
        where TMetadata : class, new()
    {
        result.Metadata ??= [];
        result.Metadata[typeof(TMetadata).FullName!] = value;
    }

    /// <summary>
    /// Gets a strongly-typed metadata value from a Result.
    /// Returns null if not found or type mismatch.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type to retrieve.</typeparam>
    /// <param name="result">The Result to retrieve metadata from.</param>
    /// <returns>The metadata value, or null if not found.</returns>
    /// <example>
    /// <code>
    /// var context = result.GetMetadata&lt;ProcessingContext&gt;();
    /// if (context != null)
    /// {
    ///     Console.WriteLine($"Duration: {context.Duration}");
    /// }
    /// </code>
    /// </example>
    public static TMetadata? GetMetadata<TMetadata>(this Result result)
        where TMetadata : class, new()
    {
        var key = typeof(TMetadata).FullName!;
        return result.Metadata?.TryGetValue(key, out var value) == true && value is TMetadata typed
            ? typed
            : null;
    }

    /// <summary>
    /// Tries to get a strongly-typed metadata value from a Result.
    /// Returns false if not found or type mismatch.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata type to retrieve.</typeparam>
    /// <param name="result">The Result to retrieve metadata from.</param>
    /// <param name="value">When this method returns, contains the metadata value if found; otherwise, null.</param>
    /// <returns>true if metadata was found; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// if (result.TryGetMetadata&lt;ProcessingContext&gt;(out var context))
    /// {
    ///     Console.WriteLine($"Duration: {context.Duration}");
    /// }
    /// </code>
    /// </example>
    public static bool TryGetMetadata<TMetadata>(this Result result, out TMetadata? value)
        where TMetadata : class, new()
    {
        var key = typeof(TMetadata).FullName!;
        if (result.Metadata?.TryGetValue(key, out var obj) == true && obj is TMetadata typed)
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }

    // ==================== Result<T> ====================

    /// <summary>
    /// Sets a metadata value on a Result{T} using the type as the key.
    /// Type name is used as dictionary key, ensuring type safety and refactoring support.
    /// </summary>
    /// <typeparam name="T">The Result value type.</typeparam>
    /// <typeparam name="TMetadata">The metadata type (must be reference type with parameterless constructor).</typeparam>
    /// <param name="result">The Result{T} to attach metadata to.</param>
    /// <param name="value">The metadata value to store.</param>
    /// <example>
    /// <code>
    /// var result = Result&lt;Document&gt;.Success(doc);
    /// result.SetMetadata(new ExtractionMetadata { Confidence = 0.87 });
    /// </code>
    /// </example>
    public static void SetMetadata<T, TMetadata>(this Result<T> result, TMetadata value)
        where TMetadata : class, new()
    {
        result.Metadata ??= [];
        result.Metadata[typeof(TMetadata).FullName!] = value;
    }

    /// <summary>
    /// Gets a strongly-typed metadata value from a Result{T}.
    /// Returns null if not found or type mismatch.
    /// </summary>
    /// <typeparam name="T">The Result value type.</typeparam>
    /// <typeparam name="TMetadata">The metadata type to retrieve.</typeparam>
    /// <param name="result">The Result{T} to retrieve metadata from.</param>
    /// <returns>The metadata value, or null if not found.</returns>
    /// <example>
    /// <code>
    /// var metadata = result.GetMetadata&lt;Document, ExtractionMetadata&gt;();
    /// if (metadata?.Confidence > 0.85)
    /// {
    ///     // High confidence, auto-process
    /// }
    /// </code>
    /// </example>
    public static TMetadata? GetMetadata<T, TMetadata>(this Result<T> result)
        where TMetadata : class, new()
    {
        var key = typeof(TMetadata).FullName!;
        return result.Metadata?.TryGetValue(key, out var value) == true && value is TMetadata typed
            ? typed
            : null;
    }

    /// <summary>
    /// Tries to get a strongly-typed metadata value from a Result{T}.
    /// Returns false if not found or type mismatch.
    /// </summary>
    /// <typeparam name="T">The Result value type.</typeparam>
    /// <typeparam name="TMetadata">The metadata type to retrieve.</typeparam>
    /// <param name="result">The Result{T} to retrieve metadata from.</param>
    /// <param name="value">When this method returns, contains the metadata value if found; otherwise, null.</param>
    /// <returns>true if metadata was found; otherwise, false.</returns>
    /// <example>
    /// <code>
    /// if (result.TryGetMetadata&lt;Document, ExtractionMetadata&gt;(out var metadata))
    /// {
    ///     Console.WriteLine($"Confidence: {metadata.Confidence}");
    /// }
    /// </code>
    /// </example>
    public static bool TryGetMetadata<T, TMetadata>(this Result<T> result, out TMetadata? value)
        where TMetadata : class, new()
    {
        var key = typeof(TMetadata).FullName!;
        if (result.Metadata?.TryGetValue(key, out var obj) == true && obj is TMetadata typed)
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }
}
