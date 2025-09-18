namespace IndQuestResults.Tests.Unit.Extensions.Async;

public class ResultAsyncMapAndTraverseTests
{
    [Fact]
    public async Task MapAsync_WithSuccessfulResult_TransformsValue()
    {
        var input = Task.FromResult(Result<int>.Success(5));
        var mapped = await input.MapAsync(async x => { await Task.Delay(5); return x * 2; });

        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe(10);
    }

    [Fact]
    public async Task MapAsync_WithFailedResult_PropagatesErrors()
    {
        var input = Task.FromResult(Result<int>.WithFailure("e1"));
        var mapped = await input.MapAsync(x => Task.FromResult(x * 2));

        mapped.IsFailure.ShouldBeTrue();
        mapped.Errors.ShouldContain("e1");
    }

    [Fact]
    public async Task MapAsync_WithException_ReturnsFailure()
    {
        var input = Task.FromResult(Result<int>.Success(1));
        var mapped = await input.MapAsync<int, int>(async _ => { await Task.Delay(1); throw new InvalidOperationException("boom"); });

        mapped.IsFailure.ShouldBeTrue();
        mapped.Error!.ShouldContain("Async map operation failed");
        mapped.Error!.ShouldContain("boom");
    }

    [Fact]
    public async Task MapAsync_WithCancellation_ReturnsCancelled()
    {
        var input = Task.FromResult(Result<int>.Success(2));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var mapped = await input.MapAsync(x => Task.FromResult(x * 3), cts.Token);
        mapped.IsFailure.ShouldBeTrue();
        mapped.Error.ShouldBe(ResultErrors.OperationCancelled);
    }

    [Fact]
    public async Task TraverseAsync_AllSuccess_ShouldSucceedWithAllOutputs()
    {
        var inputs = Enumerable.Range(1, 5);
        static async Task<Result<int>> Fn(int x) { await Task.Delay(2); return Result<int>.Success(x * x); }

        var res = await ResultAsync.TraverseAsync(inputs, Fn);

        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBeAssignableTo<IEnumerable<int>>();
        res.Value!.ShouldBe([1, 4, 9, 16, 25]);
    }

    [Fact]
    public async Task TraverseAsync_WithFailures_ShouldAggregateErrors()
    {
        var inputs = new[] { 1, 2, 3 };
        static Task<Result<int>> Fn(int x) => Task.FromResult(x == 2 ? Result<int>.WithFailure($"bad-{x}") : Result<int>.Success(x));

        var res = await ResultAsync.TraverseAsync(inputs, Fn);

        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("bad-2");
    }

    [Fact]
    public async Task TraverseAsync_Cancellation_ReturnsCancelled()
    {
        var inputs = Enumerable.Range(1, 3);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        static Task<Result<int>> Fn(int x) => Task.FromResult(Result<int>.Success(x));
        var res = await ResultAsync.TraverseAsync(inputs, Fn, cts.Token);

        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldBe(ResultErrors.OperationCancelled);
    }

    [Fact]
    public async Task SequenceAsync_MixedResults_ShouldAggregate()
    {
        Task<Result<int>>[] tasks = [
            Task.FromResult(Result<int>.Success(1)),
            Task.FromResult(Result<int>.WithFailure("nope")),
            Task.FromResult(Result<int>.Success(3))
        ];

        var res = await ResultAsync.SequenceAsync(tasks);
        res.IsFailure.ShouldBeTrue();
        res.Errors.ShouldContain("nope");
    }

    [Fact]
    public async Task TraverseParallelAsync_AllSuccess_ShouldSucceed()
    {
        var inputs = Enumerable.Range(1, 8);
        static async Task<Result<int>> Fn(int x) { await Task.Delay(2); return Result<int>.Success(x + 1); }

        var res = await ResultAsync.TraverseParallelAsync(inputs, Fn, maxDegreeOfParallelism: 4);
        res.IsSuccess.ShouldBeTrue();
        res.Value!.ShouldBe(Enumerable.Range(2, 8));
    }

    [Fact]
    public async Task TraverseParallelAsync_InvalidDegree_ShouldThrow()
    {
        var inputs = new[] { 1 };
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Should.ThrowAsync<ArgumentException>(async () =>
            await ResultAsync.TraverseParallelAsync(inputs, x => Task.FromResult(Result<int>.Success(x)), 0, cts.Token));
    }
}



