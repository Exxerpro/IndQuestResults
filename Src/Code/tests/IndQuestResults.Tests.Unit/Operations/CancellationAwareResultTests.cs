using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using global::IndQuestResults.Operations;

namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Verifies cancellation-aware wrappers: timeout vs external-cancel precedence and success paths.
/// Adds edge-focused tests to ensure branch guards are observable and deterministic.
/// </summary>
public class CancellationAwareResultTests
{
    /// <summary>
    /// Ensures that when only the timeout token is triggered, a timeout failure is returned.
    /// </summary>
    [Fact]
    public async Task WrapWithTimeout_ShouldReturnTimeoutFailure_WhenOnlyTimeoutTriggers()
    {
        // Arrange: long-running operation, short timeout, no external cancel
        // Act
        var result = await CancellationAwareResult.WrapWithTimeout(
            async ct => { await Task.Delay(TimeSpan.FromMilliseconds(200), ct); return 42; },
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert: timeout failure (not external cancellation)
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(ResultErrors.OperationTimedOut);
    }

    /// <summary>
    /// Ensures the generic wrapper returns success when the external token is not cancelled.
    /// </summary>
    [Fact]
    public async Task WrapCancellationAware_Generic_ShouldReturnSuccess_WhenNotCancelled()
    {
        // Arrange: operation completes quickly, token not cancelled
        // Act
        var result = await CancellationAwareResult.WrapCancellationAware<int>(
            operation: async ct => { await Task.Delay(10, ct); return 7; },
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(7);
    }

    /// <summary>
    /// Ensures the wrapper around a Result&lt;T&gt; returning operation yields the inner success value.
    /// </summary>
    [Fact]
    public async Task WrapResultOperation_ShouldReturnInnerResult_WhenNotCancelled()
    {
        // Arrange
        // Act
        var result = await CancellationAwareResult.WrapResultOperation(async ct =>
        {
            await Task.Delay(5, ct);
            return Result<int>.Success(11);
        }, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(11);
    }

    /// <summary>
    /// Ensures non-generic wrapper returns success when there is no cancellation.
    /// </summary>
    [Fact]
    public async Task WrapCancellationAware_NonGeneric_ShouldReturnSuccess_WhenNotCancelled()
    {
        // Arrange
        // Act
        var result = await CancellationAwareResult.WrapCancellationAware(ct => Task.Delay(10, ct), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Ensures that a fast operation with a generous timeout completes successfully (no timeout).
    /// </summary>
    [Fact]
    public async Task WrapWithTimeout_ShouldReturnSuccess_WhenOperationCompletesBeforeTimeout()
    {
        // Arrange: fast operation, generous timeout
        // Act
        var result = await CancellationAwareResult.WrapWithTimeout(
            async ct => { await Task.Delay(TimeSpan.FromMilliseconds(20), ct); return 123; },
            TimeSpan.FromMilliseconds(500),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(123);
    }
    /// <summary>
    /// Ensures that when the external token is cancelled, the result reports Cancelled, not Timeout.
    /// </summary>
    [Fact]
    public async Task WrapWithTimeout_ShouldReturnCancelled_WhenExternalTokenCancels()
    {
        // Arrange: long-running operation, generous timeout, but external cancel
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await CancellationAwareResult.WrapWithTimeout(
            async ct => { await Task.Delay(TimeSpan.FromMilliseconds(200), ct); return 42; },
            TimeSpan.FromSeconds(5),
            cts.Token);

        // Assert: reported as cancelled
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Ensures the generic wrapper returns Cancelled when the token is cancelled before invocation.
    /// </summary>
    [Fact]
    public async Task WrapCancellationAware_Generic_ShouldReturnCancelled_OnEarlyCancellation()
    {
        // Arrange: early cancelled token
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await CancellationAwareResult.WrapCancellationAware<int>(
            operation: _ => Task.FromResult(1),
            cancellationToken: cts.Token);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(ResultErrors.OperationCancelled);
    }
}



