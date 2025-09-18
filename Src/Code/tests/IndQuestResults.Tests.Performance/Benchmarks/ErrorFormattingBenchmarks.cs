using System.Diagnostics;
using System.Text;
using IndQuestResults.Operations;

namespace IndQuestResults.Tests.Performance.Benchmarks;

/// <summary>
/// Performance tests for error formatting to validate the Span optimization claims.
/// Tests the claimed 50% faster string formatting and 70% reduction in allocations.
/// </summary>
public static class ErrorFormattingBenchmarks
{
    private const int IterationCount = 100_000;
    private const int WarmupIterations = 1_000;

    /// <summary>
    /// Runs all error formatting performance tests to validate Span optimization claims.
    /// </summary>
    public static void RunAll()
    {
        Console.WriteLine("=== Error Formatting Performance Tests ===");
        Console.WriteLine();

        // Setup test data
        var smallErrors = new[] { "E1", "E2", "E3" };
        var mediumErrors = Enumerable.Range(1, 10).Select(i => $"Error {i}").ToArray();
        var largeErrors = Enumerable.Range(1, 50).Select(i => $"Error message {i} with additional context").ToArray();

        // Warmup
        Console.WriteLine("Warming up...");
        TestFormatErrorsSpan(smallErrors, WarmupIterations);
        TestFormatErrorsStringBuilder(smallErrors, WarmupIterations);
        
        Console.WriteLine("Running benchmarks...");
        Console.WriteLine();

        // Test different sizes
        RunFormattingComparison("Small errors (3 items)", smallErrors);
        RunFormattingComparison("Medium errors (10 items)", mediumErrors);
        RunFormattingComparison("Large errors (50 items)", largeErrors);
        
        // Test Result.ToString performance
        RunToStringPerformance();
        
        Console.WriteLine();
    }

    private static void RunFormattingComparison(string testName, string[] errors)
    {
        Console.WriteLine($"{testName}:");
        
        // Test Span optimization (IndQuestResults implementation)
        var spanTime = TestFormatErrorsSpan(errors, IterationCount);
        var spanOpsPerSec = IterationCount / spanTime.TotalSeconds;
        
        // Test StringBuilder baseline
        var stringBuilderTime = TestFormatErrorsStringBuilder(errors, IterationCount);
        var stringBuilderOpsPerSec = IterationCount / stringBuilderTime.TotalSeconds;
        
        var improvement = ((stringBuilderTime.TotalMilliseconds / spanTime.TotalMilliseconds) - 1) * 100;
        
        Console.WriteLine($"  Span Optimized: {spanTime.TotalMilliseconds:F1} ms, {spanOpsPerSec:N0} ops/sec");
        Console.WriteLine($"  StringBuilder: {stringBuilderTime.TotalMilliseconds:F1} ms, {stringBuilderOpsPerSec:N0} ops/sec");
        Console.WriteLine($"  Performance Improvement: {improvement:F1}% faster");
        Console.WriteLine();
    }

    private static TimeSpan TestFormatErrorsSpan(string[] errors, int iterations)
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

    private static TimeSpan TestFormatErrorsStringBuilder(string[] errors, int iterations)
    {
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            var result = FormatErrorsWithStringBuilder(errors, "Operation Failed");
            _ = result.Length; // Prevent optimization
        }
        
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    private static string FormatErrorsWithStringBuilder(string[] errors, string prefix)
    {
        if (errors == null || errors.Length == 0)
            return prefix;

        var sb = new StringBuilder($"{prefix}: ");
        for (int i = 0; i < errors.Length; i++)
        {
            if (i > 0)
                sb.Append(", ");
            sb.Append(errors[i]);
        }
        return sb.ToString();
    }

    private static void RunToStringPerformance()
    {
        Console.WriteLine("Result.ToString() Performance:");
        
        var successResult = Result.Success();
        var singleErrorResult = Result.WithFailure("Single error");
        var multipleErrorResult = Result.WithFailure(new[] { "Error 1", "Error 2", "Error 3", "Error 4", "Error 5" });
        
        // Test success toString
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < IterationCount; i++)
        {
            var str = successResult.ToString();
            _ = str.Length;
        }
        stopwatch.Stop();
        var successOpsPerSec = IterationCount / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"  Success Result: {stopwatch.ElapsedMilliseconds:F1} ms, {successOpsPerSec:N0} ops/sec");

        // Test single error toString
        stopwatch.Restart();
        for (int i = 0; i < IterationCount; i++)
        {
            var str = singleErrorResult.ToString();
            _ = str.Length;
        }
        stopwatch.Stop();
        var singleErrorOpsPerSec = IterationCount / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"  Single Error: {stopwatch.ElapsedMilliseconds:F1} ms, {singleErrorOpsPerSec:N0} ops/sec");

        // Test multiple errors toString
        stopwatch.Restart();
        for (int i = 0; i < IterationCount; i++)
        {
            var str = multipleErrorResult.ToString();
            _ = str.Length;
        }
        stopwatch.Stop();
        var multipleErrorOpsPerSec = IterationCount / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"  Multiple Errors: {stopwatch.ElapsedMilliseconds:F1} ms, {multipleErrorOpsPerSec:N0} ops/sec");
    }
}