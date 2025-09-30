namespace IndQuestResults.Tests.Unit.Async;

/// <summary>
/// Additional async tests covering IAsyncEnumerable sequencing and traversal helpers.
/// Verifies error aggregation and mapping behavior for async streams.
/// </summary>
public class ResultAsyncExtraTests
{
    private static async IAsyncEnumerable<Result<int>> GetAsyncResults([EnumeratorCancellation] CancellationToken ct = default)
    {
        yield return Result<int>.Success(1);
        await Task.Yield();
        yield return Result<int>.WithFailure("err");
        await Task.Yield();
        yield return Result<int>.Success(3);
    }

    /// <summary>
    /// Ensures that SequenceAsync over an async stream aggregates failures.
    /// </summary>
    [Fact]
    public async Task SequenceAsync_IAsyncEnumerable_CollectsErrors()
    {
        var r = await ResultAsync.SequenceAsync(GetAsyncResults());
        r.IsFailure.ShouldBeTrue();
        r.Errors.ShouldContain("err");
    }

    /// <summary>
    /// Ensures that TraverseAsync over an async stream maps values successfully.
    /// </summary>
    [Fact]
    public async Task TraverseAsync_IAsyncEnumerable_MapsValues()
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async IAsyncEnumerable<int> Src()
        {
            yield return 2;
            yield return 3;
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        var r = await ResultAsync.TraverseAsync(Src(), v => Task.FromResult(Result<int>.Success(v * 10)));
        r.IsSuccess.ShouldBeTrue();
        r.Value!.ShouldContain(20);
        r.Value!.ShouldContain(30);
    }
}