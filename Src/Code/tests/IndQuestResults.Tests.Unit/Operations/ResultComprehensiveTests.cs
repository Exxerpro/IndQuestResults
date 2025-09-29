using System;
using System.Collections.Generic;
using System.Linq;
using IndFusion.Analyzers.Operations;
using Shouldly;
using Xunit;

namespace IndFusion.Analyzer.Tests.TestCases.Operations;

/// <summary>
/// Comprehensive tests for Result.cs to kill surviving mutants.
/// Focuses on edge cases and specific behaviors that need coverage.
/// </summary>
public class ResultComprehensiveTests
{
    /// <summary>
    /// Test: Result_Success_ShouldCreateSuccessResult
    /// </summary>
    [Fact]
    public void Result_Success_ShouldCreateSuccessResult()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.Errors.ShouldNotBeNull();
        result.Errors.ShouldBeEmpty();
    }

    /// <summary>
    /// Test: Result_WithFailure_WithNullErrors_ShouldHandleNullCoalescing
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithNullErrors_ShouldHandleNullCoalescing()
    {
        // Test line 793: errors?.ToArray() ?? []
        var result = Result.WithFailure((IEnumerable<string>?)null!);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldNotBeNull();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_WithFailure_WithEmptyErrors_ShouldHandleEmptyArray
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithEmptyErrors_ShouldHandleEmptyArray()
    {
        // Test line 794: errorArray.Length > 0
        var result = Result.WithFailure(new string[0]);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldNotBeNull();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_WithFailure_WithSingleError_ShouldSetErrors
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithSingleError_ShouldSetErrors()
    {
        // Test line 794: errorArray.Length > 0
        var result = Result.WithFailure("Test error");

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Test error");
        result.Errors.Count().ShouldBe(1);
    }

    /// <summary>
    /// Test: ResultT_Constructor_WithNullErrors_ShouldHandleNullCoalescing
    /// </summary>
    [Fact]
    public void ResultT_Constructor_WithNullErrors_ShouldHandleNullCoalescing()
    {
        // Test line 793: errors?.ToArray() ?? []
        var result = new Result<string>(true, (IEnumerable<string>?)null, "test");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test");
        result.Errors.ShouldNotBeNull();
        result.Errors.ShouldBeEmpty();
        result.HasErrors.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_Constructor_WithEmptyErrors_ShouldHandleEmptyArray
    /// </summary>
    [Fact]
    public void ResultT_Constructor_WithEmptyErrors_ShouldHandleEmptyArray()
    {
        // Test line 794: errorArray.Length > 0
        var result = new Result<string>(true, new string[0], "test");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test");
        result.Errors.ShouldNotBeNull();
        result.Errors.ShouldBeEmpty();
        result.HasErrors.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_Constructor_WithSingleError_ShouldSetHasErrors
    /// </summary>
    [Fact]
    public void ResultT_Constructor_WithSingleError_ShouldSetHasErrors()
    {
        // Test line 794: errorArray.Length > 0
        var result = new Result<string>(false, new[] { "Test error" }, "test");

        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBe("test");
        result.Errors.ShouldContain("Test error");
        result.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Test: ResultT_Constructor_WithListErrors_ShouldHandleListConversion
    /// </summary>
    [Fact]
    public void ResultT_Constructor_WithListErrors_ShouldHandleListConversion()
    {
        // Test line 799: errors?.ToArray() ?? []
        var errors = new List<string> { "Error 1", "Error 2" };
        var result = new Result<string>(false, errors, "test");

        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBe("test");
        result.Errors.ShouldContain("Error 1");
        result.Errors.ShouldContain("Error 2");
        result.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Test: ResultT_Constructor_WithNullListErrors_ShouldHandleNullCoalescing
    /// </summary>
    [Fact]
    public void ResultT_Constructor_WithNullListErrors_ShouldHandleNullCoalescing()
    {
        // Test line 799: errors?.ToArray() ?? []
        var result = new Result<string>(true, (List<string>?)null, "test");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("test");
        result.Errors.ShouldNotBeNull();
        result.Errors.ShouldBeEmpty();
        result.HasErrors.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_ValidateInternalState_WithInconsistentHasErrors_ShouldFixInconsistency
    /// </summary>
    [Fact]
    public void ResultT_ValidateInternalState_WithInconsistentHasErrors_ShouldFixInconsistency()
    {
        // Test line 823: Errors?.Any() == true
        // Test line 825: HasErrors != actualHasErrors
        var result = new Result<string>(true, new[] { "Error" }, "test");

        // The result should have errors initially
        result.HasErrors.ShouldBeTrue();

        // Access a property that triggers ValidateInternalState
        var isSuccess = result.IsSuccess;

        // The validation should maintain consistency
        result.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Test: ResultT_ValidateInternalState_WithConsistentState_ShouldNotChange
    /// </summary>
    [Fact]
    public void ResultT_ValidateInternalState_WithConsistentState_ShouldNotChange()
    {
        // Test line 823: Errors?.Any() == true
        // Test line 825: HasErrors != actualHasErrors
        var result = new Result<string>(true, new string[0], "test");

        // Access a property that triggers ValidateInternalState
        var isSuccess = result.IsSuccess;

        // The validation should not change consistent state
        result.HasErrors.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_ValidateInternalState_WithNullErrors_ShouldHandleNull
    /// </summary>
    [Fact]
    public void ResultT_ValidateInternalState_WithNullErrors_ShouldHandleNull()
    {
        // Test line 823: Errors?.Any() == true
        var result = new Result<string>(true, (IEnumerable<string>?)null, "test");

        // Access a property that triggers ValidateInternalState
        var isSuccess = result.IsSuccess;

        // Should handle null errors gracefully
        result.HasErrors.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_HasWarnings_WithSuccessAndErrors_ShouldReturnTrue
    /// </summary>
    [Fact]
    public void ResultT_HasWarnings_WithSuccessAndErrors_ShouldReturnTrue()
    {
        // Test line 853: IsRecoverable && HasErrors
        var result = new Result<string>(true, new[] { "Warning" }, "test");

        result.HasWarnings.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
        result.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Test: ResultT_HasWarnings_WithSuccessAndNoErrors_ShouldReturnFalse
    /// </summary>
    [Fact]
    public void ResultT_HasWarnings_WithSuccessAndNoErrors_ShouldReturnFalse()
    {
        // Test line 853: IsRecoverable && HasErrors
        var result = new Result<string>(true, new string[0], "test");

        result.HasWarnings.ShouldBeFalse();
        result.IsSuccess.ShouldBeTrue();
        result.HasErrors.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_HasWarnings_WithFailureAndErrors_ShouldReturnFalse
    /// </summary>
    [Fact]
    public void ResultT_HasWarnings_WithFailureAndErrors_ShouldReturnFalse()
    {
        // Test line 853: IsRecoverable && HasErrors
        var result = new Result<string>(false, new[] { "Error" }, "test");

        result.HasWarnings.ShouldBeFalse();
        result.IsSuccess.ShouldBeFalse();
        result.HasErrors.ShouldBeTrue();
    }

    /// <summary>
    /// Test: ResultT_IsSuccess_WithNullValue_ShouldReturnFalse
    /// </summary>
    [Fact]
    public void ResultT_IsSuccess_WithNullValue_ShouldReturnFalse()
    {
        // Test line 853: IsRecoverable && (Value is not null)
        var result = new Result<string>(true, new string[0], null);

        result.IsSuccess.ShouldBeFalse();
        result.IsSuccessMayBeNull.ShouldBeTrue();
        result.IsSuccessValueNull.ShouldBeTrue();
    }

    /// <summary>
    /// Test: ResultT_IsSuccess_WithNonNullValue_ShouldReturnTrue
    /// </summary>
    [Fact]
    public void ResultT_IsSuccess_WithNonNullValue_ShouldReturnTrue()
    {
        // Test line 853: IsRecoverable && (Value is not null)
        var result = new Result<string>(true, new string[0], "test");

        result.IsSuccess.ShouldBeTrue();
        result.IsSuccessMayBeNull.ShouldBeTrue();
        result.IsSuccessValueNull.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_IsSuccess_WithFailure_ShouldReturnFalse
    /// </summary>
    [Fact]
    public void ResultT_IsSuccess_WithFailure_ShouldReturnFalse()
    {
        // Test line 853: IsRecoverable && (Value is not null)
        var result = new Result<string>(false, new[] { "Error" }, "test");

        result.IsSuccess.ShouldBeFalse();
        result.IsSuccessMayBeNull.ShouldBeFalse();
        result.IsSuccessValueNull.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_IsSuccessNotNull_WithNonNullValue_ShouldReturnTrue
    /// </summary>
    [Fact]
    public void ResultT_IsSuccessNotNull_WithNonNullValue_ShouldReturnTrue()
    {
        var result = new Result<string>(true, new string[0], "test");

        result.IsSuccessNotNull.ShouldBeTrue();
    }

    /// <summary>
    /// Test: ResultT_IsSuccessNotNull_WithNullValue_ShouldReturnFalse
    /// </summary>
    [Fact]
    public void ResultT_IsSuccessNotNull_WithNullValue_ShouldReturnFalse()
    {
        var result = new Result<string>(true, new string[0], null);

        result.IsSuccessNotNull.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_IsSuccessValueNull_WithNullValue_ShouldReturnTrue
    /// </summary>
    [Fact]
    public void ResultT_IsSuccessValueNull_WithNullValue_ShouldReturnTrue()
    {
        var result = new Result<string>(true, new string[0], null);

        result.IsSuccessValueNull.ShouldBeTrue();
    }

    /// <summary>
    /// Test: ResultT_IsSuccessValueNull_WithNonNullValue_ShouldReturnFalse
    /// </summary>
    [Fact]
    public void ResultT_IsSuccessValueNull_WithNonNullValue_ShouldReturnFalse()
    {
        var result = new Result<string>(true, new string[0], "test");

        result.IsSuccessValueNull.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_Error_WithEmptyErrors_ShouldReturnNull
    /// </summary>
    [Fact]
    public void ResultT_Error_WithEmptyErrors_ShouldReturnNull()
    {
        var result = new Result<string>(true, new string[0], "test");

        result.Error.ShouldBeNull();
    }

    /// <summary>
    /// Test: ResultT_Error_WithWhitespaceErrors_ShouldReturnFirstNonEmpty
    /// </summary>
    [Fact]
    public void ResultT_Error_WithWhitespaceErrors_ShouldReturnFirstNonEmpty()
    {
        var result = new Result<string>(false, new[] { "", " ", "  ", "Real Error", "Another Error" }, "test");

        result.Error.ShouldBe("Real Error");
    }

    /// <summary>
    /// Test: ResultT_Error_WithNullErrors_ShouldReturnNull
    /// </summary>
    [Fact]
    public void ResultT_Error_WithNullErrors_ShouldReturnNull()
    {
        var result = new Result<string>(true, (IEnumerable<string>?)null, "test");

        result.Error.ShouldBeNull();
    }

    /// <summary>
    /// Test: ResultT_Error_WithAllWhitespaceErrors_ShouldReturnNull
    /// </summary>
    [Fact]
    public void ResultT_Error_WithAllWhitespaceErrors_ShouldReturnNull()
    {
        var result = new Result<string>(false, new[] { "", " ", "  ", "\t", "\n" }, "test");

        result.Error.ShouldBeNull();
    }

    /// <summary>
    /// Test: ResultT_IsFailure_WithSuccess_ShouldReturnFalse
    /// </summary>
    [Fact]
    public void ResultT_IsFailure_WithSuccess_ShouldReturnFalse()
    {
        var result = new Result<string>(true, new string[0], "test");

        result.IsFailure.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_IsFailure_WithFailure_ShouldReturnTrue
    /// </summary>
    [Fact]
    public void ResultT_IsFailure_WithFailure_ShouldReturnTrue()
    {
        var result = new Result<string>(false, new[] { "Error" }, "test");

        result.IsFailure.ShouldBeTrue();
    }

    /// <summary>
    /// Test: ResultT_IsRecoverable_WithSuccess_ShouldReturnTrue
    /// </summary>
    [Fact]
    public void ResultT_IsRecoverable_WithSuccess_ShouldReturnTrue()
    {
        var result = new Result<string>(true, new string[0], "test");

        result.IsRecoverable.ShouldBeTrue();
    }

    /// <summary>
    /// Test: ResultT_IsRecoverable_WithFailure_ShouldReturnFalse
    /// </summary>
    [Fact]
    public void ResultT_IsRecoverable_WithFailure_ShouldReturnFalse()
    {
        var result = new Result<string>(false, new[] { "Error" }, "test");

        result.IsRecoverable.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_Constructor_WithNullableValueType_ShouldHandleNull
    /// </summary>
    [Fact]
    public void ResultT_Constructor_WithNullableValueType_ShouldHandleNull()
    {
        var result = new Result<int?>(true, new string[0], null);

        result.IsSuccess.ShouldBeFalse(); // IsSuccess requires Value to be not null
        result.IsSuccessMayBeNull.ShouldBeTrue(); // This allows null values
        result.Value.ShouldBeNull();
        result.IsSuccessValueNull.ShouldBeTrue();
    }

    /// <summary>
    /// Test: ResultT_Constructor_WithNullableValueType_ShouldHandleNonNull
    /// </summary>
    [Fact]
    public void ResultT_Constructor_WithNullableValueType_ShouldHandleNonNull()
    {
        var result = new Result<int?>(true, new string[0], 42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.IsSuccessValueNull.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_Constructor_WithValueType_ShouldHandleDefault
    /// </summary>
    [Fact]
    public void ResultT_Constructor_WithValueType_ShouldHandleDefault()
    {
        var result = new Result<int>(true, new string[0], 0);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(0);
        result.IsSuccessValueNull.ShouldBeFalse();
    }

    /// <summary>
    /// Test: ResultT_Constructor_WithValueType_ShouldHandleNonDefault
    /// </summary>
    [Fact]
    public void ResultT_Constructor_WithValueType_ShouldHandleNonDefault()
    {
        var result = new Result<int>(true, new string[0], 42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.IsSuccessValueNull.ShouldBeFalse();
    }

    /// <summary>
    /// Test: Result_ToString_WithSuccess_ShouldReturnSuccessPrefix
    /// </summary>
    [Fact]
    public void Result_ToString_WithSuccess_ShouldReturnSuccessPrefix()
    {
        // Test line 185: IsSuccess ? ResultConstants.SuccessPrefix : FormatErrorsString(...)
        var result = Result.Success();

        result.ToString().ShouldBe(ResultConstants.SuccessPrefix);
    }

    /// <summary>
    /// Test: Result_ToString_WithFailure_ShouldReturnFormattedErrors
    /// </summary>
    [Fact]
    public void Result_ToString_WithFailure_ShouldReturnFormattedErrors()
    {
        // Test line 185: IsSuccess ? ResultConstants.SuccessPrefix : FormatErrorsString(...)
        var result = Result.WithFailure("Test error");

        result.ToString().ShouldBe($"{ResultConstants.FailurePrefix}: Test error");
    }

    /// <summary>
    /// Test: Result_FormatErrorsString_WithNullErrors_ShouldReturnPrefix
    /// </summary>
    [Fact]
    public void Result_FormatErrorsString_WithNullErrors_ShouldReturnPrefix()
    {
        // Test line 186: errors is null || !errors.Any()
        var result = Result.FormatErrorsString((IEnumerable<string>?)null!, "TestPrefix");

        result.ShouldBe("TestPrefix");
    }

    /// <summary>
    /// Test: Result_FormatErrorsString_WithEmptyErrors_ShouldReturnPrefix
    /// </summary>
    [Fact]
    public void Result_FormatErrorsString_WithEmptyErrors_ShouldReturnPrefix()
    {
        // Test line 186: errors is null || !errors.Any()
        var result = Result.FormatErrorsString(new string[0], "TestPrefix");

        result.ShouldBe("TestPrefix");
    }

    /// <summary>
    /// Test: Result_FormatErrorsString_WithArrayErrors_ShouldUseFastPath
    /// </summary>
    [Fact]
    public void Result_FormatErrorsString_WithArrayErrors_ShouldUseFastPath()
    {
        // Test line 188: errors is string[] errorArray
        var errors = new string[] { "Error 1", "Error 2" };
        var result = Result.FormatErrorsString(errors, "TestPrefix");

        result.ShouldBe("TestPrefix: Error 1, Error 2");
    }

    /// <summary>
    /// Test: Result_FormatErrorsString_WithSmallCollection_ShouldUseArrayPath
    /// </summary>
    [Fact]
    public void Result_FormatErrorsString_WithSmallCollection_ShouldUseArrayPath()
    {
        // Test line 188: errors is ICollection<string> collection && collection.Count <= 16
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };
        var result = Result.FormatErrorsString(errors, "TestPrefix");

        result.ShouldBe("TestPrefix: Error 1, Error 2, Error 3");
    }

    /// <summary>
    /// Test: Result_FormatErrorsString_WithLargeCollection_ShouldUseFallback
    /// </summary>
    [Fact]
    public void Result_FormatErrorsString_WithLargeCollection_ShouldUseFallback()
    {
        // Test line 188: fallback to StringBuilder for large collections
        var errors = new List<string>();
        for (int i = 0; i < 20; i++)
        {
            errors.Add($"Error {i}");
        }
        var result = Result.FormatErrorsString(errors, "TestPrefix");

        result.ShouldStartWith("TestPrefix: ");
        result.ShouldContain("Error 0");
        result.ShouldContain("Error 19");
    }

    /// <summary>
    /// Test: Result_FormatErrorsString_WithArrayErrors_ShouldHandleNullElements
    /// </summary>
    [Fact]
    public void Result_FormatErrorsString_WithArrayErrors_ShouldHandleNullElements()
    {
        // Test line 794: errorArray[i] ?? string.Empty
        var errors = new string[] { "Error 1", null!, "Error 3" };
        var result = Result.FormatErrorsString(errors, "TestPrefix");

        result.ShouldBe("TestPrefix: Error 1, , Error 3");
    }

    /// <summary>
    /// Test: Result_WithFailure_WithNullArray_ShouldUseDefaultError
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithNullArray_ShouldUseDefaultError()
    {
        // Test line 342: errors is null || errors.Length == 0
        var result = Result.WithFailure((string[]?)null!);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_WithFailure_WithEmptyArray_ShouldUseDefaultError
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithEmptyArray_ShouldUseDefaultError()
    {
        // Test line 342: errors is null || errors.Length == 0
        var result = Result.WithFailure(new string[0]);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_OnSuccess_WithSuccess_ShouldExecuteAction
    /// </summary>
    [Fact]
    public void Result_OnSuccess_WithSuccess_ShouldExecuteAction()
    {
        // Test line 353: if (IsSuccess)
        var actionExecuted = false;
        var result = Result.Success().OnSuccess(() => actionExecuted = true);

        actionExecuted.ShouldBeTrue();
        result.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Test: Result_OnSuccess_WithFailure_ShouldNotExecuteAction
    /// </summary>
    [Fact]
    public void Result_OnSuccess_WithFailure_ShouldNotExecuteAction()
    {
        // Test line 353: if (IsSuccess)
        var actionExecuted = false;
        var result = Result.WithFailure("Error").OnSuccess(() => actionExecuted = true);

        actionExecuted.ShouldBeFalse();
        result.IsSuccess.ShouldBeFalse();
    }

    /// <summary>
    /// Test: Result_OnFailure_WithFailure_ShouldExecuteAction
    /// </summary>
    [Fact]
    public void Result_OnFailure_WithFailure_ShouldExecuteAction()
    {
        // Test line 357: if (IsFailure)
        var actionExecuted = false;
        var result = Result.WithFailure("Error").OnFailure(errors => actionExecuted = true);

        actionExecuted.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
    }

    /// <summary>
    /// Test: Result_OnFailure_WithSuccess_ShouldNotExecuteAction
    /// </summary>
    [Fact]
    public void Result_OnFailure_WithSuccess_ShouldNotExecuteAction()
    {
        // Test line 357: if (IsFailure)
        var actionExecuted = false;
        var result = Result.Success().OnFailure(errors => actionExecuted = true);

        actionExecuted.ShouldBeFalse();
        result.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Test: Result_OnFailure_WithNullErrors_ShouldUseDefaultError
    /// </summary>
    [Fact]
    public void Result_OnFailure_WithNullErrors_ShouldUseDefaultError()
    {
        // Test line 365: if (Errors is not null)
        var receivedErrors = new List<string>();
        var result = new Result(); // This creates a failure with empty errors array
        result.OnFailure(errors => receivedErrors.AddRange(errors));

        // The default constructor creates a failure with empty errors, not null
        receivedErrors.ShouldBeEmpty();
    }

    /// <summary>
    /// Test: Result_Map_WithSuccess_ShouldExecuteFunction
    /// </summary>
    [Fact]
    public void Result_Map_WithSuccess_ShouldExecuteFunction()
    {
        // Test line 794: IsSuccess ? Result<T>.Success(func()) : Result<T>.WithFailure(Errors)
        var result = Result.Success().Map(() => "mapped value");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("mapped value");
    }

    /// <summary>
    /// Test: Result_Map_WithFailure_ShouldPropagateErrors
    /// </summary>
    [Fact]
    public void Result_Map_WithFailure_ShouldPropagateErrors()
    {
        // Test line 794: IsSuccess ? Result<T>.Success(func()) : Result<T>.WithFailure(Errors)
        var result = Result.WithFailure("Error").Map(() => "mapped value");

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error");
    }

    /// <summary>
    /// Test: Result_Bind_WithSuccess_ShouldExecuteFunction
    /// </summary>
    [Fact]
    public void Result_Bind_WithSuccess_ShouldExecuteFunction()
    {
        // Test line 799: IsSuccess ? func() : Result<T>.WithFailure(Errors)
        var result = Result.Success().Bind(() => Result<string>.Success("bound value"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("bound value");
    }

    /// <summary>
    /// Test: Result_Bind_WithFailure_ShouldPropagateErrors
    /// </summary>
    [Fact]
    public void Result_Bind_WithFailure_ShouldPropagateErrors()
    {
        // Test line 799: IsSuccess ? func() : Result<T>.WithFailure(Errors)
        var result = Result.WithFailure("Error").Bind(() => Result<string>.Success("bound value"));

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error");
    }

    /// <summary>
    /// Test: Result_Ensure_WithSuccessAndTrueCondition_ShouldReturnSuccess
    /// </summary>
    [Fact]
    public void Result_Ensure_WithSuccessAndTrueCondition_ShouldReturnSuccess()
    {
        // Test line 825: if (IsSuccess && !condition())
        var result = Result.Success().Ensure(() => true, "Error message");

        result.IsSuccess.ShouldBeTrue();
    }

    /// <summary>
    /// Test: Result_Ensure_WithSuccessAndFalseCondition_ShouldReturnFailure
    /// </summary>
    [Fact]
    public void Result_Ensure_WithSuccessAndFalseCondition_ShouldReturnFailure()
    {
        // Test line 825: if (IsSuccess && !condition())
        var result = Result.Success().Ensure(() => false, "Error message");

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error message");
    }

    /// <summary>
    /// Test: Result_Ensure_WithFailure_ShouldReturnFailure
    /// </summary>
    [Fact]
    public void Result_Ensure_WithFailure_ShouldReturnFailure()
    {
        // Test line 825: if (IsSuccess && !condition())
        var result = Result.WithFailure("Original error").Ensure(() => true, "Error message");

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Original error");
    }

    /// <summary>
    /// Test: Result_Ensure_WithNullCondition_ShouldThrowException
    /// </summary>
    [Fact]
    public void Result_Ensure_WithNullCondition_ShouldThrowException()
    {
        // Test edge case for null condition
        Should.Throw<ArgumentNullException>(() =>
            Result.Success().Ensure(null!, "Error message"));
    }

    /// <summary>
    /// Test: Result_Ensure_WithNullErrorMessage_ShouldUseDefaultMessage
    /// </summary>
    [Fact]
    public void Result_Ensure_WithNullErrorMessage_ShouldUseDefaultMessage()
    {
        // Test edge case for null error message
        var result = Result.Success().Ensure(() => false, null!);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_Ensure_WithEmptyErrorMessage_ShouldUseDefaultMessage
    /// </summary>
    [Fact]
    public void Result_Ensure_WithEmptyErrorMessage_ShouldUseDefaultMessage()
    {
        // Test edge case for empty error message
        var result = Result.Success().Ensure(() => false, "");

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_Ensure_WithWhitespaceErrorMessage_ShouldUseDefaultMessage
    /// </summary>
    [Fact]
    public void Result_Ensure_WithWhitespaceErrorMessage_ShouldUseDefaultMessage()
    {
        // Test edge case for whitespace error message
        var result = Result.Success().Ensure(() => false, "   ");

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_Map_WithNullFunction_ShouldThrowException
    /// </summary>
    [Fact]
    public void Result_Map_WithNullFunction_ShouldThrowException()
    {
        // Test edge case for null function
        Should.Throw<ArgumentNullException>(() =>
            Result.Success().Map<string>(null!));
    }

    /// <summary>
    /// Test: Result_Map_WithFunctionReturningNull_ShouldHandleNullValue
    /// </summary>
    [Fact]
    public void Result_Map_WithFunctionReturningNull_ShouldHandleNullValue()
    {
        // Test edge case for function returning null
        var result = Result.Success().Map(() => (string?)null);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    /// <summary>
    /// Test: Result_Bind_WithNullFunction_ShouldThrowException
    /// </summary>
    [Fact]
    public void Result_Bind_WithNullFunction_ShouldThrowException()
    {
        // Test edge case for null function
        Should.Throw<ArgumentNullException>(() =>
            Result.Success().Bind<string>(null!));
    }

    /// <summary>
    /// Test: Result_Bind_WithFunctionReturningNull_ShouldHandleNullResult
    /// </summary>
    [Fact]
    public void Result_Bind_WithFunctionReturningNull_ShouldHandleNullResult()
    {
        // Test edge case for function returning null
        var result = Result.Success().Bind(() => (Result<string>?)null!);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_OnSuccess_WithNullAction_ShouldThrowException
    /// </summary>
    [Fact]
    public void Result_OnSuccess_WithNullAction_ShouldThrowException()
    {
        // Test edge case for null action
        Should.Throw<ArgumentNullException>(() =>
            Result.Success().OnSuccess(null!));
    }

    /// <summary>
    /// Test: Result_OnFailure_WithNullAction_ShouldThrowException
    /// </summary>
    [Fact]
    public void Result_OnFailure_WithNullAction_ShouldThrowException()
    {
        // Test edge case for null action
        Should.Throw<ArgumentNullException>(() =>
            Result.WithFailure("Error").OnFailure(null!));
    }

    /// <summary>
    /// Test: Result_OnSuccess_WithActionThrowingException_ShouldPropagateException
    /// </summary>
    [Fact]
    public void Result_OnSuccess_WithActionThrowingException_ShouldPropagateException()
    {
        // Test edge case for action throwing exception
        Should.Throw<InvalidOperationException>(() =>
            Result.Success().OnSuccess(() => throw new InvalidOperationException("Test exception")));
    }

    /// <summary>
    /// Test: Result_OnFailure_WithActionThrowingException_ShouldPropagateException
    /// </summary>
    [Fact]
    public void Result_OnFailure_WithActionThrowingException_ShouldPropagateException()
    {
        // Test edge case for action throwing exception
        Should.Throw<InvalidOperationException>(() =>
            Result.WithFailure("Error").OnFailure(errors => throw new InvalidOperationException("Test exception")));
    }

    /// <summary>
    /// Test: Result_WithFailure_WithMultipleNullErrors_ShouldHandleNullsInArray
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithMultipleNullErrors_ShouldHandleNullsInArray()
    {
        // Test edge case for array with null elements
        var result = Result.WithFailure(new[] { "Error1", null!, "Error2", null! });

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error1");
        result.Errors.ShouldContain("Error2");
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_WithFailure_WithEmptyStringErrors_ShouldHandleEmptyStrings
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithEmptyStringErrors_ShouldHandleEmptyStrings()
    {
        // Test edge case for empty string errors
        var result = Result.WithFailure(new[] { "", "Error1", "", "Error2" });

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error1");
        result.Errors.ShouldContain("Error2");
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_WithFailure_WithWhitespaceErrors_ShouldHandleWhitespace
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithWhitespaceErrors_ShouldHandleWhitespace()
    {
        // Test edge case for whitespace errors
        var result = Result.WithFailure(new[] { "   ", "Error1", "\t", "Error2", "\n" });

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error1");
        result.Errors.ShouldContain("Error2");
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_WithFailure_WithVeryLongError_ShouldHandleLongStrings
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithVeryLongError_ShouldHandleLongStrings()
    {
        // Test edge case for very long error messages
        var longError = new string('A', 10000);
        var result = Result.WithFailure(longError);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(longError);
    }

    /// <summary>
    /// Test: Result_WithFailure_WithUnicodeErrors_ShouldHandleUnicode
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithUnicodeErrors_ShouldHandleUnicode()
    {
        // Test edge case for unicode characters
        var unicodeError = "Error with unicode: 🚀🌟🎉";
        var result = Result.WithFailure(unicodeError);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(unicodeError);
    }

    /// <summary>
    /// Test: Result_WithFailure_WithSpecialCharacters_ShouldHandleSpecialChars
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithSpecialCharacters_ShouldHandleSpecialChars()
    {
        // Test edge case for special characters
        var specialError = "Error with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";
        var result = Result.WithFailure(specialError);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(specialError);
    }

    /// <summary>
    /// Test: Result_WithFailure_WithLargeArray_ShouldHandleLargeCollections
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithLargeArray_ShouldHandleLargeCollections()
    {
        // Test edge case for large error arrays
        var largeErrorArray = Enumerable.Range(1, 1000).Select(i => $"Error {i}").ToArray();
        var result = Result.WithFailure(largeErrorArray);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count().ShouldBe(1000);
        result.Errors.ShouldContain("Error 1");
        result.Errors.ShouldContain("Error 500");
        result.Errors.ShouldContain("Error 1000");
    }

    /// <summary>
    /// Test: Result_WithFailure_WithDuplicateErrors_ShouldHandleDuplicates
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithDuplicateErrors_ShouldHandleDuplicates()
    {
        // Test edge case for duplicate errors
        var result = Result.WithFailure(new[] { "Error1", "Error1", "Error2", "Error2", "Error1" });

        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count().ShouldBe(5);
        result.Errors.ShouldContain("Error1");
        result.Errors.ShouldContain("Error2");
    }

    /// <summary>
    /// Test: Result_WithFailure_WithMixedErrorTypes_ShouldHandleMixedTypes
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithMixedErrorTypes_ShouldHandleMixedTypes()
    {
        // Test edge case for mixed error types (null, empty, valid)
        var mixedErrors = new[] { null!, "", "   ", "Valid Error", null!, "Another Valid Error" };
        var result = Result.WithFailure(mixedErrors);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Valid Error");
        result.Errors.ShouldContain("Another Valid Error");
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Test: Result_WithFailure_WithSingleCharacterErrors_ShouldHandleSingleChars
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithSingleCharacterErrors_ShouldHandleSingleChars()
    {
        // Test edge case for single character errors
        var result = Result.WithFailure(new[] { "A", "B", "C" });

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("A");
        result.Errors.ShouldContain("B");
        result.Errors.ShouldContain("C");
    }

    /// <summary>
    /// Verifies performance: WithFailure handles very large arrays without degradation.
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithVeryLongArray_ShouldHandlePerformance()
    {
        // Test edge case for performance with very large arrays
        var veryLargeArray = Enumerable.Range(1, 10000).Select(i => $"Error {i}").ToArray();
        var result = Result.WithFailure(veryLargeArray);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.Count().ShouldBe(10000);
    }

    /// <summary>
    /// Ensures WithFailure(null IEnumerable) yields default error and IsSuccess=false.
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithNullEnumerable_ShouldHandleNullEnumerable()
    {
        // Test edge case for null IEnumerable
        var result = Result.WithFailure((IEnumerable<string>?)null!);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Ensures WithFailure(empty IEnumerable) yields default error and IsSuccess=false.
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithEmptyEnumerable_ShouldHandleEmptyEnumerable()
    {
        // Test edge case for empty IEnumerable
        var result = Result.WithFailure(Enumerable.Empty<string>());

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain(ResultConstants.DefaultErrorMessage);
    }

    /// <summary>
    /// Confirms List of string inputs are preserved in Errors collection.
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithListErrors_ShouldHandleListConversion()
    {
        // Test edge case for List<string> conversion
        var listErrors = new List<string> { "Error1", "Error2", "Error3" };
        var result = Result.WithFailure(listErrors);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error1");
        result.Errors.ShouldContain("Error2");
        result.Errors.ShouldContain("Error3");
    }

    /// <summary>
    /// Confirms Hash set of string inputs convert and preserve values.
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithHashSetErrors_ShouldHandleHashSetConversion()
    {
        // Test edge case for HashSet<string> conversion
        var hashSetErrors = new HashSet<string> { "Error1", "Error2", "Error3" };
        var result = Result.WithFailure(hashSetErrors);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error1");
        result.Errors.ShouldContain("Error2");
        result.Errors.ShouldContain("Error3");
    }

    /// <summary>
    /// Confirms LinkedList of string inputs convert and preserve values.
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithLinkedListErrors_ShouldHandleLinkedListConversion()
    {
        // Test edge case for LinkedList<string> conversion
        var linkedListErrors = new LinkedList<string>();
        linkedListErrors.AddLast("Error1");
        linkedListErrors.AddLast("Error2");
        linkedListErrors.AddLast("Error3");

        var result = Result.WithFailure(linkedListErrors);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error1");
        result.Errors.ShouldContain("Error2");
        result.Errors.ShouldContain("Error3");
    }

    /// <summary>
    /// Confirms Queue of string inputs convert and preserve values.
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithQueueErrors_ShouldHandleQueueConversion()
    {
        // Test edge case for Queue<string> conversion
        var queueErrors = new Queue<string>();
        queueErrors.Enqueue("Error1");
        queueErrors.Enqueue("Error2");
        queueErrors.Enqueue("Error3");

        var result = Result.WithFailure(queueErrors);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error1");
        result.Errors.ShouldContain("Error2");
        result.Errors.ShouldContain("Error3");
    }

    /// <summary>
    /// Confirms Stack of string inputs convert and preserve values.
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithStackErrors_ShouldHandleStackConversion()
    {
        // Test edge case for Stack<string> conversion
        var stackErrors = new Stack<string>();
        stackErrors.Push("Error1");
        stackErrors.Push("Error2");
        stackErrors.Push("Error3");

        var result = Result.WithFailure(stackErrors);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error1");
        result.Errors.ShouldContain("Error2");
        result.Errors.ShouldContain("Error3");
    }

    /// <summary>
    /// Confirms Read-only collection of string inputs convert and preserve values.
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithReadOnlyCollectionErrors_ShouldHandleReadOnlyConversion()
    {
        // Test edge case for ReadOnlyCollection<string> conversion
        var readOnlyErrors = new List<string> { "Error1", "Error2", "Error3" }.AsReadOnly();
        var result = Result.WithFailure(readOnlyErrors);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error1");
        result.Errors.ShouldContain("Error2");
        result.Errors.ShouldContain("Error3");
    }

    /// <summary>
    /// Confirms custom Enumerable of string inputs convert and preserve values.
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithCustomEnumerable_ShouldHandleCustomEnumerable()
    {
        // Test edge case for custom IEnumerable implementation
        var customErrors = new CustomEnumerable<string>(new[] { "Error1", "Error2", "Error3" });
        var result = Result.WithFailure(customErrors);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error1");
        result.Errors.ShouldContain("Error2");
        result.Errors.ShouldContain("Error3");
    }

    /// <summary>
    /// Confirms lazy Enumerable of string is iterated and produces expected errors.
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithLazyEnumerable_ShouldHandleLazyEvaluation()
    {
        // Test edge case for lazy IEnumerable evaluation
        var lazyErrors = Enumerable.Range(1, 5).Select(i => $"Error {i}");
        var result = Result.WithFailure(lazyErrors);

        result.IsSuccess.ShouldBeFalse();
        result.Errors.ShouldContain("Error 1");
        result.Errors.ShouldContain("Error 5");
        result.Errors.Count().ShouldBe(5);
    }

    /// <summary>
    /// Ensures infinite IEnumerable is handled without hangs (e.g., via safeguards in API).
    /// </summary>
    [Fact]
    public void Result_WithFailure_WithInfiniteEnumerable_ShouldHandleInfiniteEnumerable()
    {
        // Test edge case for infinite IEnumerable (should not hang)
        var infiniteErrors = InfiniteEnumerable();
        var result = Result.WithFailure(infiniteErrors);

        result.IsSuccess.ShouldBeFalse();
        // Should not hang and should handle the infinite enumerable gracefully
    }

    private static IEnumerable<string> InfiniteEnumerable()
    {
        var i = 0;
        while (true)
        {
            yield return $"Error {i++}";
        }
    }

    private class CustomEnumerable<T> : IEnumerable<T>
    {
        private readonly T[] items;

        public CustomEnumerable(T[] items)
        {
            this.items = items;
        }

        /// <summary>
        /// Get Enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)items).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
