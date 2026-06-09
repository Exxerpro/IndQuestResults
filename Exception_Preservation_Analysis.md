# Exception Preservation Analysis Report

## Executive Summary

✅ **Generic Result<T> Try Extensions**: **PRESERVING** exceptions correctly  
✅ **Async Extensions**: **PRESERVING** exceptions correctly  
❌ **Reactive/Observable Extensions**: **NOT PRESERVING** exceptions (needs fixes)  
⚠️ **Non-Generic Result Try**: No methods found (not an issue)

---

## ✅ Methods CORRECTLY Preserving Exceptions

### 1. Generic Result<T> Try Extensions (`ResultTryExtensions.cs`)

All methods correctly pass the exception as the third parameter to `WithFailure`:

```36:36:Src/Code/src/IndQuestResults/Operations/ResultTryExtensions.cs
            return Result<T>.WithFailure(mapError(ex), default, ex);
```

**Methods verified:**
- ✅ `Try<T>` (line 36) - ✅ Preserves exception
- ✅ `TryAsync<T>` (line 66) - ✅ Preserves exception  
- ✅ `MapTry<T, TOut>` (line 95) - ✅ Preserves exception
- ✅ `BindTry<T, TOut>` (line 125) - ✅ Preserves exception

**Test Coverage:** Confirmed by unit tests in `ResultTryExtensionsTests.cs` (lines 76-89, 92-109, 112+)

---

### 2. Async Extensions (`ResultAsync.cs`)

All async methods correctly preserve exceptions:

```78:78:Src/Code/src/IndQuestResults/Async/ResultAsync.cs
            return Result<IEnumerable<T>>.Failure($"Async sequence operation failed: {ex.Message}", default, ex);
```

**Methods verified:**
- ✅ `SequenceAsync<T>` (line 78) - ✅ Preserves exception
- ✅ `TraverseAsync<TIn, TOut>` (lines 123, 144) - ✅ Preserves exception

---

### 3. Performance & Cancellation Extensions

**ResultMetrics.cs:**
- ✅ `TimedWithMetrics<T>` (line 94) - ✅ Preserves exception
- ✅ `TimedWithMetricsAsync<T>` (line 167) - ✅ Preserves exception

**CancellationAwareResult.cs:**
- ✅ `WrapCancellationAware<T>` (line 44) - ✅ Preserves exception

---

## ❌ Methods NOT Preserving Exceptions (NEEDS FIXES)

### Reactive/Observable Extensions (`ResultObservableBridge.cs`)

**Critical Issue:** Multiple reactive extension methods are **NOT** passing exceptions to `WithFailure`, losing stack trace information.

#### 1. `ResultBridgeObserver.OnNext` (Line 435)
```433:436:Src/Code/src/IndQuestResults/Reactive/ResultObservableBridge.cs
        catch (Exception ex)
        {
            _onNext(Result<T>.WithFailure($"Result handler error: {ex.Message}"));
        }
```
**Problem:** Missing exception parameter - should be:
```csharp
_onNext(Result<T>.WithFailure($"Result handler error: {ex.Message}", default, ex));
```

#### 2. `ResultBridgeObserver.OnError` (Line 441)
```439:442:Src/Code/src/IndQuestResults/Reactive/ResultObservableBridge.cs
    public void OnError(Exception error)
    {
        _onNext(Result<T>.WithFailure($"Observable error: {error.Message}"));
    }
```
**Problem:** Missing exception parameter - should be:
```csharp
_onNext(Result<T>.WithFailure($"Observable error: {error.Message}", default, error));
```

#### 3. `SelectResultObserver.OnNext` (Line 480)
```478:481:Src/Code/src/IndQuestResults/Reactive/ResultObservableBridge.cs
        catch (Exception ex)
        {
            _onNext(Result<TResult>.WithFailure($"SelectResult error: {ex.Message}"));
        }
```
**Problem:** Missing exception parameter - should be:
```csharp
_onNext(Result<TResult>.WithFailure($"SelectResult error: {ex.Message}", default, ex));
```

#### 4. `SelectResultObserver.OnError` (Line 486)
```484:487:Src/Code/src/IndQuestResults/Reactive/ResultObservableBridge.cs
    public void OnError(Exception error)
    {
        _onNext(Result<TResult>.WithFailure($"Observable error: {error.Message}"));
    }
```
**Problem:** Missing exception parameter - should be:
```csharp
_onNext(Result<TResult>.WithFailure($"Observable error: {error.Message}", default, error));
```

#### 5. `ReplayBridgeObserver.OnError` (Line 555)
```553:556:Src/Code/src/IndQuestResults/Reactive/ResultObservableBridge.cs
    public void OnError(Exception error)
    {
        _subject.OnNext(Result<T>.WithFailure($"Observable error: {error.Message}"));
    }
```
**Problem:** Missing exception parameter - should be:
```csharp
_subject.OnNext(Result<T>.WithFailure($"Observable error: {error.Message}", default, error));
```

#### 6. `RouteResultsObserver.OnError` (Line 525)
```523:526:Src/Code/src/IndQuestResults/Reactive/ResultObservableBridge.cs
    public void OnError(Exception error)
    {
        _failureSubject.OnNext(Result<string>.Success($"Stream error: {error.Message}"));
    }
```
**Problem:** This is creating a **Success** result with an error message instead of a Failure! Should be:
```csharp
_failureSubject.OnNext(Result<string>.WithFailure($"Stream error: {error.Message}", default, error));
```

---

## How Exception Preservation Works

### Exception Storage in Result Classes

Both `Result` and `Result<T>` store exceptions in their constructors:

```40:50:Src/Code/src/IndQuestResults/ResultGeneric.cs
    public Result(bool isSuccess, IEnumerable<string>? errors, T? value = default, Exception? exception = null)
    {
        IsRecoverable = isSuccess;
        var errorArray = errors?.ToArray() ?? [];
        HasErrors = errorArray.Length > 0;
        Errors = errorArray;
        Value = value;
        Warnings = [];
        Confidence = 1.0;
        Exception = exception;
        IsFaulted = exception is not null and not OperationCanceledException;
```

The `Exception` property preserves:
- ✅ Full stack trace
- ✅ Exception type information  
- ✅ Inner exceptions
- ✅ All exception properties

### WithFailure Method Signature

```349:352:Src/Code/src/IndQuestResults/ResultGeneric.cs
    public static Result<T> WithFailure(string error, T? value = default, Exception? exception = null)
    {
        return new Result<T>(false, [error], value, exception);
    }
```

**Correct usage pattern:**
```csharp
catch (Exception ex)
{
    return Result<T>.WithFailure("Error message", default, ex); // ✅ Exception preserved
}
```

---

## Recommendations

### Immediate Fixes Required

1. **Fix all reactive extension methods** in `ResultObservableBridge.cs`:
   - Add exception parameter to all `WithFailure` calls
   - Fix `RouteResultsObserver.OnError` to create a Failure instead of Success

2. **Add unit tests** for reactive extensions to verify exception preservation

3. **Consider adding non-generic Result Try methods** if needed:
   ```csharp
   public static Result Try(Action action, Func<Exception, string> mapError)
   {
       try
       {
           action();
           return Result.Success();
       }
       catch (Exception ex)
       {
           return Result.WithFailure(mapError(ex), ex); // ✅ Preserves exception
       }
   }
   ```

---

## Test Coverage

✅ **Existing Tests:** `ResultTryExtensionsTests.cs` confirms exception preservation for:
- `Try<T>`
- `TryAsync<T>`  
- `MapTry<T, TOut>`
- `BindTry<T, TOut>`

❌ **Missing Tests:** No tests for reactive extension exception preservation

---

## Summary Table

| Category | Methods | Exception Preservation | Status |
|----------|---------|------------------------|--------|
| Generic Try Extensions | `Try<T>`, `TryAsync<T>`, `MapTry`, `BindTry` | ✅ Yes | ✅ Correct |
| Async Extensions | `SequenceAsync`, `TraverseAsync` | ✅ Yes | ✅ Correct |
| Performance Extensions | `TimedWithMetrics`, `TimedWithMetricsAsync` | ✅ Yes | ✅ Correct |
| Cancellation Extensions | `WrapCancellationAware` | ✅ Yes | ✅ Correct |
| Reactive Extensions | `ResultBridgeObserver`, `SelectResultObserver`, etc. | ❌ No | ❌ **NEEDS FIX** |
| Non-Generic Try | N/A | N/A | ⚠️ Not implemented |

---

## Conclusion

**Good News:** Your core Try extensions and async operations are correctly preserving exceptions and stack traces! ✅

**Action Required:** Fix the reactive/observable extensions to preserve exceptions for consistent behavior across all Result operations.

