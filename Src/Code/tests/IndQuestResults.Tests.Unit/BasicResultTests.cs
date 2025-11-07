namespace IndQuestResults.Tests.Unit;

/// <summary>
/// Tests for basic Result functionality including constructors, properties, and simple operations.
/// This class focuses on the fundamental behavior of Result classes.
/// </summary>
public class BasicResultTests
{
    /// <summary>
    /// Verifies that the default constructor produces a failed result with empty errors.
    /// </summary>
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

    /// <summary>
    /// Ensures that calling <see cref="Result.Success()"/> creates a successful non-generic result.
    /// </summary>
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

    /// <summary>
    /// Ensures that <see cref="Result.WithFailure(string, Exception?)"/> creates a failed result containing a single error.
    /// </summary>
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

    /// <summary>
    /// Ensures that <see cref="Result.WithFailure(System.Collections.Generic.IEnumerable{string}, Exception?)"/> aggregates multiple errors.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="Result{T}.Success(T)"/> creates a successful result with the provided value.
    /// </summary>
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

    /// <summary>
    /// Verifies that WithFailure creates a failed result and preserves the value.
    /// </summary>
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

    /// <summary>
    /// Ensures that warnings create a successful result and are exposed via the errors collection.
    /// </summary>
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

    /// <summary>
    /// Returns the first non-empty error from the errors collection.
    /// </summary>
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

    /// <summary>
    /// Returns <c>null</c> when there are no valid errors present.
    /// </summary>
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

    /// <summary>
    /// Verifies implicit conversion from value to <see cref="Result{T}"/> produces a successful result.
    /// </summary>
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

    /// <summary>
    /// Verifies implicit conversion from <see cref="Result{T}"/> to non-generic <see cref="Result"/> preserves success state.
    /// </summary>
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

    /// <summary>
    /// Ensures that deconstruction exposes success flag, value, and errors.
    /// </summary>
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

    /// <summary>
    /// Ensures a default error message is used when a null set of errors is provided.
    /// </summary>
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


