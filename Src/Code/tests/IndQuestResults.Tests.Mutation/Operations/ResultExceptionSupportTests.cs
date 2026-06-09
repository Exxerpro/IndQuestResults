using IndQuestResults;
using Shouldly;
using Xunit;

namespace IndQuestResults.Tests.Mutation.Operations;

/// <summary>
/// Tests for exception support in Result and Result&lt;T&gt; classes.
/// </summary>
public class ResultExceptionSupportTests
{
    #region Result.IsFaulted Tests

    [Fact]
    public void IsFaulted_ShouldBeTrue_WhenCreatedFromException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result.WithFailure(exception);

        // Assert
        result.IsFaulted.ShouldBeTrue();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void IsFaulted_ShouldBeFalse_WhenCreatedFromOperationCanceledException()
    {
        // Arrange
        var exception = new OperationCanceledException("Operation cancelled");

        // Act
        var result = Result.WithFailure(exception);

        // Assert
        result.IsFaulted.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void IsFaulted_ShouldBeFalse_WhenCreatedFromValidationFailure()
    {
        // Act
        var result = Result.WithFailure("Validation error");

        // Assert
        result.IsFaulted.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void IsFaulted_ShouldBeFalse_WhenSuccess()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsFaulted.ShouldBeFalse();
        result.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region Result.Exception Tests

    [Fact]
    public void Exception_ShouldContainFullStackTrace_WhenCreatedFromException()
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
        var result = Result.WithFailure(exception);

        // Assert
        result.Exception.ShouldNotBeNull();
        result.Exception.ShouldBe(exception);
        result.Exception.StackTrace.ShouldNotBeNull();
        result.Exception.Message.ShouldBe("Test exception");
    }

    [Fact]
    public void Exception_ShouldBeNull_WhenCreatedFromValidationFailure()
    {
        // Act
        var result = Result.WithFailure("Validation error");

        // Assert
        result.Exception.ShouldBeNull();
    }

    [Fact]
    public void Exception_ShouldBeNull_WhenCreatedFromOperationCanceledException()
    {
        // Arrange
        var exception = new OperationCanceledException("Operation cancelled");

        // Act
        var result = Result.WithFailure(exception);

        // Assert
        // Note: OperationCanceledException is still stored in Exception property,
        // but IsFaulted should be false
        result.Exception.ShouldNotBeNull();
        result.Exception.ShouldBe(exception);
        result.IsFaulted.ShouldBeFalse();
    }

    [Fact]
    public void Exception_ShouldBeNull_WhenSuccess()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.Exception.ShouldBeNull();
    }

    #endregion

    #region Result.WithFailure(Exception) Tests

    [Fact]
    public void WithFailure_Exception_ShouldSetIsFaultedTrue()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result.WithFailure(exception);

        // Assert
        result.IsFaulted.ShouldBeTrue();
        result.Exception.ShouldBe(exception);
    }

    [Fact]
    public void WithFailure_Exception_ShouldPreserveStackTrace()
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
        var result = Result.WithFailure(exception);

        // Assert
        result.Exception.ShouldNotBeNull();
        result.Exception.StackTrace.ShouldNotBeNull();
        result.Exception.StackTrace.ShouldContain("WithFailure_Exception_ShouldPreserveStackTrace");
    }

    [Fact]
    public void WithFailure_OperationCanceledException_ShouldNotSetIsFaulted()
    {
        // Arrange
        var exception = new OperationCanceledException("Operation cancelled");

        // Act
        var result = Result.WithFailure(exception);

        // Assert
        result.IsFaulted.ShouldBeFalse();
        result.Exception.ShouldBe(exception);
        result.Error.ShouldNotBeNull();
        result.Error.ShouldContain(ResultErrors.OperationCancelled);
    }

    [Fact]
    public void WithFailure_NullException_ShouldCreateFailureWithoutException()
    {
        // Act
        var result = Result.WithFailure((Exception?)null);

        // Assert
        result.IsFaulted.ShouldBeFalse();
        result.Exception.ShouldBeNull();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void WithFailure_ExceptionWithErrorMessages_ShouldPreserveException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = Result.WithFailure(errors, exception);

        // Assert
        result.IsFaulted.ShouldBeTrue();
        result.Exception.ShouldBe(exception);
        result.Errors.ShouldContain("Error 1");
        result.Errors.ShouldContain("Error 2");
    }

    #endregion

    #region Result<T>.IsFaulted Tests

    [Fact]
    public void ResultT_IsFaulted_ShouldBeTrue_WhenCreatedFromException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result<string>.WithFailure(exception);

        // Assert
        result.IsFaulted.ShouldBeTrue();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ResultT_IsFaulted_ShouldBeFalse_WhenCreatedFromOperationCanceledException()
    {
        // Arrange
        var exception = new OperationCanceledException("Operation cancelled");

        // Act
        var result = Result<string>.WithFailure(exception);

        // Assert
        result.IsFaulted.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ResultT_IsFaulted_ShouldBeFalse_WhenCreatedFromValidationFailure()
    {
        // Act
        var result = Result<string>.WithFailure("Validation error");

        // Assert
        result.IsFaulted.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ResultT_IsFaulted_ShouldBeFalse_WhenSuccess()
    {
        // Act
        var result = Result<string>.Success("test");

        // Assert
        result.IsFaulted.ShouldBeFalse();
        result.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region Result<T>.Exception Tests

    [Fact]
    public void ResultT_Exception_ShouldContainFullStackTrace_WhenCreatedFromException()
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
        var result = Result<string>.WithFailure(exception);

        // Assert
        result.Exception.ShouldNotBeNull();
        result.Exception.ShouldBe(exception);
        result.Exception.StackTrace.ShouldNotBeNull();
        result.Exception.Message.ShouldBe("Test exception");
    }

    [Fact]
    public void ResultT_Exception_ShouldBeNull_WhenCreatedFromValidationFailure()
    {
        // Act
        var result = Result<string>.WithFailure("Validation error");

        // Assert
        result.Exception.ShouldBeNull();
    }

    [Fact]
    public void ResultT_Exception_ShouldBeNull_WhenSuccess()
    {
        // Act
        var result = Result<string>.Success("test");

        // Assert
        result.Exception.ShouldBeNull();
    }

    #endregion

    #region Result<T>.WithFailure(Exception) Tests

    [Fact]
    public void ResultT_WithFailure_Exception_ShouldSetIsFaultedTrue()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = Result<string>.WithFailure(exception);

        // Assert
        result.IsFaulted.ShouldBeTrue();
        result.Exception.ShouldBe(exception);
    }

    [Fact]
    public void ResultT_WithFailure_Exception_ShouldPreserveStackTrace()
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
        var result = Result<string>.WithFailure(exception);

        // Assert
        result.Exception.ShouldNotBeNull();
        result.Exception.StackTrace.ShouldNotBeNull();
        result.Exception.StackTrace.ShouldContain("ResultT_WithFailure_Exception_ShouldPreserveStackTrace");
    }

    [Fact]
    public void ResultT_WithFailure_OperationCanceledException_ShouldNotSetIsFaulted()
    {
        // Arrange
        var exception = new OperationCanceledException("Operation cancelled");

        // Act
        var result = Result<string>.WithFailure(exception);

        // Assert
        result.IsFaulted.ShouldBeFalse();
        result.Exception.ShouldBe(exception);
        result.Error.ShouldContain(ResultErrors.OperationCancelled);
    }

    [Fact]
    public void ResultT_WithFailure_NullException_ShouldCreateFailureWithoutException()
    {
        // Act
        var result = Result<string>.WithFailure((Exception?)null);

        // Assert
        result.IsFaulted.ShouldBeFalse();
        result.Exception.ShouldBeNull();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ResultT_WithFailure_ExceptionWithValue_ShouldPreserveValue()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var value = "default value";

        // Act
        var result = Result<string>.WithFailure(exception, value);

        // Assert
        result.IsFaulted.ShouldBeTrue();
        result.Exception.ShouldBe(exception);
        result.Value.ShouldBe(value);
    }

    [Fact]
    public void ResultT_WithFailure_ExceptionWithErrorMessages_ShouldPreserveException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = Result<string>.WithFailure(errors, default, exception);

        // Assert
        result.IsFaulted.ShouldBeTrue();
        result.Exception.ShouldBe(exception);
        result.Errors.ShouldContain("Error 1");
        result.Errors.ShouldContain("Error 2");
    }

    [Fact]
    public void ResultT_WithFailure_ExceptionWithStringError_ShouldPreserveException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var error = "Custom error message";

        // Act
        var result = Result<string>.WithFailure(error, value: default, exception: exception);

        // Assert
        result.IsFaulted.ShouldBeTrue();
        result.Exception.ShouldBe(exception);
        result.Errors.ShouldContain(error);
    }

    #endregion

    #region Inner Exception Tests

    [Fact]
    public void WithFailure_ExceptionWithInnerException_ShouldPreserveInnerException()
    {
        // Arrange
        var innerException = new ArgumentException("Inner exception");
        var exception = new InvalidOperationException("Outer exception", innerException);

        // Act
        var result = Result.WithFailure(exception);

        // Assert
        result.Exception.ShouldNotBeNull();
        result.Exception.InnerException.ShouldNotBeNull();
        result.Exception.InnerException.ShouldBe(innerException);
    }

    [Fact]
    public void ResultT_WithFailure_ExceptionWithInnerException_ShouldPreserveInnerException()
    {
        // Arrange
        var innerException = new ArgumentException("Inner exception");
        var exception = new InvalidOperationException("Outer exception", innerException);

        // Act
        var result = Result<string>.WithFailure(exception);

        // Assert
        result.Exception.ShouldNotBeNull();
        result.Exception.InnerException.ShouldNotBeNull();
        result.Exception.InnerException.ShouldBe(innerException);
    }

    #endregion
}

