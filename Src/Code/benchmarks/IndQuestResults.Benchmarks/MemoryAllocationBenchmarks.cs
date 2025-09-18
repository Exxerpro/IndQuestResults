using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Diagnosers;
using IndQuestResults.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IndQuestResults.Benchmarks;

public static class BenchmarkExtensions
{
    public static Result ToNonGeneric<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? Result.Success()
            : Result.WithFailure(result.Errors);
    }
}

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public class MemoryAllocationBenchmarks
{
    private Result<int>[] _resultArray;
    private List<Result<int>> _resultList;
    private Result<string> _complexResult;
    
    [Params(10, 100, 1000)]
    public int CollectionSize { get; set; }

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

    [Benchmark]
    public Result SuccessCreation_ZeroAllocation()
    {
        return Result.Success();
    }

    [Benchmark]
    public Result<int> SuccessWithValue_MinimalAllocation()
    {
        return Result<int>.Success(42);
    }

    [Benchmark]
    public Result SingleErrorFailure()
    {
        return Result.WithFailure("Single error");
    }

    [Benchmark]
    public Result MultipleErrorsFailure()
    {
        var errors = new[] { "Error 1", "Error 2", "Error 3" };
        return Result.WithFailure(errors);
    }

    [Benchmark]
    public int ProcessResultArray_Allocations()
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

    [Benchmark]
    public List<int> ExtractSuccessValues_WithAllocations()
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

    [Benchmark]
    public int ExtractSuccessValues_NoAllocations()
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

    [Benchmark]
    public Result<string> ChainOperations_Allocations()
    {
        return _complexResult
            .Map(s => s.ToUpper())
            .Map(s => s.Replace(" ", "_"))
            .Map(s => $"[{s}]")
            .Ensure(s => s.Length < 100, "Too long");
    }

    [Benchmark]
    public string ErrorAggregation_StringJoin()
    {
        var failedResults = _resultArray.Where(r => r.IsFailure).Take(10);
        var errors = failedResults.SelectMany(r => r.Errors);
        return string.Join(", ", errors);
    }

    [Benchmark]
    public string ErrorAggregation_Manual()
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

    [Benchmark]
    public Result CombineResults_SmallSet()
    {
        return _resultArray.Take(5).Aggregate(
            Result.Success(),
            (acc, result) => acc.Combine(result.ToNonGeneric())
        );
    }

    [Benchmark]
    public Result CombineResults_LargeSet()
    {
        var results = _resultArray.Select(r => r.ToNonGeneric()).ToArray();
        return results[0].Combine(results.Skip(1).ToArray());
    }

    [Benchmark]
    public int BoxingAvoidance_Generic()
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

    [Benchmark]
    public int BoxingScenario_Object()
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

    [Benchmark]
    public Result<int> RecoverWithoutAllocation()
    {
        return Result<int>.WithFailure("Initial failure")
            .Recover(() => Result<int>.Success(55)); // 1+2+...+10 = 55
    }

    [Benchmark]
    public void TapWithClosure()
    {
        var capturedValue = 0;
        
        foreach (var result in _resultArray.Take(10))
        {
            result.Tap(value => capturedValue += value); // Closure allocation
        }
    }

    [Benchmark]
    public void TapWithoutClosure()
    {
        foreach (var result in _resultArray.Take(10))
        {
            result.Tap(value => Console.WriteLine(value)); // No closure
        }
    }

    [Benchmark]
    public Result<string[]> ArrayTransformation()
    {
        return Result<int[]>.Success(_resultArray.Where(r => r.IsSuccess).Select(r => r.Value).ToArray())
            .Map(arr => arr.Select(i => i.ToString()).ToArray());
    }

    [Benchmark]
    public IEnumerable<string> LazyErrorEnumeration()
    {
        return _resultArray
            .Where(r => r.IsFailure)
            .SelectMany(r => r.Errors);
    }

    [Benchmark]
    public List<string> EagerErrorCollection()
    {
        return _resultArray
            .Where(r => r.IsFailure)
            .SelectMany(r => r.Errors)
            .ToList();
    }
}