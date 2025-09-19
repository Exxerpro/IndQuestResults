using IndQuestResults.Async;

namespace IndQuestResults.Tests.Unit.Async;

public class ResultAsyncEdgeAndTheoryTests
{
    [Fact]
    public async Task TraverseAsync_NullInputs_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await ResultAsync.TraverseAsync<string, int>(null!, _ => Task.FromResult(Result<int>.Success(0))));
    }

    [Fact]
    public async Task TraverseAsync_NullFunc_Throws()
    {
        var inputs = new[] { "a" };
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await ResultAsync.TraverseAsync<string, int>(inputs, null!));
    }

    [Fact]
    public async Task TraverseAsync_EmptyInputs_SucceedsWithEmpty()
    {
        var res = await ResultAsync.TraverseAsync<int, int>([], x => Task.FromResult(Result<int>.Success(x)));
        res.IsSuccess.ShouldBeTrue();
        res.Value!.ShouldBeEmpty();
    }

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
