namespace IndQuestResults.Tests.Unit;

/// <summary>
/// Unit tests for the generic Result&lt;T&gt; class.
/// Tests cover basic functionality, edge cases, type safety, and performance scenarios.
/// </summary>
public class ResultGenericTests
{
    /// <summary>
    /// Ensures <see cref="Result{T}.Success(T)"/> creates a successful result with value and no errors.
    /// </summary>
    [Fact]
    public void Success_ShouldCreateSuccessfulResultWithValue()
    {
        // Arrange
        const string value = "test value";

        // Act
        var result = Result<string>.Success(value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsSuccessMayBeNull.ShouldBeTrue();
        result.IsSuccessNotNull.ShouldBeTrue();
        result.IsSuccessValueNull.ShouldBeFalse();
        result.IsFailure.ShouldBeFalse();
        result.HasErrors.ShouldBeFalse();
        result.HasWarnings.ShouldBeFalse();
        result.IsRecoverable.ShouldBeTrue();
        result.Value.ShouldBe(value);
        result.Errors.ShouldBeEmpty();
        result.Error.ShouldBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Verifies OnSuccess executes for nullable value type T? even when Value is null.
    /// </summary>
    [Fact]
    public void OnSuccess_ShouldInvoke_ForNullableValueType_WhenValueIsNull()
    {
        var r = new Result<int?>(true, errors: null, value: null);
        var invoked = false;
        r.OnSuccess(_ => invoked = true);
        invoked.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies Map executes for nullable value type T? even when Value is null.
    /// </summary>
    [Fact]
    public void Map_ShouldInvoke_ForNullableValueType_WhenValueIsNull()
    {
        var r = new Result<int?>(true, errors: null, value: null);
        var mapped = r.Map(i => (i ?? 0) + 1);
        mapped.IsSuccess.ShouldBeTrue();
        mapped.Value.ShouldBe(1);
    }

    /// <summary>
    /// Verifies Bind executes for nullable value type T? even when Value is null.
    /// </summary>
    [Fact]
    public void Bind_ShouldInvoke_ForNullableValueType_WhenValueIsNull()
    {
        var r = new Result<int?>(true, errors: null, value: null);
        var bound = r.Bind(i => Result<string>.Success((i ?? 0).ToString()));
        bound.IsSuccess.ShouldBeTrue();
        bound.Value.ShouldBe("0");
    }

    /// <summary>
    /// Verifies the failure branch hands DefaultErrorMessage to the onFailure function when
    /// Errors is null or empty.
    /// </summary>
    [Fact]
    public void Match_ShouldPassDefaultErrorArray_WhenErrorsIsNull()
    {
        // Errors null OR empty must supply DefaultErrorMessage to failure branch
        var rNull = new Result<int>(false, errors: null, value: 0);
        var rEmpty = Result<int>.WithFailure([]);

        var outNull = rNull.Match(
            onSuccess: _ => "OK",
            onFailure: errs => string.Join(";", errs)
        );
        var outEmpty = rEmpty.Match(
            onSuccess: _ => "OK",
            onFailure: errs => string.Join(";", errs)
        );

        outNull.IsSuccess.ShouldBeTrue();
        outNull.Value.ShouldBe(ResultConstants.DefaultErrorMessage);
        outEmpty.IsSuccess.ShouldBeTrue();
        outEmpty.Value.ShouldBe(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Ensures <see cref="Result{T}.Success(T)"/> handles <c>null</c> values and sets success flags appropriately.
    /// </summary>
    [Fact]
    public void Success_WithNullValue_ShouldCreateSuccessfulResultWithNullValue()
    {
        // Act - Intentionally testing null value behavior
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        var result = Result<string>.Success(null);
#pragma warning restore CS8625

        // Assert
        result.IsSuccess.ShouldBeFalse(); // IsSuccess checks for non-null value
        result.IsSuccessMayBeNull.ShouldBeTrue(); // IsSuccessMayBeNull allows null
        result.IsSuccessNotNull.ShouldBeFalse();
        result.IsSuccessValueNull.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.HasErrors.ShouldBeFalse();
        result.HasWarnings.ShouldBeFalse();
        result.IsRecoverable.ShouldBeTrue();
        result.Value.ShouldBeNull();
        result.Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// Ensures <see cref="Result{T}.WithFailure(string, T)"/> creates a failed result with a single error.
    /// </summary>
    [Fact]
    public void WithFailure_SingleError_ShouldCreateFailedResult()
    {
        // Arrange
        const string errorMessage = "Something went wrong";
        const string value = "test value";

        // Act
        var result = Result<string>.WithFailure(errorMessage, value);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsSuccessMayBeNull.ShouldBeFalse();
        result.IsSuccessNotNull.ShouldBeFalse();
        result.IsSuccessValueNull.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.HasErrors.ShouldBeTrue();
        result.HasWarnings.ShouldBeFalse();
        result.IsRecoverable.ShouldBeFalse();
        result.Value.ShouldBe(value);
        result.Errors.ShouldHaveSingleItem();
        result.Error.ShouldBe(errorMessage);
    }

    /// <summary>
    /// Ensures <see cref="Result{T}.WithFailure(System.Collections.Generic.IEnumerable{string}, T)"/> aggregates all errors.
    /// </summary>
    [Fact]
    public void WithFailure_MultipleErrors_ShouldCreateFailedResultWithAllErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2", "Error 3" };

        // Act
        var result = Result<int>.WithFailure(errors);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.HasErrors.ShouldBeTrue();
        result.HasWarnings.ShouldBeFalse();
        result.Errors.Count().ShouldBe(3);
        result.Error.ShouldBe("Error 1");
        result.Errors.ShouldContain("Error 2");
        result.Errors.ShouldContain("Error 3");
    }

    /// <summary>
    /// Ensures <see cref="Result{T}.WithWarnings(System.Collections.Generic.IEnumerable{string}, T)"/> returns success with warnings.
    /// </summary>
    [Fact]
    public void WithWarnings_ShouldCreateSuccessfulResultWithWarnings()
    {
        // Arrange
        var warnings = new[] { "Warning 1", "Warning 2" };
        const string value = "test value";

        // Act
        var result = Result<string>.WithWarnings(warnings, value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.IsSuccessMayBeNull.ShouldBeTrue();
        result.IsSuccessNotNull.ShouldBeTrue();
        result.IsSuccessValueNull.ShouldBeFalse();
        result.IsFailure.ShouldBeFalse();
        result.HasErrors.ShouldBeTrue(); // HasErrors includes warnings
        result.HasWarnings.ShouldBeTrue();
        result.IsRecoverable.ShouldBeTrue();
        result.Value.ShouldBe(value);
        result.Errors.Count().ShouldBe(2);
        result.Errors.ShouldContain("Warning 1");
        result.Errors.ShouldContain("Warning 2");
    }

    /// <summary>
    /// Ensures enhanced overload sets warnings and metadata (confidence, missing data ratio).
    /// </summary>
    [Fact]
    public void WithWarnings_Enhanced_ShouldSetWarningsAndMetadata()
    {
        // Arrange
        var warnings = new[] { "Low confidence due to partial input", "Heuristic used" };
        const string value = "computed";
        const double confidence = 0.8;
        const double missing = 0.25;

        // Act
        var result = Result<string>.WithWarnings(warnings, value, confidence, missing);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.HasWarnings.ShouldBeTrue();
        result.IsRecoverable.ShouldBeTrue();
        result.Value.ShouldBe(value);
        // Warnings should be exposed
        result.Warnings.ShouldNotBeNull();
        result.Warnings.ShouldContain("Low confidence due to partial input");
        result.Warnings.ShouldContain("Heuristic used");
        // For backward-compat expectations in docs/tests
        result.Errors.ShouldNotBeNull();
        result.Errors.ShouldContain("Low confidence due to partial input");
        result.Errors.ShouldContain("Heuristic used");
        // Metadata
        result.Confidence.ShouldBe(confidence, 3);
        result.MissingDataRatio.ShouldBe(missing, 3);
    }

    /// <summary>
    /// Ensures confidence and missing data ratio are clamped to [0,1].
    /// </summary>
    [Fact]
    public void WithWarnings_Metadata_ShouldClampOutOfRangeValues()
    {
        // Arrange
        var warnings = new[] { "Partial data" };

        // Act
        var resultLow = Result<string>.WithWarnings(warnings, "v", confidence: -0.5, missingDataRatio: -1.0);
        var resultHigh = Result<string>.WithWarnings(warnings, "v", confidence: 2.5, missingDataRatio: 3.0);

        // Assert
        resultLow.Confidence.ShouldBe(0.0, 3);
        resultLow.MissingDataRatio.ShouldBe(0.0, 3);
        resultHigh.Confidence.ShouldBe(1.0, 3);
        resultHigh.MissingDataRatio.ShouldBe(1.0, 3);
    }

    /// <summary>
    /// Ensures defaults for metadata on standard successes.
    /// </summary>
    [Fact]
    public void Success_DefaultMetadata_ShouldBeApplied()
    {
        // Act
        var result = Result<string>.Success("ok");

        // Assert
        result.Confidence.ShouldBe(1.0, 3);
        result.MissingDataRatio.ShouldBe(0.0, 3);
        result.HasWarnings.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies implicit conversion from value to <see cref="Result{T}"/> yields a successful result.
    /// </summary>
    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateSuccessfulResult()
    {
        // Act
        Result<int> result = 42;

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// Verifies implicit conversion to non-generic <see cref="Result"/> preserves success state.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ToNonGenericResult_WhenSuccessful_ShouldCreateSuccessfulResult()
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
    /// Verifies implicit conversion to non-generic <see cref="Result"/> propagates errors when failed.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ToNonGenericResult_WhenFailed_ShouldPropagateErrors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var genericResult = Result<string>.WithFailure(errors);

        // Act
        Result nonGenericResult = genericResult;

        // Assert
        nonGenericResult.IsSuccess.ShouldBeFalse();
        nonGenericResult.Errors.ShouldBe(errors);
    }

    /// <summary>
    /// Verifies deconstruction returns success state, value, and errors.
    /// </summary>
    [Fact]
    public void Deconstruct_ShouldProvideSuccessStateValueAndErrors()
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
    /// Ensures <see cref="Result{T}.Map{TResult}(System.Func{T, TResult})"/> transforms value when successful.
    /// </summary>
    [Fact]
    public void OnSuccess_WhenSuccessful_ShouldExecuteActionWithValue()
    {
        // Arrange
        var result = Result<string>.Success("test value");
        string? capturedValue = null;

        // Act
        var returnedResult = result.OnSuccess(value => capturedValue = value);

        // Assert
        capturedValue.ShouldBe("test value");
        returnedResult.ShouldBeSameAs(result);
    }

    /// <summary>
    /// Ensures <see cref="Result{T}.OnSuccess(System.Action{T})"/> does not execute when failed.
    /// </summary>
    [Fact]
    public void OnSuccess_WhenFailed_ShouldNotExecuteAction()
    {
        // Arrange
        var result = Result<string>.WithFailure("Error");
        var executed = false;

        // Act
        var returnedResult = result.OnSuccess(_ => executed = true);

        // Assert
        executed.ShouldBeFalse();
        returnedResult.ShouldBeSameAs(result);
    }

    /// <summary>
    /// Ensures <see cref="Result{T}.Map{TResult}(System.Func{T, TResult})"/> transforms value when successful.
    /// </summary>
    [Fact]
    public void Map_WhenSuccessful_ShouldTransformValue()
    {
        // Arrange
        var result = Result<string>.Success("hello");

        // Act
        var mappedResult = result.Map(s => s.Length);

        // Assert
        mappedResult.IsSuccess.ShouldBeTrue();
        mappedResult.Value.ShouldBe(5);
    }

    /// <summary>
    /// Ensures mapping a failed result propagates errors and does not transform.
    /// </summary>
    [Fact]
    public void Map_WhenFailed_ShouldPropagateErrorsAndNotTransform()
    {
        // Arrange
        var errors = new[] { "Error 1" };
        var result = Result<string>.WithFailure(errors);

        // Act
        var mappedResult = result.Map(s => s.Length);

        // Assert
        mappedResult.IsSuccess.ShouldBeFalse();
        mappedResult.Errors.ShouldBe(errors);
        mappedResult.Value.ShouldBe(0); // Default value for int
    }

    /// <summary>
    /// Verifies monadic bind chains operations when successful.
    /// </summary>
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
        boundResult.IsSuccess.ShouldBeTrue();
        boundResult.Value.ShouldBe(42);
    }

    /// <summary>
    /// Verifies bind propagates errors when the source result is failed.
    /// </summary>
    [Fact]
    public void Bind_WhenFailed_ShouldPropagateErrorsAndNotChain()
    {
        // Arrange
        var errors = new[] { "Error 1" };
        var result = Result<string>.WithFailure(errors);

        // Act
        var boundResult = result.Bind(s => Result<int>.Success(42));

        // Assert
        boundResult.IsSuccess.ShouldBeFalse();
        boundResult.Errors.ShouldBe(errors);
    }

    /// <summary>
    /// Ensures Ensure returns the same result when predicate holds.
    /// </summary>
    [Fact]
    public void Ensure_WhenSuccessfulAndConditionTrue_ShouldReturnSameResult()
    {
        // Arrange
        var result = Result<string>.Success("hello");

        // Act
        var ensuredResult = result.Ensure(s => s.Length > 0, "String cannot be empty");

        // Assert
        ensuredResult.ShouldBeSameAs(result);
        ensuredResult.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Ensures Ensure returns failure when predicate fails.
    /// </summary>
    [Fact]
    public void Ensure_WhenSuccessfulAndConditionFalse_ShouldReturnFailure()
    {
        // Arrange
        var result = Result<string>.Success("hello");

        // Act
        var ensuredResult = result.Ensure(s => s.Length > 10, "String too short");

        // Assert
        ensuredResult.IsSuccess.ShouldBeFalse();
        ensuredResult.Errors.ShouldHaveSingleItem();
        ensuredResult.Error.ShouldBe("String too short");
    }

    /// <summary>
    /// Ensures Ensure with null value produces failure and meaningful error.
    /// </summary>
    [Fact]
    public void Ensure_WithNullValue_ShouldReturnFailure()
    {
        // Arrange - Intentionally testing null value behavior
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        var result = Result<string>.Success(null);
#pragma warning restore CS8625

        // Act
        var ensuredResult = result.Ensure(s => s.Length > 0, "String cannot be empty");

        // Assert
        ensuredResult.IsSuccess.ShouldBeFalse();
        ensuredResult.Error.ShouldContain(ResultConstants.ConditionEvaluationWithNullValue);
    }

    /// <summary>
    /// Verifies <see cref="Result{T}.Match{TResult}(System.Func{T, TResult}, System.Func{System.Collections.Generic.IEnumerable{string}, TResult})"/>
    /// executes the success branch when successful.
    /// </summary>
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
        matchResult.IsSuccess.ShouldBeTrue();
        matchResult.Value.ShouldBe("Success: hello");
    }

    /// <summary>
    /// Verifies Match executes the failure branch when failed.
    /// </summary>
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
        matchResult.IsSuccess.ShouldBeTrue();
        matchResult.Value.ShouldBe("Failure: Error");
    }

    /// <summary>
    /// Ensures Recover returns original result when already successful.
    /// </summary>
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

    /// <summary>
    /// Ensures Recover executes recovery function when failed.
    /// </summary>
    [Fact]
    public void Recover_WhenFailed_ShouldExecuteRecoveryFunction()
    {
        // Arrange
        var result = Result<string>.WithFailure("Error");

        // Act
        var recoveredResult = result.Recover(() => Result<string>.Success("recovered"));

        // Assert
        recoveredResult.ShouldNotBeSameAs(result);
        recoveredResult.IsSuccess.ShouldBeTrue();
        recoveredResult.Value.ShouldBe("recovered");
    }

    /// <summary>
    /// Ensures RecoverWith can convert compatible types while preserving success.
    /// </summary>
    [Fact]
    public void RecoverWith_WhenSuccessfulAndTypesCompatible_ShouldConvertType()
    {
        // Arrange
        var result = Result<string>.Success("42");

        // Act
        var recoveredResult = result.RecoverWith<object>(() => Result<object>.Success("fallback"));

        // Assert
        recoveredResult.IsSuccess.ShouldBeTrue();
        recoveredResult.Value.ShouldBe("42");
    }

    /// <summary>
    /// Ensures RecoverWith returns failure when types are incompatible.
    /// </summary>
    [Fact]
    public void RecoverWith_WhenSuccessfulAndTypesIncompatible_ShouldReturnFailure()
    {
        // Arrange
        var result = Result<string>.Success("hello");

        // Act
        var recoveredResult = result.RecoverWith<int>(() => Result<int>.Success(42));

        // Assert
        recoveredResult.IsSuccess.ShouldBeFalse();
        recoveredResult.Error.ShouldContain("Cannot convert");
    }

    /// <summary>
    /// Verifies IsCancelled extension returns false for null generic result.
    /// </summary>
    [Fact]
    public void IsCancelled_ShouldBeFalse_ForNullResult_Generic()
    {
        Result<string>? r = null;
        var isCancelled = ResultExtensions.IsCancelled(r!);
        isCancelled.ShouldBeFalse();
    }

    /// <summary>
    /// Ensures ToString on a successful result contains the success prefix and value.
    /// </summary>
    [Fact]
    public void ToString_WhenSuccessful_ShouldIncludeValue()
    {
        // Arrange
        var result = Result<string>.Success("test value");

        // Act
        var resultString = result.ToString();

        // Assert
        resultString.ShouldStartWith(ResultConstants.SuccessPrefix);
        resultString.ShouldContain("test value");
    }

    /// <summary>
    /// Ensures ToString on a failed result formats errors with the failure prefix.
    /// </summary>
    [Fact]
    public void ToString_WhenFailed_ShouldFormatErrorsWithPrefix()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };
        var result = Result<string>.WithFailure(errors);

        // Act
        var resultString = result.ToString();

        // Assert
        resultString.ShouldStartWith(ResultConstants.FailurePrefix);
        resultString.ShouldContain("Error 1");
        resultString.ShouldContain("Error 2");
    }

    /// <summary>
    /// Parameterized verification that Success handles various string values consistently.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("test")]
    public void Success_WithVariousStringValues_ShouldHandleCorrectly(string? value)
    {
        // Act - Use pattern matching for proper null checking
        var result = value is not null
            ? Result<string>.Success(value)
            : CreateResultWithNullValue();

        // Helper method to handle null case with proper warning suppression
        static Result<string> CreateResultWithNullValue()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
            return Result<string>.Success(null);
#pragma warning restore CS8625
        }

        // Assert
        result.IsSuccessMayBeNull.ShouldBeTrue();
        result.Value.ShouldBe(value);

        if (value is not null)
        {
            result.IsSuccess.ShouldBeTrue();
            result.IsSuccessNotNull.ShouldBeTrue();
            result.IsSuccessValueNull.ShouldBeFalse();
        }
        else
        {
            result.IsSuccess.ShouldBeFalse();
            result.IsSuccessNotNull.ShouldBeFalse();
            result.IsSuccessValueNull.ShouldBeTrue();
        }
    }
}
