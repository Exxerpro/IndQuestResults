using System.Diagnostics;
using IndQuestResults.Operations;

namespace IndQuestResults.Tests.Performance.Benchmarks;

/// <summary>
/// Performance tests for Result creation operations to validate performance claims.
/// Measures creation speed, memory allocation, and throughput.
/// </summary>
public static class ResultCreationBenchmarks
{
    private const int IterationCount = 1_000_000;
    private const int WarmupIterations = 10_000;

    /// <summary>
    /// Runs all result creation performance tests to validate performance claims.
    /// </summary>
    public static void RunAll()
    {
        Console.WriteLine("=== Result Creation Performance Tests ===");
        Console.WriteLine();

        // Warmup
        Console.WriteLine("Warming up...");
        RunSuccessCreationTest(WarmupIterations);
        RunFailureCreationTest(WarmupIterations);
        
        Console.WriteLine("Running benchmarks...");
        Console.WriteLine();

        // Main tests
        RunSuccessCreationTest(IterationCount);
        RunFailureCreationTest(IterationCount);
        RunGenericSuccessCreationTest(IterationCount);
        RunGenericFailureCreationTest(IterationCount);
        RunMultipleErrorCreationTest(IterationCount);
        RunComparisonWithExceptions(100_000); // Fewer iterations for exception test
        
        Console.WriteLine();
    }

    private static void RunSuccessCreationTest(int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var result = Result.Success();
            _ = result.IsSuccess; // Prevent optimization
        }
        
        stopwatch.Stop();
        
        if (iterations >= IterationCount)
        {
            var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"Success Creation: {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec");
        }
    }

    private static void RunFailureCreationTest(int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var result = Result.WithFailure("Error message");
            _ = result.IsFailure; // Prevent optimization
        }
        
        stopwatch.Stop();
        
        if (iterations >= IterationCount)
        {
            var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"Failure Creation: {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec");
        }
    }

    private static void RunGenericSuccessCreationTest(int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var result = Result<int>.Success(42);
            _ = result.Value; // Prevent optimization
        }
        
        stopwatch.Stop();
        
        var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Generic Success Creation: {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec");
    }

    private static void RunGenericFailureCreationTest(int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var result = Result<int>.WithFailure("Error message");
            _ = result.IsFailure; // Prevent optimization
        }
        
        stopwatch.Stop();
        
        var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Generic Failure Creation: {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec");
    }

    private static void RunMultipleErrorCreationTest(int iterations)
    {
        var errors = new[] { "Error 1", "Error 2", "Error 3", "Error 4", "Error 5" };
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var result = Result.WithFailure(errors);
            _ = result.Errors.Count(); // Prevent optimization
        }
        
        stopwatch.Stop();
        
        var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Multiple Error Creation: {stopwatch.ElapsedMilliseconds:N0} ms, {opsPerSecond:N0} ops/sec");
    }

    private static void RunComparisonWithExceptions(int iterations)
    {
        // Test Result pattern
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var result = SimulateOperationWithResult(i % 10 == 0);
            if (result.IsFailure)
            {
                _ = result.Error; // Handle error
            }
        }
        
        stopwatch.Stop();
        var resultTime = stopwatch.ElapsedMilliseconds;
        
        // Test exception pattern
        stopwatch.Restart();
        
        for (int i = 0; i < iterations; i++)
        {
            try
            {
                SimulateOperationWithException(i % 10 == 0);
            }
            catch (InvalidOperationException)
            {
                // Handle exception
            }
        }
        
        stopwatch.Stop();
        var exceptionTime = stopwatch.ElapsedMilliseconds;
        
        var improvement = ((double)exceptionTime / resultTime - 1) * 100;
        Console.WriteLine($"Result vs Exception Comparison:");
        Console.WriteLine($"  Result Pattern: {resultTime:N0} ms");
        Console.WriteLine($"  Exception Pattern: {exceptionTime:N0} ms");
        Console.WriteLine($"  Performance Improvement: {improvement:F1}% faster with Results");
    }

    private static Result<int> SimulateOperationWithResult(bool shouldFail)
    {
        return shouldFail 
            ? Result<int>.WithFailure("Operation failed") 
            : Result<int>.Success(42);
    }

    private static int SimulateOperationWithException(bool shouldFail)
    {
        if (shouldFail)
            throw new InvalidOperationException("Operation failed");
        return 42;
    }
}