namespace IndQuestResults.Tests.Unit.Async;

/// <summary>
/// Tests for async sequencing helpers ensuring aggregation of values and errors,
/// and correct behavior for empty and cancelled scenarios.
/// </summary>
public class ResultAsyncSequenceTests
{
    /// <summary>
    /// Ensures SequenceAsync returns all values when all tasks succeed.
    /// </summary>
    [Fact]
    public async Task SequenceAsync_AllSuccess_ReturnsAll()
    {
        Task<Result<int>>[] tasks =
        [
            Task.FromResult(Result<int>.Success(1)),
            Task.FromResult(Result<int>.Success(2)),
            Task.FromResult(Result<int>.Success(3)),
        ];

        var res = await ResultAsync.SequenceAsync(tasks);
        res.IsSuccess.ShouldBeTrue();
        res.Value!.ShouldBe([1, 2, 3]);
    }

    /// <summary>
    /// Ensures SequenceAsync aggregates errors from failed tasks.
    /// </summary>
    [Fact]
    public async Task SequenceAsync_WithFailure_AggregatesErrors()
    {
        Task<Result<int>>[] tasks =
        [
            Task.FromResult(Result<int>.Success(1)),
            Task.FromResult(Result<int>.WithFailure("e1")),
            Task.FromResult(Result<int>.WithFailure("e2")),
        ];

        var res = await ResultAsync.SequenceAsync(tasks);
        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("e1");
        res.Errors.ShouldContain("e2");
    }

    /// <summary>
    /// Ensures SequenceAsync returns an empty success on empty input.
    /// </summary>
    [Fact]
    public async Task SequenceAsync_Empty_ReturnsEmptySuccess()
    {
        var res = await ResultAsync.SequenceAsync(Array.Empty<Task<Result<int>>>());
        res.IsSuccess.ShouldBeTrue();
        res.Value!.ShouldBeEmpty();
    }

    /// <summary>
    /// Ensures SequenceAsync returns cancelled when token is pre-cancelled.
    /// </summary>
    [Fact]
    public async Task SequenceAsync_PreCancelled_ReturnsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Task<Result<int>>[] tasks = [Task.FromResult(Result<int>.Success(1))];
        var res = await ResultAsync.SequenceAsync(tasks, cts.Token);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultErrors.OperationCancelled);
    }
}
