using System.Diagnostics;
using IndQuestResults.Operations;
using IndQuestResults.Extensions.Async;
using IndQuestResults.Extensions.Collections;
using IndQuestResults.Extensions.Functional;

namespace IndQuestResults.Tests.Performance.Benchmarks;

/// <summary>
/// Performance tests for fluent API chains and extension methods.
/// Tests the performance of chained operations across all extension namespaces.
/// </summary>
public static class FluentChainBenchmarks
{
    private const int IterationCount = 100_000;
    private const int AsyncIterationCount = 10_000;
    private const int WarmupIterations = 1_000;

    public static void RunAll()
    {
        Console.WriteLine("=== Fluent API Chain Performance Tests ===");
        Console.WriteLine();

        // Warmup
        Console.WriteLine("Warming up...");
        RunBasicChainTest(WarmupIterations);
        RunAsyncChainTest(100).Wait();
        
        Console.WriteLine("Running benchmarks...");
        Console.WriteLine();

        // Sync tests
        RunBasicChainTest(IterationCount);
        RunComplexChainTest(IterationCount);
        RunCollectionsChainTest(IterationCount);
        RunFunctionalChainTest(IterationCount);
        
        // Async tests
        RunAsyncChainTest(AsyncIterationCount).Wait();
        RunAsyncVsSyncComparison().Wait();
        
        Console.WriteLine();
    }

    private static void RunBasicChainTest(int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var result = Result<int>.Success(10)
                .Map(x => x * 2)
                .Bind(x => Result<int>.Success(x + 5))
                .Map(x => x.ToString())
                .Ensure(s => s.Length > 0, "Empty string");
                
            _ = result.IsSuccess; // Prevent optimization
        }
        
        stopwatch.Stop();
        
        if (iterations >= IterationCount)
        {
            var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"Basic Chain (Map->Bind->Map->Ensure): {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec");
        }
    }

    private static void RunComplexChainTest(int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var result = Result<int>.Success(i % 100)
                .Map(x => x * 2)
                .Bind(x => x > 50 ? Result<int>.Success(x) : Result<int>.WithFailure("Too small"))
                .Map(x => x + 10)
                .Ensure(x => x < 200, "Too large")
                .Tap(x => { /* Side effect */ })
                .Map(x => x.ToString())
                .Bind(s => Result<double>.Success(double.Parse(s)))
                .Recover(() => Result<double>.Success(0.0));
                
            _ = result.Value; // Prevent optimization
        }
        
        stopwatch.Stop();
        
        var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Complex Chain (8 operations): {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec");
    }

    private static void RunCollectionsChainTest(int iterations)
    {
        var data = Enumerable.Range(1, 10).ToArray();
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var result = data
                .TraverseResults(x => x % 3 == 0 
                    ? Result<int>.WithFailure($"Error {x}") 
                    : Result<int>.Success(x * 2))
                .Map(values => values.Sum())
                .Ensure(sum => sum > 0, "Invalid sum");
                
            _ = result.IsSuccess; // Prevent optimization
        }
        
        stopwatch.Stop();
        
        var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Collections Chain (Traverse->Map->Ensure): {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec");
    }

    private static void RunFunctionalChainTest(int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var nameResult = Result<string>.Success("John");
            var ageResult = Result<int>.Success(30);
            var emailResult = Result<string>.Success("john@example.com");
            
            var userResult = ResultApplicative.Apply(
                nameResult,
                ageResult,
                emailResult,
                (name, age, email) => new { Name = name, Age = age, Email = email })
                .Map(user => $"{user.Name} ({user.Age}) - {user.Email}")
                .Ensure(s => s.Contains("@"), "Invalid format");
                
            _ = userResult.IsSuccess; // Prevent optimization
        }
        
        stopwatch.Stop();
        
        var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Functional Chain (Apply->Map->Ensure): {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec");
    }

    private static async Task RunAsyncChainTest(int iterations)
    {
        var tasks = new List<Task>();
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var task = Task.FromResult(Result<int>.Success(10))
                .BindAsync(async x => 
                {
                    await Task.Delay(1); // Simulate async work
                    return Result<int>.Success(x * 2);
                })
                .MapAsync(async x =>
                {
                    await Task.Delay(1); // Simulate async work
                    return x.ToString();
                })
                .TapAsync(async s =>
                {
                    await Task.Delay(1); // Simulate async work
                    _ = s.Length;
                });
                
            tasks.Add(task);
            
            // Process in batches to avoid overwhelming the thread pool
            if (tasks.Count >= 100)
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }
        
        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
        
        stopwatch.Stop();
        
        var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Async Chain (BindAsync->MapAsync->TapAsync): {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec");
    }

    private static async Task RunAsyncVsSyncComparison()
    {
        const int comparisonIterations = 10_000;
        
        // Sync version
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < comparisonIterations; i++)
        {
            var result = Result<int>.Success(10)
                .Map(x => x * 2)
                .Map(x => x + 5)
                .Map(x => x.ToString());
            _ = result.Value;
        }
        stopwatch.Stop();
        var syncTime = stopwatch.Elapsed;
        
        // Async version
        stopwatch.Restart();
        var asyncTasks = new List<Task>();
        
        for (int i = 0; i < comparisonIterations; i++)
        {
            var task = Task.FromResult(Result<int>.Success(10))
                .MapAsync(async x => 
                {
                    await Task.Yield();
                    return x * 2;
                })
                .MapAsync(async x =>
                {
                    await Task.Yield();
                    return x + 5;
                })
                .MapAsync(async x =>
                {
                    await Task.Yield();
                    return x.ToString();
                });
                
            asyncTasks.Add(task);
            
            if (asyncTasks.Count >= 100)
            {
                await Task.WhenAll(asyncTasks);
                asyncTasks.Clear();
            }
        }
        
        if (asyncTasks.Count > 0)
        {
            await Task.WhenAll(asyncTasks);
        }
        
        stopwatch.Stop();
        var asyncTime = stopwatch.Elapsed;
        
        var syncOpsPerSec = comparisonIterations / syncTime.TotalSeconds;
        var asyncOpsPerSec = comparisonIterations / asyncTime.TotalSeconds;
        
        Console.WriteLine($"Sync vs Async Comparison ({comparisonIterations:N0} operations):");
        Console.WriteLine($"  Sync: {syncTime.TotalMilliseconds:F1} ms, {syncOpsPerSec:N0} ops/sec");
        Console.WriteLine($"  Async: {asyncTime.TotalMilliseconds:F1} ms, {asyncOpsPerSec:N0} ops/sec");
        Console.WriteLine($"  Overhead: {(asyncTime.TotalMilliseconds / syncTime.TotalMilliseconds - 1) * 100:F1}% slower for async");
    }
}