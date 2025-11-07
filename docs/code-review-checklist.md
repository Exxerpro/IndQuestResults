# Code Review Checklist for ROP Compliance, Exception Handling, and Null Parameter Validation

This checklist ensures all code changes maintain Railway-Oriented Programming (ROP) principles, proper exception handling, and consistent null parameter validation.

## ROP Compliance

### ✅ Null Parameter Handling
- [ ] **No `ArgumentNullException.ThrowIfNull`** - All null checks return `Result` failures
- [ ] **Early return pattern** - Null checks use early returns with braces
- [ ] **Consistent error messages** - Null parameter errors follow pattern: `"{ParameterName} cannot be null"`
- [ ] **No-op implementations** - Methods returning `IDisposable` return `NoOpDisposable.Instance` for null parameters
- [ ] **Reactive patterns** - Null handlers return no-op actions/functions

### ✅ Exception Handling
- [ ] **No exceptions for control flow** - Methods return `Result` failures instead of throwing
- [ ] **Exception preservation** - All catch blocks pass exception to `WithFailure(ex)`
- [ ] **Stack trace preservation** - Exceptions are preserved with full stack traces
- [ ] **OperationCanceledException handling** - Cancellation exceptions set `IsFaulted = false`
- [ ] **Inner exception preservation** - Inner exceptions are preserved in exception chain

### ✅ Functional Composition
- [ ] **Chainable operations** - All operations can be chained without try-catch
- [ ] **Error propagation** - Errors automatically propagate through chains
- [ ] **No side effects in chains** - Side effects use `Tap` methods
- [ ] **Immutable results** - Result objects are immutable after creation

## Exception Handling

### ✅ Exception Preservation
- [ ] **All catch blocks preserve exceptions** - `catch (Exception ex)` passes `ex` to `WithFailure`
- [ ] **Async methods preserve exceptions** - Async catch blocks include exception parameter
- [ ] **Exception type preserved** - Original exception type is maintained
- [ ] **Stack trace available** - `result.Exception.StackTrace` is not null
- [ ] **Inner exceptions preserved** - `result.Exception.InnerException` is preserved

### ✅ Exception Classification
- [ ] **IsFaulted set correctly** - Regular exceptions set `IsFaulted = true`
- [ ] **Cancellation not a fault** - `OperationCanceledException` sets `IsFaulted = false`
- [ ] **Validation not a fault** - Validation failures have `IsFaulted = false`
- [ ] **Exception property set** - `result.Exception` is set when exception occurs

### ✅ Async Exception Handling
- [ ] **Async catch blocks** - All async methods have proper catch blocks
- [ ] **Exception in async chains** - Exceptions in async chains are preserved
- [ ] **Cancellation in async** - Cancellation tokens properly handled
- [ ] **Timeout handling** - Timeout exceptions properly classified

## Null Parameter Validation

### ✅ Extension Methods
- [ ] **Null result handling** - Extension methods check for null `result` parameter
- [ ] **Null delegate handling** - Extension methods check for null function/action parameters
- [ ] **Consistent pattern** - All null checks follow same pattern:
  ```csharp
  if (parameter is null) { return Result<T>.WithFailure("Parameter cannot be null"); }
  ```
- [ ] **Early returns** - Null checks use early return pattern with braces

### ✅ Static Methods
- [ ] **Null parameter checks** - Static methods validate all parameters
- [ ] **Result returns** - Null parameters return `Result` failures
- [ ] **No exceptions** - No `ArgumentNullException` thrown

### ✅ Reactive Patterns
- [ ] **No-op disposables** - Null handlers return `NoOpDisposable.Instance`
- [ ] **No-op actions** - Null actions return `_ => { }`
- [ ] **No-op functions** - Null async functions return `_ => Task.CompletedTask`
- [ ] **Safe disposal** - No-op disposables can be safely disposed multiple times

## Code Quality

### ✅ Code Style
- [ ] **Braces on if statements** - All if statements use braces (IDE0011)
- [ ] **Early return pattern** - Early returns used for null checks (IDE0046 suppressed with pragma)
- [ ] **Consistent formatting** - Code follows project formatting rules
- [ ] **No warnings** - Build passes with zero warnings

### ✅ Documentation
- [ ] **XML documentation** - Public APIs have XML documentation
- [ ] **Exception documentation** - Exception handling behavior documented
- [ ] **ROP examples** - Code examples show ROP patterns
- [ ] **Null handling documented** - Null parameter behavior documented

### ✅ Testing
- [ ] **Null parameter tests** - Tests verify null parameter handling
- [ ] **Exception preservation tests** - Tests verify exceptions are preserved
- [ ] **ROP compliance tests** - Tests verify ROP patterns
- [ ] **All tests passing** - All unit tests pass

## Performance

### ✅ Allocation Optimization
- [ ] **Span<T> usage** - Small collections use Span<T> optimizations
- [ ] **Stack allocation** - Small error collections use stack allocation
- [ ] **No unnecessary allocations** - No LINQ allocations in hot paths
- [ ] **String formatting** - Pre-calculated capacity for string formatting

## Security

### ✅ Input Validation
- [ ] **Null checks** - All inputs validated for null
- [ ] **Error messages** - Error messages don't leak sensitive data
- [ ] **Exception messages** - Exception messages sanitized if needed
- [ ] **Thread safety** - Concurrent operations are thread-safe

## Backward Compatibility

### ✅ API Compatibility
- [ ] **No breaking changes** - Existing APIs maintain backward compatibility
- [ ] **Deprecation strategy** - Old APIs marked as obsolete with migration path
- [ ] **Documentation updates** - Migration guides provided for breaking changes
- [ ] **Version bump** - Version bumped appropriately for changes

## Checklist Usage

### For Code Reviews
1. Review each section systematically
2. Check all applicable items
3. Document any exceptions or deviations
4. Ensure all tests pass before approval

### For New Code
1. Follow ROP principles from the start
2. Use this checklist during development
3. Run static analysis before committing
4. Update tests for new patterns

### For Refactoring
1. Verify backward compatibility
2. Update tests to match new behavior
3. Document behavior changes
4. Update Result-Manual.md if needed

## Common Issues to Watch For

### ❌ Anti-Patterns
- `ArgumentNullException.ThrowIfNull` in extension methods
- `throw new Exception()` for control flow
- Missing exception parameter in `WithFailure` calls
- No null checks in public methods
- Exceptions swallowed without preservation

### ✅ Correct Patterns
- Early return with `Result` failure for null parameters
- Exception preservation in all catch blocks
- No-op implementations for reactive patterns
- Consistent error messages
- Proper cancellation handling

## Examples

### ❌ Incorrect (Throws Exception)
```csharp
public static Result<TOut> Map<T, TOut>(this Result<T> result, Func<T, TOut> selector)
{
    ArgumentNullException.ThrowIfNull(result);
    ArgumentNullException.ThrowIfNull(selector);
    return result.IsSuccess
        ? Result<TOut>.Success(selector(result.Value!))
        : Result<TOut>.WithFailure(result.Errors);
}
```

### ✅ Correct (ROP-Compliant)
```csharp
public static Result<TOut> Map<T, TOut>(this Result<T> result, Func<T, TOut> selector)
{
#pragma warning disable IDE0046 // Convert to conditional expression - early return pattern is intentional
    if (result is null) { return Result<TOut>.WithFailure("Result cannot be null"); }
    if (selector is null) { return Result<TOut>.WithFailure("Selector function cannot be null"); }
#pragma warning restore IDE0046
    
    return result.IsSuccess
        ? Result<TOut>.Success(selector(result.Value!))
        : Result<TOut>.WithFailure(result.Errors);
}
```

### ❌ Incorrect (Exception Not Preserved)
```csharp
catch (Exception ex)
{
    return Result<T>.WithFailure($"Operation failed: {ex.Message}");
}
```

### ✅ Correct (Exception Preserved)
```csharp
catch (Exception ex)
{
    return Result<T>.WithFailure($"Operation failed: {ex.Message}", default, ex);
}
```

## References

- [Result-Manual.md](./Result-Manual.md) - Complete API documentation
- [Exception Support Documentation](./Result-Manual.md#exception-support) - Exception handling guide
- [ROP Best Practices](./Result-Manual.md#railway-oriented-programming-rop-best-practices) - ROP patterns
- [Validation Utilities](./Result-Manual.md#validation-utilities) - Null parameter validation

