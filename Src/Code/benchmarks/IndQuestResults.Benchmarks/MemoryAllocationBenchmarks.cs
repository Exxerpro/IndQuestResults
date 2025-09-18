namespace IndQuestResults.Benchmarks;

/// <summary>
/// Extension methods providing utility operations for benchmark scenarios.
/// </summary>
public static class BenchmarkExtensions
{
    /// <summary>
    /// Converts a generic Result&lt;T&gt; to a non-generic Result for compatibility scenarios.
    /// Preserves success/failure state and error information while discarding the typed value.
    /// </summary>
    /// <typeparam name="T">The type parameter of the source Result.</typeparam>
    /// <param name="result">The generic Result to convert.</param>
    /// <returns>A non-generic Result with equivalent success/failure state.</returns>
    public static Result ToNonGeneric<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? Result.Success()
            : Result.WithFailure(result.Errors);
    }
}

/// <summary>
/// Benchmarks focused on memory allocation patterns and garbage collection impact.
/// Validates the claimed 40% reduction in memory pressure and zero allocations for successful operations.
/// Tests various scenarios including boxing avoidance, closure allocations, and collection processing efficiency.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class MemoryAllocationBenchmarks
{
    private Result<int>[] _resultArray = null!;
    private List<Result<int>> _resultList = null!;
    private Result<string> _complexResult = null!;

    /// <summary>
    /// Gets or sets the size of Result collections used in parameterized benchmarks.
    /// Tests memory behavior across different collection sizes to measure scaling characteristics.
    /// </summary>
    [Params(10, 100, 1000)]
    public int CollectionSize { get; set; }

    /// <summary>
    /// Initializes test data with Result collections of varying sizes and success/failure ratios.
    /// Creates arrays and lists for testing different memory access patterns.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _resultArray = new Result<int>[CollectionSize];
        _resultList = new List<Result<int>>(CollectionSize);

        for (int i = 0; i < CollectionSize; i++)
        {
            var result = i % 10 == 0
                ? Result<int>.WithFailure($"Failed at {i}")
                : Result<int>.Success(i);
            _resultArray[i] = result;
            _resultList.Add(result);
        }

        _complexResult = Result<string>.Success("Initial value");
    }

    /// <summary>
    /// Benchmarks creation of successful non-generic Results to validate zero allocation claims.
    /// Tests the memory efficiency of basic Result success creation.
    /// </summary>
    /// <returns>A successful Result instance.</returns>
    [Benchmark]
    public static Result SuccessCreationZeroAllocation()
    {
        return Result.Success();
    }

    /// <summary>
    /// Benchmarks creation of successful generic Results with minimal memory allocation.
    /// Tests allocation patterns when Results contain typed values.
    /// </summary>
    /// <returns>A successful Result&lt;int&gt; instance with value 42.</returns>
    [Benchmark]
    public static Result<int> SuccessWithValueMinimalAllocation()
    {
        return Result<int>.Success(42);
    }

    /// <summary>
    /// Benchmarks creation of failed Results with single error messages.
    /// Tests memory allocation for simple failure scenarios.
    /// </summary>
    /// <returns>A failed Result instance with one error message.</returns>
    [Benchmark]
    public Result SingleErrorFailure()
    {
        return Result.WithFailure("Single error");
    }

    /// <summary>
    /// Benchmarks creation of failed Results with multiple error messages.
    /// Tests memory allocation for complex failure scenarios with error collections.
    /// </summary>
    /// <returns>A failed Result instance with multiple error messages.</returns>
    [Benchmark]
    public Result MultipleErrorsFailure()
    {
        var errors = new[] { "Error 1", "Error 2", "Error 3" };
        return Result.WithFailure(errors);
    }

    /// <summary>
    /// Benchmarks processing Result arrays using Match operations.
    /// Tests memory allocation patterns during Result collection iteration and pattern matching.
    /// </summary>
    /// <returns>Sum of successful values from the Result array.</returns>
    [Benchmark]
    public int ProcessResultArrayAllocations()
    {
        var sum = 0;
        foreach (var result in _resultArray)
        {
            result.Match(
                onSuccess: value => sum += value,
                onFailure: _ => sum += 0
            );
        }
        return sum;
    }

    /// <summary>
    /// Benchmarks extracting successful values into a new collection.
    /// Tests memory allocation when creating collections from Result processing.
    /// </summary>
    /// <returns>List containing all successful values from the Result array.</returns>
    [Benchmark]
    public List<int> ExtractSuccessValuesWithAllocations()
    {
        var values = new List<int>();
        foreach (var result in _resultArray)
        {
            if (result.IsSuccess)
            {
                values.Add(result.Value);
            }
        }
        return values;
    }

    /// <summary>
    /// Benchmarks processing Results without creating intermediate collections.
    /// Tests zero-allocation patterns for Result collection processing.
    /// </summary>
    /// <returns>Count of successful Results without allocating a collection.</returns>
    [Benchmark]
    public int ExtractSuccessValuesNoAllocations()
    {
        var count = 0;
        foreach (var result in _resultArray)
        {
            if (result.IsSuccess)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Benchmarks chained operations that create intermediate Result instances.
    /// Tests memory allocation during fluent API chaining with transformations.
    /// </summary>
    /// <returns>Result after a chain of string transformation operations.</returns>
    [Benchmark]
    public Result<string> ChainOperationsAllocations()
    {
        return _complexResult
            .Map(s => s.ToUpper())
            .Map(s => s.Replace(" ", "_"))
            .Map(s => $"[{s}]")
            .Ensure(s => s.Length < 100, "Too long");
    }

    /// <summary>
    /// Benchmarks error aggregation using LINQ and string operations.
    /// Tests memory allocation patterns during error collection processing.
    /// </summary>
    /// <returns>Concatenated string of aggregated error messages.</returns>
    [Benchmark]
    public string ErrorAggregationStringJoin()
    {
        var failedResults = _resultArray.Where(r => r.IsFailure).Take(10);
        var errors = failedResults.SelectMany(r => r.Errors);
        return string.Join(", ", errors);
    }

    /// <summary>
    /// Benchmarks manual error aggregation to compare with LINQ-based approaches.
    /// Tests allocation differences between manual iteration and LINQ operations.
    /// </summary>
    /// <returns>Concatenated string of manually aggregated error messages.</returns>
    [Benchmark]
    public string ErrorAggregationManual()
    {
        var errorList = new List<string>();
        var count = 0;

        foreach (var result in _resultArray)
        {
            if (result.IsFailure && count < 10)
            {
                foreach (var error in result.Errors)
                {
                    errorList.Add(error);
                }
                count++;
            }
        }

        return string.Join(", ", errorList);
    }

    /// <summary>
    /// Benchmarks combining a small set of Results with minimal allocations.
    /// Tests memory efficiency of Result combination operations on small collections.
    /// </summary>
    /// <returns>Combined Result containing all errors from failed input Results.</returns>
    [Benchmark]
    public Result CombineResultsSmallSet()
    {
        return _resultArray.Take(5).Aggregate(
            Result.Success(),
            (acc, result) => acc.Combine(result.ToNonGeneric())
        );
    }

    /// <summary>
    /// Benchmarks combining large sets of Results to test scaling characteristics.
    /// Tests memory allocation patterns when combining many Results simultaneously.
    /// </summary>
    /// <returns>Combined Result containing all errors from the large Result set.</returns>
    [Benchmark]
    public Result CombineResultsLargeSet()
    {
        var results = _resultArray.Select(r => r.ToNonGeneric()).ToArray();
        return results[0].Combine(results.Skip(1).ToArray());
    }

    /// <summary>
    /// Benchmarks generic value access to demonstrate boxing avoidance.
    /// Tests memory efficiency when accessing typed values from generic Results.
    /// </summary>
    /// <returns>Sum of values accessed without boxing overhead.</returns>
    [Benchmark]
    public int BoxingAvoidanceGeneric()
    {
        var sum = 0;
        foreach (var result in _resultArray)
        {
            if (result.IsSuccess)
            {
                sum += result.Value; // No boxing
            }
        }
        return sum;
    }

    /// <summary>
    /// Benchmarks scenarios where boxing occurs for comparison with generic access.
    /// Tests memory allocation impact of boxing/unboxing operations.
    /// </summary>
    /// <returns>Sum of values accessed through boxing/unboxing operations.</returns>
    [Benchmark]
    public int BoxingScenarioObject()
    {
        var sum = 0;
        foreach (var result in _resultArray)
        {
            if (result.IsSuccess)
            {
                object value = result.Value; // Boxing occurs
                sum += (int)value; // Unboxing
            }
        }
        return sum;
    }

    /// <summary>
    /// Benchmarks Recover operations that perform allocations during recovery logic.
    /// Tests memory allocation patterns in error recovery scenarios with intermediate collections.
    /// </summary>
    /// <returns>Recovered Result with value computed through allocation-heavy operations.</returns>
    [Benchmark]
    public Result<int> RecoverWithAllocation()
    {
        return Result<int>.WithFailure("Initial failure")
            .Recover(() =>
            {
                var items = Enumerable.Range(1, 10).ToList();
                return Result<int>.Success(items.Sum());
            });
    }

    /// <summary>
    /// Benchmarks Recover operations that avoid allocations during recovery logic.
    /// Tests memory efficiency of optimized error recovery patterns.
    /// </summary>
    /// <returns>Recovered Result with pre-computed value avoiding allocations.</returns>
    [Benchmark]
    public Result<int> RecoverWithoutAllocation()
    {
        return Result<int>.WithFailure("Initial failure")
            .Recover(() => Result<int>.Success(55)); // 1+2+...+10 = 55
    }

    /// <summary>
    /// Benchmarks Tap operations that create closures, causing additional allocations.
    /// Tests memory allocation impact of closure creation in side-effect operations.
    /// </summary>
    [Benchmark]
    public void TapWithClosure()
    {
        var capturedValue = 0;

        foreach (var result in _resultArray.Take(10))
        {
            result.Tap(value => capturedValue += value); // Closure allocation
        }
    }

    /// <summary>
    /// Benchmarks Tap operations that avoid closure creation for comparison.
    /// Tests memory efficiency of Tap operations without captured variables.
    /// </summary>
    [Benchmark]
    public void TapWithoutClosure()
    {
        foreach (var result in _resultArray.Take(10))
        {
            result.Tap(value => Console.WriteLine(value)); // No closure
        }
    }

    /// <summary>
    /// Benchmarks array transformations through Result chains.
    /// Tests memory allocation patterns during collection transformations with Results.
    /// </summary>
    /// <returns>Result containing transformed string array from successful integer values.</returns>
    [Benchmark]
    public Result<string[]> ArrayTransformation()
    {
        return Result<int[]>.Success(_resultArray.Where(r => r.IsSuccess).Select(r => r.Value).ToArray())
            .Map(arr => arr.Select(i => i.ToString()).ToArray());
    }

    /// <summary>
    /// Benchmarks lazy enumeration of error messages for memory efficiency comparison.
    /// Tests deferred execution patterns that minimize upfront allocations.
    /// </summary>
    /// <returns>Lazy enumerable of error messages from failed Results.</returns>
    [Benchmark]
    public IEnumerable<string> LazyErrorEnumeration()
    {
        return _resultArray
            .Where(r => r.IsFailure)
            .SelectMany(r => r.Errors);
    }

    /// <summary>
    /// Benchmarks eager materialization of error collections for comparison with lazy approaches.
    /// Tests allocation patterns when immediately materializing error enumerables.
    /// </summary>
    /// <returns>Materialized list of error messages from failed Results.</returns>
    [Benchmark]
    public List<string> EagerErrorCollection()
    {
        return _resultArray
            .Where(r => r.IsFailure)
            .SelectMany(r => r.Errors)
            .ToList();
    }
}
