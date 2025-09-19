using IndQuestResults.Async;

namespace IndQuestResults.Tests.Unit.Async;

public class ResultAsyncTheoryTests
{
    [Theory]
    [InlineData(0, 0)]            // empty
    [InlineData(3, 0)]            // all success
    [InlineData(5, 2)]            // single failure
    [InlineData(6, 3)]            // one failure mid
    public async Task SequenceAsync_VariousCounts_AndFailures(int count, int failAt)
    {
        var tasks = BuildResultTasks(count, failAt);
        var res = await ResultAsync.SequenceAsync(tasks);

        if (count == 0)
        {
            res.IsSuccess.ShouldBeTrue();
            res.Value!.ShouldBeEmpty();
        }
        else if (failAt == 0)
        {
            res.IsSuccess.ShouldBeTrue();
            res.Value!.ShouldBe(Enumerable.Range(1, count));
        }
        else
        {
            res.IsFailure.ShouldBeTrue();
            res.Errors.ShouldNotBeEmpty();
        }
    }

    private static Task<Result<int>>[] BuildResultTasks(int count, int failAt)
    {
        var list = new List<Task<Result<int>>>(count);
        for (int i = 1; i <= count; i++)
        {
            if (failAt == i)
            {
                list.Add(Task.FromResult(Result<int>.WithFailure($"e{i}")));
            }
            else
            {
                list.Add(Task.FromResult(Result<int>.Success(i)));
            }
        }
        return list.ToArray();
    }

    [Theory]
    [InlineData(0, false, false)]   // empty inputs
    [InlineData(4, false, false)]   // all success
    [InlineData(5, true, false)]    // one failure
    public async Task TraverseAsync_Various(int count, bool injectFailure, bool throwException)
    {
        var inputs = Enumerable.Range(1, count).ToArray();

        Func<int, Task<Result<int>>> fn = throwException
            ? _ => throw new InvalidOperationException("boom")
            : (x) => Task.FromResult(injectFailure && x == 3 ? Result<int>.WithFailure("bad3") : Result<int>.Success(x * 2));

        var res = await ResultAsync.TraverseAsync(inputs, fn);

        if (count == 0)
        {
            res.IsSuccess.ShouldBeTrue();
            res.Value!.ShouldBeEmpty();
        }
        else if (throwException)
        {
            res.IsFailure.ShouldBeTrue();
            res.Error!.ShouldContain("Async traverse failed");
        }
        else if (injectFailure)
        {
            res.IsFailure.ShouldBeTrue();
            res.Errors.ShouldContain("bad3");
        }
        else
        {
            res.IsSuccess.ShouldBeTrue();
            res.Value!.ShouldBe(inputs.Select(v => v * 2));
        }
    }

    [Theory]
    [InlineData(8, 2, false)]
    [InlineData(16, 4, true)]
    public async Task TraverseParallelAsync_Various(int count, int dop, bool includeFailure)
    {
        var inputs = Enumerable.Range(1, count).ToArray();
        Task<Result<int>> FnLocal(int x) => Task.FromResult(includeFailure && x % 7 == 0 ? Result<int>.WithFailure("bad") : Result<int>.Success(x));

        Func<int, Task<Result<int>>> fn = FnLocal;

        var res = await ResultAsync.TraverseParallelAsync(inputs, fn, maxDegreeOfParallelism: dop);
        if (includeFailure)
        {
            res.IsFailure.ShouldBeTrue();
            res.Errors.ShouldContain("bad");
        }
        else
        {
            res.IsSuccess.ShouldBeTrue();
            res.Value!.ShouldBe(inputs);
        }
    }
}
