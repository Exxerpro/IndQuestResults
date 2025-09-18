namespace IndQuestResults.Tests.Unit.Validation;

public class ValidationErrorTypesTests
{
    [Fact]
    public void NullArgumentError_ShouldSetProperties_AndFormatMessage()
    {
        var e = new IndQuestResults.Validation.NullArgumentError("param");
        e.ParameterName.ShouldBe("param");
        e.Message.ShouldBe("Parameter 'param' cannot be null.");
        e.ToString().ShouldBe(e.Message);
    }

    [Fact]
    public void NullArgumentError_ShouldRespectCustomMessage()
    {
        var e = new IndQuestResults.Validation.NullArgumentError("x", "Custom");
        e.Message.ShouldBe("Custom");
    }

    [Fact]
    public void NullArgumentError_ShouldThrow_WhenParameterNameNull()
    {
        Should.Throw<ArgumentNullException>(() => new IndQuestResults.Validation.NullArgumentError(null!));
    }

    [Fact]
    public void MultipleNullArgumentsError_ShouldRenderSinglePair()
    {
        var e = new IndQuestResults.Validation.MultipleNullArgumentsError("p1");
        e.ParameterNames.Length.ShouldBe(1);
        e.ParameterNames[0].ShouldBe("p1");
        e.ToString().ShouldBe("Parameter 'p1' cannot be null.");
    }

    [Fact]
    public void MultipleNullArgumentsError_ShouldRenderTwo()
    {
        var e = new IndQuestResults.Validation.MultipleNullArgumentsError("p1", "p2");
        e.ParameterNames.Length.ShouldBe(2);
        e.ParameterNames[0].ShouldBe("p1");
        e.ParameterNames[1].ShouldBe("p2");
        e.ToString().ShouldBe("Parameters 'p1' and 'p2' cannot be null.");
    }

    [Fact]
    public void MultipleNullArgumentsError_ShouldRenderMany()
    {
        var e = new IndQuestResults.Validation.MultipleNullArgumentsError("p1", "p2", "p3");
        e.ToString().ShouldBe("Parameters 'p1', 'p2' and 'p3' cannot be null.");
    }

    [Fact]
    public void MultipleNullArgumentsError_ShouldFilterWhitespaceAndThrowWhenEmpty()
    {
        Should.Throw<ArgumentException>(() => new IndQuestResults.Validation.MultipleNullArgumentsError());
        Should.Throw<ArgumentException>(() => new IndQuestResults.Validation.MultipleNullArgumentsError("  "));
    }

    [Fact]
    public void ErrorTypes_EqualityAndHashCode()
    {
        var a1 = new IndQuestResults.Validation.NullArgumentError("p");
        var a2 = new IndQuestResults.Validation.NullArgumentError("p");
        a1.ShouldBe(a2);
        a1.GetHashCode().ShouldBe(a2.GetHashCode());

        var m1 = new IndQuestResults.Validation.MultipleNullArgumentsError("a", "b");
        var m2 = new IndQuestResults.Validation.MultipleNullArgumentsError("a", "b");
        m1.ShouldBe(m2);
        m1.GetHashCode().ShouldBe(m2.GetHashCode());
    }
}
