using System.Collections.Concurrent;
using System.Diagnostics;
using IndQuestResults.Operations;
using IndQuestResults.Extensions.Async;
using IndQuestResults.Extensions.Collections;

namespace IndQuestResults.Tests.Performance.Benchmarks;

/// <summary>
/// Performance tests for concurrent operations to validate thread-safety and performance under load.
/// Tests the immutable design's performance characteristics in multi-threaded scenarios.
/// </summary>
public static class ConcurrencyBenchmarks
{
    private const int ThreadCount = Environment.ProcessorCount;
    private const int OperationsPerThread = 50_000;
    private const int WarmupOperations = 1_000;

    public static void RunAll()
    {
        Console.WriteLine("=== Concurrency Performance Tests ===");
        Console.WriteLine($"Using {ThreadCount} threads with {OperationsPerThread:N0} operations per thread");
        Console.WriteLine();

        // Warmup
        Console.WriteLine("Warming up...");
        RunConcurrentCreationTest(2, WarmupOperations);
        
        Console.WriteLine("Running benchmarks...");
        Console.WriteLine();

        // Core concurrency tests
        RunConcurrentCreationTest(ThreadCount, OperationsPerThread);
        RunConcurrentReadTest(ThreadCount, OperationsPerThread);
        RunConcurrentChainTest(ThreadCount, OperationsPerThread);
        RunConcurrentCollectionTest(ThreadCount, OperationsPerThread / 10);
        
        // Async concurrency tests
        RunAsyncConcurrencyTest().Wait();
        
        // Scalability test
        RunScalabilityTest();
        
        Console.WriteLine();
    }

    private static void RunConcurrentCreationTest(int threadCount, int operationsPerThread)
    {
        var barrier = new Barrier(threadCount);
        var stopwatch = new Stopwatch();
        var tasks = new Task[threadCount];
        
        for (int t = 0; t < threadCount; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                barrier.SignalAndWait(); // Synchronize start
                
                for (int i = 0; i < operationsPerThread; i++)
                {
                    var successResult = Result<int>.Success(i);
                    var failureResult = Result<int>.WithFailure($"Error {i}");
                    
                    _ = successResult.IsSuccess;
                    _ = failureResult.IsFailure;
                }
            });
        }
        
        stopwatch.Start();
        Task.WaitAll(tasks);
        stopwatch.Stop();
        
        if (operationsPerThread >= OperationsPerThread)
        {
            var totalOperations = threadCount * operationsPerThread * 2; // Success + failure
            var opsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"Concurrent Creation: {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec total");
        }
    }

    private static void RunConcurrentReadTest(int threadCount, int operationsPerThread)
    {
        var sharedSuccessResult = Result<string>.Success("Shared value");
        var sharedFailureResult = Result<string>.WithFailure(new[] { "Error 1", "Error 2", "Error 3" });
        
        var barrier = new Barrier(threadCount);
        var stopwatch = new Stopwatch();
        var tasks = new Task[threadCount];
        
        for (int t = 0; t < threadCount; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                barrier.SignalAndWait(); // Synchronize start
                
                for (int i = 0; i < operationsPerThread; i++)
                {
                    _ = sharedSuccessResult.IsSuccess;
                    _ = sharedSuccessResult.Value;
                    _ = sharedFailureResult.IsFailure;
                    _ = sharedFailureResult.Errors.Count();
                    _ = sharedFailureResult.Error;
                }
            });
        }
        
        stopwatch.Start();
        Task.WaitAll(tasks);
        stopwatch.Stop();
        
        var totalOperations = threadCount * operationsPerThread * 5; // 5 operations per iteration
        var opsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Concurrent Read: {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec total");
    }

    private static void RunConcurrentChainTest(int threadCount, int operationsPerThread)
    {
        var barrier = new Barrier(threadCount);
        var stopwatch = new Stopwatch();
        var tasks = new Task[threadCount];
        
        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            tasks[t] = Task.Run(() =>
            {
                barrier.SignalAndWait(); // Synchronize start
                
                for (int i = 0; i < operationsPerThread; i++)
                {
                    var result = Result<int>.Success(threadId * 1000 + i)
                        .Map(x => x * 2)
                        .Bind(x => x > 500 ? Result<int>.Success(x) : Result<int>.WithFailure("Too small"))
                        .Map(x => x.ToString())
                        .Ensure(s => s.Length > 0, "Empty string");
                        
                    _ = result.IsSuccess;
                }
            });
        }
        
        stopwatch.Start();
        Task.WaitAll(tasks);
        stopwatch.Stop();
        
        var totalOperations = threadCount * operationsPerThread;
        var opsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Concurrent Chains: {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec total");
    }

    private static void RunConcurrentCollectionTest(int threadCount, int operationsPerThread)
    {
        var sharedData = Enumerable.Range(1, 20).ToArray();
        var barrier = new Barrier(threadCount);
        var stopwatch = new Stopwatch();
        var tasks = new Task[threadCount];
        
        for (int t = 0; t < threadCount; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                barrier.SignalAndWait(); // Synchronize start
                
                for (int i = 0; i < operationsPerThread; i++)
                {
                    var results = sharedData.TraverseResults(x => 
                        x % 7 == 0 ? Result<int>.WithFailure($"Error {x}") : Result<int>.Success(x * 2));
                        
                    _ = results.IsSuccess;
                    if (results.IsSuccess)
                    {
                        _ = results.Value.Count();
                    }
                }
            });
        }
        
        stopwatch.Start();
        Task.WaitAll(tasks);
        stopwatch.Stop();
        
        var totalOperations = threadCount * operationsPerThread;
        var opsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Concurrent Collections: {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec total");
    }

    private static async Task RunAsyncConcurrencyTest()
    {
        const int concurrentTasks = 100;
        const int operationsPerTask = 1_000;
        
        var stopwatch = Stopwatch.StartNew();
        
        var tasks = Enumerable.Range(0, concurrentTasks)
            .Select(async taskId =>
            {
                for (int i = 0; i < operationsPerTask; i++)
                {
                    var result = await Task.FromResult(Result<int>.Success(taskId * 1000 + i))
                        .BindAsync(async x =>
                        {
                            await Task.Yield(); // Simulate async work
                            return Result<int>.Success(x * 2);
                        })
                        .MapAsync(async x =>
                        {
                            await Task.Yield(); // Simulate async work
                            return x.ToString();
                        });
                        
                    _ = result.IsSuccess;
                }
            });
            
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        var totalOperations = concurrentTasks * operationsPerTask;
        var opsPerSecond = totalOperations / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Async Concurrency: {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec total");
    }

    private static void RunScalabilityTest()
    {
        Console.WriteLine("Thread Scalability Test:");
        
        var threadCounts = new[] { 1, 2, 4, 8, 16, 32 };
        const int opsPerThread = 10_000;
        
        foreach (var threads in threadCounts)
        {
            if (threads > Environment.ProcessorCount * 4) continue; // Skip if too many threads
            
            var barrier = new Barrier(threads);
            var stopwatch = new Stopwatch();
            var tasks = new Task[threads];
            
            for (int t = 0; t < threads; t++)
            {
                tasks[t] = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    
                    for (int i = 0; i < opsPerThread; i++)
                    {
                        var result = Result<int>.Success(i)
                            .Map(x => x * 2)
                            .Bind(x => Result<string>.Success(x.ToString()));
                        _ = result.Value;
                    }
                });
            }
            
            stopwatch.Start();
            Task.WaitAll(tasks);
            stopwatch.Stop();
            
            var totalOps = threads * opsPerThread;
            var opsPerSecond = totalOps / stopwatch.Elapsed.TotalSeconds;
            var efficiency = opsPerSecond / threads;
            
            Console.WriteLine($"  {threads,2} threads: {stopwatch.ElapsedMilliseconds,4:N0} ms, " +
                            $"{opsPerSecond,8:N0} total ops/sec, {efficiency,6:N0} ops/sec per thread");
        }
    }
}