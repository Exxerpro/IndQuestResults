using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using IndQuestResults.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndQuestResults.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[RPlotExporter]
public class ComparisonBenchmarks
{
    private Result _successResult;
    private Result _failureResult;
    private Result<int> _successResultWithValue;
    private Result<int> _failureResultWithValue;
    private List<string> _errors;

    [GlobalSetup]
    public void Setup()
    {
        _successResult = Result.Success();
        _errors = Enumerable.Range(1, 10).Select(i => $"Error {i}").ToList();
        _failureResult = Result.WithFailure(_errors);
        _successResultWithValue = Result<int>.Success(42);
        _failureResultWithValue = Result<int>.WithFailure(_errors);
    }

    [Benchmark]
    public Result CreateSuccess() => Result.Success();

    [Benchmark]
    public Result<int> CreateSuccessWithValue() => Result<int>.Success(42);

    [Benchmark]
    public Result CreateFailure() => Result.WithFailure("Operation failed");

    [Benchmark]
    public Result CreateFailureWithMultipleErrors() => Result.WithFailure(_errors);

    [Benchmark]
    public bool CheckSuccess() => _successResult.IsSuccess;

    [Benchmark]
    public bool CheckFailure() => _failureResult.IsFailure;

    [Benchmark]
    public string GetFirstError() => _failureResult.Error;

    [Benchmark]
    public IEnumerable<string> GetAllErrors() => _failureResult.Errors;

    [Benchmark]
    public Result CombineMultipleResults()
    {
        var result1 = Result.WithFailure("Error 1");
        var result2 = Result.WithFailure("Error 2");
        var result3 = Result.Success();
        return result1.Combine(result2, result3);
    }

    [Benchmark]
    public Result<int> MapOperation()
    {
        return _successResult.Map(() => 42);
    }

    [Benchmark]
    public Result<int> BindOperation()
    {
        return _successResult.Bind(() => Result<int>.Success(42));
    }

    [Benchmark]
    public string MatchOperationSuccess()
    {
        return _successResultWithValue.Match(
            onSuccess: value => $"Success: {value}",
            onFailure: errors => $"Failure: {string.Join(", ", errors)}"
        );
    }

    [Benchmark]
    public string MatchOperationFailure()
    {
        return _failureResultWithValue.Match(
            onSuccess: value => $"Success: {value}",
            onFailure: errors => $"Failure: {string.Join(", ", errors)}"
        );
    }

    [Benchmark]
    public Result EnsureOperation()
    {
        return _successResult.Ensure(() => true, "Condition failed");
    }

    [Benchmark]
    public Result RecoverOperation()
    {
        return _failureResult.Recover(() => Result.Success());
    }

    [Benchmark]
    public string ToStringSuccess()
    {
        return _successResult.ToString();
    }

    [Benchmark]
    public string ToStringFailure()
    {
        return _failureResult.ToString();
    }

    [Benchmark]
    public Result CombineErrorsOperation()
    {
        var primaryErrors = new[] { "Primary 1", "Primary 2" };
        var secondaryErrors = new[] { "Secondary 1", "Secondary 2" };
        return Result.CombineErrors(primaryErrors, secondaryErrors);
    }

    [Benchmark]
    public Result ChainedOperations()
    {
        return Result.Success()
            .Ensure(() => true, "Check 1 failed")
            .Tap(() => { /* Side effect */ })
            .Ensure(() => true, "Check 2 failed");
    }

    [Benchmark]
    public Result<string> ChainedGenericOperations()
    {
        return Result<int>.Success(10)
            .Map(x => x * 2)
            .Bind(x => Result<int>.Success(x + 5))
            .Map(x => x.ToString());
    }
}