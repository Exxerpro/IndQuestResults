# Implementation Plan: Exception Preservation Fixes

## Analysis Summary

### ✅ What's Working
- Generic `Result<T>` Try extensions correctly preserve exceptions
- Async extensions preserve exceptions
- Performance & cancellation extensions preserve exceptions

### ❌ What Needs Fixing
1. **Reactive/Observable Extensions** - 6 methods missing exception parameters
2. **Non-Generic Try Extensions** - Missing basic Try methods

---

## Implementation Plan

### Part 1: Fix Reactive Extensions (ResultObservableBridge.cs)

**File:** `Src/Code/src/IndQuestResults/Reactive/ResultObservableBridge.cs`

**Changes needed:**

1. **ResultBridgeObserver.OnNext** (line 435)
   - Current: `Result<T>.WithFailure($"Result handler error: {ex.Message}")`
   - Fix: `Result<T>.WithFailure($"Result handler error: {ex.Message}", default, ex)`

2. **ResultBridgeObserver.OnError** (line 441)
   - Current: `Result<T>.WithFailure($"Observable error: {error.Message}")`
   - Fix: `Result<T>.WithFailure($"Observable error: {error.Message}", default, error)`

3. **SelectResultObserver.OnNext** (line 480)
   - Current: `Result<TResult>.WithFailure($"SelectResult error: {ex.Message}")`
   - Fix: `Result<TResult>.WithFailure($"SelectResult error: {ex.Message}", default, ex)`

4. **SelectResultObserver.OnError** (line 486)
   - Current: `Result<TResult>.WithFailure($"Observable error: {error.Message}")`
   - Fix: `Result<TResult>.WithFailure($"Observable error: {error.Message}", default, error)`

5. **ReplayBridgeObserver.OnError** (line 555)
   - Current: `Result<T>.WithFailure($"Observable error: {error.Message}")`
   - Fix: `Result<T>.WithFailure($"Observable error: {error.Message}", default, error)`

6. **RouteResultsObserver.OnError** (line 525) - **CRITICAL FIX**
   - Current: `Result<string>.Success($"Stream error: {error.Message}")` ❌ Wrong!
   - Fix: `Result<string>.WithFailure($"Stream error: {error.Message}", default, error)`

**Impact:** Low - Reactive extensions are least used, but consistency is important.

---

### Part 2: Add Non-Generic Try Extensions (ResultTryExtensions.cs)

**File:** `Src/Code/src/IndQuestResults/Operations/ResultTryExtensions.cs`

**New methods to add:**

1. **`Try(Action action, Func<Exception, string> mapError)`**
   - Wraps an `Action` that may throw
   - Returns `Result.Success()` on success
   - Returns `Result.WithFailure(mapError(ex), ex)` on exception
   - Pattern matches generic `Try<T>` but for void operations

2. **`TryAsync(Func<Task> action, Func<Exception, string> mapError)`**
   - Wraps an async `Func<Task>` that may throw
   - Returns `Task<Result>` 
   - Returns `Result.Success()` on success
   - Returns `Result.WithFailure(mapError(ex), ex)` on exception
   - Pattern matches generic `TryAsync<T>` but for void operations

**Design decisions:**
- ✅ Keep it simple - just the two basic methods
- ✅ Match the pattern of generic Try extensions
- ✅ Use same parameter order and naming conventions
- ❌ Skip `MapTry`/`BindTry` equivalents - non-generic Result doesn't have Map/Bind
- ❌ Skip `TapTry` - Tap is for side effects, less useful for Try pattern

**Usage examples:**
```csharp
// Synchronous void operation
var result = ResultTryExtensions.Try(
    () => DoSomethingThatMayThrow(),
    ex => $"Operation failed: {ex.Message}"
);

// Async void operation
var result = await ResultTryExtensions.TryAsync(
    async () => await DoSomethingAsyncThatMayThrow(),
    ex => $"Async operation failed: {ex.Message}"
);
```

---

### Part 3: Add Unit Tests

**File:** `Src/Code/tests/IndQuestResults.Tests.Unit/Operations/ResultTryExtensionsTests.cs`

**New test methods:**

1. `Try_NonGeneric_Catches_Exception_MapsError()`
   - Verify exception is caught and mapped
   - Verify exception is preserved in Exception property
   - Verify IsFaulted is true

2. `Try_NonGeneric_Success_ReturnsSuccess()`
   - Verify successful Action returns Success result

3. `TryAsync_NonGeneric_Catches_Exception_MapsError()`
   - Verify async exception is caught and mapped
   - Verify exception is preserved in Exception property
   - Verify IsFaulted is true

4. `TryAsync_NonGeneric_Success_ReturnsSuccess()`
   - Verify successful async Action returns Success result

**Test pattern:** Follow existing test structure in the file.

---

## Implementation Order

1. ✅ Fix reactive extensions (6 methods)
2. ✅ Add non-generic Try extensions (2 methods)
3. ✅ Add unit tests (4 test methods)
4. ✅ Run tests to verify
5. ✅ Check for any linter errors

---

## Risk Assessment

**Low Risk:**
- Reactive extensions are least used
- Non-generic Try extensions are additive (new functionality)
- All changes follow existing patterns
- Backward compatible (no breaking changes)

**Testing:**
- Existing tests should continue to pass
- New tests verify new functionality
- No changes to existing public APIs

---

## Consistency Check

All exception handling will follow the same pattern:
```csharp
catch (Exception ex)
{
    return Result<T>.WithFailure(mapError(ex), default, ex); // Generic
    return Result.WithFailure(mapError(ex), ex);              // Non-generic
}
```

This ensures:
- ✅ Full stack trace preservation
- ✅ Exception type information
- ✅ Inner exceptions
- ✅ Consistent behavior across all Result operations

