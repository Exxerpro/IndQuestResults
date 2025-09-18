namespace IndQuestResults.Benchmarks;

/// <summary>
/// Benchmarks specifically testing Span&lt;T&gt; optimizations and their performance impact.
/// Validates the claimed 70% reduction in allocations through stack-allocated Span usage
/// for small error collections (≤16 items) versus traditional heap-allocated approaches.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RPlotExporter]
public class SpanOptimizationBenchmarks
{
    private string[] _smallErrorArray = null!;
    private string[] _mediumErrorArray = null!;
    private string[] _largeErrorArray = null!;
    private List<string> _smallErrorList = null!;
    private List<string> _mediumErrorList = null!;
    private List<string> _largeErrorList = null!;

    /// <summary>
    /// Gets or sets the error collection size for parameterized Span optimization tests.
    /// Tests performance across the Span optimization threshold (16 items) and beyond.
    /// </summary>
    [Params(4, 8, 16, 32, 64, 128)]
    public int ErrorSize { get; set; }

    /// <summary>
    /// Initializes test data with error collections of various sizes for Span optimization testing.
    /// Creates arrays and lists to compare Span-optimized vs. traditional collection processing.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // Small errors (suitable for Span optimization)
        _smallErrorArray = Enumerable.Range(1, 8).Select(i => $"Err{i}").ToArray();
        _smallErrorList = _smallErrorArray.ToList();

        // Medium errors (edge case for Span vs StringBuilder)
        _mediumErrorArray = Enumerable.Range(1, 16).Select(i => $"Medium error message {i}").ToArray();
        _mediumErrorList = _mediumErrorArray.ToList();

        // Large errors (should use StringBuilder)
        _largeErrorArray = Enumerable.Range(1, 100).Select(i => $"This is a large error message number {i} with additional context").ToArray();
        _largeErrorList = _largeErrorArray.ToList();
    }

    /// <summary>
    /// Benchmarks error formatting using Span optimizations.
    /// </summary>
    [Benchmark]
    public string FormatWithSpanOptimization()
    {
        var errors = GenerateErrors(ErrorSize);
        return Result.FormatErrorsString(errors, "Operation Failed");
    }

    /// <summary>
    /// Benchmarks error formatting using StringBuilder (baseline).
    /// </summary>
    [Benchmark(Baseline = true)]
    public string FormatWithStringBuilder()
    {
        var errors = GenerateErrors(ErrorSize);
        return FormatUsingStringBuilder(errors, "Operation Failed");
    }

    /// <summary>
    /// Benchmarks small array formatting with Span optimization.
    /// </summary>
    [Benchmark]
    public string FormatSmallArrayWithSpan()
    {
        return Result.FormatErrorsString(_smallErrorArray, "Failed");
    }

    /// <summary>
    /// Benchmarks small list formatting with Span optimization.
    /// </summary>
    [Benchmark]
    public string FormatSmallListWithSpan()
    {
        return Result.FormatErrorsString(_smallErrorList, "Failed");
    }

    /// <summary>
    /// Benchmarks medium array formatting with Span optimization.
    /// </summary>
    [Benchmark]
    public string FormatMediumArrayWithSpan()
    {
        return Result.FormatErrorsString(_mediumErrorArray, "Failed");
    }

    /// <summary>
    /// Benchmarks large array formatting fallback to StringBuilder.
    /// </summary>
    [Benchmark]
    public string FormatLargeArrayFallback()
    {
        return Result.FormatErrorsString(_largeErrorArray, "Failed");
    }

    /// <summary>
    /// Benchmarks combining small error collections using Span optimization.
    /// </summary>
    [Benchmark]
    public Result CombineErrorsSmallSpan()
    {
        var primary = new[] { "P1", "P2", "P3", "P4" };
        var secondary = new[] { "S1", "S2", "S3", "S4" };
        return Result.CombineErrors(primary, secondary);
    }

    /// <summary>
    /// Benchmarks combining medium error collections using Span optimization.
    /// </summary>
    [Benchmark]
    public Result CombineErrorsMediumSpan()
    {
        return Result.CombineErrors(_smallErrorArray, _mediumErrorArray);
    }

    /// <summary>
    /// Benchmarks combining large error collections with StringBuilder fallback.
    /// </summary>
    [Benchmark]
    public Result CombineErrorsLargeFallback()
    {
        return Result.CombineErrors(_mediumErrorArray, _largeErrorArray);
    }

    /// <summary>
    /// Benchmarks formatting errors with variable message lengths.
    /// </summary>
    [Benchmark]
    public string FormatVariableLengthErrors()
    {
        var errors = Enumerable.Range(1, ErrorSize)
            .Select(i => new string('X', i))
            .ToArray();
        return Result.FormatErrorsString(errors, "Variable");
    }

    /// <summary>
    /// Benchmarks multiple small error collection combinations.
    /// </summary>
    [Benchmark]
    public Result MultipleSmallCombines()
    {
        var result = Result.Success();

        for (int i = 0; i < 10; i++)
        {
            var errors = new[] { $"E{i}1", $"E{i}2" };
            result = result.Combine(Result.WithFailure(errors));
        }

        return result;
    }

    /// <summary>
    /// Benchmarks formatting empty error collections.
    /// </summary>
    [Benchmark]
    public string FormatEmptyErrors()
    {
        return Result.FormatErrorsString([], "Empty");
    }

    /// <summary>
    /// Benchmarks formatting single error messages.
    /// </summary>
    [Benchmark]
    public string FormatSingleError()
    {
        return Result.FormatErrorsString(["Single error"], "Failed");
    }

    /// <summary>
    /// Benchmarks formatting error arrays containing null values.
    /// </summary>
    [Benchmark]
    public string FormatWithNullsInArray()
    {
        var errors = new string?[] { "Error1", null, "Error3", null, "Error5" };
        return Result.FormatErrorsString(errors.Where(e => e != null)!, "WithNulls");
    }

    /// <summary>
    /// Benchmarks to determine optimal Span vs StringBuilder threshold.
    /// </summary>
    [Benchmark]
    public int SpanVsStringBuilderThreshold()
    {
        var count = 0;

        // Test different sizes to find optimal threshold
        for (int size = 1; size <= 32; size++)
        {
            var errors = Enumerable.Range(1, size).Select(i => $"E{i}").ToArray();
            var result = Result.FormatErrorsString(errors, "Test");
            count += result.Length;
        }

        return count;
    }

    /// <summary>
    /// Benchmarks combining Results with various error collection sizes.
    /// </summary>
    [Benchmark]
    public Result CombineWithVariousSizes()
    {
        var results = new[]
        {
            Result.WithFailure(["A"]),
            Result.WithFailure(["B1", "B2"]),
            Result.WithFailure(["C1", "C2", "C3", "C4"]),
            Result.WithFailure(_smallErrorArray),
            Result.Success()
        };

        return results[0].Combine(results.Skip(1).ToArray());
    }

    private string[] GenerateErrors(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => $"Error message {i}")
            .ToArray();
    }

    private static string FormatUsingStringBuilder(IEnumerable<string> errors, string prefix)
    {
        var sb = new StringBuilder($"{prefix}: ");
        var isFirst = true;

        foreach (var error in errors)
        {
            if (!isFirst)
            {
                sb.Append(", ");
            }

            sb.Append(error);
            isFirst = false;
        }

        return sb.ToString();
    }
}
