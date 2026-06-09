namespace IndQuestResults.Tests.Mutation.Async;

/// <summary>
/// Edge and theory tests for async traversal and parallel traversal helpers in ResultAsync.
/// Validates null arguments, empty inputs, cancellation, and timeout behavior.
/// </summary>
public class ResultAsyncEdgeAndTheoryTests
{
    /// <summary>
    /// Ensures TraverseAsync returns failure when inputs collection is null.
    /// </summary>
    [Fact]
    public async Task TraverseAsync_NullInputs_ReturnsFailure()
    {
        var result = await ResultAsync.TraverseAsync<string, int>((IEnumerable<string>)null!, _ => Task.FromResult(Result<int>.Success(0)));
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain("Inputs cannot be null");
    }

    /// <summary>
    /// Ensures TraverseAsync returns failure when mapping function is null.
    /// </summary>
    [Fact]
    public async Task TraverseAsync_NullFunc_ReturnsFailure()
    {
        var inputs = new[] { "a" };
        var result = await ResultAsync.TraverseAsync<string, int>(inputs, null!);
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain("Function cannot be null");
    }

    /// <summary>
    /// Ensures TraverseAsync with empty inputs returns a successful empty result set.
    /// </summary>
    [Fact]
    public async Task TraverseAsync_EmptyInputs_SucceedsWithEmpty()
    {
        var res = await ResultAsync.TraverseAsync<int, int>([], x => Task.FromResult(Result<int>.Success(x)));
        res.IsSuccess.ShouldBeTrue();
        res.Value!.ShouldBeEmpty();
    }

    /// <summary>
    /// Ensures TraverseParallelAsync returns a cancelled result when token is cancelled mid-execution.
    /// </summary>
    [Fact]
    public async Task TraverseParallelAsync_CancelMidway_ReturnsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(10);
        var inputs = Enumerable.Range(1, 20);
        var res = await ResultAsync.TraverseParallelAsync(inputs, async x => { await Task.Delay(5); return Result<int>.Success(x); }, 4, cts.Token);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultErrors.OperationCancelled);
    }

    /// <summary>
    /// Ensures pre-cancelled token produces a cancelled result for TraverseAsync.
    /// </summary>
    [Fact]
    public async Task TraverseAsync_PreCancelled_ReturnsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var inputs = new[] { 1, 2, 3 };
        var res = await ResultAsync.TraverseAsync(inputs, x => Task.FromResult(Result<int>.Success(x)), cts.Token);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultErrors.OperationCancelled);
    }
}
