using IndQuestResults;
using IndQuestResults.Operations;

namespace IndQuestResults.Tests.Unit.Operations;

public class ResultTryExtensionsTests
{
    [Fact]
    public void Try_CatchesException_ToFailure()
    {
        var r = ResultTryExtensions.Try<object>(() => throw new InvalidOperationException("bad"), ex => $"X:{ex.GetType().Name}");
        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe("X:InvalidOperationException");
    }

    [Fact]
    public void MapTry_CatchesException_ToFailure()
    {
        var ok = Result<string>.Success("abc");
        var mapped = ok.MapTry<string, int>(_ => int.Parse("NaN"), ex => "parse-failed");
        mapped.IsFailure.ShouldBeTrue();
        mapped.Error.ShouldBe("parse-failed");
    }

    [Fact]
    public void BindTry_CatchesException_ToFailure()
    {
        var ok = Result<string>.Success("abc");
        var bound = ok.BindTry<string, object>(_ => throw new Exception("boom"), ex => "bound-failed");
        bound.IsFailure.ShouldBeTrue();
        bound.Error.ShouldBe("bound-failed");
    }
}
