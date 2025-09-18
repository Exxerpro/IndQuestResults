using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using IndQuestResults.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IndQuestResults.Benchmarks;

/// <summary>
/// Benchmarks testing the thread-safety and performance characteristics of Result operations under concurrent load.
/// Validates the immutable design's performance in multi-threaded scenarios and measures contention effects.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class ConcurrencyBenchmarks
{
    private const int ThreadCount = 8;
    private const int OperationsPerThread = 10000;

    private Result _sharedSuccessResult = null!;
    private Result<int> _sharedSuccessResultWithValue = null!;
    private Result _sharedFailureResult = null!;

    /// <summary>
    /// Initializes shared Result instances for concurrent access testing.
    /// Creates immutable Result objects that will be safely accessed from multiple threads.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _sharedSuccessResult = Result.Success();
        _sharedSuccessResultWithValue = Result<int>.Success(42);
        _sharedFailureResult = Result.WithFailure("Shared error");
    }

    /// <summary>
    /// Benchmarks concurrent creation of new Result instances across multiple threads.
    /// Tests the performance of Result construction under high concurrency.
    /// </summary>
    /// <returns>Task representing the asynchronous benchmark operation.</returns>
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
                    var isSuccess = result.IsSuccess;
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmarks concurrent read access to shared Result instances.
    /// Tests thread-safety and contention effects when multiple threads read the same immutable Results.
    /// </summary>
    /// <returns>Task representing the asynchronous benchmark operation.</returns>
    [Benchmark]
    public async Task ConcurrentResultReading()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    var isSuccess = _sharedSuccessResult.IsSuccess;
                    var isFailure = _sharedFailureResult.IsFailure;
                    var error = _sharedFailureResult.Error;
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmarks concurrent Map operations creating new Results from a shared source.
    /// Tests performance of functional transformations under concurrent load.
    /// </summary>
    /// <returns>Task representing the asynchronous benchmark operation.</returns>
    [Benchmark]
    public async Task ConcurrentMapOperations()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    var result = _sharedSuccessResult.Map(() => i * 2);
                    if (result.IsSuccess)
                    {
                        _ = result.Value;
                    }
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmarks concurrent Bind operations creating chained Results from a shared source.
    /// Tests performance of monadic composition under concurrent load.
    /// </summary>
    /// <returns>Task representing the asynchronous benchmark operation.</returns>
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
                    if (result.IsSuccess)
                    {
                        _ = result.Value;
                    }
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmarks concurrent Combine operations merging multiple Results.
    /// Tests performance of error aggregation when multiple threads perform combinations.
    /// </summary>
    /// <returns>Task representing the asynchronous benchmark operation.</returns>
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
                    var isSuccess = combined.IsSuccess;
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmarks concurrent Match operations for pattern matching on Results.
    /// Tests performance of conditional execution based on Result state under concurrent load.
    /// </summary>
    /// <returns>Task representing the asynchronous benchmark operation.</returns>
    [Benchmark]
    public async Task ConcurrentMatchOperations()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    var value = _sharedSuccessResultWithValue.IsSuccess
                        ? _sharedSuccessResultWithValue.Value + i
                        : -1;
                    _ = value; // Use value to prevent optimization
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmarks concurrent access to error collections in failed Results.
    /// Tests thread-safety and performance when multiple threads iterate over shared error collections.
    /// </summary>
    /// <returns>Task representing the asynchronous benchmark operation.</returns>
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

    /// <summary>
    /// Benchmarks concurrent execution of complex chained Result operations.
    /// Tests performance of functional composition pipelines under concurrent load.
    /// </summary>
    /// <returns>Task representing the asynchronous benchmark operation.</returns>
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
                    if (result.IsSuccess)
                    {
                        _ = result.Value;
                    }
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Benchmarks parallel processing of Result collections using Parallel.ForEach.
    /// Tests scalability of Result operations when processed in parallel batches.
    /// </summary>
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
            var processedValue = result.IsSuccess ? result.Value * 2 : result.Errors.Count();
        });
    }

    /// <summary>
    /// Benchmarks concurrent string conversion operations on shared Results.
    /// Tests performance of ToString() method under concurrent access patterns.
    /// </summary>
    /// <returns>Task representing the asynchronous benchmark operation.</returns>
    [Benchmark]
    public async Task ConcurrentToStringOperations()
    {
        var tasks = Enumerable.Range(0, ThreadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < OperationsPerThread; i++)
                {
                    var str1 = _sharedSuccessResult.ToString();
                    var str2 = _sharedFailureResult.ToString();
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);
    }
}
