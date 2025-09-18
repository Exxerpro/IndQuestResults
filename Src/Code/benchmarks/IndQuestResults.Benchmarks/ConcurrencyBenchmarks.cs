using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using IndQuestResults.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IndQuestResults.Benchmarks;

[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class ConcurrencyBenchmarks
{
    private const int ThreadCount = 8;
    private const int OperationsPerThread = 10000;
    
    private Result _sharedSuccessResult;
    private Result<int> _sharedSuccessResultWithValue;
    private Result _sharedFailureResult;
    
    [GlobalSetup]
    public void Setup()
    {
        _sharedSuccessResult = Result.Success();
        _sharedSuccessResultWithValue = Result<int>.Success(42);
        _sharedFailureResult = Result.WithFailure("Shared error");
    }

    [Benchmark]
    public async Task ConcurrentResultCreation()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    var result = i % 2 == 0 
                        ? Result.Success() 
                        : Result.WithFailure($"Error {i}");
                    _ = result.IsSuccess;
                }
            }))
            .ToArray();
            
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentResultReading()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    _ = _sharedSuccessResult.IsSuccess;
                    _ = _sharedFailureResult.IsFailure;
                    _ = _sharedFailureResult.Error;
                }
            }))
            .ToArray();
            
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentMapOperations()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    var result = _sharedSuccessResult.Map(() => i * 2);
                    _ = result.Value;
                }
            }))
            .ToArray();
            
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentBindOperations()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    var result = _sharedSuccessResultWithValue.Bind(value => 
                        Result<int>.Success(value + i));
                    _ = result.Value;
                }
            }))
            .ToArray();
            
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentCombineOperations()
    {
        var results = Enumerable.Range(0, 10)
            .Select(i => i % 2 == 0 ? Result.Success() : Result.WithFailure($"Error {i}"))
            .ToArray();
            
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    var combined = results[0].Combine(results.Skip(1).ToArray());
                    _ = combined.IsSuccess;
                }
            }))
            .ToArray();
            
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentMatchOperations()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    var value = _sharedSuccessResultWithValue.Match(
                        onSuccess: v => v + i,
                        onFailure: _ => -1
                    );
                    _ = value;
                }
            }))
            .ToArray();
            
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentErrorAccess()
    {
        var multiErrorResult = Result.WithFailure(
            Enumerable.Range(1, 100).Select(i => $"Error {i}").ToArray()
        );
        
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    foreach (var error in multiErrorResult.Errors)
                    {
                        _ = error.Length;
                    }
                }
            }))
            .ToArray();
            
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task ConcurrentChainedOperations()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread / 10; i++)
                {
                    var result = Result<int>.Success(i)
                        .Map(x => x * 2)
                        .Bind(x => Result<int>.Success(x + 1))
                        .Map(x => x.ToString())
                        .Bind(x => Result<double>.Success(double.Parse(x)))
                        .Map(x => (int)x);
                    _ = result.Value;
                }
            }))
            .ToArray();
            
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public void ParallelResultProcessing()
    {
        var results = Enumerable.Range(0, 10000)
            .Select(i => i % 3 == 0 
                ? Result<int>.Success(i) 
                : Result<int>.WithFailure($"Failed at {i}"))
            .ToArray();
            
        Parallel.ForEach(results, result =>
        {
            _ = result.Match(
                onSuccess: value => value * 2,
                onFailure: errors => errors.Count()
            );
        });
    }

    [Benchmark]
    public async Task ConcurrentToStringOperations()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    _ = _sharedSuccessResult.ToString();
                    _ = _sharedFailureResult.ToString();
                }
            }))
            .ToArray();
            
        await Task.WhenAll(tasks);
    }
}