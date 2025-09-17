namespace IndQuestResults.Tests.Unit.Operations;

/// <summary>
/// Unit tests for the non-generic Result class.
/// Tests cover basic functionality, edge cases, and performance scenarios.
/// </summary>
public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Empty(result.Errors);
        Assert.Null(result.Error);
    }

    [Fact]
    public void WithFailure_SingleError_ShouldCreateFailedResult()
    {
        // Arrange
        const string errorMessage = "Something went wrong";

        // Act
        var result = Result.WithFailure(errorMessage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal(errorMessage, result.Error);
    }

    [Fact]
    public void WithFailure_MultipleErrors_ShouldCreateFailedResultWithAllErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = Result.WithFailure(errors);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(3, result.Errors.Count());
        Assert.Equal("Error 1", result.Error);
        Assert.Contains("Error 2", result.Errors);
        Assert.Contains("Error 3", result.Errors);
    }

    [Fact]
    public void WithFailure_NullErrors_ShouldUseDefaultErrorMessage()
    {
        // Act
        var result = Result.WithFailure((IEnumerable<string>)null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal(ResultConstants.DefaultErrorMessage, result.Error);
    }

    [Fact]
    public void WithFailure_EmptyErrors_ShouldUseDefaultErrorMessage()
    {
        // Act
        var result = Result.WithFailure(Array.Empty<string>());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal(ResultConstants.DefaultErrorMessage, result.Error);
    }

    [Fact]
    public void OnSuccess_WhenSuccessful_ShouldExecuteAction()
    {
        // Arrange
        var result = Result.Success();
        var executed = false;

        // Act
        var returnedResult = result.OnSuccess(() => executed = true);

        // Assert
        Assert.True(executed);
        Assert.Same(result, returnedResult);
    }

    [Fact]
    public void OnSuccess_WhenFailed_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result.WithFailure("Error");
        var executed = false;

        // Act
        var returnedResult = result.OnSuccess(() => executed = true);

        // Assert
        Assert.False(executed);
        Assert.Same(result, returnedResult);
    }

    [Fact]
    public void OnFailure_WhenSuccessful_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result.Success();
        var executed = false;

        // Act
        var returnedResult = result.OnFailure(_ => executed = true);

        // Assert
        Assert.False(executed);
        Assert.Same(result, returnedResult);
    }

    [Fact]
    public void OnFailure_WhenFailed_ShouldExecuteActionWithErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result.WithFailure(errors);
        IEnumerable<string> capturedErrors = null;

        // Act
        var returnedResult = result.OnFailure(errs => capturedErrors = errs);

        // Assert
        Assert.NotNull(capturedErrors);
        Assert.Equal(errors, capturedErrors);
        Assert.Same(result, returnedResult);
    }

    [Fact]
    public void Map_WhenSuccessful_ShouldExecuteFunctionAndReturnResultT()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var mappedResult = result.Map(() => "mapped value");

        // Assert
        Assert.True(mappedResult.IsSuccess);
        Assert.Equal("mapped value", mappedResult.Value);
    }

    [Fact]
    public void Map_WhenFailed_ShouldPropagateErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result.WithFailure(errors);

        // Act
        var mappedResult = result.Map(() => "mapped value");

        // Assert
        Assert.False(mappedResult.IsSuccess);
        Assert.Equal(errors, mappedResult.Errors);
        Assert.Equal(default(string), mappedResult.Value);
    }

    [Fact]
    public void Bind_WhenSuccessful_ShouldExecuteFunctionAndReturnResult()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var boundResult = result.Bind(() => Result<int>.Success(42));

        // Assert
        Assert.True(boundResult.IsSuccess);
        Assert.Equal(42, boundResult.Value);
    }

    [Fact]
    public void Bind_WhenFailed_ShouldPropagateErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result.WithFailure(errors);

        // Act
        var boundResult = result.Bind(() => Result<int>.Success(42));

        // Assert
        Assert.False(boundResult.IsSuccess);
        Assert.Equal(errors, boundResult.Errors);
    }

    [Fact]
    public void Ensure_WhenSuccessfulAndConditionTrue_ShouldReturnSameResult()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var ensuredResult = result.Ensure(() => true, "Condition failed");

        // Assert
        Assert.True(ensuredResult.IsSuccess);
        Assert.Same(result, ensuredResult);
    }

    [Fact]
    public void Ensure_WhenSuccessfulAndConditionFalse_ShouldReturnFailure()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var ensuredResult = result.Ensure(() => false, "Condition failed");

        // Assert
        Assert.False(ensuredResult.IsSuccess);
        Assert.Single(ensuredResult.Errors);
        Assert.Equal("Condition failed", ensuredResult.Error);
    }

    [Fact]
    public void Ensure_WhenFailed_ShouldReturnSameResult()
    {
        // Arrange
        var result = Result.WithFailure("Original error");

        // Act
        var ensuredResult = result.Ensure(() => false, "Condition failed");

        // Assert
        Assert.Same(result, ensuredResult);
    }

    [Fact]
    public void Combine_WithAllSuccessful_ShouldReturnSuccess()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.Success();
        var result3 = Result.Success();

        // Act
        var combinedResult = result1.Combine(result2, result3);

        // Assert
        Assert.True(combinedResult.IsSuccess);
        Assert.Empty(combinedResult.Errors);
    }

    [Fact]
    public void Combine_WithSomeFailures_ShouldAggregateAllErrors()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.WithFailure("Error 2");
        var result3 = Result.WithFailure(new[] { "Error 3a", "Error 3b" });

        // Act
        var combinedResult = result1.Combine(result2, result3);

        // Assert
        Assert.False(combinedResult.IsSuccess);
        Assert.Equal(3, combinedResult.Errors.Count());
        Assert.Contains("Error 2", combinedResult.Errors);
        Assert.Contains("Error 3a", combinedResult.Errors);
        Assert.Contains("Error 3b", combinedResult.Errors);
    }

    [Fact]
    public void Match_WhenSuccessful_ShouldExecuteSuccessFunction()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var matchResult = result.Match(
            onSuccess: () => "success",
            onFailure: errors => "failure"
        );

        // Assert
        Assert.Equal("success", matchResult);
    }

    [Fact]
    public void Match_WhenFailed_ShouldExecuteFailureFunction()
    {
        // Arrange
        var result = Result.WithFailure("Error");

        // Act
        var matchResult = result.Match(
            onSuccess: () => "success",
            onFailure: errors => $"failure: {string.Join(", ", errors)}"
        );

        // Assert
        Assert.Equal("failure: Error", matchResult);
    }

    [Fact]
    public void Recover_WhenSuccessful_ShouldReturnOriginalResult()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var recoveredResult = result.Recover(() => Result.WithFailure("Recovery error"));

        // Assert
        Assert.Same(result, recoveredResult);
        Assert.True(recoveredResult.IsSuccess);
    }

    [Fact]
    public void Recover_WhenFailed_ShouldExecuteRecoveryFunction()
    {
        // Arrange
        var result = Result.WithFailure("Original error");

        // Act
        var recoveredResult = result.Recover(() => Result.Success());

        // Assert
        Assert.NotSame(result, recoveredResult);
        Assert.True(recoveredResult.IsSuccess);
    }

    [Fact]
    public void ToString_WhenSuccessful_ShouldReturnSuccessPrefix()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.Equal(ResultConstants.SuccessPrefix, result.ToString());
    }

    [Fact]
    public void ToString_WhenFailed_ShouldFormatErrorsWithPrefix()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result.WithFailure(errors);

        // Act
        var resultString = result.ToString();

        // Assert
        Assert.StartsWith(ResultConstants.FailurePrefix, resultString);
        Assert.Contains("Error 1", resultString);
        Assert.Contains("Error 2", resultString);
    }
}