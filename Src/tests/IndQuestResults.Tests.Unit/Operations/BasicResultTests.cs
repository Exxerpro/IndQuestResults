namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Tests for basic Result functionality including constructors, properties, and simple operations.
/// This class focuses on the fundamental behavior of Result classes.
/// </summary>
public class BasicResultTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance_WithDefaultValues()
    {
        // Arrange & Act
        var instance = new Result();

        // Assert
        instance.ShouldNotBeNull();
        instance.IsSuccess.ShouldBeFalse();
        instance.IsFailure.ShouldBeTrue();
        instance.Errors.ShouldNotBeNull();
        instance.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void WithFailure_ShouldCreateFailedResult_WithSingleError()
    {
        // Arrange
        var errorMessage = "Test error occurred";

        // Act
        var result = Result.WithFailure(errorMessage);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(errorMessage);
        result.Errors.Count().ShouldBe(1);
    }

    [Fact]
    public void WithFailure_ShouldCreateFailedResult_WithMultipleErrors()
    {
        // Arrange
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = Result.WithFailure(errors);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain("Error 1");
        result.Errors.ShouldContain("Error 2");
        result.Errors.ShouldContain("Error 3");
        result.Errors.Count().ShouldBe(3);
    }

    [Fact]
    public void GenericSuccess_ShouldCreateSuccessfulResult_WithValue()
    {
        // Arrange
        var testValue = "test data";

        // Act
        var result = Result<string>.Success(testValue);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value!.ShouldBe(testValue);
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void GenericWithFailure_ShouldCreateFailedResult_WithErrorsAndValue()
    {
        // Arrange
        var errorMessage = "Operation failed";
        var fallbackValue = "fallback";

        // Act
        var result = Result<string>.WithFailure(errorMessage, fallbackValue);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Value!.ShouldBe(fallbackValue);
        result.Errors.ShouldContain(errorMessage);
        result.Errors.Count().ShouldBe(1);
    }

    [Fact]
    public void WithWarnings_ShouldCreateSuccessfulResult_WithWarnings()
    {
        // Arrange
        var warnings = new List<string> { "Warning 1", "Warning 2" };
        var value = "test-value";

        // Act
        var result = Result<string>.WithWarnings(warnings, value);

        // Assert - Warnings are successful operations with diagnostic messages
        result.IsSuccess.ShouldBeTrue(); // Fixed: Warnings should be successful
        result.HasWarnings.ShouldBeTrue();
        result.IsRecoverable.ShouldBeTrue();
        result.Value!.ShouldBe(value);
        result.Errors.ShouldContain("Warning 1");
        result.Errors.ShouldContain("Warning 2");
    }

    [Fact]
    public void Error_Property_ShouldReturnFirstNonEmptyError()
    {
        // Arrange
        var errors = new List<string> { "", "First error", "Second error" };
        var result = Result.WithFailure(errors);

        // Act
        var firstError = result.Error;

        // Assert
        firstError.ShouldBe("First error");
    }

    [Fact]
    public void Error_Property_ShouldReturnNull_WhenNoValidErrors()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var error = result.Error;

        // Assert
        error.ShouldBeNull();
    }

    [Fact]
    public void ImplicitConversion_ShouldConvertValueToSuccessfulResult()
    {
        // Arrange & Act
        Result<string> result = "test value";

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.ShouldBe("test value");
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void ImplicitConversion_ShouldConvertGenericToNonGeneric()
    {
        // Arrange
        var genericResult = Result<string>.Success("test");

        // Act
        Result nonGenericResult = genericResult;

        // Assert
        nonGenericResult.IsSuccess.ShouldBeTrue();
        nonGenericResult.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Deconstruction_ShouldProvideAllComponents()
    {
        // Arrange
        var result = Result<string>.Success("test value");

        // Act
        var (succeeded, data, errors) = result;

        // Assert
        succeeded.ShouldBeTrue();
        data.ShouldBe("test value");
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void NullErrorHandling_ShouldUseDefaultMessage()
    {
        // Act
        var result = Result<string>.WithFailure((IEnumerable<string>?)null);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeEmpty();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }
}