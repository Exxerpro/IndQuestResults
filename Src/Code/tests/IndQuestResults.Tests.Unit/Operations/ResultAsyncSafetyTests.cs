using global::IndQuestResults.Operations;

namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Critical async safety tests for Result classes with Span&lt;T&gt; optimizations.
/// These tests ensure that internal Span&lt;T&gt; usage doesn't leak into async boundaries
/// and that Result&lt;T&gt; objects work correctly in async/await scenarios.
/// </summary>
public class ResultAsyncSafetyTests
{
    private static readonly string[] ExtraArray = ["Extra"];
    private static readonly string[] ExtraAsyncArray = ["extra"];
    
/// <summary>
/// Begin Tests Test Constants
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Test constants for async safety validation.
    /// </summary>
    private static class AsyncTestConstants
    {
        public const int SmallDelayMs = 100; // 0.1 seconds as user suggested
        public const int AsyncOperationCount = 10;
        public const string AsyncTestError = "Async test error";
        public const string AsyncTestValue = "Async test value";
        public static readonly string[] SmallErrorArray = ["Error1", "Error2", "Error3"];
    }

/// <summary>
/// End Tests Test Constants
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests Basic Async Compatibility Tests
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Tests that Result creation with Span optimizations works in async methods.
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task Result_WithSpanOptimizations_ShouldWorkInAsyncMethodsAsync()
    {
        // Arrange - Create results that should trigger Span optimizations
        var errors = AsyncTestConstants.SmallErrorArray;

        // Act - This should not cause compiler errors about ref struct in async methods
        await Task.Delay(AsyncTestConstants.SmallDelayMs, cancellationToken: CancellationToken.None);

        var result = Result.WithFailure(errors);

        await Task.Delay(AsyncTestConstants.SmallDelayMs, cancellationToken: CancellationToken.None);

        var stringRepresentation = result.ToString();

        await Task.Delay(AsyncTestConstants.SmallDelayMs, cancellationToken: CancellationToken.None);

        // Assert
        stringRepresentation.ShouldNotBeNull();
        stringRepresentation.ShouldContain(ResultConstants.FailurePrefix);
        stringRepresentation.ShouldContain("Error1");
    }

    /// <summary>
    /// Tests that CombineErrors with Span optimizations works in async methods.
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task CombineErrors_WithSpanOptimizations_ShouldWorkInAsyncMethodsAsync()
    {
        // Arrange - Small collections that should trigger Span optimization
        var primaryErrors = new List<string> { "Primary1", "Primary2" };
        var secondaryErrors = new List<string> { "Secondary1", "Secondary2" };

        // Act - Test across async boundaries
        await Task.Delay(AsyncTestConstants.SmallDelayMs, cancellationToken: CancellationToken.None);

        var combinedResult = Result.CombineErrors(primaryErrors, secondaryErrors);

        await Task.Delay(AsyncTestConstants.SmallDelayMs, cancellationToken: CancellationToken.None);

        // Assert
        combinedResult.IsFailure.ShouldBeTrue();
        combinedResult.Errors.Count().ShouldBe(4);
    }

/// <summary>
/// End Tests Basic Async Compatibility Tests
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests Task Continuation and Exception Handling Tests
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Tests that Result operations work correctly in task continuations.
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task Result_TaskContinuations_ShouldWorkCorrectlyAsync()
    {
        // Arrange & Act
        var finalResult = await Task.Run(async () =>
        {
            await Task.Delay(AsyncTestConstants.SmallDelayMs, cancellationToken: CancellationToken.None);

            // Use Span optimizations in continuation
            var errors = AsyncTestConstants.SmallErrorArray;
            var errorResult = Result<string>.WithFailure(errors);
            var _ = errorResult.ToString();

            var result = Result<string>.Success("Initial");
            return result.IsSuccess
                ? Result<string>.Success($"Continued: {result.Value}")
                : Result<string>.WithFailure("Continuation failed");
        });

        // Assert
        finalResult.IsSuccess.ShouldBeTrue();
        finalResult.Value!.ShouldBe("Continued: Initial");
    }

    /// <summary>
    /// Tests exception handling in async methods with Result Span optimizations.
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the test finishes.</returns>
    public async Task Result_AsyncExceptionHandling_ShouldWorkCorrectlyAsync()
    {
        // Arrange
        var exceptionThrown = false;
        Result<string>? result = null!;

        try
        {
            // Act - Simulate async operation that might throw
            await Task.Delay(AsyncTestConstants.SmallDelayMs, cancellationToken: CancellationToken.None);
            result = await SimulateAsyncOperationWithSpanOptimizationsAsync(false, cancellationToken: CancellationToken.None);
        }
        catch (Exception)
        {
            exceptionThrown = true;
        }

        // Assert
        exceptionThrown.ShouldBeFalse();
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBe("Async operation succeeded");
    }

    /// <summary>
    /// Helper method that simulates async operations with Span optimizations.
    /// </summary>
    /// <param name="shouldThrow">If set to <c>true</c>, the operation throws to simulate errors.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>A task producing a <see cref="Result{T}"/> with the simulated outcome.</returns>
    private static async Task<Result<string>> SimulateAsyncOperationWithSpanOptimizationsAsync(bool shouldThrow, CancellationToken cancellationToken = default)
    {
        await Task.Delay(AsyncTestConstants.SmallDelayMs, cancellationToken: cancellationToken);

        if (shouldThrow)
        {
            throw new InvalidOperationException("Simulated async exception");
        }

        // Use operations that trigger Span optimizations
        var tempErrors = AsyncTestConstants.SmallErrorArray;
        var tempResult = Result.WithFailure(tempErrors);
        var _ = tempResult.ToString(); // Span optimization

        var combinedResult = Result.CombineErrors(tempErrors, ExtraArray);
        var __ = combinedResult.ToString(); // Span optimization

        return Result<string>.Success("Async operation succeeded");
    }

/// <summary>
/// End Tests Task Continuation and Exception Handling Tests
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests Stress Tests for Async Safety
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Stress test with many concurrent async operations using Span optimizations.
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the stress test finishes.</returns>
    public async Task Result_HighConcurrencyStressTest_ShouldWorkCorrectlyAsync()
    {
        // Arrange - Create many concurrent tasks
        const int highConcurrencyCount = 100;
        var tasks = new List<Task<bool>>();

        for (var i = 0; i < highConcurrencyCount; i++)
        {
            var taskId = i;
            tasks.Add(PerformConcurrentAsyncOperationAsync(taskId));
        }

        // Act - Wait for all tasks
        var results = await Task.WhenAll(tasks);

        // Assert - All operations should succeed
        results.Length.ShouldBe(highConcurrencyCount);
        results.All(success => success).ShouldBeTrue();
    }

    /// <summary>
    /// Helper method for high concurrency stress testing.
    /// </summary>
    /// <param name="taskId">Sequential identifier used to produce unique values.</param>
    /// <returns>A task indicating whether the operation succeeded.</returns>
    private static async Task<bool> PerformConcurrentAsyncOperationAsync(int taskId)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken: CancellationToken.None); // Very short delay for high concurrency

            // Perform operations that use Span optimizations
            var errors = new[] { $"Error{taskId}A", $"Error{taskId}B", $"Error{taskId}C" };

            var result1 = Result.WithFailure(errors);
            var str1 = result1.ToString();

            var result2 = Result<int>.WithFailure(errors, taskId);
            var str2 = result2.ToString();

            var combined = Result.CombineErrors(errors, [$"Combined{taskId}"]);
            var str3 = combined.ToString();

            await Task.Delay(TimeSpan.FromMilliseconds(1), cancellationToken: CancellationToken.None);

            // Verify results
            return result1.IsFailure && result2.IsFailure && combined.IsFailure &&
                   str1.Contains($"Error{taskId}A") && str2.Contains($"Error{taskId}B") && str3.Contains($"Combined{taskId}");
        }
        catch
        {
            return false;
        }
    }

/// <summary>
/// End Tests Stress Tests for Async Safety
/// </summary>
/// <returns></returns>

/// <summary>
/// Begin Tests Documentation and Validation Tests
/// </summary>
/// <returns></returns>

    /// <summary>
    /// Documents that our Span optimizations are safe in async contexts.
    /// This test serves as living documentation of async safety guarantees.
    /// </summary>
    [Fact]
    /// <returns>A task that completes when the documentation test finishes.</returns>
    public async Task Result_AsyncSafetyDocumentation_ShouldDemonstrateCorrectUsageAsync()
    {
        // ✅ SAFE: Span<T> is used only in internal static methods
        // ✅ SAFE: Result<T> stores arrays, not Span<T>
        // ✅ SAFE: Optimizations complete before method returns
        // ✅ SAFE: No ref struct crosses async boundaries

        await Task.Delay(AsyncTestConstants.SmallDelayMs, cancellationToken: CancellationToken.None);

        // These operations use Span optimizations internally but are async-safe
        var result = Result.WithFailure(AsyncTestConstants.SmallErrorArray);
        var genericResult = Result<string>.WithFailure(AsyncTestConstants.SmallErrorArray, "value");
        var combined = Result.CombineErrors(AsyncTestConstants.SmallErrorArray, ExtraAsyncArray);

        await Task.Delay(AsyncTestConstants.SmallDelayMs, cancellationToken: CancellationToken.None);

        // Test string representations (triggers Span optimizations)
        var str1 = result.ToString();
        var str2 = genericResult.ToString();
        var str3 = combined.ToString();

        // All operations complete successfully
        result.IsFailure.ShouldBeTrue();
        genericResult.IsFailure.ShouldBeTrue();
        combined.IsFailure.ShouldBeTrue();

        str1.ShouldContain("Error1");
        str2.ShouldContain("Error1");
        str3.ShouldContain("Error1");

        // ✅ CONCLUSION: Span optimizations are completely safe in async contexts
        true.ShouldBeTrue("Async safety validation completed successfully");
    }

/// <summary>
/// End Tests Documentation and Validation Tests
/// </summary>
/// <returns></returns>
}
