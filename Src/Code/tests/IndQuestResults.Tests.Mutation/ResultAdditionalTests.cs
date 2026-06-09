namespace IndQuestResults.Tests.Mutation;

/// <summary>
/// Additional unit tests for the non-generic Result class covering newly added behaviors and edge cases.
/// </summary>
public class ResultAdditionalTests
{
    /// <summary>
    /// Ensures Tap executes the provided action when the result is successful and returns the same instance.
    /// </summary>
    [Fact]
    public void Tap_WhenSuccessful_ShouldExecuteActionAndReturnSame()
    {
        // Arrange
        var result = Result.Success();
        var executed = false;

        // Act
        var returned = result.Tap(() => executed = true);

        // Assert
        executed.ShouldBeTrue();
        returned.ShouldBeSameAs(result);
        returned.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Ensures Tap does not execute the action on a failed result and still returns the same instance.
    /// </summary>
    [Fact]
    public void Tap_WhenFailed_ShouldNotExecuteActionAndReturnSame()
    {
        // Arrange
        var result = Result.WithFailure(["E1"]);
        var executed = false;

        // Act
        var returned = result.Tap(() => executed = true);

        // Assert
        executed.ShouldBeFalse();
        returned.ShouldBeSameAs(result);
        returned.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that the Error property returns the first non-empty error string.
    /// </summary>
    [Fact]
    public void ErrorProperty_ShouldReturnFirstNonEmpty()
    {
        // Arrange
        var result = Result.WithFailure(["", "   ", "A", "B"]);

        // Act & Assert
        result.Error.ShouldBe("A");
    }

    /// <summary>
    /// Ensures the parameterless constructor initializes a failed Result with an empty error list.
    /// </summary>
    [Fact]
    public void DefaultConstructor_ShouldBeFailureWithEmptyErrors()
    {
        // Act
        var result = new Result();

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.Error.ShouldBeNull();
        result.ToString().ShouldBe(ResultConstants.FailurePrefix);
    }

    /// <summary>
    /// Ensures WithFailure(string[] errors) handles null array by producing the default error message.
    /// </summary>
    [Fact]
    public void WithFailure_StringArrayNull_ShouldUseDefaultErrorMessage()
    {
        // Arrange
        string[]? errors = null;

        // Act
        var result = Result.WithFailure(errors!);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count().ShouldBe(1);
        result.Error.ShouldBe(ResultConstants.DefaultErrorMessage);
    }
}


