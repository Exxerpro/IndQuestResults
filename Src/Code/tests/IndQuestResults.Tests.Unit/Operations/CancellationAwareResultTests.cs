using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using global::IndQuestResults.Operations;

namespace IndQuestResults.Tests.Unit.Operations;

public class CancellationAwareResultTests
{
    [Fact]
    public async Task WrapWithTimeout_ShouldReturnTimeoutFailure_WhenOnlyTimeoutTriggers()
    {
        // Arrange: long-running operation, short timeout, no external cancel
        async Task<int> Op(CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
            return 42;
        }

        // Act
        var result = await CancellationAwareResult.WrapWithTimeout(
            Op,
            TimeSpan.FromMilliseconds(50),
            CancellationToken.None);

        // Assert: timeout failure (not external cancellation)
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(ResultErrors.OperationTimedOut);
    }

    [Fact]
    public async Task WrapCancellationAware_Generic_ShouldReturnSuccess_WhenNotCancelled()
    {
        // Arrange: operation completes quickly, token not cancelled
        async Task<int> Op(CancellationToken ct)
        {
            await Task.Delay(10, ct);
            return 7;
        }

        // Act
        var result = await CancellationAwareResult.WrapCancellationAware<int>(
            operation: Op,
            cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(7);
    }

    [Fact]
    public async Task WrapResultOperation_ShouldReturnInnerResult_WhenNotCancelled()
    {
        // Arrange
        async Task<Result<int>> Op(CancellationToken ct)
        {
            await Task.Delay(5, ct);
            return Result<int>.Success(11);
        }

        // Act
        var result = await CancellationAwareResult.WrapResultOperation(Op, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(11);
    }

    [Fact]
    public async Task WrapCancellationAware_NonGeneric_ShouldReturnSuccess_WhenNotCancelled()
    {
        // Arrange
        async Task Op(CancellationToken ct)
        {
            await Task.Delay(10, ct);
        }

        // Act
        var result = await CancellationAwareResult.WrapCancellationAware(Op, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task WrapWithTimeout_ShouldReturnSuccess_WhenOperationCompletesBeforeTimeout()
    {
        // Arrange: fast operation, generous timeout
        async Task<int> Op(CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), ct);
            return 123;
        }

        // Act
        var result = await CancellationAwareResult.WrapWithTimeout(
            Op,
            TimeSpan.FromMilliseconds(500),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(123);
    }
    [Fact]
    public async Task WrapWithTimeout_ShouldReturnCancelled_WhenExternalTokenCancels()
    {
        // Arrange: long-running operation, generous timeout, but external cancel
        async Task<int> Op(CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
            return 42;
        }

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await CancellationAwareResult.WrapWithTimeout(
            Op,
            TimeSpan.FromSeconds(5),
            cts.Token);

        // Assert: reported as cancelled
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(ResultErrors.OperationCancelled);
    }

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
