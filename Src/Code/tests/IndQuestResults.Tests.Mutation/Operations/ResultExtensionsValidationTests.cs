namespace IndQuestResults.Tests.Mutation.Operations;

public class ResultExtensionsValidationTests
{
    // EnsureNotNull (reference type)
    [Fact]
    public void EnsureNotNull_RefType_NullValue_ReturnsFailure()
    {
        string? value = null;
        var res = ResultExtensions.EnsureNotNull(value, "name");
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("name");
    }

    // FailForNullArguments<T>
    [Fact]
    public void FailForNullArguments_Generic_NullArray_ReturnsGuardMessage()
    {
        var res = ResultExtensions.FailForNullArguments<int>(null!);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Parameter names cannot be null, empty, or contain empty values");
    }

    [Fact]
    public void FailForNullArguments_Generic_EmptyArray_ReturnsGuardMessage()
    {
        var res = ResultExtensions.FailForNullArguments<int>([]);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Parameter names cannot be null, empty, or contain empty values");
    }

    [Fact]
    public void FailForNullArguments_Generic_ContainsEmpty_ReturnsGuardMessage()
    {
        var res = ResultExtensions.FailForNullArguments<int>("a", "");
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Parameter names cannot be null, empty, or contain empty values");
    }

    [Fact]
    public void FailForNullArguments_Generic_ValidNames_ContainsAllNamesInMessage()
    {
        var res = ResultExtensions.FailForNullArguments<int>("a", "b");
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("'a'");
        res.Error.ShouldContain("'b'");
    }

    // FailForNullArguments (non-generic)
    [Fact]
    public void FailForNullArguments_NonGeneric_NullArray_ReturnsGuardMessage()
    {
        var res = ResultExtensions.FailForNullArguments(null!);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Parameter names cannot be null, empty, or contain empty values");
    }

    [Fact]
    public void FailForNullArguments_NonGeneric_EmptyArray_ReturnsGuardMessage()
    {
        var res = ResultExtensions.FailForNullArguments([]);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Parameter names cannot be null, empty, or contain empty values");
    }

    [Fact]
    public void FailForNullArguments_NonGeneric_ContainsEmpty_ReturnsGuardMessage()
    {
        var res = ResultExtensions.FailForNullArguments("x", "");
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Parameter names cannot be null, empty, or contain empty values");
    }

    [Fact]
    public void FailForNullArguments_NonGeneric_ValidNames_ContainsAllNamesInMessage()
    {
        var res = ResultExtensions.FailForNullArguments("p", "q");
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("'p'");
        res.Error.ShouldContain("'q'");
    }

    [Fact]
    public void EnsureNotNull_RefType_ValidValue_ReturnsSuccess()
    {
        var value = "ok";
        var res = ResultExtensions.EnsureNotNull(value, "name");
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe("ok");
    }

    [Fact]
    public void EnsureNotNull_RefType_EmptyParameterName_ReturnsFailure()
    {
        var res = ResultExtensions.EnsureNotNull("v", "");
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Parameter name cannot be null or empty");
    }

    // EnsureNotNull (nullable struct)
    [Fact]
    public void EnsureNotNull_Struct_NullValue_ReturnsFailure()
    {
        int? value = null;
        var res = ResultExtensions.EnsureNotNull(value, "count");
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("count");
    }

    [Fact]
    public void EnsureNotNull_Struct_HasValue_ReturnsSuccess()
    {
        int? value = 5;
        var res = ResultExtensions.EnsureNotNull(value, "count");
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(5);
    }

    [Fact]
    public void EnsureNotNull_Struct_EmptyParameterName_ReturnsFailure()
    {
        int? value = 1;
        var res = ResultExtensions.EnsureNotNull(value, "");
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Parameter name cannot be null or empty");
    }

    // ValidateNotNull
    [Fact]
    public void ValidateNotNull_NullArray_ReturnsFailure()
    {
        var res = ResultExtensions.ValidateNotNull(null!);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Validations cannot be null or empty");
    }

    [Fact]
    public void ValidateNotNull_EmptyArray_ReturnsFailure()
    {
        var res = ResultExtensions.ValidateNotNull([]);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Validations cannot be null or empty");
    }

    [Fact]
    public void ValidateNotNull_AllValid_ReturnsSuccess()
    {
        var res = ResultExtensions.ValidateNotNull((new object(), "a"), (1, "b"));
        res.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ValidateNotNull_SingleNullParam_ReturnsFailureWithName()
    {
        var res = ResultExtensions.ValidateNotNull((null, "missing"));
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("missing");
    }

    [Fact]
    public void ValidateNotNull_MultipleNullParams_ReturnsFailureWithAllNames()
    {
        var res = ResultExtensions.ValidateNotNull((null, "x"), (null, "y"));
        res.IsFailure.ShouldBeTrue();
        // Combined message should contain each parameter name
        res.Errors.First().ShouldContain("x");
        res.Errors.First().ShouldContain("y");
    }

    // CreateIfValid
    [Fact]
    public void CreateIfValid_NullFactory_ReturnsFailure()
    {
        var res = ResultExtensions.CreateIfValid<string>(null!, (new object(), "a"));
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Factory function cannot be null");
    }

    [Fact]
    public void CreateIfValid_NullValidations_ReturnsFailure()
    {
        var res = ResultExtensions.CreateIfValid(() => 42, null!);
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("Validations cannot be null");
    }

    [Fact]
    public void CreateIfValid_AllValid_ReturnsFactoryValue()
    {
        var res = ResultExtensions.CreateIfValid(() => 7, (new object(), "ok"));
        res.IsSuccess.ShouldBeTrue();
        res.Value.ShouldBe(7);
    }

    [Fact]
    public void CreateIfValid_SingleNullParam_ReturnsFailureWithName()
    {
        var res = ResultExtensions.CreateIfValid(() => "v", ((object?)null, "p"));
        res.IsFailure.ShouldBeTrue();
        res.Error.ShouldNotBeNull();
        res.Error.ShouldContain("p");
    }

    [Fact]
    public void CreateIfValid_MultipleNullParams_ReturnsFailureWithAllNames()
    {
        var res = ResultExtensions.CreateIfValid(() => 1, ((object?)null, "a"), ((object?)null, "b"));
        res.IsFailure.ShouldBeTrue();
        res.Errors.First().ShouldContain("a");
        res.Errors.First().ShouldContain("b");
    }
}
