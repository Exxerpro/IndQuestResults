using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using IndQuestResults.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndQuestResults.Benchmarks;

/// <summary>
/// Benchmarks testing error formatting and string manipulation performance in Results.
/// Validates the claimed 50% improvement in error formatting through optimized string operations
/// and tests various error collection scenarios including Span&lt;T&gt; optimizations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RPlotExporter]
public class ErrorFormattingBenchmarks
{
    private string[] _smallErrorSet = null!;
    private string[] _mediumErrorSet = null!;
    private string[] _largeErrorSet = null!;
    private List<string> _smallErrorList = null!;
    private List<string> _mediumErrorList = null!;
    private List<string> _largeErrorList = null!;
    private const string TestPrefix = "Operation Failed";

    /// <summary>
    /// Gets or sets the number of errors to include in parameterized benchmarks.
    /// Tests different error collection sizes to measure scaling characteristics.
    /// </summary>
    [Params(1, 5, 10, 16, 32, 100)]
    public int ErrorCount { get; set; }

    /// <summary>
    /// Initializes test data with various error collection sizes and message lengths.
    /// Creates arrays and lists for testing different collection types and performance characteristics.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // Setup error arrays
        _smallErrorSet = Enumerable.Range(1, 5).Select(i => $"Error message {i}").ToArray();
        _mediumErrorSet = Enumerable.Range(1, 16).Select(i => $"Medium length error message number {i} with additional context").ToArray();
        _largeErrorSet = Enumerable.Range(1, 100).Select(i => $"This is a much longer error message {i} that contains detailed information about what went wrong in the operation").ToArray();
        
        // Setup error lists
        _smallErrorList = _smallErrorSet.ToList();
        _mediumErrorList = _mediumErrorSet.ToList();
        _largeErrorList = _largeErrorSet.ToList();
    }

    /// <summary>
    /// Benchmarks the FormatErrorsString method using arrays as input.
    /// Tests the performance of the optimized string formatting with array collections.
    /// </summary>
    /// <returns>Formatted error string containing all errors with prefix.</returns>
    [Benchmark]
    public string FormatErrorsString_Array()
    {
        var errors = Enumerable.Range(1, ErrorCount).Select(i => $"Error {i}").ToArray();
        return Result.FormatErrorsString(errors, TestPrefix);
    }

    /// <summary>
    /// Benchmarks the FormatErrorsString method using Lists as input.
    /// Tests performance differences between array and list collection types.
    /// </summary>
    /// <returns>Formatted error string containing all errors with prefix.</returns>
    [Benchmark]
    public string FormatErrorsString_List()
    {
        var errors = Enumerable.Range(1, ErrorCount).Select(i => $"Error {i}").ToList();
        return Result.FormatErrorsString(errors, TestPrefix);
    }

    /// <summary>
    /// Benchmarks the FormatErrorsString method using IEnumerable as input.
    /// Tests performance with lazy enumeration vs. materialized collections.
    /// </summary>
    /// <returns>Formatted error string containing all errors with prefix.</returns>
    [Benchmark]
    public string FormatErrorsString_Enumerable()
    {
        var errors = Enumerable.Range(1, ErrorCount).Select(i => $"Error {i}");
        return Result.FormatErrorsString(errors, TestPrefix);
    }

    /// <summary>
    /// Benchmarks ToString() method on Results with a single error message.
    /// Tests the baseline performance for simple error formatting.
    /// </summary>
    /// <returns>String representation of the Result with one error.</returns>
    [Benchmark]
    public string ToString_SingleError()
    {
        var result = Result.WithFailure("Single error message");
        return result.ToString();
    }

    /// <summary>
    /// Benchmarks ToString() method on Results with a small error collection.
    /// Tests Span&lt;T&gt; optimization performance with 5 errors.
    /// </summary>
    /// <returns>String representation of the Result with small error set.</returns>
    [Benchmark]
    public string ToString_SmallErrorSet()
    {
        var result = Result.WithFailure(_smallErrorSet);
        return result.ToString();
    }

    /// <summary>
    /// Benchmarks ToString() method on Results with a medium error collection.
    /// Tests performance at the edge of Span&lt;T&gt; optimization (16 errors).
    /// </summary>
    /// <returns>String representation of the Result with medium error set.</returns>
    [Benchmark]
    public string ToString_MediumErrorSet()
    {
        var result = Result.WithFailure(_mediumErrorSet);
        return result.ToString();
    }

    /// <summary>
    /// Benchmarks ToString() method on Results with a large error collection.
    /// Tests performance when fallback methods are used beyond Span&lt;T&gt; optimization limits.
    /// </summary>
    /// <returns>String representation of the Result with large error set.</returns>
    [Benchmark]
    public string ToString_LargeErrorSet()
    {
        var result = Result.WithFailure(_largeErrorSet);
        return result.ToString();
    }

    /// <summary>
    /// Benchmarks CombineErrors operation with small error collections.
    /// Tests performance of error aggregation with collections that fit in Span&lt;T&gt; optimization.
    /// </summary>
    /// <returns>Combined Result containing errors from both input collections.</returns>
    [Benchmark]
    public Result CombineErrors_SmallCollections()
    {
        var primary = new[] { "Primary 1", "Primary 2" };
        var secondary = new[] { "Secondary 1", "Secondary 2" };
        return Result.CombineErrors(primary, secondary);
    }

    /// <summary>
    /// Benchmarks CombineErrors operation with medium-sized error collections.
    /// Tests performance as collections approach Span&lt;T&gt; optimization limits.
    /// </summary>
    /// <returns>Combined Result containing errors from both input collections.</returns>
    [Benchmark]
    public Result CombineErrors_MediumCollections()
    {
        return Result.CombineErrors(_smallErrorSet, _mediumErrorSet);
    }

    /// <summary>
    /// Benchmarks CombineErrors operation with large error collections.
    /// Tests performance when collections exceed Span&lt;T&gt; limits and use fallback methods.
    /// </summary>
    /// <returns>Combined Result containing errors from both input collections.</returns>
    [Benchmark]
    public Result CombineErrors_LargeCollections()
    {
        return Result.CombineErrors(_mediumErrorSet, _largeErrorSet);
    }

    /// <summary>
    /// Benchmarks CombineErrors operation when one collection is null.
    /// Tests null handling performance in error aggregation scenarios.
    /// </summary>
    /// <returns>Result containing errors from the non-null collection.</returns>
    [Benchmark]
    public Result CombineErrors_OneNull()
    {
        return Result.CombineErrors(_smallErrorSet, null);
    }

    /// <summary>
    /// Benchmarks CombineErrors operation when both collections are null.
    /// Tests edge case handling performance in error aggregation.
    /// </summary>
    /// <returns>Result representing the null input state.</returns>
    [Benchmark]
    public Result CombineErrors_BothNull()
    {
        return Result.CombineErrors(null, null);
    }

    /// <summary>
    /// Benchmarks CombineErrors operation with empty error collections.
    /// Tests performance with zero-length collections.
    /// </summary>
    /// <returns>Result representing the empty collection state.</returns>
    [Benchmark]
    public Result CombineErrors_EmptyCollections()
    {
        return Result.CombineErrors(Array.Empty<string>(), Array.Empty<string>());
    }

    /// <summary>
    /// Benchmarks formatting of very short error messages.
    /// Tests performance characteristics with minimal string content.
    /// </summary>
    /// <returns>Formatted string with short error messages.</returns>
    [Benchmark]
    public string FormatShortMessages()
    {
        var errors = Enumerable.Range(1, ErrorCount).Select(i => $"E{i}").ToArray();
        return Result.FormatErrorsString(errors, TestPrefix);
    }

    /// <summary>
    /// Benchmarks formatting of very long error messages.
    /// Tests performance impact of large string content on formatting operations.
    /// </summary>
    /// <returns>Formatted string with long error messages.</returns>
    [Benchmark]
    public string FormatLongMessages()
    {
        var errors = Enumerable.Range(1, ErrorCount)
            .Select(i => new string('X', 50) + $" Error {i} " + new string('Y', 50))
            .ToArray();
        return Result.FormatErrorsString(errors, TestPrefix);
    }

    /// <summary>
    /// Benchmarks formatting with null entries in the error collection.
    /// Tests null handling performance in string formatting operations.
    /// </summary>
    /// <returns>Formatted string with some null error entries filtered out.</returns>
    [Benchmark]
    public string FormatWithNullErrors()
    {
        var errors = Enumerable.Range(1, ErrorCount)
            .Select(i => i % 3 == 0 ? null : $"Error {i}")
            .ToArray();
        return Result.FormatErrorsString(errors.Where(e => e != null)!, TestPrefix);
    }

    /// <summary>
    /// Benchmarks creation of Results with dynamically generated error collections.
    /// Tests performance of Result construction with varying collection sizes.
    /// </summary>
    /// <returns>Failed Result containing the dynamically generated error collection.</returns>
    [Benchmark]
    public Result MultipleFailureCreation()
    {
        var errors = new List<string>(ErrorCount);
        for (int i = 0; i < ErrorCount; i++)
        {
            errors.Add($"Dynamic error {i} generated during operation");
        }
        return Result.WithFailure(errors);
    }

    /// <summary>
    /// Benchmarks complex error formatting involving multiple Result combinations.
    /// Tests performance of string formatting in compound error scenarios.
    /// </summary>
    /// <returns>String representation of combined Results with multiple error sets.</returns>
    [Benchmark]
    public string ComplexErrorFormatting()
    {
        var result = Result.WithFailure(_smallErrorSet)
            .Combine(
                Result.WithFailure(_mediumErrorSet),
                Result.WithFailure("Additional error")
            );
        return result.ToString();
    }

    /// <summary>
    /// Benchmarks iteration over error collections within Results.
    /// Tests enumeration performance and string access patterns.
    /// </summary>
    /// <returns>Total character count of all error messages (for preventing optimization).</returns>
    [Benchmark]
    public int ErrorEnumerationPerformance()
    {
        var result = Result.WithFailure(_largeErrorSet);
        var count = 0;
        foreach (var error in result.Errors)
        {
            count += error.Length;
        }
        return count;
    }
}