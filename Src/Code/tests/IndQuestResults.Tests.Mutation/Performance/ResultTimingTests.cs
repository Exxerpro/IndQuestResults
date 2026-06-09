namespace IndQuestResults.Tests.Mutation.Performance;

public class ResultTimingTests
{
    [Fact]
    public void Timed_SuccessfulOperation_ReturnsSuccessWithTiming()
    {
        var timedResult = ResultTiming.Timed(() =>
        {
            Thread.Sleep(10); // Small delay to ensure measurable time
            return Result<string>.Success("test-value");
        });

        timedResult.IsSuccess.ShouldBeTrue();
        timedResult.Result.Value.ShouldBe("test-value");
        timedResult.ElapsedMilliseconds.ShouldBeGreaterThan(5);
        timedResult.ElapsedMicroseconds.ShouldBeGreaterThan(5000);
    }

    [Fact]
    public void Timed_FailingOperation_ReturnsFailureWithTiming()
    {
        var timedResult = ResultTiming.Timed(() =>
        {
            Thread.Sleep(5);
            return Result<string>.WithFailure("operation-failed");
        });

        timedResult.IsFailure.ShouldBeTrue();
        timedResult.Result.Errors.ShouldContain("operation-failed");
        timedResult.ElapsedMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Timed_ExceptionInOperation_ReturnsFailureWithTiming()
    {
        var timedResult = ResultTiming.Timed<string>(() =>
        {
            Thread.Sleep(5);
            throw new InvalidOperationException("test-exception");
        });

        timedResult.IsFailure.ShouldBeTrue();
        timedResult.Result.Error!.ShouldContain("Operation failed: test-exception");
        timedResult.ElapsedMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Timed_NonGeneric_SuccessfulOperation_ReturnsSuccessWithTiming()
    {
        var timedResult = ResultTiming.Timed(() =>
        {
            Thread.Sleep(5);
            return Result.Success();
        });

        timedResult.IsSuccess.ShouldBeTrue();
        timedResult.ElapsedMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Timed_NonGeneric_FailingOperation_ReturnsFailureWithTiming()
    {
        var timedResult = ResultTiming.Timed(() =>
        {
            Thread.Sleep(5);
            return Result.WithFailure("operation-failed");
        });

        timedResult.IsFailure.ShouldBeTrue();
        timedResult.Result.Errors.ShouldContain("operation-failed");
        timedResult.ElapsedMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task TimedAsync_SuccessfulOperation_ReturnsSuccessWithTiming()
    {
        var timedResult = await ResultTiming.TimedAsync(async () =>
        {
            await Task.Delay(10);
            return Result<string>.Success("async-value");
        });

        timedResult.IsSuccess.ShouldBeTrue();
        timedResult.Result.Value.ShouldBe("async-value");
        timedResult.ElapsedMilliseconds.ShouldBeGreaterThan(5);
    }

    [Fact]
    public async Task TimedAsync_FailingOperation_ReturnsFailureWithTiming()
    {
        var timedResult = await ResultTiming.TimedAsync(async () =>
        {
            await Task.Delay(5);
            return Result<string>.WithFailure("async-failed");
        });

        timedResult.IsFailure.ShouldBeTrue();
        timedResult.Result.Errors.ShouldContain("async-failed");
        timedResult.ElapsedMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task TimedAsync_CancelledOperation_ReturnsCancelledWithTiming()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var timedResult = await ResultTiming.TimedAsync(async () =>
        {
            await Task.Delay(1000, cts.Token);
            return Result<string>.Success("should-not-reach");
        }, cts.Token);

        timedResult.IsFailure.ShouldBeTrue();
        timedResult.Result.Error.ShouldBe(ResultErrors.OperationCancelled);
        timedResult.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task TimedAsync_ExceptionInOperation_ReturnsFailureWithTiming()
    {
        var timedResult = await ResultTiming.TimedAsync<string>(async () =>
        {
            await Task.Delay(5);
            throw new InvalidOperationException("async-exception");
        });

        timedResult.IsFailure.ShouldBeTrue();
        timedResult.Result.Error!.ShouldContain("Async operation failed: async-exception");
        timedResult.ElapsedMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task TimedAsync_NonGeneric_SuccessfulOperation_ReturnsSuccessWithTiming()
    {
        var timedResult = await ResultTiming.TimedAsync(async () =>
        {
            await Task.Delay(5);
            return Result.Success();
        });

        timedResult.IsSuccess.ShouldBeTrue();
        timedResult.ElapsedMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TimedWithCallback_SuccessfulOperation_CallsCallbackAndReturnsResult()
    {
        Result<string>? callbackResult = null;
        TimeSpan? callbackElapsed = null;

        var result = ResultTiming.TimedWithCallback(
            () =>
            {
                Thread.Sleep(5);
                return Result<string>.Success("callback-test");
            },
            (r, e) =>
            {
                callbackResult = r;
                callbackElapsed = e;
            });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("callback-test");
        
        callbackResult.ShouldNotBeNull();
        callbackResult!.IsSuccess.ShouldBeTrue();
        callbackResult.Value.ShouldBe("callback-test");
        
        callbackElapsed.ShouldNotBeNull();
        callbackElapsed!.Value.TotalMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TimedWithCallback_CallbackThrows_DoesNotAffectResult()
    {
        var result = ResultTiming.TimedWithCallback(
            () => Result<string>.Success("callback-exception-test"),
            (r, e) => throw new InvalidOperationException("callback-failed"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("callback-exception-test");
    }

    [Fact]
    public void TimedWithCallback_NonGeneric_CallsCallbackAndReturnsResult()
    {
        Result? callbackResult = null;
        TimeSpan? callbackElapsed = null;

        var result = ResultTiming.TimedWithCallback(
            () =>
            {
                Thread.Sleep(5);
                return Result.Success();
            },
            (r, e) =>
            {
                callbackResult = r;
                callbackElapsed = e;
            });

        result.IsSuccess.ShouldBeTrue();
        callbackResult.ShouldNotBeNull();
        callbackResult!.IsSuccess.ShouldBeTrue();
        callbackElapsed.ShouldNotBeNull();
        callbackElapsed.Value.TotalMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task TimedWithCallbackAsync_SuccessfulOperation_CallsCallbackAndReturnsResult()
    {
        Result<string>? callbackResult = null;
        TimeSpan? callbackElapsed = null;

        var result = await ResultTiming.TimedWithCallbackAsync(
            async () =>
            {
                await Task.Delay(10);
                return Result<string>.Success("async-callback-test");
            },
            (r, e) =>
            {
                callbackResult = r;
                callbackElapsed = e;
            });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("async-callback-test");
        
        callbackResult.ShouldNotBeNull();
        callbackResult!.IsSuccess.ShouldBeTrue();
        callbackResult.Value.ShouldBe("async-callback-test");
        
        callbackElapsed.ShouldNotBeNull();
        callbackElapsed.Value.TotalMilliseconds.ShouldBeGreaterThan(5);
    }

    [Fact]
    public void TimedResult_Deconstruction_WorksCorrectly()
    {
        var timedResult = ResultTiming.Timed(() =>
        {
            Thread.Sleep(5);
            return Result<int>.Success(42);
        });

        var (result, elapsed) = timedResult;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        elapsed.TotalMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TimedResult_NonGeneric_Deconstruction_WorksCorrectly()
    {
        var timedResult = ResultTiming.Timed(() =>
        {
            Thread.Sleep(5);
            return Result.Success();
        });

        var (result, elapsed) = timedResult;

        result.IsSuccess.ShouldBeTrue();
        elapsed.TotalMilliseconds.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TimedResult_ToString_IncludesTypeAndTiming()
    {
        var timedResult = ResultTiming.Timed(() => Result<string>.Success("test"));
        
        var str = timedResult.ToString();
        
        str.ShouldContain("TimedResult<String>");
        str.ShouldContain("Success");
        str.ShouldContain("ms");
    }

    [Fact]
    public void TimedResult_NonGeneric_ToString_IncludesTiming()
    {
        var timedResult = ResultTiming.Timed(() => Result.WithFailure("test-error"));
        
        var str = timedResult.ToString();
        
        str.ShouldContain("TimedResult:");
        str.ShouldContain("Failure");
        str.ShouldContain("ms");
    }

    [Fact]
    public void Timed_NullOperation_ReturnsFailureTimedResult()
    {
        var timedResult = ResultTiming.Timed<string>(null!);
        
        timedResult.Result.IsFailure.ShouldBeTrue();
        timedResult.Elapsed.ShouldBe(TimeSpan.Zero);
        timedResult.Result.Errors.ShouldContain("Operation function cannot be null");
    }

    [Fact]
    public void TimedWithCallback_NullOperation_ReturnsFailureResult()
    {
        var result = ResultTiming.TimedWithCallback<string>(null!, (r, e) => { });
        
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain("Operation function cannot be null");
    }

    [Fact]
    public void TimedWithCallback_NullCallback_ReturnsFailureResult()
    {
        var result = ResultTiming.TimedWithCallback(() => Result<string>.Success("test"), null!);
        
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain("Callback function cannot be null");
    }

    [Fact]
    public async Task TimedAsync_NullOperation_ReturnsFailureTimedResult()
    {
        var timedResult = await ResultTiming.TimedAsync<string>(null!);
        
        timedResult.Result.IsFailure.ShouldBeTrue();
        timedResult.Elapsed.ShouldBe(TimeSpan.Zero);
        timedResult.Result.Errors.ShouldContain("Operation function cannot be null");
    }

    [Fact]
    public void TimedResult_Properties_ProvideCorrectValues()
    {
        var result = Result<int>.Success(123);
        var elapsed = TimeSpan.FromMilliseconds(250.5);
        var timedResult = new TimedResult<int>(result, elapsed);

        timedResult.Result.ShouldBe(result);
        timedResult.Elapsed.ShouldBe(elapsed);
        timedResult.ElapsedMilliseconds.ShouldBe(250.5);
        timedResult.ElapsedMicroseconds.ShouldBe(250500);
        timedResult.IsSuccess.ShouldBeTrue();
        timedResult.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void TimedResult_NonGeneric_Properties_ProvideCorrectValues()
    {
        var result = Result.WithFailure("test-error");
        var elapsed = TimeSpan.FromMicroseconds(1500);
        var timedResult = new TimedResult(result, elapsed);

        timedResult.Result.ShouldBe(result);
        timedResult.Elapsed.ShouldBe(elapsed);
        timedResult.ElapsedMilliseconds.ShouldBe(1.5);
        timedResult.ElapsedMicroseconds.ShouldBe(1500);
        timedResult.IsSuccess.ShouldBeFalse();
        timedResult.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void TimedResult_WithHighPrecisionTiming_CapturesAccurateElapsed()
    {
        // This test verifies that timing is captured with reasonable precision
        var stopwatch = Stopwatch.StartNew();
        
        var timedResult = ResultTiming.Timed(() =>
        {
            // Busy wait for more precise timing than Thread.Sleep
            var start = Stopwatch.GetTimestamp();
            while (Stopwatch.GetElapsedTime(start).TotalMicroseconds < 100)
            {
                // Busy wait
            }
            return Result<string>.Success("precision-test");
        });

        stopwatch.Stop();

        timedResult.IsSuccess.ShouldBeTrue();
        timedResult.ElapsedMicroseconds.ShouldBeGreaterThan(50); // At least 50 microseconds
        timedResult.ElapsedMicroseconds.ShouldBeLessThan(stopwatch.Elapsed.TotalMicroseconds + 1000); // Reasonable upper bound
    }

    [Fact]
    public async Task TimedAsync_CancellationDuringOperation_MeasuresTimeUntilCancellation()
    {
        using var cts = new CancellationTokenSource();
        
        var timedTask = ResultTiming.TimedAsync(async () =>
        {
            await Task.Delay(100, cts.Token);
            return Result<string>.Success("should-not-complete");
        }, cts.Token);

        // Cancel after 20ms
        cts.CancelAfter(20);
        
        var timedResult = await timedTask;

        timedResult.IsFailure.ShouldBeTrue();
        timedResult.Result.Error.ShouldBe(ResultErrors.OperationCancelled);
        timedResult.ElapsedMilliseconds.ShouldBeGreaterThan(10); // Cancelled after ~20ms
        // Allow generous headroom for scheduler/timer variance on CI
        timedResult.ElapsedMilliseconds.ShouldBeLessThan(200);
    }
}




