namespace IndQuestResults.Benchmarks;

/// <summary>
/// Benchmarks focused on Result creation performance across different scenarios.
/// Tests construction costs for successful and failed Results with varying error collection sizes.
/// Validates performance characteristics of Result instantiation patterns.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RPlotExporter]
public class ResultCreationBenchmarks
{
    private string[] _errorArray = null!;
    private string _singleError = null!;

    /// <summary>
    /// Gets or sets the number of error messages used in parameterized creation benchmarks.
    /// Tests how Result creation performance scales with error collection size.
    /// </summary>
    [Params(0, 1, 5, 10, 50, 100)]
    public int ErrorCount { get; set; }

    /// <summary>
    /// Initializes test data including error messages and collections for creation benchmarks.
    /// Sets up error arrays of varying sizes to test scaling characteristics.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _singleError = "Operation failed due to invalid input";
        _errorArray = ErrorCount > 0
            ? Enumerable.Range(1, ErrorCount).Select(i => $"Error message number {i}").ToArray()
            : [];
    }

    /// <summary>
    /// Benchmarks creation of a successful Result without value.
    /// </summary>
    [Benchmark]
    public Result CreateSuccessResult()
    {
        return Result.Success();
    }

    /// <summary>
    /// Benchmarks creation of a successful Result with integer value.
    /// </summary>
    [Benchmark]
    public Result<int> CreateSuccessResultWithValue()
    {
        return Result<int>.Success(42);
    }

    /// <summary>
    /// Benchmarks creation of a successful Result with string value.
    /// </summary>
    [Benchmark]
    public Result<string> CreateSuccessResultWithStringValue()
    {
        return Result<string>.Success("Success value");
    }

    /// <summary>
    /// Benchmarks creation of a successful Result with complex object value.
    /// </summary>
    [Benchmark]
    public Result<object> CreateSuccessResultWithComplexObject()
    {
        return Result<object>.Success(new { Id = 1, Name = "Test", Values = new[] { 1, 2, 3 } });
    }

    /// <summary>
    /// Benchmarks creation of a failed Result with single error.
    /// </summary>
    [Benchmark]
    public Result CreateFailureResultSingleError()
    {
        return Result.WithFailure(_singleError);
    }

    /// <summary>
    /// Benchmarks creation of a failed Result with multiple errors.
    /// </summary>
    [Benchmark]
    public Result CreateFailureResultMultipleErrors()
    {
        return Result.WithFailure(_errorArray);
    }

    /// <summary>
    /// Benchmarks creation of a failed Result&lt;int&gt; with single error.
    /// </summary>
    [Benchmark]
    public Result<int> CreateFailureResultWithValueSingleError()
    {
        return Result<int>.WithFailure(_singleError);
    }

    /// <summary>
    /// Benchmarks creation of a failed Result&lt;int&gt; with multiple errors.
    /// </summary>
    [Benchmark]
    public Result<int> CreateFailureResultWithValueMultipleErrors()
    {
        return Result<int>.WithFailure(_errorArray);
    }

    /// <summary>
    /// Benchmarks creation of a failed Result&lt;int&gt; with single error (alternative).
    /// </summary>
    [Benchmark]
    public Result<int> CreateFailureWithSingleError()
    {
        return Result<int>.WithFailure(_singleError);
    }

    /// <summary>
    /// Benchmarks creation of a successful Result with nullable string value.
    /// </summary>
    [Benchmark]
    public Result<string> CreateSuccessNullableValue()
    {
        return Result<string>.Success(null!);
    }

    /// <summary>
    /// Benchmarks creation of a Result with warnings.
    /// </summary>
    [Benchmark]
    public Result<int> CreateWithWarnings()
    {
        var warnings = new[] { "Warning 1", "Warning 2", "Warning 3" };
        return Result<int>.WithWarnings(warnings, 42);
    }

    /// <summary>
    /// Benchmarks conditional Result creation based on parameter.
    /// </summary>
    [Benchmark]
    public Result CreateFromCondition()
    {
        var condition = ErrorCount % 2 == 0;
        return condition ? Result.Success() : Result.WithFailure("Condition failed");
    }

    /// <summary>
    /// Benchmarks creation of generic Result with type constraint.
    /// </summary>
    [Benchmark]
    public Result<T> CreateGenericResult<T>() where T : new()
    {
        return ErrorCount > 50
            ? Result<T>.WithFailure("Too many errors")
            : Result<T>.Success(new T());
    }

    /// <summary>
    /// Benchmarks creation of Result with dynamically generated errors.
    /// </summary>
    [Benchmark]
    public Result CreateWithDynamicErrors()
    {
        var errors = Enumerable.Range(1, ErrorCount)
            .Select(i => $"Dynamic error {i} at {DateTime.UtcNow.Ticks}")
            .ToList();
        return Result.WithFailure(errors);
    }

    /// <summary>
    /// Benchmarks creation of default failure Result.
    /// </summary>
    [Benchmark]
    public Result CreateDefaultFailure()
    {
        return new Result();
    }

    /// <summary>
    /// Benchmarks creation of failure Result with empty error collection.
    /// </summary>
    [Benchmark]
    public Result CreateFailureEmptyErrors()
    {
        return Result.WithFailure([]);
    }

    /// <summary>
    /// Benchmarks creation of failure Result with null error collection.
    /// </summary>
    [Benchmark]
    public Result CreateFailureNullErrors()
    {
        return Result.WithFailure((string[])null!);
    }

    /// <summary>
    /// Benchmarks chained Result creation with pattern matching.
    /// </summary>
    [Benchmark]
    public Result<int> CreateChainedResult()
    {
        return ErrorCount switch
        {
            0 => Result<int>.Success(100),
            < 10 => Result<int>.WithFailure($"Error count too low: {ErrorCount}"),
            < 50 => Result<int>.Success(ErrorCount * 2),
            _ => Result<int>.WithFailure($"Error count too high: {ErrorCount}")
        };
    }

    /// <summary>
    /// Benchmarks Result creation from computation with exception handling.
    /// </summary>
    [Benchmark]
    public Result<double> CreateResultFromComputation()
    {
        try
        {
            var value = Math.Sqrt(ErrorCount);
            return value > 0 ? Result<double>.Success(value) : Result<double>.WithFailure("Invalid computation");
        }
        catch (Exception ex)
        {
            return Result<double>.WithFailure(ex.Message);
        }
    }

    /// <summary>
    /// Benchmarks batch creation and combination of multiple Results.
    /// </summary>
    [Benchmark]
    public Result BatchCreateResults()
    {
        var finalResult = Result.Success();

        for (int i = 0; i < 10; i++)
        {
            var result = i < 5
                ? Result.Success()
                : Result.WithFailure($"Batch error {i}");
            finalResult = finalResult.Combine(result);
        }

        return finalResult;
    }
}
