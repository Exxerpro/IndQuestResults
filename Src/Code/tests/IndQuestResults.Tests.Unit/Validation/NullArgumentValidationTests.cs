namespace IndQuestResults.Tests.Unit.Validation;

public class NullArgumentValidationTests
{
    [Fact]
    public void ValidateSingle_ClassType_ShouldBeValid_WhenNotNull()
    {
        var r = IndQuestResults.Validation.NullArgumentValidation.ValidateSingle("value", "value");
        r.IsValid.ShouldBeTrue();
        r.IsInvalid.ShouldBeFalse();
        r.ErrorMessage.ShouldBeNull();
        r.ToString().ShouldBe("Valid");
    }

    [Fact]
    public void ValidateSingle_ClassType_ShouldBeInvalid_WhenNull()
    {
        string? value = null;
        var r = IndQuestResults.Validation.NullArgumentValidation.ValidateSingle(value, "value");
        r.IsValid.ShouldBeFalse();
        r.IsInvalid.ShouldBeTrue();
        r.ErrorMessage.ShouldNotBeNull();
        r.ToString().ShouldStartWith("Invalid:");
        r.ErrorMessage!.ShouldContain("value");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ValidateSingle_ClassType_ShouldBeInvalid_WhenParameterNameEmpty(string? param)
    {
        var r = IndQuestResults.Validation.NullArgumentValidation.ValidateSingle("x", param!);
        r.IsValid.ShouldBeFalse();
        r.ErrorMessage.ShouldBe("Parameter name cannot be null or empty.");
    }

    [Fact]
    public void ValidateSingle_NullableStruct_ShouldBeValid_WhenHasValue()
    {
        int? n = 1;
        var r = IndQuestResults.Validation.NullArgumentValidation.ValidateSingle(n, nameof(n));
        r.IsValid.ShouldBeTrue();
        r.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void ValidateSingle_NullableStruct_ShouldBeInvalid_WhenNull()
    {
        int? n = null;
        var r = IndQuestResults.Validation.NullArgumentValidation.ValidateSingle(n, nameof(n));
        r.IsValid.ShouldBeFalse();
        r.ErrorMessage.ShouldNotBeNull();
        r.ErrorMessage!.ShouldContain("n");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ValidateSingle_NullableStruct_ShouldBeInvalid_WhenParameterNameEmpty(string? param)
    {
        int? n = 5;
        var r = IndQuestResults.Validation.NullArgumentValidation.ValidateSingle(n, param!);
        r.IsValid.ShouldBeFalse();
        r.ErrorMessage.ShouldBe("Parameter name cannot be null or empty.");
    }

    [Fact]
    public void ValidateMultiple_ShouldBeValid_WhenAllNonNull()
    {
        var r = IndQuestResults.Validation.NullArgumentValidation.ValidateMultiple(("a", "p1"), (1, "p2"));
        r.IsValid.ShouldBeTrue();
        r.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void ValidateMultiple_ShouldBeInvalid_WhenNoInputs()
    {
        var r = IndQuestResults.Validation.NullArgumentValidation.ValidateMultiple();
        r.IsValid.ShouldBeFalse();
        r.ErrorMessage.ShouldBe("Validations cannot be null or empty.");
    }

    [Fact]
    public void ValidateMultiple_ShouldReturnSingleNullArgumentError_WhenOneNull()
    {
        var r = IndQuestResults.Validation.NullArgumentValidation.ValidateMultiple((null, "a"), ("b", "b"));
        r.IsValid.ShouldBeFalse();
        r.ErrorMessage.ShouldBe("Parameter 'a' cannot be null.");
    }

    [Fact]
    public void ValidateMultiple_ShouldReturnMultipleNullArgumentsError_WhenManyNull()
    {
        var r = IndQuestResults.Validation.NullArgumentValidation.ValidateMultiple((null, "a"), (null, "b"), (null, "c"));
        r.IsValid.ShouldBeFalse();
        r.ErrorMessage.ShouldBe("Parameters 'a', 'b' and 'c' cannot be null.");
    }
}



