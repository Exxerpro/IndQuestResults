namespace IndQuestResults.Tests.Unit.Operations;

public class ResultLinqExtensionsTests2
{
    [Fact]
    public void Linq_Select_Maps_Value()
    {
        var r = Result<string>.Success("ab");
        var mapped = from s in r select s.Length;
        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe(2);
    }

    [Fact]
    public void Linq_SelectMany_Binds()
    {
        var r1 = Result<int>.Success(2);
        var r2 = Result<int>.Success(3);
        var bound = from a in r1 from b in r2 select a + b;
        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBe(5);
    }

    [Fact]
    public void Linq_SelectMany_TwoSources_Binds()
    {
        var r1 = Result<int>.Success(2);
        var r2 = Result<int>.Success(5);
        var bound = r1.SelectMany(r2, (a, b) => Result<int>.Success(a * b));
        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBe(10);
    }

    [Fact]
    public void Linq_Where_Filters_WithFailure_OnFalse()
    {
        var r = Result<int>.Success(1);
        var filtered = r.Where(x => x > 10);
        filtered.IsFailure.ShouldBeTrue();
        filtered.Errors.ShouldNotBeEmpty();
    }
}

