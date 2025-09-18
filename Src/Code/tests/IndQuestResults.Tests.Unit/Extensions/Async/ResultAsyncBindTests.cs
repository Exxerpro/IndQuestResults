namespace IndQuestResults.Tests.Unit.Extensions.Async;

/// <summary>
/// Comprehensive tests for ResultAsync.BindAsync method.
/// Tests cover success cases, error propagation, cancellation, and exception handling.
/// </summary>
public class ResultAsyncBindTests
{
    /// <summary>
    /// Tests that BindAsync chains successful operations correctly.
    /// </summary>
    [Fact]
    public async Task BindAsync_WithSuccessfulResult_ShouldChainOperation()
    {
        // Arrange
        var initialResult = Result<int>.Success(42);
        var resultTask = Task.FromResult(initialResult);

        // Act
        var boundResult = await resultTask.BindAsync(async value =>
        {
            await Task.Delay(10);
            return Result<string>.Success($"Value: {value}");
        });

        // Assert
        boundResult.IsSuccess.ShouldBeTrue();
        boundResult.Value.ShouldBe("Value: 42");
        boundResult.Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that BindAsync propagates errors from failed input Result.
    /// </summary>
    [Fact]
    public async Task BindAsync_WithFailedResult_ShouldPropagateErrors()
    {
        // Arrange
        var errors = new[] { "Input error" };
        var initialResult = Result<int>.WithFailure(errors);
        var resultTask = Task.FromResult(initialResult);

        // Act
        var boundResult = await resultTask.BindAsync(async value =>
        {
            await Task.Delay(10);
            return Result<string>.Success($"Value: {value}");
        });

        // Assert
        boundResult.IsFailure.ShouldBeTrue();
        boundResult.Errors.ShouldBe(errors);
        boundResult.Value.ShouldBeNull();
    }

    /// <summary>
    /// Tests that BindAsync handles exceptions in the bound function.
    /// </summary>
    [Fact]
    public async Task BindAsync_WithExceptionInBoundFunction_ShouldReturnFailure()
    {
        // Arrange
        var initialResult = Result<int>.Success(42);
        var resultTask = Task.FromResult(initialResult);

        // Act
        var boundResult = await resultTask.BindAsync<int, string>(async value =>
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Test exception");
        });

        // Assert
        boundResult.IsFailure.ShouldBeTrue();
        boundResult.Error.ShouldNotBeNull();
        boundResult.Error.ShouldContain("Async bind operation failed");
        boundResult.Error.ShouldContain("Test exception");
    }

    /// <summary>
    /// Tests that BindAsync handles cancellation correctly.
    /// </summary>
    [Fact]
    public async Task BindAsync_WithCancellation_ShouldReturnCancelledResult()
    {
        // Arrange
        var initialResult = Result<int>.Success(42);
        var resultTask = Task.FromResult(initialResult);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var boundResult = await resultTask.BindAsync(async value =>
        {
            await Task.Delay(10);
            return Result<string>.Success($"Value: {value}");
        }, cts.Token);

        // Assert
        boundResult.IsFailure.ShouldBeTrue();
        boundResult.Error.ShouldBe(ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Tests that BindAsync handles OperationCanceledException.
    /// </summary>
    [Fact]
    public async Task BindAsync_WithOperationCanceledException_ShouldReturnCancelledResult()
    {
        // Arrange
        var initialResult = Result<int>.Success(42);
        var resultTask = Task.FromResult(initialResult);

        // Act
        var boundResult = await resultTask.BindAsync<int, string>(async value =>
        {
            await Task.Delay(10);
            throw new OperationCanceledException("Operation was cancelled");
        });

        // Assert
        boundResult.IsFailure.ShouldBeTrue();
        boundResult.Error.ShouldBe(ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Tests that BindAsync throws ArgumentNullException for null resultTask.
    /// </summary>
    [Fact]
    public async Task BindAsync_WithNullResultTask_ShouldThrowArgumentNullException()
    {
        // Arrange
        Task<Result<int>>? nullTask = null;

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () => await nullTask!.BindAsync(async value =>
            {
                await Task.Delay(10);
                return Result<string>.Success($"Value: {value}");
            }));
    }

    /// <summary>
    /// Tests that BindAsync throws ArgumentNullException for null function.
    /// </summary>
    [Fact]
    public async Task BindAsync_WithNullFunction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var initialResult = Result<int>.Success(42);
        var resultTask = Task.FromResult(initialResult);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () => await resultTask.BindAsync<int, string>(null!));
    }

    /// <summary>
    /// Tests that BindAsync preserves multiple errors from failed Result.
    /// </summary>
    [Fact]
    public async Task BindAsync_WithMultipleErrorsInResult_ShouldPreserveAllErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };
        var initialResult = Result<int>.WithFailure(errors);
        var resultTask = Task.FromResult(initialResult);

        // Act
        var boundResult = await resultTask.BindAsync(async value =>
        {
            await Task.Delay(10);
            return Result<string>.Success($"Value: {value}");
        });

        // Assert
        boundResult.IsFailure.ShouldBeTrue();
        boundResult.Errors.ShouldBe(errors);
    }

    /// <summary>
    /// Tests that BindAsync propagates errors from the bound function.
    /// </summary>
    [Fact]
    public async Task BindAsync_WithFailureFromBoundFunction_ShouldPropagateErrors()
    {
        // Arrange
        var initialResult = Result<int>.Success(42);
        var resultTask = Task.FromResult(initialResult);
        var boundErrors = new[] { "Bound function error" };

        // Act
        var boundResult = await resultTask.BindAsync(async value =>
        {
            await Task.Delay(10);
            return Result<string>.WithFailure(boundErrors);
        });

        // Assert
        boundResult.IsFailure.ShouldBeTrue();
        boundResult.Errors.ShouldBe(boundErrors);
    }

    /// <summary>
    /// Tests that BindAsync works with ConfigureAwait(false) patterns.
    /// </summary>
    [Fact]
    public async Task BindAsync_WithConfigureAwaitFalse_ShouldWorkCorrectly()
    {
        // Arrange
        var initialResult = Result<int>.Success(42);
        var resultTask = Task.FromResult(initialResult);

        // Act
        var boundResult = await resultTask.BindAsync(async value =>
        {
            await Task.Delay(10).ConfigureAwait(false);
            return Result<string>.Success($"Value: {value}");
        });

        // Assert
        boundResult.IsSuccess.ShouldBeTrue();
        boundResult.Value.ShouldBe("Value: 42");
    }
}


