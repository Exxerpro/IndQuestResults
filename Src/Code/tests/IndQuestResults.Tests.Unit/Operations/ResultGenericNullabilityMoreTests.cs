using IndQuestResults;

namespace IndQuestResults.Tests.Unit.Operations;

public class ResultGenericNullabilityMoreTests
{
    [Fact]
    public void Map_NonNullable_WithDefaultValue_Maps()
    {
        Result<int> r = new Result<int>(isSuccess: true, errors: [], value: 0);
        var mapped = r.Map(i => i + 1);
        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe(1);
    }

    [Fact]
    public void Bind_NonNullable_WithDefaultValue_Binds()
    {
        Result<int> r = new Result<int>(isSuccess: true, errors: [], value: 0);
        var bound = r.Bind(i => Result<string>.Success((i + 1).ToString()));
        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBe("1");
    }
}


