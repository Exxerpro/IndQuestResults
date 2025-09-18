using System.Diagnostics;
using System.Text;
using IndQuestResults.Operations;

namespace IndQuestResults.Tests.Performance.Benchmarks;

/// <summary>
/// Performance tests specifically for Span optimizations to validate the claimed 70% reduction in allocations.
/// Tests memory efficiency and performance of Span-based operations.
/// </summary>
public static class SpanOptimizationBenchmarks
{
    private const int IterationCount = 100_000;
    private const int WarmupIterations = 1_000;

    /// <summary>
    /// Runs all Span optimization performance tests to validate allocation reduction claims.
    /// </summary>
    public static void RunAll()
    {
        Console.WriteLine("=== Span Optimization Performance Tests ===");
        Console.WriteLine();

        // Setup test data
        var smallErrorSet = new[] { "E1", "E2", "E3", "E4" };
        var mediumErrorSet = Enumerable.Range(1, 16).Select(i => $"Error {i}").ToArray();
        var largeErrorSet = Enumerable.Range(1, 100).Select(i => $"Error {i}").ToArray();

        // Warmup
        Console.WriteLine("Warming up...");
        TestSpanOptimization(smallErrorSet, WarmupIterations);
        TestStringBuilderBaseline(smallErrorSet, WarmupIterations);

        Console.WriteLine("Running benchmarks...");
        Console.WriteLine();

        // Test different error set sizes
        RunSpanVsBaselineComparison("Small Error Set (4 items)", smallErrorSet);
        RunSpanVsBaselineComparison("Medium Error Set (16 items)", mediumErrorSet);
        RunSpanVsBaselineComparison("Large Error Set (100 items)", largeErrorSet);

        // Test memory allocation patterns
        RunMemoryAllocationTest();

        // Test CombineErrors optimization
        RunCombineErrorsTest();

        Console.WriteLine();
    }

    private static void RunSpanVsBaselineComparison(string testName, string[] errors)
    {
        Console.WriteLine($"{testName}:");

        // Test Span optimization
        var spanTime = TestSpanOptimization(errors, IterationCount);
        var spanOpsPerSec = IterationCount / spanTime.TotalSeconds;

        // Test StringBuilder baseline
        var baselineTime = TestStringBuilderBaseline(errors, IterationCount);
        var baselineOpsPerSec = IterationCount / baselineTime.TotalSeconds;

        var improvement = ((baselineTime.TotalMilliseconds / spanTime.TotalMilliseconds) - 1) * 100;

        Console.WriteLine($"  Span Optimized: {spanTime.TotalMilliseconds:F1} ms, {spanOpsPerSec:N0} ops/sec");
        Console.WriteLine($"  StringBuilder Baseline: {baselineTime.TotalMilliseconds:F1} ms, {baselineOpsPerSec:N0} ops/sec");
        Console.WriteLine($"  Performance Improvement: {improvement:F1}% faster with Span");
        Console.WriteLine();
    }

    private static TimeSpan TestSpanOptimization(string[] errors, int iterations)
    {
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var result = Result.FormatErrorsString(errors, "Operation Failed");
            _ = result.Length; // Prevent optimization
        }

        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    private static TimeSpan TestStringBuilderBaseline(string[] errors, int iterations)
    {
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var result = FormatUsingStringBuilder(errors, "Operation Failed");
            _ = result.Length; // Prevent optimization
        }

        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    private static string FormatUsingStringBuilder(string[] errors, string prefix)
    {
        if (errors == null || errors.Length == 0)
        {
            return prefix;
        }

        var sb = new StringBuilder($"{prefix}: ");
        for (int i = 0; i < errors.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append(errors[i]);
        }
        return sb.ToString();
    }

    private static void RunMemoryAllocationTest()
    {
        Console.WriteLine("Memory Allocation Comparison:");

        var errors = new[] { "Error 1", "Error 2", "Error 3", "Error 4", "Error 5" };
        const int testIterations = 10_000;

        // Force garbage collection before test
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(false);

        // Test Span optimization
        for (int i = 0; i < testIterations; i++)
        {
            var result = Result.FormatErrorsString(errors, "Test");
            _ = result.Length;
        }

        var spanMemory = GC.GetTotalMemory(false) - initialMemory;

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        initialMemory = GC.GetTotalMemory(false);

        // Test StringBuilder baseline
        for (int i = 0; i < testIterations; i++)
        {
            var result = FormatUsingStringBuilder(errors, "Test");
            _ = result.Length;
        }

        var baselineMemory = GC.GetTotalMemory(false) - initialMemory;

        var memoryReduction = ((double)(baselineMemory - spanMemory) / baselineMemory) * 100;

        Console.WriteLine($"  Span Optimized Memory: {spanMemory:N0} bytes");
        Console.WriteLine($"  StringBuilder Memory: {baselineMemory:N0} bytes");
        Console.WriteLine($"  Memory Reduction: {memoryReduction:F1}%");
        Console.WriteLine();
    }

    private static void RunCombineErrorsTest()
    {
        Console.WriteLine("CombineErrors Span Optimization:");

        var primaryErrors = new[] { "Primary 1", "Primary 2", "Primary 3" };
        var secondaryErrors = new[] { "Secondary 1", "Secondary 2", "Secondary 3" };

        // Test Result.CombineErrors (uses Span optimization for small collections)
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < IterationCount; i++)
        {
            var result = Result.CombineErrors(primaryErrors, secondaryErrors);
            _ = result.Errors.Count(); // Prevent optimization
        }

        stopwatch.Stop();
        var spanTime = stopwatch.Elapsed;

        // Test manual List<string> approach
        stopwatch.Restart();

        for (int i = 0; i < IterationCount; i++)
        {
            var errors = new List<string>();
            errors.AddRange(primaryErrors);
            errors.AddRange(secondaryErrors);
            var result = Result.WithFailure(errors);
            _ = result.Errors.Count(); // Prevent optimization
        }

        stopwatch.Stop();
        var listTime = stopwatch.Elapsed;

        var improvement = ((listTime.TotalMilliseconds / spanTime.TotalMilliseconds) - 1) * 100;
        var spanOpsPerSec = IterationCount / spanTime.TotalSeconds;
        var listOpsPerSec = IterationCount / listTime.TotalSeconds;

        Console.WriteLine($"  CombineErrors (Span): {spanTime.TotalMilliseconds:F1} ms, {spanOpsPerSec:N0} ops/sec");
        Console.WriteLine($"  Manual List: {listTime.TotalMilliseconds:F1} ms, {listOpsPerSec:N0} ops/sec");
        Console.WriteLine($"  Performance Improvement: {improvement:F1}% faster");
        Console.WriteLine();
    }
}
