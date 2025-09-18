using IndQuestResults.Performance;

namespace IndQuestResults.Benchmarks;

/// <summary>
/// Benchmarks to measure the overhead of Result timing extensions compared to manual timing.
/// Validates that timing extensions add minimal performance impact while providing convenience.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[CategoriesColumn]
public class TimingBenchmarks
{
    private static readonly Random Random = new(42);

    /// <summary>
    /// Baseline: Manual timing with Stopwatch - the traditional approach.
    /// </summary>
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Sync")]
    public TimeSpan ManualTiming_Success()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = CreateSuccessResult();
        stopwatch.Stop();

        // Simulate using the result
        return !result.IsSuccess ? throw new InvalidOperationException("Unexpected failure") : stopwatch.Elapsed;
    }

    /// <summary>
    /// Result timing extension - measures overhead compared to manual timing.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Sync")]
    public TimeSpan ResultTiming_Success()
    {
        var timedResult = ResultTiming.Timed(CreateSuccessResult);

        // Simulate using the result
        return !timedResult.IsSuccess ? throw new InvalidOperationException("Unexpected failure") : timedResult.Elapsed;
    }

    /// <summary>
    /// Manual timing with callback - traditional approach with immediate action.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Sync")]
    public Result<int> ManualTiming_WithCallback()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = CreateSuccessResult();
        stopwatch.Stop();

        // Simulate logging/monitoring callback
        ProcessTimingResult(result, stopwatch.Elapsed);

        return result;
    }

    /// <summary>
    /// Result timing with callback - measures convenience vs performance trade-off.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Sync")]
    public Result<int> ResultTiming_WithCallback()
    {
        return ResultTiming.TimedWithCallback(
            CreateSuccessResult,
            ProcessTimingResult);
    }

    /// <summary>
    /// Manual async timing - baseline for async operations.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Async")]
    public async Task<TimeSpan> ManualTiming_Async()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await CreateSuccessResultAsync();
        stopwatch.Stop();

        return !result.IsSuccess ? throw new InvalidOperationException("Unexpected failure") : stopwatch.Elapsed;
    }

    /// <summary>
    /// Result async timing - measures async timing extension overhead.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Async")]
    public async Task<TimeSpan> ResultTiming_Async()
    {
        var timedResult = await ResultTiming.TimedAsync(CreateSuccessResultAsync);

        return !timedResult.IsSuccess ? throw new InvalidOperationException("Unexpected failure") : timedResult.Elapsed;
    }

    /// <summary>
    /// Manual async timing with callback.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Async")]
    public async Task<Result<int>> ManualTiming_AsyncWithCallback()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await CreateSuccessResultAsync();
        stopwatch.Stop();

        ProcessTimingResult(result, stopwatch.Elapsed);

        return result;
    }

    /// <summary>
    /// Result async timing with callback.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Async")]
    public async Task<Result<int>> ResultTiming_AsyncWithCallback()
    {
        return await ResultTiming.TimedWithCallbackAsync(
            CreateSuccessResultAsync,
            ProcessTimingResult);
    }

    /// <summary>
    /// Timing overhead for failure scenarios.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Failure")]
    public TimeSpan ResultTiming_Failure()
    {
        var timedResult = ResultTiming.Timed(CreateFailureResult);

        return timedResult.IsSuccess ? throw new InvalidOperationException("Expected failure") : timedResult.Elapsed;
    }

    /// <summary>
    /// Timing overhead for exception scenarios.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("Exception")]
    public TimeSpan ResultTiming_Exception()
    {
        var timedResult = ResultTiming.Timed(CreateExceptionResult);

        return timedResult.IsSuccess ? throw new InvalidOperationException("Expected failure from exception") : timedResult.Elapsed;
    }

    /// <summary>
    /// High-frequency timing operations to test overhead at scale.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("HighFrequency")]
    public long HighFrequency_ManualTiming()
    {
        long totalMicroseconds = 0;

        for (int i = 0; i < 1000; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = CreateFastSuccessResult(i);
            stopwatch.Stop();

            if (result.IsSuccess)
            {
                totalMicroseconds += (long)stopwatch.Elapsed.TotalMicroseconds;
            }
        }

        return totalMicroseconds;
    }

    /// <summary>
    /// High-frequency timing with Result extensions.
    /// </summary>
    [Benchmark]
    [BenchmarkCategory("HighFrequency")]
    public long HighFrequency_ResultTiming()
    {
        long totalMicroseconds = 0;

        for (int i = 0; i < 1000; i++)
        {
            var timedResult = ResultTiming.Timed(() => CreateFastSuccessResult(i));

            if (timedResult.IsSuccess)
            {
                totalMicroseconds += (long)timedResult.ElapsedMicroseconds;
            }
        }

        return totalMicroseconds;
    }

    // Helper methods for creating test scenarios

    private static Result<int> CreateSuccessResult()
    {
        // Simulate some work with a small computation
        var value = Random.Next(1, 1000);
        return Result<int>.Success(value * 2);
    }

    private static async Task<Result<int>> CreateSuccessResultAsync()
    {
        // Simulate async work
        await Task.Delay(1);
        var value = Random.Next(1, 1000);
        return Result<int>.Success(value * 2);
    }

    private static Result<int> CreateFailureResult()
    {
        // Simulate validation failure
        return Result<int>.WithFailure("Validation failed");
    }

    private static Result<int> CreateExceptionResult()
    {
        // Simulate exception during operation
        throw new InvalidOperationException("Simulated operation failure");
    }

    private static Result<int> CreateFastSuccessResult(int input)
    {
        // Very fast operation for high-frequency testing
        return Result<int>.Success(input % 100);
    }

    private static void ProcessTimingResult(Result<int> result, TimeSpan elapsed)
    {
        // Simulate logging or monitoring callback
        // Use the result to prevent optimization
        if (result.IsSuccess && elapsed.TotalMicroseconds > 0)
        {
            // Simulate minimal processing
            _ = result.Value + (int)elapsed.TotalMicroseconds;
        }
    }
}
