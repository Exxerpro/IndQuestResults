namespace IndQuestResults.Tests.Mutation.Collections;

public class ResultCollectionsBoundaryTests
{
    [Fact]
    public void Sequence_EmptyCollection_SucceedsWithEmpty()
    {
        var results = Array.Empty<Result<int>>();
        var seq = IndQuestResults.Collections.ResultCollections.Sequence(results);
        seq.IsRecoverable.ShouldBeTrue();
        seq.Value!.ShouldBeEmpty();
    }

    [Fact]
    public void SequenceFailFast_StopsOnFirstFailure()
    {
        var items = new[]
        {
            Result<int>.Success(1),
            Result<int>.WithFailure("e1"),
            Result<int>.Success(2) // should not be observed
        };
        var seq = IndQuestResults.Collections.ResultCollections.SequenceFailFast(items);
        seq.IsFailure.ShouldBeTrue();
        seq.Errors.ShouldContain("e1");
    }

    private sealed class CustomEnumerable : IEnumerable<Result<int>>
    {
        public IEnumerator<Result<int>> GetEnumerator()
        {
            yield return Result<int>.Success(1);
            yield return null!; // library skips null items
            yield return Result<int>.WithFailure("err");
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void Partition_CustomEnumerable_HandlesNullItems()
    {
        var (successes, failures) = IndQuestResults.Collections.ResultCollections.Partition(new CustomEnumerable());
        successes.Single().ShouldBe(1);
        failures.Single().ShouldBe("err");
    }
}

