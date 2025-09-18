namespace IndQuestResults.Benchmarks;

/// <summary>
/// Benchmarks comparing Result pattern operations against traditional approaches.
/// Measures performance of core Result operations including creation, checking, error handling,
/// and functional operations like Map, Bind, and Match.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RPlotExporter]
public class ComparisonBenchmarks
{
    private Result _successResult = null!;
    private Result _failureResult = null!;
    private Result<int> _successResultWithValue = null!;
    private Result<int> _failureResultWithValue = null!;
    private List<string> _errors = null!;

    /// <summary>
    /// Initializes test data for benchmarks including success/failure results and error collections.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _successResult = Result.Success();
        _errors = Enumerable.Range(1, 10).Select(i => $"Error {i}").ToList();
        _failureResult = Result.WithFailure(_errors);
        _successResultWithValue = Result<int>.Success(42);
        _failureResultWithValue = Result<int>.WithFailure(_errors);
    }

    /// <summary>
    /// Benchmarks the creation of a successful Result without a value.
    /// </summary>
    /// <returns>A successful Result instance.</returns>
    [Benchmark]
    public Result CreateSuccess() => Result.Success();

    /// <summary>
    /// Benchmarks the creation of a successful Result with a typed value.
    /// </summary>
    /// <returns>A successful Result&lt;int&gt; instance with value 42.</returns>
    [Benchmark]
    public Result<int> CreateSuccessWithValue() => Result<int>.Success(42);

    /// <summary>
    /// Benchmarks the creation of a failed Result with a single error message.
    /// </summary>
    /// <returns>A failed Result instance with one error.</returns>
    [Benchmark]
    public Result CreateFailure() => Result.WithFailure("Operation failed");

    /// <summary>
    /// Benchmarks the creation of a failed Result with multiple error messages.
    /// </summary>
    /// <returns>A failed Result instance with multiple errors.</returns>
    [Benchmark]
    public Result CreateFailureWithMultipleErrors() => Result.WithFailure(_errors);

    /// <summary>
    /// Benchmarks checking if a Result represents a successful operation.
    /// </summary>
    /// <returns>True if the result is successful, false otherwise.</returns>
    [Benchmark]
    public bool CheckSuccess() => _successResult.IsSuccess;

    /// <summary>
    /// Benchmarks checking if a Result represents a failed operation.
    /// </summary>
    /// <returns>True if the result is failed, false otherwise.</returns>
    [Benchmark]
    public bool CheckFailure() => _failureResult.IsFailure;

    /// <summary>
    /// Benchmarks retrieving the first error message from a failed Result.
    /// </summary>
    /// <returns>The first error message string.</returns>
    [Benchmark]
    public string? GetFirstError() => _failureResult.Error;

    /// <summary>
    /// Benchmarks retrieving all error messages from a failed Result.
    /// </summary>
    /// <returns>Collection of all error messages.</returns>
    [Benchmark]
    public IEnumerable<string> GetAllErrors() => _failureResult.Errors;

    /// <summary>
    /// Benchmarks combining multiple Results into a single Result.
    /// Tests the performance of error aggregation when combining failed and successful results.
    /// </summary>
    /// <returns>A combined Result containing all errors from failed input results.</returns>
    [Benchmark]
    public Result CombineMultipleResults()
    {
        var result1 = Result.WithFailure("Error 1");
        var result2 = Result.WithFailure("Error 2");
        var result3 = Result.Success();
        return result1.Combine(result2, result3);
    }

    /// <summary>
    /// Benchmarks the Map operation that transforms a successful Result without a value into a Result with a value.
    /// </summary>
    /// <returns>A Result&lt;int&gt; containing the mapped value or propagated errors.</returns>
    [Benchmark]
    public Result<int> MapOperation()
    {
        return _successResult.Map(() => 42);
    }

    /// <summary>
    /// Benchmarks the Bind operation that chains Results monodically, allowing for composable error handling.
    /// </summary>
    /// <returns>A Result&lt;int&gt; from the bound operation or propagated errors.</returns>
    [Benchmark]
    public Result<int> BindOperation()
    {
        return _successResult.Bind(() => Result<int>.Success(42));
    }

    /// <summary>
    /// Benchmarks the Match operation on a successful Result, measuring pattern matching performance.
    /// </summary>
    /// <returns>String representation based on success/failure state.</returns>
    [Benchmark]
    public Result<string> MatchOperationSuccess()
    {
        return _successResultWithValue.Map(value => $"Success: {value}");
    }

    /// <summary>
    /// Benchmarks the Match operation on a failed Result, measuring pattern matching performance with error handling.
    /// </summary>
    /// <returns>String representation based on success/failure state.</returns>
    [Benchmark]
    public Result<string> MatchOperationFailure()
    {
        return _failureResultWithValue.Bind(value => Result<string>.WithFailure(_failureResultWithValue.Errors));
    }

    /// <summary>
    /// Benchmarks the Ensure operation that adds conditional validation to a Result.
    /// </summary>
    /// <returns>Original Result if condition passes, or new failed Result if condition fails.</returns>
    [Benchmark]
    public Result EnsureOperation()
    {
        return _successResult.Ensure(() => true, "Condition failed");
    }

    /// <summary>
    /// Benchmarks the Recover operation that allows providing fallback logic for failed Results.
    /// </summary>
    /// <returns>Recovery Result if original failed, or original Result if successful.</returns>
    [Benchmark]
    public Result RecoverOperation()
    {
        return _failureResult.Recover(() => Result.Success());
    }

    /// <summary>
    /// Benchmarks string conversion of a successful Result.
    /// </summary>
    /// <returns>String representation of the successful Result.</returns>
    [Benchmark]
    public string ToStringSuccess()
    {
        return _successResult.ToString();
    }

    /// <summary>
    /// Benchmarks string conversion of a failed Result with multiple errors.
    /// </summary>
    /// <returns>String representation of the failed Result including all errors.</returns>
    [Benchmark]
    public string ToStringFailure()
    {
        return _failureResult.ToString();
    }

    /// <summary>
    /// Benchmarks the static CombineErrors operation that merges two error collections.
    /// Tests the performance of error aggregation utility methods.
    /// </summary>
    /// <returns>A failed Result containing all errors from both input collections.</returns>
    [Benchmark]
    public Result CombineErrorsOperation()
    {
        var primaryErrors = new[] { "Primary 1", "Primary 2" };
        var secondaryErrors = new[] { "Secondary 1", "Secondary 2" };
        return Result.CombineErrors(primaryErrors, secondaryErrors);
    }

    /// <summary>
    /// Benchmarks a chain of Result operations including Ensure and Tap.
    /// Measures the performance overhead of fluent operation chaining.
    /// </summary>
    /// <returns>Final Result after all chained operations.</returns>
    [Benchmark]
    public Result ChainedOperations()
    {
        return Result.Success()
            .Ensure(() => true, "Check 1 failed")
            .Tap(() => { /* Side effect */ })
            .Ensure(() => true, "Check 2 failed");
    }

    /// <summary>
    /// Benchmarks a complex chain of generic Result operations including Map and Bind.
    /// Tests the performance of functional composition with typed Results.
    /// </summary>
    /// <returns>Final typed Result after all transformations and operations.</returns>
    [Benchmark]
    public Result<string> ChainedGenericOperations()
    {
        return Result<int>.Success(10)
            .Map(x => x * 2)
            .Bind(x => Result<int>.Success(x + 5))
            .Map(x => x.ToString());
    }
}
