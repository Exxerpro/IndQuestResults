using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace IndQuestResults.Tests.Unit.Async;

public class ResultAsyncValueTaskEdgeTests
{
    [Fact]
    public async Task ThenAsync_OnFailure_PreservesErrors()
    {
        var failure = new ValueTask<Result<int>>(Result<int>.WithFailure("boom"));
        var nextCalled = false;
        var result = await ResultAsync.ThenAsync(failure, _ =>
        {
            nextCalled = true;
            return new ValueTask<Result<string>>(Result<string>.Success("ok"));
        });

        nextCalled.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain("boom");
    }

    [Fact]
    public async Task ThenMap_OnFailure_ReturnsFailureWithDefaultMessageWhenNoErrors()
    {
        var failedNoErrors = new ValueTask<Result<int>>(Result<int>.WithFailure(errors: Array.Empty<string>(), value: default));
        var mapped = await ResultAsync.ThenMap(failedNoErrors, i => i + 1);
        mapped.IsFailure.ShouldBeTrue();
        mapped.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    [Fact]
    public async Task ThenTap_OnFailure_DoesNotInvokeAction()
    {
        var failure = new ValueTask<Result<int>>(Result<int>.WithFailure("e"));
        var tapped = false;
        var result = await ResultAsync.ThenTap(failure, _ =>
        {
            tapped = true;
            return new ValueTask();
        });
        tapped.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task BindAsync_TaskCancelled_ReturnsCancelled()
    {
        using var cts = new CancellationTokenSource();
        var tcs = new TaskCompletionSource<Result<int>>();
        var task = tcs.Task;

        var bindTask = ResultAsync.BindAsync(task, _ => Task.FromResult(Result<string>.Success("x")), cts.Token);
        cts.Cancel();
        tcs.SetResult(Result<int>.Success(1));
        var bound = await bindTask;
        bound.IsFailure.ShouldBeTrue();
        bound.IsCancelled().ShouldBeTrue();
    }

    [Fact]
    public async Task TraverseParallelAsync_InvalidDegree_Throws()
    {
        await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await ResultAsync.TraverseParallelAsync(new[] { 1 }, i => Task.FromResult(Result<string>.Success(i.ToString())), 0);
        });
    }

    [Fact]
    public async Task SequenceAsync_TaskResults_Exception_ReturnsFailure()
    {
        var good = Task.FromResult(Result<int>.Success(1));
        var bad = Task.FromException<Result<int>>(new InvalidOperationException("nope"));
        var result = await ResultAsync.SequenceAsync(new[] { good, bad });
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldContain("Async sequence failed");
    }
}
