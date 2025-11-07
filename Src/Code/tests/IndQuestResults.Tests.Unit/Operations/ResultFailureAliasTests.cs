using IndQuestResults;
using Shouldly;
using Xunit;

namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Comprehensive tests for Failure alias methods in Result and Result&lt;T&gt; classes.
/// </summary>
public class ResultFailureAliasTests
{
    #region Result.Failure Tests

    [Fact]
    public void Failure_WithIEnumerableErrors_ShouldMatchWithFailure()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var failureResult = Result.Failure(errors);
        var withFailureResult = Result.WithFailure(errors);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.Errors.ShouldBe(errors);
        failureResult.Errors.ShouldBe(withFailureResult.Errors);
    }

    [Fact]
    public void Failure_WithStringArrayErrors_ShouldMatchWithFailure()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var failureResult = Result.Failure(errors);
        var withFailureResult = Result.WithFailure(errors);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.Errors.ShouldBe(errors);
        failureResult.Errors.ShouldBe(withFailureResult.Errors);
    }

    [Fact]
    public void Failure_WithSingleError_ShouldMatchWithFailure()
    {
        // Arrange
        var error = "Single error message";

        // Act
        var failureResult = Result.Failure(error);
        var withFailureResult = Result.WithFailure(error);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.Error.ShouldBe(error);
        failureResult.Error.ShouldBe(withFailureResult.Error);
    }

    [Fact]
    public void Failure_WithException_ShouldMatchWithFailure()
    {
        // Arrange
        InvalidOperationException exception;
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        // Act
        var failureResult = Result.Failure(exception);
        var withFailureResult = Result.WithFailure(exception);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.IsFaulted.ShouldBeTrue();
        failureResult.Exception.ShouldBe(exception);
        failureResult.Exception.ShouldBe(withFailureResult.Exception);
        failureResult.IsFaulted.ShouldBe(withFailureResult.IsFaulted);
    }

    [Fact]
    public void Failure_WithExceptionAndErrors_ShouldMatchWithFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var failureResult = Result.Failure(errors, exception);
        var withFailureResult = Result.WithFailure(errors, exception);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.IsFaulted.ShouldBeTrue();
        failureResult.Exception.ShouldBe(exception);
        failureResult.Errors.ShouldBe(errors);
        failureResult.Exception.ShouldBe(withFailureResult.Exception);
        failureResult.Errors.ShouldBe(withFailureResult.Errors);
    }

    [Fact]
    public void Failure_WithNullErrors_ShouldUseDefaultErrorMessage()
    {
        // Act
        var result = Result.Failure((IEnumerable<string>?)null);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    [Fact]
    public void Failure_WithEmptyErrors_ShouldUseDefaultErrorMessage()
    {
        // Act
        var result = Result.Failure(Array.Empty<string>());

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    [Fact]
    public void Failure_WithOperationCanceledException_ShouldNotSetIsFaulted()
    {
        // Arrange
        var exception = new OperationCanceledException("Operation cancelled");

        // Act
        var result = Result.Failure(exception);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsFaulted.ShouldBeFalse();
        result.Exception.ShouldBe(exception);
        result.Error.ShouldNotBeNull();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain(ResultErrors.OperationCancelled);
    }

    #endregion

    #region Result<T>.Failure Tests

    [Fact]
    public void ResultT_Failure_WithIEnumerableErrors_ShouldMatchWithFailure()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var failureResult = Result<string>.Failure(errors);
        var withFailureResult = Result<string>.WithFailure(errors);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.Errors.ShouldBe(errors);
        failureResult.Errors.ShouldBe(withFailureResult.Errors);
    }

    [Fact]
    public void ResultT_Failure_WithStringArrayErrors_ShouldMatchWithFailure()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var value = "default value";

        // Act
        var failureResult = Result<string>.Failure(errors, value);
        var withFailureResult = Result<string>.WithFailure(errors, value);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.Value.ShouldBe(value);
        failureResult.Errors.ShouldBe(errors);
        failureResult.Value.ShouldBe(withFailureResult.Value);
        failureResult.Errors.ShouldBe(withFailureResult.Errors);
    }

    [Fact]
    public void ResultT_Failure_WithSingleError_ShouldMatchWithFailure()
    {
        // Arrange
        var error = "Single error message";
        var value = "default value";

        // Act
        var failureResult = Result<string>.Failure(error, value);
        var withFailureResult = Result<string>.WithFailure(error, value);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.Error.ShouldBe(error);
        failureResult.Value.ShouldBe(value);
        failureResult.Error.ShouldBe(withFailureResult.Error);
        failureResult.Value.ShouldBe(withFailureResult.Value);
    }

    [Fact]
    public void ResultT_Failure_WithException_ShouldMatchWithFailure()
    {
        // Arrange
        InvalidOperationException exception;
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (InvalidOperationException ex)
        {
            exception = ex;
        }

        // Act
        var failureResult = Result<string>.Failure(exception);
        var withFailureResult = Result<string>.WithFailure(exception);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.IsFaulted.ShouldBeTrue();
        failureResult.Exception.ShouldBe(exception);
        failureResult.Exception.ShouldBe(withFailureResult.Exception);
        failureResult.IsFaulted.ShouldBe(withFailureResult.IsFaulted);
    }

    [Fact]
    public void ResultT_Failure_WithExceptionAndValue_ShouldMatchWithFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var value = "default value";

        // Act
        var failureResult = Result<string>.Failure(exception, value);
        var withFailureResult = Result<string>.WithFailure(exception, value);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.IsFaulted.ShouldBeTrue();
        failureResult.Exception.ShouldBe(exception);
        failureResult.Value.ShouldBe(value);
        failureResult.Exception.ShouldBe(withFailureResult.Exception);
        failureResult.Value.ShouldBe(withFailureResult.Value);
    }

    [Fact]
    public void ResultT_Failure_WithValueAndErrors_ShouldMatchWithFailure()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var value = "default value";

        // Act
        var failureResult = Result<string>.Failure(value, errors);
        var withFailureResult = Result<string>.WithFailure(value, errors);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.Value.ShouldBe(value);
        failureResult.Errors.ShouldBe(errors);
        failureResult.Value.ShouldBe(withFailureResult.Value);
        failureResult.Errors.ShouldBe(withFailureResult.Errors);
    }

    [Fact]
    public void ResultT_Failure_WithAllParameters_ShouldMatchWithFailure()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var errors = new[] { "Error 1", "Error 2" };
        var value = "default value";

        // Act
        var failureResult = Result<string>.Failure(errors, value, exception);
        var withFailureResult = Result<string>.WithFailure(errors, value, exception);

        // Assert
        failureResult.IsFailure.ShouldBeTrue();
        failureResult.IsFaulted.ShouldBeTrue();
        failureResult.Exception.ShouldBe(exception);
        failureResult.Value.ShouldBe(value);
        failureResult.Errors.ShouldBe(errors);
        failureResult.Exception.ShouldBe(withFailureResult.Exception);
        failureResult.Value.ShouldBe(withFailureResult.Value);
        failureResult.Errors.ShouldBe(withFailureResult.Errors);
    }

    [Fact]
    public void ResultT_Failure_WithNullErrors_ShouldUseDefaultErrorMessage()
    {
        // Act
        var result = Result<string>.Failure((IEnumerable<string>?)null);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    [Fact]
    public void ResultT_Failure_WithEmptyErrors_ShouldUseDefaultErrorMessage()
    {
        // Act
        var result = Result<string>.Failure(Array.Empty<string>());

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    [Fact]
    public void ResultT_Failure_WithOperationCanceledException_ShouldNotSetIsFaulted()
    {
        // Arrange
        var exception = new OperationCanceledException("Operation cancelled");

        // Act
        var result = Result<string>.Failure(exception);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.IsFaulted.ShouldBeFalse();
        result.Exception.ShouldBe(exception);
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain(ResultErrors.OperationCancelled);
    }

    #endregion
}

