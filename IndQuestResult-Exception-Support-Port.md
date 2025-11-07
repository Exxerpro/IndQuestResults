# IndQuestResult Exception Support Port Plan

## Overview

This document outlines the changes needed to port exception support (`IsFaulted` and `Exception?` properties) from the minimal `IndFusion.Analyzer.Operations.Result` implementation to the main `IndQuestResult` library.

## Session Summary - Railway-Oriented Programming Refactoring

### What We Accomplished Today

**Date:** Current Session  
**Branch:** `Kat3`  
**Commit:** `refactor: align extension methods with railway-oriented programming patterns`

#### Completed Work:

1. **Comprehensive ROP Audit & Refactoring**
   - Audited all extension methods across the codebase for ROP violations
   - Refactored `ResultExtensions.cs`: `ThenAsync`, `ThenMap`, `ThenTap`, `ThenDo`, `ThenValidate`, `ThenValidateAsync`, `ThenEnsure`, `ThenSwitch`, `ThenLogErrors` to use `Map`/`Bind`/`Tap` patterns
   - Refactored `ResultAsync.cs`: Updated `BindAsync` to follow ROP principles
   - Refactored `ResultLinqExtensions.cs`: `SelectMany` now uses `Bind` and `Map`
   - Refactored `ResultTryExtensions.cs`: `MapTry` and `BindTry` use `Bind` pattern

2. **Error Handling Standardization**
   - Replaced all `WithFailure` calls with `Failure` method for consistency
   - Updated error handling to return `Result.Failure` instead of throwing exceptions (ROP-aligned behavior)
   - All extension methods now follow railway-oriented programming: automatic error propagation through monadic chains

3. **Test Updates**
   - Updated tests to expect `Result.Failure` instead of exceptions (ROP-aligned behavior)
   - Fixed `ResultMutationKillerTests` to match new constructor signature
   - Updated async extension tests to align with new ROP behavior

4. **Code Quality**
   - All extension methods now use railway-oriented programming patterns
   - Eliminated explicit `IsSuccess`/`IsFailure` checks in favor of monadic composition
   - Improved code maintainability and consistency

**Files Changed:** 20 files (1,481 insertions, 236 deletions)  
**Status:** Committed and pushed to `origin/Kat3` branch

### What Follows - Next Steps

#### 1. Code Review
- [ ] **Careful code review** of all ROP refactoring changes
- [ ] Review for any edge cases or missed scenarios
- [ ] Verify all monadic patterns are correctly implemented
- [ ] Check for any performance regressions

#### 2. Test Bed Expansion
- [ ] **Widen the test bed** - Add more comprehensive test coverage
- [ ] Test all ROP refactored methods with various edge cases
- [ ] Add integration tests for complex ROP chains
- [ ] Verify backward compatibility with existing code

#### 3. Regression Testing
- [ ] **Full regression test suite** execution
- [ ] Run all 925+ tests across both projects (main library + analyzers)
- [ ] Fix any remaining test failures (currently 4 failures reported)
- [ ] Verify analyzer tests pass (2 failures need investigation)
- [ ] Performance regression testing

#### 4. Analyzer Impact Study
- [ ] **Impact study on the analyzer** - Critical for next steps
- [ ] Analyze how ROP refactoring affects analyzer diagnostics
- [ ] Review `PreferResultAsyncAnalyzer` - currently has 2 failing tests:
  - `ThenAsync_OnTaskResult_ProducesDiagnostic` - Analyzer test
  - `CodeFix_Rewrites_ThenAsync_To_ResultAsync_BindAsync` - Code fix test
- [ ] Determine if analyzer logic needs updates for new ROP patterns
- [ ] Verify analyzer still correctly identifies ROP violations
- [ ] Update analyzer to recognize new patterns if needed

#### 5. Documentation Updates
- [ ] **Update manual** with ROP patterns and best practices
- [ ] Document the new ROP-aligned extension methods
- [ ] Add examples of proper ROP usage
- [ ] Update API documentation

#### 6. Version & Release
- [ ] **Update version** number (semantic versioning)
- [ ] **Publish NuGet package** with ROP refactoring
- [ ] Create release notes highlighting ROP improvements
- [ ] Tag release in git

### Known Issues to Address

1. **Test Failures (4 total):**
   - 2 failures in analyzer tests (need impact study)
   - 2 failures in main library tests (need investigation)

2. **Analyzer Compatibility:**
   - Analyzer may need updates to work with new ROP patterns
   - Code fix providers may need adjustment for new method signatures

### Notes

- All changes maintain backward compatibility
- ROP refactoring improves code quality and maintainability
- Exception support port (this document) should be done after ROP refactoring is fully tested and released

## Background

- **Current Implementation**: `IndFusion.Analyzer.Operations.Result` is a minimal implementation for .NET Core 2.0 support
- **Target Implementation**: `IndQuestResult` library (in separate repo) is much richer and feature-complete
- **Goal**: Port exception support to the main library and update the NuGet package

## Changes to Port

### 1. Add Exception Properties to Result Classes

#### For `Result` (non-generic):
```csharp
/// <summary>
/// Gets a value indicating whether the result was created from an exception (excluding cancellation).
/// </summary>
public bool IsFaulted { get; }

/// <summary>
/// Gets the exception that caused the failure, if any.
/// Contains the full stack trace for non-cancelled exceptions.
/// </summary>
public Exception? Exception { get; }
```

#### For `Result<T>` (generic):
```csharp
/// <summary>
/// Gets a value indicating whether the result was created from an exception (excluding cancellation).
/// </summary>
public bool IsFaulted { get; }

/// <summary>
/// Gets the exception that caused the failure, if any.
/// Contains the full stack trace for non-cancelled exceptions.
/// </summary>
public Exception? Exception { get; }
```

### 2. Update Constructors

#### For `Result`:
- Add optional `Exception? exception = null` parameter to private constructor
- Initialize `Exception` property
- Initialize `IsFaulted` property: `IsFaulted = exception is not null && exception is not OperationCanceledException`

#### For `Result<T>`:
- Add optional `Exception? exception = null` parameter to all constructors
- Initialize `Exception` property
- Initialize `IsFaulted` property: `IsFaulted = exception is not null && exception is not OperationCanceledException`

### 3. Update Factory Methods

#### For `Result`:
- Update `WithFailure(IEnumerable<string> errors)` → `WithFailure(IEnumerable<string> errors, Exception? exception = null)`
- Update `WithFailure(string[] errors)` → `WithFailure(string[] errors, Exception? exception = null)`
- Update `WithFailure(string error)` → `WithFailure(string error, Exception? exception = null)`
- Add new `WithFailure(Exception exception)` overload

#### For `Result<T>`:
- Update all `WithFailure` overloads to accept optional `Exception? exception = null` parameter
- Add new `WithFailure(Exception exception, T? value = default)` overload

### 4. Replace Validation Throws with Result Returns

#### In `ResultAsync.cs` and all async extension methods:
- Replace `ArgumentNullException.ThrowIfNull(param)` with:
  ```csharp
  if (param is null)
  {
      return Result<T>.WithFailure("Parameter cannot be null");
  }
  ```

#### In all factory methods and constructors:
- Replace `throw new ArgumentException(...)` with:
  ```csharp
  return Result<T>.WithFailure("Error message");
  ```

### 5. Update Exception Handling in Async Methods

#### In all `ResultAsync` methods:
- Update catch blocks to pass exception to `WithFailure`:
  ```csharp
  catch (Exception ex)
  {
      return Result<T>.WithFailure($"Operation failed: {ex.Message}", default, ex);
  }
  ```

### 6. Update ResultAsync.cs

- Replace all `ArgumentNullException.ThrowIfNull` calls with null checks returning `Result<T>.WithFailure`
- Update all catch blocks to use `Result<T>.WithFailure(message, default, ex)` to preserve exceptions

## Implementation Checklist

- [ ] Add `IsFaulted` and `Exception?` properties to `Result` class
- [ ] Add `IsFaulted` and `Exception?` properties to `Result<T>` class
- [ ] Update `Result` constructors to accept `Exception?` parameter
- [ ] Update `Result<T>` constructors to accept `Exception?` parameter
- [ ] Update all `Result.WithFailure` methods to accept `Exception?` parameter
- [ ] Update all `Result<T>.WithFailure` methods to accept `Exception?` parameter
- [ ] Add `Result.WithFailure(Exception)` overload
- [ ] Add `Result<T>.WithFailure(Exception, T?)` overload
- [ ] Replace `ArgumentNullException.ThrowIfNull` with null checks in `ResultAsync`
- [ ] Update all catch blocks in `ResultAsync` to pass exceptions to `WithFailure`
- [ ] Update any other validation throws to return `Result<T>.WithFailure`
- [ ] Add unit tests for `IsFaulted` property
- [ ] Add unit tests for `Exception` property with stack trace
- [ ] Verify exception stack traces are preserved
- [ ] Update NuGet package version
- [ ] Update package documentation

## Testing Requirements

### Unit Tests Needed:
1. **IsFaulted Property Tests**:
   - `IsFaulted_ShouldBeTrue_WhenCreatedFromException`
   - `IsFaulted_ShouldBeFalse_WhenCreatedFromOperationCanceledException`
   - `IsFaulted_ShouldBeFalse_WhenCreatedFromValidationFailure`

2. **Exception Property Tests**:
   - `Exception_ShouldContainFullStackTrace_WhenCreatedFromException`
   - `Exception_ShouldBeNull_WhenCreatedFromValidationFailure`
   - `Exception_ShouldBeNull_WhenCreatedFromOperationCanceledException`

3. **WithFailure(Exception) Overload Tests**:
   - `WithFailure_Exception_ShouldSetIsFaultedTrue`
   - `WithFailure_Exception_ShouldPreserveStackTrace`
   - `WithFailure_OperationCanceledException_ShouldNotSetIsFaulted`

## Migration Notes

- The changes are **backward compatible** - all existing code will continue to work
- The `Exception?` parameter is optional in all methods
- Existing `WithFailure` calls without exception parameter will work as before
- New code can optionally pass exceptions to preserve stack traces

## Reference Implementation

See the changes in:
- `ExxerRules/src/code/Analyzer/IndFusion.Analyzer/Operations/Result.cs`
- `ExxerRules/ResultAsync.cs` (if it still exists in the repo)

These files contain the complete implementation that needs to be ported to `IndQuestResult`.

## Key Implementation Details

### IsFaulted Logic
```csharp
IsFaulted = exception is not null && exception is not OperationCanceledException
```

This ensures:
- Cancellation exceptions don't set `IsFaulted = true` (they use `IsCancelled()` instead)
- Only actual faults (non-cancellation exceptions) set `IsFaulted = true`
- Validation failures (no exception) have `IsFaulted = false`

### Exception Preservation
When creating a failure from an exception:
```csharp
public static Result<T> WithFailure(Exception exception, T? value = default)
{
    if (exception is null)
    {
        return new Result<T>(false, [ResultConstants.DefaultErrorMessage], value, null);
    }

    var errorMessage = exception is OperationCanceledException
        ? ResultErrors.OperationCancelled
        : $"{exception.GetType().Name}: {exception.Message}";

    return new Result<T>(false, [errorMessage], value, exception);
}
```

This preserves:
- Full exception object with stack trace
- Exception type information
- Inner exceptions
- All exception properties

