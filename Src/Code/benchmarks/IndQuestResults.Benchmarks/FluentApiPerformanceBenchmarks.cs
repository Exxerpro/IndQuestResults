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
public class FluentApiPerformanceBenchmarks
{
    private Result<int> _startValue;
    private Result _baseResult;
    private List<Result<int>> _resultCollection;

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

    [Benchmark]
    public Result<string> SimpleChain()
    {
        return _startValue
            .Map(x => x * 2)
            .Map(x => x + 5)
            .Map(x => x.ToString());
    }

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

    [Benchmark]
    public string MatchWithComplexLogic()
    {
        return _startValue
            .Map(x => x * 3)
            .Bind(x => Result<double>.Success(Math.Pow(x, 2)))
            .Match(
                onSuccess: value => 
                {
                    var result = $"Success: {value:F2}";
                    // Simulate complex processing
                    for (int i = 0; i < 10; i++)
                    {
                        result = result.ToUpper();
                        result = result.ToLower();
                    }
                    return result;
                },
                onFailure: errors => string.Join(" | ", errors)
            );
    }

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

    [Benchmark]
    public int AggregateResults()
    {
        return _resultCollection
            .Where(r => r.IsSuccess)
            .Select(r => r.Map(x => x * 2))
            .Aggregate(0, (sum, result) => 
                result.Match(
                    onSuccess: value => sum + value,
                    onFailure: _ => sum
                ));
    }

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

    [Benchmark]
    public Result<string> DeepNesting()
    {
        return _startValue.Bind(a =>
            Result<int>.Success(a + 1).Bind(b =>
                Result<int>.Success(b * 2).Bind(c =>
                    Result<int>.Success(c - 3).Bind(d =>
                        Result<string>.Success($"Result: {a},{b},{c},{d}")))));
    }

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