namespace IndQuestResults.Benchmarks;

/// <summary>
/// Benchmarks testing the performance of fluent API operations and method chaining in Results.
/// Measures the overhead of functional composition patterns and railway-oriented programming constructs
/// including Map, Bind, Ensure, Tap, Match, and Recover operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RPlotExporter]
public class FluentApiPerformanceBenchmarks
{
    private Result<int> _startValue = null!;
    private Result _baseResult = null!;
    private List<Result<int>> _resultCollection = null!;

    /// <summary>
    /// Initializes test data including successful Results and mixed success/failure collections.
    /// Sets up baseline values for measuring fluent API operation overhead.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _startValue = Result<int>.Success(10);
        _baseResult = Result.Success();
        _resultCollection = Enumerable.Range(1, 100)
            .Select(i => i % 10 == 0
                ? Result<int>.WithFailure($"Failed at {i}")
                : Result<int>.Success(i))
            .ToList();
    }

    /// <summary>
    /// Benchmarks a simple chain of Map operations transforming values through a pipeline.
    /// Tests the baseline performance overhead of fluent API chaining.
    /// </summary>
    /// <returns>Result containing the final transformed string value.</returns>
    [Benchmark]
    public Result<string> SimpleChain()
    {
        return _startValue
            .Map(x => x * 2)
            .Map(x => x + 5)
            .Map(x => x.ToString());
    }

    /// <summary>
    /// Benchmarks a complex chain mixing Map, Bind, Ensure, and Tap operations.
    /// Tests performance of comprehensive functional composition with conditional logic.
    /// </summary>
    /// <returns>Result containing the final computed double value or aggregated errors.</returns>
    [Benchmark]
    public Result<double> ComplexChain()
    {
        return _startValue
            .Map(x => x * 2)
            .Bind(x => x > 15 ? Result<int>.Success(x) : Result<int>.WithFailure("Too small"))
            .Map(x => x + 10)
            .Ensure(x => x < 100, "Too large")
            .Map(x => (double)x)
            .Tap(x => { /* Log value */ })
            .Map(x => Math.Sqrt(x));
    }

    /// <summary>
    /// Benchmarks chain execution when an early failure occurs, testing short-circuit behavior.
    /// Measures performance when subsequent operations are skipped due to initial failure.
    /// </summary>
    /// <returns>Result containing the initial failure that bypassed all subsequent operations.</returns>
    [Benchmark]
    public Result<string> ChainWithEarlyFailure()
    {
        return Result<int>.WithFailure("Initial failure")
            .Map(x => x * 2)
            .Bind(x => Result<int>.Success(x + 5))
            .Map(x => x.ToString())
            .Ensure(x => x.Length > 0, "Empty string")
            .Tap(x => { /* Never executed */ });
    }

    /// <summary>
    /// Benchmarks multiple consecutive Ensure operations for validation chaining.
    /// Tests performance of multiple validation steps in sequence.
    /// </summary>
    /// <returns>Result after all validation checks pass or first failure occurs.</returns>
    [Benchmark]
    public Result MultipleEnsures()
    {
        return _baseResult
            .Ensure(() => true, "Check 1")
            .Ensure(() => true, "Check 2")
            .Ensure(() => true, "Check 3")
            .Ensure(() => true, "Check 4")
            .Ensure(() => true, "Check 5");
    }

    /// <summary>
    /// Benchmarks conditional chains using Bind for business logic branching.
    /// Tests performance of conditional execution patterns within Result chains.
    /// </summary>
    /// <returns>Result after conditional processing with range validations.</returns>
    [Benchmark]
    public Result<int> ConditionalChain()
    {
        return _startValue
            .Bind(x => x > 5
                ? Result<int>.Success(x * 2)
                : Result<int>.WithFailure("Value too small"))
            .Bind(x => x < 50
                ? Result<int>.Success(x + 10)
                : Result<int>.WithFailure("Value too large"))
            .Map(x => x - 5);
    }

    /// <summary>
    /// Benchmarks Match operation with complex processing logic in success and failure branches.
    /// Tests performance of pattern matching with computationally intensive operations.
    /// </summary>
    /// <returns>String result from either success processing or error aggregation.</returns>
    [Benchmark]
    public string MatchWithComplexLogic()
    {
        var chainedResult = _startValue
            .Map(x => x * 3)
            .Bind(x => Result<double>.Success(Math.Pow(x, 2)));

        if (chainedResult.IsSuccess)
        {
            var result = $"Success: {chainedResult.Value:F2}";
            // Simulate complex processing
            for (int i = 0; i < 10; i++)
            {
                result = result.ToUpper();
                result = result.ToLower();
            }
            return result;
        }
        else
        {
            return string.Join(" | ", chainedResult.Errors);
        }
    }

    /// <summary>
    /// Benchmarks Recover operation for error handling and fallback logic.
    /// Tests performance of error recovery patterns with multiple recovery attempts.
    /// </summary>
    /// <returns>Result after recovery operations with final transformed value.</returns>
    [Benchmark]
    public Result<int> RecoverChain()
    {
        return Result<int>.WithFailure("Initial error")
            .Recover(() => Result<int>.Success(5))
            .Map(x => x * 2)
            .Bind(x => x > 20
                ? Result<int>.WithFailure("Too large after recovery")
                : Result<int>.Success(x))
            .Recover(() => Result<int>.Success(15));
    }

    /// <summary>
    /// Benchmarks Combine operation for aggregating multiple Results.
    /// Tests performance of error accumulation when combining success and failure Results.
    /// </summary>
    /// <returns>Combined Result containing all errors from failed input Results.</returns>
    [Benchmark]
    public Result CombineChain()
    {
        var result1 = _baseResult.Ensure(() => true, "Check 1");
        var result2 = _baseResult.Ensure(() => false, "Check 2 failed");
        var result3 = _baseResult.Ensure(() => true, "Check 3");

        return result1
            .Combine(result2, result3)
            .Tap(() => { /* Process combined result */ });
    }

    /// <summary>
    /// Benchmarks LINQ integration with Result collections for aggregation operations.
    /// Tests performance when processing collections of Results with functional operations.
    /// </summary>
    /// <returns>Aggregated integer value from successful Results in the collection.</returns>
    [Benchmark]
    public int AggregateResults()
    {
        return _resultCollection
            .Where(r => r.IsSuccess)
            .Select(r => r.Map(x => x * 2))
            .Aggregate(0, (sum, result) =>
                result.IsSuccess ? sum + result.Value : sum);
    }

    /// <summary>
    /// Benchmarks collection processing with OnSuccess and OnFailure callbacks.
    /// Tests performance of side-effect patterns for Result collection processing.
    /// </summary>
    /// <returns>Result containing successful values or aggregated errors from collection processing.</returns>
    [Benchmark]
    public Result<List<int>> CollectSuccessfulResults()
    {
        var successfulValues = new List<int>();
        var errors = new List<string>();

        foreach (var result in _resultCollection)
        {
            result
                .OnSuccess(value => successfulValues.Add(value))
                .OnFailure(errs => errors.AddRange(errs));
        }

        return errors.Any()
            ? Result<List<int>>.WithFailure(errors)
            : Result<List<int>>.Success(successfulValues);
    }

    /// <summary>
    /// Benchmarks deeply nested Bind operations for monadic composition.
    /// Tests performance characteristics of complex nested functional compositions.
    /// </summary>
    /// <returns>Result containing formatted string with all intermediate computation values.</returns>
    [Benchmark]
    public Result<string> DeepNesting()
    {
        return _startValue.Bind(a =>
            Result<int>.Success(a + 1).Bind(b =>
                Result<int>.Success(b * 2).Bind(c =>
                    Result<int>.Success(c - 3).Bind(d =>
                        Result<string>.Success($"Result: {a},{b},{c},{d}")))));
    }

    /// <summary>
    /// Benchmarks multiple Tap operations for side-effect performance measurement.
    /// Tests the overhead of side-effect operations in fluent chains.
    /// </summary>
    /// <returns>Result with final value incorporating side-effect counter.</returns>
    [Benchmark]
    public Result<int> TapPerformance()
    {
        var sideEffectCount = 0;

        return _startValue
            .Tap(x => sideEffectCount++)
            .Tap(x => sideEffectCount++)
            .Tap(x => sideEffectCount++)
            .Tap(x => sideEffectCount++)
            .Tap(x => sideEffectCount++)
            .Map(x => x + sideEffectCount);
    }

    /// <summary>
    /// Benchmarks string transformation pipeline with multiple Map operations.
    /// Tests performance of sequential transformations on string data through Result chains.
    /// </summary>
    /// <returns>Result containing final transformed string after all pipeline operations.</returns>
    [Benchmark]
    public Result<string> TransformationPipeline()
    {
        return _startValue
            .Map(x => x.ToString())
            .Map(x => x.PadLeft(5, '0'))
            .Map(x => x.ToUpper())
            .Map(x => x.Replace('0', 'O'))
            .Ensure(x => x.Length == 5, "Invalid length after transformations");
    }

    /// <summary>
    /// Benchmarks conditional recovery logic with retry-like behavior.
    /// Tests performance of complex recovery patterns with stateful conditions.
    /// </summary>
    /// <returns>Result after conditional recovery attempts with final value transformation.</returns>
    [Benchmark]
    public Result<int> ConditionalRecovery()
    {
        var attempt = 0;

        return Result<int>.WithFailure("Initial failure")
            .Recover(() =>
            {
                attempt++;
                return attempt < 3
                    ? Result<int>.WithFailure($"Attempt {attempt} failed")
                    : Result<int>.Success(42);
            })
            .Map(x => x * 2);
    }
}
