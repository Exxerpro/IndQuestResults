namespace IndQuestResults.Tests.Unit.Operations;

public class ResultFailureAndCombineTests
{
    [Fact]
    public void WithFailure_SingleString_CreatesFailureWithThatError()
    {
        var r = Result.WithFailure("oops");

        r.IsFailure.ShouldBeTrue();
        r.Error.ShouldBe("oops");
        r.ToString().ShouldBe("WithFailure: oops");
    }

    [Fact]
    public void WithFailure_EmptyCollection_UsesDefaultErrorMessage()
    {
        var r1 = Result.WithFailure(Array.Empty<string>());
        r1.IsFailure.ShouldBeTrue();
        r1.Error.ShouldBe(ResultConstants.DefaultErrorMessage);

        var r2 = Result.WithFailure((IEnumerable<string>)new List<string>());
        r2.IsFailure.ShouldBeTrue();
        r2.Error.ShouldBe(ResultConstants.DefaultErrorMessage);
    }

    [Fact]
    public void OnFailure_InvokesHandler_WithErrors()
    {
        var captured = new List<string>();
        var r = Result.WithFailure(new[] { "a", "b" });

        r.OnFailure(errors => captured.AddRange(errors));

        captured.ShouldBe(new[] { "a", "b" });
    }

    [Fact]
    public void OnFailure_DoesNotInvoke_ForSuccess()
    {
        var invoked = false;
        var r = Result.Success();

        r.OnFailure(_ => invoked = true);

        invoked.ShouldBeFalse();
    }

    [Fact]
    public void Combine_MergesErrors_FromCurrentAndParams()
    {
        var a = Result.WithFailure("A1");
        var b = Result.WithFailure("B1");
        var c = Result.Success();

        var combined = a.Combine(b, c);

        combined.IsFailure.ShouldBeTrue();
        combined.Errors.ShouldBe(new[] { "A1", "B1" });
    }

    [Fact]
    public void Combine_AllSuccess_ReturnsSuccess()
    {
        var a = Result.Success();
        var b = Result.Success();

        var combined = a.Combine(b);

        combined.IsSuccess.ShouldBeTrue();
    }
}


