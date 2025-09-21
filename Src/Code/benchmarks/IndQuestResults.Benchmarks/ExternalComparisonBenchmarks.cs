namespace IndQuestResults.Benchmarks;

/// <summary>
/// Optional comparison benchmarks against other Result libraries. By default, only
/// IndQuestResults cases are compiled. To include external libraries, add packages
/// to this benchmarks project and define symbols as needed (see README.md).
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class ExternalComparisonBenchmarks
{
    private IEnumerable<int> _inputs = null!;

    /// <summary>
    /// Initializes shared input data for all benchmark runs.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _inputs = Enumerable.Range(1, 100);
    }

    // --- IndQuestResults baselines ---

    /// <summary>
    /// Baseline: sequences a collection of successful <see cref="Result{T}"/> values.
    /// </summary>
    [Benchmark(Baseline = true)]
    public Result<IEnumerable<int>> IQR_Sequence_Success()
    {
        var results = _inputs.Select(i => Result<int>.Success(i));
        return IndQuestResults.Collections.ResultCollections.Sequence(results);
    }

    /// <summary>
    /// Sequences a collection that contains failures every 10 items.
    /// </summary>
    [Benchmark]
    public Result<IEnumerable<int>> IQR_Sequence_Failure()
    {
        var results = _inputs.Select(i => i % 10 == 0 ? Result<int>.WithFailure("err") : Result<int>.Success(i));
        return IndQuestResults.Collections.ResultCollections.Sequence(results);
    }

    /// <summary>
    /// Traverses inputs and maps/binds, producing failures for multiples of 33.
    /// </summary>
    [Benchmark]
    public Result<IEnumerable<int>> IQR_Traverse_MapBind()
    {
        return IndQuestResults.Collections.ResultCollections.Traverse(
            _inputs,
            x => x % 33 == 0 ? Result<int>.WithFailure("bad") : Result<int>.Success(x * 2)
        );
    }

#if FLUENTRESULTS
    // --- FluentResults comparisons ---
    // Requires: dotnet add package FluentResults and -define:FLUENTRESULTS
    [Benchmark]
    public FluentResults.Result<IEnumerable<int>> FR_Sequence_Success()
    {
        var results = _inputs.Select(i => FluentResults.Result.Ok(i));
        // FluentResults has no built-in Sequence; emulate
        var values = new List<int>();
        var errors = new List<string>();
        foreach (var r in results)
        {
            if (r.IsSuccess) values.Add(r.Value);
            else errors.Add(string.Join(", ", r.Reasons.Select(rn => rn.Message)));
        }
        return errors.Count == 0 ? FluentResults.Result.Ok<IEnumerable<int>>(values) : FluentResults.Result.Fail<IEnumerable<int>>(string.Join(", ", errors));
    }
#endif

#if CSFEXT
    // --- CSharpFunctionalExtensions comparisons ---
    // Requires: dotnet add package CSharpFunctionalExtensions and -define:CSFEXT
    [Benchmark]
    public CSharpFunctionalExtensions.Result<IEnumerable<int>> CSFE_Sequence_Success()
    {
        var results = _inputs.Select(i => CSharpFunctionalExtensions.Result.Success(i));
        var values = new List<int>();
        foreach (var r in results)
        {
            if (r.IsSuccess) values.Add(r.Value);
            else return CSharpFunctionalExtensions.Result.Failure<IEnumerable<int>>(r.Error);
        }
        return CSharpFunctionalExtensions.Result.Success<IEnumerable<int>>(values);
    }
#endif
}

