namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Unit tests for the generic Result&lt;T&gt; class.
/// Tests cover basic functionality, edge cases, type safety, and performance scenarios.
/// </summary>
public class ResultGenericTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResultWithValue()
    {
        // Arrange
        const string value = "test value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsSuccessMayBeNull);
        Assert.True(result.IsSuccessNotNull);
        Assert.False(result.IsSuccessValueNull);
        Assert.False(result.IsFailure);
        Assert.False(result.HasErrors);
        Assert.False(result.HasWarnings);
        Assert.True(result.IsRecoverable);
        Assert.Equal(value, result.Value);
        Assert.Empty(result.Errors);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Success_WithNullValue_ShouldCreateSuccessfulResultWithNullValue()
    {
        // Act
        var result = Result<string>.Success(null);

        // Assert
        Assert.False(result.IsSuccess); // IsSuccess checks for non-null value
        Assert.True(result.IsSuccessMayBeNull); // IsSuccessMayBeNull allows null
        Assert.False(result.IsSuccessNotNull);
        Assert.True(result.IsSuccessValueNull);
        Assert.False(result.IsFailure);
        Assert.False(result.HasErrors);
        Assert.False(result.HasWarnings);
        Assert.True(result.IsRecoverable);
        Assert.Null(result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void WithFailure_SingleError_ShouldCreateFailedResult()
    {
        // Arrange
        const string errorMessage = "Something went wrong";
        const string value = "test value";

        // Act
        var result = Result<string>.WithFailure(errorMessage, value);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.False(result.IsSuccessMayBeNull);
        Assert.False(result.IsSuccessNotNull);
        Assert.False(result.IsSuccessValueNull);
        Assert.True(result.IsFailure);
        Assert.True(result.HasErrors);
        Assert.False(result.HasWarnings);
        Assert.False(result.IsRecoverable);
        Assert.Equal(value, result.Value);
        Assert.Single(result.Errors);
        Assert.Equal(errorMessage, result.Error);
    }

    [Fact]
    public void WithFailure_MultipleErrors_ShouldCreateFailedResultWithAllErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = Result<int>.WithFailure(errors);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.True(result.HasErrors);
        Assert.False(result.HasWarnings);
        Assert.Equal(3, result.Errors.Count());
        Assert.Equal("Error 1", result.Error);
        Assert.Contains("Error 2", result.Errors);
        Assert.Contains("Error 3", result.Errors);
    }

    [Fact]
    public void WithWarnings_ShouldCreateSuccessfulResultWithWarnings()
    {
        // Arrange
        var warnings = new[] { "Warning 1", "Warning 2" };
        const string value = "test value";

        // Act
        var result = Result<string>.WithWarnings(warnings, value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsSuccessMayBeNull);
        Assert.True(result.IsSuccessNotNull);
        Assert.False(result.IsSuccessValueNull);
        Assert.False(result.IsFailure);
        Assert.True(result.HasErrors); // HasErrors includes warnings
        Assert.True(result.HasWarnings);
        Assert.True(result.IsRecoverable);
        Assert.Equal(value, result.Value);
        Assert.Equal(2, result.Errors.Count());
        Assert.Contains("Warning 1", result.Errors);
        Assert.Contains("Warning 2", result.Errors);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccessfulResult()
    {
        // Act
        Result<int> result = 42;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ImplicitConversion_ToNonGenericResult_WhenSuccessful_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var genericResult = Result<string>.Success("test");

        // Act
        Result nonGenericResult = genericResult;

        // Assert
        Assert.True(nonGenericResult.IsSuccess);
        Assert.Empty(nonGenericResult.Errors);
    }

    [Fact]
    public void ImplicitConversion_ToNonGenericResult_WhenFailed_ShouldPropagateErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var genericResult = Result<string>.WithFailure(errors);

        // Act
        Result nonGenericResult = genericResult;

        // Assert
        Assert.False(nonGenericResult.IsSuccess);
        Assert.Equal(errors, nonGenericResult.Errors);
    }

    [Fact]
    public void Deconstruct_ShouldProvideSuccessStateValueAndErrors()
    {
        // Arrange
        var result = Result<string>.Success("test value");

        // Act
        var (succeeded, data, errors) = result;

        // Assert
        Assert.True(succeeded);
        Assert.Equal("test value", data);
        Assert.Empty(errors);
    }

    [Fact]
    public void OnSuccess_WhenSuccessful_ShouldExecuteActionWithValue()
    {
        // Arrange
        var result = Result<string>.Success("test value");
        string capturedValue = null;

        // Act
        var returnedResult = result.OnSuccess(value => capturedValue = value);

        // Assert
        Assert.Equal("test value", capturedValue);
        Assert.Same(result, returnedResult);
    }

    [Fact]
    public void OnSuccess_WhenFailed_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result<string>.WithFailure("Error");
        var executed = false;

        // Act
        var returnedResult = result.OnSuccess(_ => executed = true);

        // Assert
        Assert.False(executed);
        Assert.Same(result, returnedResult);
    }

    [Fact]
    public void Map_WhenSuccessful_ShouldTransformValue()
    {
        // Arrange
        var result = Result<string>.Success("hello");

        // Act
        var mappedResult = result.Map(s => s.Length);

        // Assert
        Assert.True(mappedResult.IsSuccess);
        Assert.Equal(5, mappedResult.Value);
    }

    [Fact]
    public void Map_WhenFailed_ShouldPropagateErrorsAndNotTransform()
    {
        // Arrange
        var errors = new[] { "Error 1" };
        var result = Result<string>.WithFailure(errors);

        // Act
        var mappedResult = result.Map(s => s.Length);

        // Assert
        Assert.False(mappedResult.IsSuccess);
        Assert.Equal(errors, mappedResult.Errors);
        Assert.Equal(0, mappedResult.Value); // Default value for int
    }

    [Fact]
    public void Bind_WhenSuccessful_ShouldChainOperations()
    {
        // Arrange
        var result = Result<string>.Success("42");

        // Act
        var boundResult = result.Bind(s => 
            int.TryParse(s, out var number) 
                ? Result<int>.Success(number) 
                : Result<int>.WithFailure("Parse failed"));

        // Assert
        Assert.True(boundResult.IsSuccess);
        Assert.Equal(42, boundResult.Value);
    }

    [Fact]
    public void Bind_WhenFailed_ShouldPropagateErrorsAndNotChain()
    {
        // Arrange
        var errors = new[] { "Error 1" };
        var result = Result<string>.WithFailure(errors);

        // Act
        var boundResult = result.Bind(s => Result<int>.Success(42));

        // Assert
        Assert.False(boundResult.IsSuccess);
        Assert.Equal(errors, boundResult.Errors);
    }

    [Fact]
    public void Ensure_WhenSuccessfulAndConditionTrue_ShouldReturnSameResult()
    {
        // Arrange
        var result = Result<string>.Success("hello");

        // Act
        var ensuredResult = result.Ensure(s => s.Length > 0, "String cannot be empty");

        // Assert
        Assert.Same(result, ensuredResult);
        Assert.True(ensuredResult.IsSuccess);
    }

    [Fact]
    public void Ensure_WhenSuccessfulAndConditionFalse_ShouldReturnFailure()
    {
        // Arrange
        var result = Result<string>.Success("hello");

        // Act
        var ensuredResult = result.Ensure(s => s.Length > 10, "String too short");

        // Assert
        Assert.False(ensuredResult.IsSuccess);
        Assert.Single(ensuredResult.Errors);
        Assert.Equal("String too short", ensuredResult.Error);
    }

    [Fact]
    public void Ensure_WithNullValue_ShouldReturnFailure()
    {
        // Arrange
        var result = Result<string>.Success(null);

        // Act
        var ensuredResult = result.Ensure(s => s.Length > 0, "String cannot be empty");

        // Assert
        Assert.False(ensuredResult.IsSuccess);
        Assert.Contains(ResultConstants.ConditionEvaluationWithNullValue, ensuredResult.Error);
    }

    [Fact]
    public void Match_WhenSuccessful_ShouldExecuteSuccessFunction()
    {
        // Arrange
        var result = Result<string>.Success("hello");

        // Act
        var matchResult = result.Match(
            onSuccess: value => $"Success: {value}",
            onFailure: errors => $"Failure: {string.Join(", ", errors)}"
        );

        // Assert
        Assert.True(matchResult.IsSuccess);
        Assert.Equal("Success: hello", matchResult.Value);
    }

    [Fact]
    public void Match_WhenFailed_ShouldExecuteFailureFunction()
    {
        // Arrange
        var result = Result<string>.WithFailure("Error");

        // Act
        var matchResult = result.Match(
            onSuccess: value => $"Success: {value}",
            onFailure: errors => $"Failure: {string.Join(", ", errors)}"
        );

        // Assert
        Assert.True(matchResult.IsSuccess);
        Assert.Equal("Failure: Error", matchResult.Value);
    }

    [Fact]
    public void Recover_WhenSuccessful_ShouldReturnOriginalResult()
    {
        // Arrange
        var result = Result<string>.Success("original");

        // Act
        var recoveredResult = result.Recover(() => Result<string>.Success("recovered"));

        // Assert
        Assert.Same(result, recoveredResult);
        Assert.Equal("original", recoveredResult.Value);
    }

    [Fact]
    public void Recover_WhenFailed_ShouldExecuteRecoveryFunction()
    {
        // Arrange
        var result = Result<string>.WithFailure("Error");

        // Act
        var recoveredResult = result.Recover(() => Result<string>.Success("recovered"));

        // Assert
        Assert.NotSame(result, recoveredResult);
        Assert.True(recoveredResult.IsSuccess);
        Assert.Equal("recovered", recoveredResult.Value);
    }

    [Fact]
    public void RecoverWith_WhenSuccessfulAndTypesCompatible_ShouldConvertType()
    {
        // Arrange
        var result = Result<string>.Success("42");

        // Act
        var recoveredResult = result.RecoverWith<object>(() => Result<object>.Success("fallback"));

        // Assert
        Assert.True(recoveredResult.IsSuccess);
        Assert.Equal("42", recoveredResult.Value);
    }

    [Fact]
    public void RecoverWith_WhenSuccessfulAndTypesIncompatible_ShouldReturnFailure()
    {
        // Arrange
        var result = Result<string>.Success("hello");

        // Act
        var recoveredResult = result.RecoverWith<int>(() => Result<int>.Success(42));

        // Assert
        Assert.False(recoveredResult.IsSuccess);
        Assert.Contains("Cannot convert", recoveredResult.Error);
    }

    [Fact]
    public void ToString_WhenSuccessful_ShouldIncludeValue()
    {
        // Arrange
        var result = Result<string>.Success("test value");

        // Act
        var resultString = result.ToString();

        // Assert
        Assert.StartsWith(ResultConstants.SuccessPrefix, resultString);
        Assert.Contains("test value", resultString);
    }

    [Fact]
    public void ToString_WhenFailed_ShouldFormatErrorsWithPrefix()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result<string>.WithFailure(errors);

        // Act
        var resultString = result.ToString();

        // Assert
        Assert.StartsWith(ResultConstants.FailurePrefix, resultString);
        Assert.Contains("Error 1", resultString);
        Assert.Contains("Error 2", resultString);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("test")]
    public void Success_WithVariousStringValues_ShouldHandleCorrectly(string value)
    {
        // Act
        var result = Result<string>.Success(value);

        // Assert
        Assert.True(result.IsSuccessMayBeNull);
        Assert.Equal(value, result.Value);

        if (value is not null)
        {
            Assert.True(result.IsSuccess);
            Assert.True(result.IsSuccessNotNull);
            Assert.False(result.IsSuccessValueNull);
        }
        else
        {
            Assert.False(result.IsSuccess);
            Assert.False(result.IsSuccessNotNull);
            Assert.True(result.IsSuccessValueNull);
        }
    }
}