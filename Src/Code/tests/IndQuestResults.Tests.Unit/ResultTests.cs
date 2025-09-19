namespace IndQuestResults.Tests.Unit;

/// <summary>
/// Unit tests for the non-generic Result class.
/// Tests cover basic functionality, edge cases, and performance scenarios.
/// </summary>
public class ResultTests
{
    private static readonly string[] TestErrorArray = ["Error 3a", "Error 3b"];
    /// <summary>
    /// Ensures <see cref="Result.Success()"/> creates a successful non-generic result.
    /// </summary>
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Errors.ShouldBeEmpty();
        result.Error.ShouldBeNull();
    }

    /// <summary>
    /// Verifies IsCancelled extension returns false when the non-generic result is null.
    /// </summary>
    [Fact]
    public void IsCancelled_ShouldBeFalse_ForNullResult_NonGeneric()
    {
        Result? r = null;
        var isCancelled = ResultExtensions.IsCancelled(r!);
        isCancelled.ShouldBeFalse();
    }

    /// <summary>
    /// Ensures <see cref="Result.WithFailure(string)"/> creates a failed result with a single error.
    /// </summary>
    [Fact]
    public void WithFailure_SingleError_ShouldCreateFailedResult()
    {
        // Arrange
        const string errorMessage = "Something went wrong";

        // Act
        var result = Result.WithFailure(errorMessage);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem();
        result.Error.ShouldBe(errorMessage);
    }

    /// <summary>
    /// Ensures <see cref="Result.WithFailure(System.Collections.Generic.IEnumerable{string})"/> collects all errors.
    /// </summary>
    [Fact]
    public void WithFailure_MultipleErrors_ShouldCreateFailedResultWithAllErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = Result.WithFailure(errors);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Errors.Count().ShouldBe(3);
        result.Error.ShouldBe("Error 1");
        result.Errors.ShouldContain("Error 2");
        result.Errors.ShouldContain("Error 3");
    }

    /// <summary>
    /// Ensures combining two empty collections returns the default no-errors message.
    /// </summary>
    [Fact]
    public void CombineErrors_BothEmptyCollections_ShouldReturnDefaultMessage()
    {
        // Arrange
        var empty1 = Array.Empty<string>();
        var empty2 = Array.Empty<string>();

        // Act
        var result = Result.CombineErrors(empty1, empty2);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(ResultConstants.NoErrorsFoundMessage);
    }

    /// <summary>
    /// Verifies a default error message is used when null errors are provided.
    /// </summary>
    [Fact]
    public void WithFailure_NullErrors_ShouldUseDefaultErrorMessage()
    {
        // Act
        var result = Result.WithFailure((IEnumerable<string>)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem();
        result.Error.ShouldBe(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Verifies a default error message is used when an empty error list is provided.
    /// </summary>
    [Fact]
    public void WithFailure_EmptyErrors_ShouldUseDefaultErrorMessage()
    {
        // Act
        var result = Result.WithFailure([]);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldHaveSingleItem();
        result.Error.ShouldBe(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Ensures <see cref="Result.OnSuccess(System.Action)"/> executes when the result is successful.
    /// </summary>
    [Fact]
    public void OnSuccess_WhenSuccessful_ShouldExecuteAction()
    {
        // Arrange
        var result = Result.Success();
        var executed = false;

        // Act
        var returnedResult = result.OnSuccess(() => executed = true);

        // Assert
        executed.ShouldBeTrue();
        returnedResult.ShouldBeSameAs(result);
    }

    /// <summary>
    /// Ensures <see cref="Result.OnSuccess(System.Action)"/> does not execute when the result is failed.
    /// </summary>
    [Fact]
    public void OnSuccess_WhenFailed_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result.WithFailure("Error");
        var executed = false;

        // Act
        var returnedResult = result.OnSuccess(() => executed = true);

        // Assert
        executed.ShouldBeFalse();
        returnedResult.ShouldBeSameAs(result);
    }

    /// <summary>
    /// Ensures <see cref="Result.OnFailure(System.Action{System.Collections.Generic.IEnumerable{string}})"/> does not execute for success.
    /// </summary>
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

    /// <summary>
    /// Ensures <see cref="Result.OnFailure(System.Action{System.Collections.Generic.IEnumerable{string}})"/> executes for failures.
    /// </summary>
    [Fact]
    public void OnFailure_WhenFailed_ShouldExecuteActionWithErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result.WithFailure(errors);
        IEnumerable<string>? capturedErrors = null;

        // Act
        var returnedResult = result.OnFailure(errs => capturedErrors = errs);

        // Assert
        capturedErrors.ShouldNotBeNull();
        capturedErrors.ShouldBe(errors);
        returnedResult.ShouldBeSameAs(result);
    }

    /// <summary>
    /// Ensures <see cref="Result.Map{T}(System.Func{T})"/> executes and returns a successful result.
    /// </summary>
    [Fact]
    public void Map_WhenSuccessful_ShouldExecuteFunctionAndReturnResultT()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var mappedResult = result.Map(() => "mapped value");

        // Assert
        mappedResult.IsSuccess.ShouldBeTrue();
        mappedResult.Value.ShouldBe("mapped value");
    }

    /// <summary>
    /// Ensures mapping a failed result propagates the same errors.
    /// </summary>
    [Fact]
    public void Map_WhenFailed_ShouldPropagateErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result.WithFailure(errors);

        // Act
        var mappedResult = result.Map(() => "mapped value");

        // Assert
        mappedResult.IsSuccess.ShouldBeFalse();
        mappedResult.Errors.ShouldBe(errors);
        mappedResult.Value.ShouldBe(default(string));
    }

    /// <summary>
    /// Ensures <see cref="Result.Bind{T}(System.Func{Result{T}})"/> executes and returns the produced result.
    /// </summary>
    [Fact]
    public void Bind_WhenSuccessful_ShouldExecuteFunctionAndReturnResult()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var boundResult = result.Bind(() => Result<int>.Success(42));

        // Assert
        boundResult.IsSuccess.ShouldBeTrue();
        boundResult.Value.ShouldBe(42);
    }

    /// <summary>
    /// Ensures bind over a failed result propagates errors and does not execute.
    /// </summary>
    [Fact]
    public void Bind_WhenFailed_ShouldPropagateErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result.WithFailure(errors);

        // Act
        var boundResult = result.Bind(() => Result<int>.Success(42));

        // Assert
        boundResult.IsSuccess.ShouldBeFalse();
        boundResult.Errors.ShouldBe(errors);
    }

    /// <summary>
    /// Ensures <see cref="Result.Ensure(System.Func{bool}, string)"/> returns the same result when the predicate is true.
    /// </summary>
    [Fact]
    public void Ensure_WhenSuccessfulAndConditionTrue_ShouldReturnSameResult()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var ensuredResult = result.Ensure(() => true, "Condition failed");

        // Assert
        ensuredResult.IsSuccess.ShouldBeTrue();
        ensuredResult.ShouldBeSameAs(result);
    }

    /// <summary>
    /// Ensures Ensure returns failure when the predicate is false.
    /// </summary>
    [Fact]
    public void Ensure_WhenSuccessfulAndConditionFalse_ShouldReturnFailure()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var ensuredResult = result.Ensure(() => false, "Condition failed");

        // Assert
        ensuredResult.IsSuccess.ShouldBeFalse();
        ensuredResult.Errors.ShouldHaveSingleItem();
        ensuredResult.Error.ShouldBe("Condition failed");
    }

    /// <summary>
    /// Ensures Ensure on an already failed result returns the original result unchanged.
    /// </summary>
    [Fact]
    public void Ensure_WhenFailed_ShouldReturnSameResult()
    {
        // Arrange
        var result = Result.WithFailure("Original error");

        // Act
        var ensuredResult = result.Ensure(() => false, "Condition failed");

        // Assert
        ensuredResult.ShouldBeSameAs(result);
    }

    /// <summary>
    /// Verifies combining only successful results yields a successful aggregate result.
    /// </summary>
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
        combinedResult.IsSuccess.ShouldBeTrue();
        combinedResult.Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies combining results aggregates all failure errors.
    /// </summary>
    [Fact]
    public void Combine_WithSomeFailures_ShouldAggregateAllErrors()
    {
        // Arrange
        var result1 = Result.Success();
        var result2 = Result.WithFailure("Error 2");
        var result3 = Result.WithFailure(TestErrorArray);

        // Act
        var combinedResult = result1.Combine(result2, result3);

        // Assert
        combinedResult.IsSuccess.ShouldBeFalse();
        combinedResult.Errors.Count().ShouldBe(3);
        combinedResult.Errors.ShouldContain("Error 2");
        combinedResult.Errors.ShouldContain("Error 3a");
        combinedResult.Errors.ShouldContain("Error 3b");
    }

    /// <summary>
    /// Ensures <see cref="Result.Match{T}(System.Func{T}, System.Func{System.Collections.Generic.IEnumerable{string}, T})"/> executes the success branch.
    /// </summary>
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
        matchResult.ShouldBe("success");
    }

    /// <summary>
    /// Ensures Match executes the failure branch for failed results.
    /// </summary>
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
        matchResult.ShouldBe("failure: Error");
    }

    /// <summary>
    /// Ensures <see cref="Result.Recover(System.Func{Result})"/> returns the original result when already successful.
    /// </summary>
    [Fact]
    public void Recover_WhenSuccessful_ShouldReturnOriginalResult()
    {
        // Arrange
        var result = Result.Success();

        // Act
        var recoveredResult = result.Recover(() => Result.WithFailure("Recovery error"));

        // Assert
        recoveredResult.ShouldBeSameAs(result);
        recoveredResult.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Ensures Recover executes the recovery function when the source is failed.
    /// </summary>
    [Fact]
    public void Recover_WhenFailed_ShouldExecuteRecoveryFunction()
    {
        // Arrange
        var result = Result.WithFailure("Original error");

        // Act
        var recoveredResult = result.Recover(Result.Success);

        // Assert
        recoveredResult.ShouldNotBeSameAs(result);
        recoveredResult.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Ensures <see cref="object.ToString()"/> on a successful result returns the success prefix.
    /// </summary>
    [Fact]
    public void ToString_WhenSuccessful_ShouldReturnSuccessPrefix()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.ToString().ShouldBe(ResultConstants.SuccessPrefix);
    }

    /// <summary>
    /// Ensures <see cref="object.ToString()"/> on a failed result includes the failure prefix and errors.
    /// </summary>
    [Fact]
    public void ToString_WhenFailed_ShouldFormatErrorsWithPrefix()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result.WithFailure(errors);

        // Act
        var resultString = result.ToString();

        // Assert
        resultString.ShouldStartWith(ResultConstants.FailurePrefix);
        resultString.ShouldContain("Error 1");
        resultString.ShouldContain("Error 2");
    }
}



