namespace IndQuestResults.Tests.Mutation.Operations;

public class ResultLinqExtensionsTests
{
    [Fact]
    public void Linq_Query_Composes_Map_Bind_Where()
    {
        var query =
            from a in Result<int>.Success(2)
            from b in Result<int>.Success(5)
            where a + b == 7
            select (a, b, sum: a + b);

        query.IsSuccess.ShouldBeTrue();
        query.Value.sum.ShouldBe(7);
    }

    [Fact]
    public void Linq_Query_Where_Fails_WhenPredicateFalse()
    {
        var query =
            from s in Result<string>.Success("abc")
            where s.Length > 5
            select s;

        query.IsFailure.ShouldBeTrue();
        query.Error.ShouldNotBeNull();
        query.Error.ShouldContain("Where predicate returned false");
    }
}
