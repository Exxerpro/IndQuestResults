## IndQuestResults Result Manual

This manual documents the consumer-facing API for `Result` and `Result<T>` and their fluent extensions, grounded in the current code. All behaviors are validated by unit tests under `Src/Code/tests/IndQuestResults.Tests.Unit`.

### Overview

- Purpose: Functional, type-safe success/failure flow without exceptions for control flow.
- Core types and namespaces:
  - `IndQuestResults.Result` (non-generic)
  - `IndQuestResults.Result<T>` (generic)
  - Extensions in `IndQuestResults.Operations` (validation, async chaining, LINQ, helpers)
- Highlights: fluent composition (Map/Bind/Match), error aggregation, warnings + quality metadata on success, async pipelines, validation utilities.

### Quick Start

```csharp
using IndQuestResults;
using IndQuestResults.Operations;

// Success/failure
var ok = Result.Success();
var fail = Result.WithFailure("Something went wrong");

// Typed
Result<int> parsed = Result<int>.Success(42);
Result<int> bad = Result<int>.WithFailure("Parse error");

// Fluent chain
var length = Result<string>.Success("hello")
    .Map(s => s.Length)                  // Success(5)
    .Ensure(n => n > 0, "Zero length"); // still Success(5)

// Async chain
var profile = await GetUserAsync(id)
    .ThenAsync(u => LoadProfileAsync(u.Id))
    .ThenTap(p => CacheAsync(p));
```

### Cheat Sheet

- Create: `Result.Success()`, `Result.WithFailure("err")`, `Result<T>.Success(v)`, `Result<T>.WithFailure("err")`
- Map/Bind: `.Map(f)`, `.Bind(f)`; LINQ: `from x in r1 from y in r2 select ...`
- Ensure/Tap: `.Ensure(pred, "err")`, `.Tap(a)`; side-effects without breaking the chain
- Match: `.Match(onSuccess, onFailure)` returns a plain value for generic; use `MatchValue` for explicit value selection or `MapBoth` to produce a `Result<TOut>`
- Combine: `r.Combine(r2, r3)` aggregates errors; `Result.CombineErrors(e1, e2)` merges sets
- Value helpers: `.ValueOr(default)`, `.OrElse(fallback)`
- Error helpers: `.MapError(map)`, `.TapError(log)`, `.Recover(errors => ...)`
- Async: `.ThenAsync`, `.ThenMap`, `.ThenTap`, `.ThenValidate`, `.ThenRecover`, `.CombineAsync`
- Cancellation: `ResultExtensions.Cancelled<T>()`, `.IsCancelled()`, `CancellationAwareResult.WrapCancellationAware`, `CancellationAwareResult.WrapWithTimeout`
- Serialization: `Result<T>` is JSON-serializable via `System.Text.Json` attributes (`[JsonConstructor]`); custom converters are not required
- Warnings: `Result<T>.WithWarnings(warnings, value, confidence, missingDataRatio)`

---

# Consumer API

## Non-generic `Result`

Namespace: `IndQuestResults`

### Construction

- `Result.Success()`
  - Returns a successful `Result` with empty `Errors`.
  - `ToString()` => `ResultConstants.SuccessPrefix`.

- `Result.WithFailure(string error)`
  - Returns failure with a single error.

- `Result.WithFailure(IEnumerable<string> errors)` / `Result.WithFailure(string[] errors)`
  - Returns failure with all provided errors.
  - Null/empty input becomes `[ResultConstants.DefaultErrorMessage]`.

- `Result.WithFailure(Exception exception)`
  - Returns failure with exception details. Sets `IsFaulted = true` (unless exception is `OperationCanceledException`).
  - Preserves full exception object including stack trace for debugging.

- `Result.WithFailure(IEnumerable<string> errors, Exception? exception)`
  - Returns failure with both error messages and exception.
  - `IsFaulted` is set based on exception type (true for exceptions, false for `OperationCanceledException`).

### State

- `IsSuccess` / `IsFailure`: success vs failure.
- `Errors` / `Error`: all messages and the first non-empty message.
- `IsFaulted`: true when the failure was caused by an exception (not validation errors or cancellation).
- `Exception`: the exception object that caused the failure, if any. Null for validation failures or when no exception was provided.

### Functional API

- `OnSuccess(Action)` executes only on success; returns same instance.
- `OnFailure(Action<IEnumerable<string>>)` executes only on failure; returns same instance.
- `Map<T>(Func<T>) : Result<T>` maps success to typed result; propagates errors on failure.
- `Bind<T>(Func<Result<T>>) : Result<T>` binds success to next result; propagates errors on failure.
- `Ensure(Func<bool>, string errorMessage) : Result` checks condition on success; returns failure when false.
- `Tap(Action) : Result` side effect on success; returns same instance.
- `Combine(params Result[]) : Result` aggregates errors across inputs (success if none).
- `Match<T>(Func<T> onSuccess, Func<IEnumerable<string>, T> onFailure) : T` selects a branch value.
- `Recover(Func<Result>) : Result` returns recovery result only on failure.
- `CombineErrors(IEnumerable<string>? primary, IEnumerable<string>? secondary) : Result` failed result with both sets (or `NoErrorsFound` if both empty).

### Formatting

- `ToString()`
  - Success: `ResultConstants.SuccessPrefix`.
  - Failure: `ResultConstants.FailurePrefix: err1, err2, ...` (uses `Result.FormatErrorsString`).

---

## Generic `Result<T>`

Namespace: `IndQuestResults`

### Construction

- `Result<T>.Success(T value)` / `WithSuccess(T value)`
  - Success with value (null is valid for nullable `T`).

- `Result<T>.WithFailure(...)`
  - Overloads: `(IEnumerable<string>? errors, T? value = default)`, `(T? value = default, IEnumerable<string>? errors = default)`, `(string[] errors, T? value = default)`, `(string error, T? value = default)`.
  - Null/empty errors become `[ResultConstants.DefaultErrorMessage]`.

- `Result<T>.WithFailure(Exception exception, T? value = default)`
  - Returns failure with exception details. Sets `IsFaulted = true` (unless exception is `OperationCanceledException`).
  - Preserves full exception object including stack trace for debugging.

- `Result<T>.WithFailure(string error, T? value = default, Exception? exception = null)`
- `Result<T>.WithFailure(IEnumerable<string> errors, T? value = default, Exception? exception = null)`
  - Returns failure with both error messages and exception.
  - `IsFaulted` is set based on exception type (true for exceptions, false for `OperationCanceledException`).

- Warnings (success with diagnostics)
  - `WithWarnings(IEnumerable<string> warnings, T value)` => success with `Warnings` and `HasWarnings`.
  - `WithWarnings(IEnumerable<string> warnings, T value, double confidence, double missingDataRatio)` => clamps metadata to [0,1].

- Conversions & deconstruction
  - Implicit `T -> Result<T>` as `Success(T)`.
  - Implicit `Result<T> -> Result` preserving success/failure.
  - Deconstructs to `(bool succeeded, T? data, IEnumerable<string> errors)`.

### State

- `Value` (T?)
- `IsSuccessMayBeNull`: success, even if `Value` is null (nullable `T`).
- `IsSuccess` / `IsSuccessNotNull`: success and non-null `Value`.
- `IsSuccessValueNull`: success with null `Value`.
- `IsFailure`: negation of success flag.
- `HasErrors`: warnings or failure messages present.
- `HasWarnings`: success with diagnostic messages.
- `Warnings`, `Confidence`, `MissingDataRatio`: diagnostics and quality metadata.
- `IsFaulted`: true when the failure was caused by an exception (not validation errors or cancellation).
- `Exception`: the exception object that caused the failure, if any. Null for validation failures or when no exception was provided.

### Functional API

- `OnSuccess(Action<T>) : Result<T>` executes for success; for nullable `T`, runs even if `Value` is null.
- `OnFailure(Action<IEnumerable<string>>) : Result<T>` executes only on failure (default error injected when none present).
- `Map<TOut>(Func<T, TOut>) : Result<TOut>` maps success; for non-nullable `T` with null `Value`, returns failure indicating null map for non-nullable type.
- `Bind<TOut>(Func<T, Result<TOut>>) : Result<TOut>` same nullability semantics as `Map`.
- `Ensure(Func<T,bool>, string errorMessage) : Result<T>` returns failure if predicate false; for success with null `Value`, returns `ConditionEvaluationWithNullValue`.
- `Tap(Action<T>) : Result<T>` side effect on success; mirrors `OnSuccess` nullability rules.
- `Combine(params Result[]) : Result<T>` aggregates errors across non-generic results and self; returns success preserving current value.
- `Match<TOut>(Func<T,TOut> onSuccess, Func<IEnumerable<string>,TOut> onFailure) : Result<TOut>` returns a success-wrapped branch value.
- `Recover(Func<Result<T>>) : Result<T>` only on failure; otherwise returns same instance.
- `RecoverWith<TOut>(Func<Result<TOut>>) : Result<TOut>` failure recovers to another type; on success attempts safe conversion to `TOut` or returns typed failure.

Nullability example (from tests):

```csharp
Result<int?> ri = new Result<int?>(true, errors: null, value: null);
ri.OnSuccess(_ => /* runs */);
ri.Map(i => (i ?? 0) + 1);            // Success(1)
ri.Bind(i => Result<string>.Success((i ?? 0).ToString()));
```

### Formatting

- Success: `"Success: <Value>"` (Value may be null for nullable `T`).
- Failure: `ResultConstants.FailurePrefix: err1, err2, ...`.

---

# Fluent Helpers

Namespace: `IndQuestResults.Operations`

- LINQ (`ResultLinqExtensions`)
  - `Select` maps via `Map`
  - `SelectMany` binds via `Bind` (overloads support 2-source queries)
  - `Where` filters via `Ensure`

- Error helpers (`ResultErrorExtensions`)
  - `MapError(this Result|Result<T>, Func<IEnumerable<string>, IEnumerable<string>>)` transforms errors on failures
  - `TapError(this Result|Result<T>, Action<IEnumerable<string>>)` side-effect on errors
  - `Recover(this Result|Result<T>, Func<IEnumerable<string>, Result|Result<T>>)` error-aware recovery

- Value helpers (`ResultValueExtensions`)
  - `ValueOr(default)` / `ValueOr(Func<T>)`
  - `OrElse(Result<T>)` / `OrElse(Func<Result<T>>)`
  - `MatchValue(onSuccess, onFailure)` returns a plain value
  - `OnBoth(Action)` / `OnBoth(Action<T>, Action<IEnumerable<string>>)` / `Finally(Action)`
  - `MapBoth(onSuccess, onFailure)` returns `Result<TOut>`

- Try helpers (`ResultTryExtensions`)
  - `ResultTryExtensions.Try<T>(Func<T>, Func<Exception,string>)`
  - `ResultTryExtensions.TryAsync<T>(Func<Task<T>>, Func<Exception,string>)`
  - `MapTry` and `BindTry` variants to wrap throwing delegates

---

# Async Fluent API

- Namespace: `IndQuestResults.Async` (preferred)
  - Chaining: `ResultAsync.BindAsync`, `ResultAsync.MapAsync`, `ResultAsync.TapAsync`, `ResultAsync.RecoverAsync`
  - Collections: `ResultAsync.SequenceAsync`, `ResultAsync.TraverseAsync`, `ResultAsync.TraverseParallelAsync`
  - ValueTask support: `ValueTask<Result<T>>.ThenAsync`, `.ThenMap`, `.ThenTap`

- Namespace: `IndQuestResults.Operations.ResultExtensions`
  - Chaining: `ThenAsync`, `ThenMap`, `ThenTap`, `ThenDo`, `ThenValidate`, `ThenValidateAsync`, `ThenEnsure`, `ThenSwitch`, `ThenRecover`, `ThenLogErrors`
  - Composition: `CombineAsync` (tuple), `WhenAllAsync`, `DefaultIfFailure`, `ThenAsyncCancellable`

- Utilities
  - `DefaultIfFailure<T>(Task<Result<T>>, T defaultValue)`
  - `ThenAsyncCancellable<TIn,TOut>(..., Func<TIn,CancellationToken,Task<Result<TOut>>>, CancellationToken)`
  - `Cancelled()` / `Cancelled<T>()` and `IsCancelled(this Result|Result<T>)`

Example:

```csharp
using IndQuestResults;
using IndQuestResults.Operations;

var result = await GetUserAsync(id)
    .ThenValidateAsync(u => ValidateUserAsync(u))
    .ThenAsync(u => CreateProfileAsync(u))
    .ThenTap(p => CacheAsync(p));

if (result.IsFailure)
{
    await LogAsync(string.Join(", ", result.Errors));
}
```

---

# Exception Support

## Overview

IndQuestResults supports exception preservation for better debugging and error diagnostics. Exceptions are captured and stored in `Result` objects without breaking the functional programming flow.

## Key Properties

- **`IsFaulted`**: Indicates whether the failure was caused by an exception (as opposed to validation errors or cancellation).
- **`Exception`**: The exception object that caused the failure, preserving full stack trace and inner exceptions.

## Exception Handling Rules

1. **Regular Exceptions**: Set `IsFaulted = true` and preserve the exception object.
2. **OperationCanceledException**: Treated as cancellation, not a fault. `IsFaulted = false`, but exception is still preserved.
3. **Validation Failures**: No exception, `IsFaulted = false`, `Exception = null`.

## Examples

### Exception Preservation

```csharp
using IndQuestResults;
using IndQuestResults.Operations;

// Exception caught and preserved
var result = ResultTryExtensions.Try<int>(
    () => throw new InvalidOperationException("Test exception"),
    ex => $"Error: {ex.Message}");

if (result.IsFailure)
{
    result.IsFaulted.ShouldBeTrue(); // Exception was the cause
    result.Exception.ShouldNotBeNull(); // Full exception preserved
    result.Exception.StackTrace.ShouldNotBeNull(); // Stack trace available
    result.Exception.InnerException?.ShouldNotBeNull(); // Inner exceptions preserved
}
```

### Async Exception Handling

```csharp
using IndQuestResults.Async;

// Exceptions in async methods are automatically preserved
var result = await ResultAsync.MapAsync(
    Task.FromResult(Result<int>.Success(5)),
    async x =>
    {
        await Task.Delay(1);
        throw new InvalidOperationException("Async error");
    });

result.IsFailure.ShouldBeTrue();
result.IsFaulted.ShouldBeTrue();
result.Exception.ShouldNotBeNull();
```

### Cancellation vs Exceptions

```csharp
using IndQuestResults.Operations;

// Cancellation is not a fault
var cancelled = await CancellationAwareResult.WrapCancellationAware<int>(
    async ct =>
    {
        await Task.Delay(100, ct);
        return 42;
    },
    cancellationToken: cancellationTokenSource.Token);

cancelled.IsFailure.ShouldBeTrue();
cancelled.IsFaulted.ShouldBeFalse(); // Cancellation is not a fault
cancelled.IsCancelled().ShouldBeTrue();
```

### Exception in Result Chains

```csharp
// Exceptions propagate through Result chains
var result = await GetUserAsync(id)
    .ThenAsync(u => LoadProfileAsync(u.Id)) // If this throws, exception is preserved
    .ThenMap(p => p.ToDto());

if (result.IsFailure && result.IsFaulted)
{
    // Log the exception for debugging
    _logger.LogError(result.Exception, "Failed to load user profile");
}
```

## Best Practices

1. **Check `IsFaulted`** to distinguish between validation errors and exceptions.
2. **Access `Exception`** for detailed debugging information (stack traces, inner exceptions).
3. **Handle `OperationCanceledException`** separately - it's not a fault but a cancellation.
4. **Preserve exceptions** in async catch blocks by passing exception to `WithFailure`.

---

# Railway-Oriented Programming (ROP) Best Practices

## Overview

IndQuestResults follows Railway-Oriented Programming principles, where operations return `Result` types instead of throwing exceptions for control flow.

## Core Principles

1. **No Exceptions for Control Flow**: Methods return `Result` failures instead of throwing exceptions.
2. **Null Parameter Handling**: Null parameters return `Result` failures, not `ArgumentNullException`.
3. **Functional Composition**: Operations chain together without try-catch blocks.
4. **Error Propagation**: Errors automatically propagate through chains.

## Null Parameter Validation

All extension methods handle null parameters gracefully by returning `Result` failures:

```csharp
// ❌ Old way (throws exception)
public static Result<TOut> Map<T, TOut>(this Result<T> result, Func<T, TOut> selector)
{
    ArgumentNullException.ThrowIfNull(result);
    ArgumentNullException.ThrowIfNull(selector);
    // ...
}

// ✅ ROP-compliant way (returns Result failure)
public static Result<TOut> Map<T, TOut>(this Result<T> result, Func<T, TOut> selector)
{
    if (result is null) { return Result<TOut>.WithFailure("Result cannot be null"); }
    if (selector is null) { return Result<TOut>.WithFailure("Selector function cannot be null"); }
    // ...
}
```

## ROP Chain Examples

### Successful Chain

```csharp
var result = Result<string>.Success("hello")
    .Map(s => s.Length)           // Success(5)
    .Ensure(n => n > 0, "Zero")  // Success(5)
    .Map(n => n * 2)              // Success(10)
    .Bind(n => Result<int>.Success(n + 1)); // Success(11)
```

### Failure Propagation

```csharp
var result = Result<string>.Success("hello")
    .Map(s => s.Length)           // Success(5)
    .Ensure(n => n > 10, "Too short") // Failure("Too short")
    .Map(n => n * 2)              // Failure("Too short") - Map not executed
    .Bind(n => Result<int>.Success(n + 1)); // Failure("Too short") - Bind not executed
```

### Null Parameter Handling

```csharp
Result<int>? nullResult = null;
var result = nullResult!
    .Map(x => x * 2)              // Failure("Result cannot be null")
    .Bind(x => Result<int>.Success(x + 1)); // Failure("Result cannot be null")

// No exception thrown - failure propagates through chain
```

## Reactive Patterns (No-Op Disposables)

For methods returning `IDisposable`, null parameters return no-op disposables:

```csharp
using IndQuestResults.Reactive;

var subject = ResultSubscriptionsCore.CreateResultSubject<int>();

// Null handler returns no-op disposable (doesn't throw)
var subscription = subject.Subscribe(onSuccess: null!);
subscription.Dispose(); // Safe to call, does nothing
```

## Best Practices

1. **Always return `Result` failures** for null parameters, never throw exceptions.
2. **Use no-op implementations** for reactive patterns (disposables, handlers).
3. **Chain operations** without try-catch blocks - errors propagate automatically.
4. **Check `IsFailure`** before accessing values, not with try-catch.

---

# Validation Utilities

Namespace: `IndQuestResults.Operations` and error types in `IndQuestResults.Validation`

## Discoverable Validation Extensions

New validation extensions are available as extension methods on `Result` types for better discoverability:

### Extension Methods on Result Types

- `result.EnsureNotNull<T>(string parameterName)` - Validates that the Result's value is not null
- `value.EnsureNotNull<T>(string parameterName)` - Validates a nullable value and returns a Result

### Static Methods

- `ResultValidationExtensions.ValidateNotNull(params (object? value, string parameterName)[] validations)`
- `ResultValidationExtensions.CreateIfValid<T>(Func<T> factory, params (object? value, string parameterName)[] validations)`

### Legacy Methods (Maintained for Backward Compatibility)

- `ResultExtensions.ValidateNotNull(params (object? value, string parameterName)[] validations)`
- `ResultExtensions.CreateIfValid<T>(Func<T> factory, params (object? value, string parameterName)[] validations)`
- `ResultExtensions.FailForNullArgument<T>(string parameterName, string? message = null)`
- `ResultExtensions.FailForNullArguments<T>(params string[] names)`
- Error types: `NullArgumentError`, `MultipleNullArgumentsError`

## Examples

### Discoverable Validation (Recommended)

```csharp
using IndQuestResults;
using IndQuestResults.Operations;

// Extension method on Result - discoverable via IntelliSense
var userResult = GetUserAsync(id)
    .EnsureNotNull(nameof(user))
    .Map(u => u.Email)
    .EnsureNotNull(nameof(email));

// Static method for multiple validations
var validated = ResultValidationExtensions.ValidateNotNull(
    (user, nameof(user)),
    (user?.Email, nameof(user.Email)),
    (user?.Name, nameof(user.Name))
);

// Create with validation
var created = ResultValidationExtensions.CreateIfValid(
    factory: () => new User(id!),
    (id, nameof(id)),
    (email, nameof(email))
);
```

### Legacy Validation (Still Supported)

```csharp
using IndQuestResults;
using IndQuestResults.Operations;

// Legacy static methods still work
var validated = ResultExtensions.ValidateNotNull(
    (user, nameof(user)),
    (user?.Email, nameof(user.Email))
);

var created = ResultExtensions.CreateIfValid(
    factory: () => new User(id!),
    (id, nameof(id))
);
```

### ROP-Compliant Null Checks

All validation methods return `Result` failures instead of throwing exceptions:

```csharp
// ❌ Old way (throws exception)
ArgumentNullException.ThrowIfNull(user);

// ✅ ROP-compliant way (returns Result failure)
var validation = userResult.EnsureNotNull(nameof(user));
if (validation.IsFailure)
{
    // Handle validation failure
}
```

---

# Formatting & Performance Notes

- `Result.ToString()`
  - Success: `ResultConstants.SuccessPrefix`
  - Failure: `"WithFailure: err1, err2, ..."` via `Result.FormatErrorsString`
- `Result<T>.ToString()`
  - Success: `"Success: <Value>"` (Value may be null for nullable `T`)
  - Failure: formatted errors as above
- Internals optimize small collections and strings via spans and pre-sizing; large collections use `StringBuilder`.

---

# Cancellation Token Handling

## Overview

IndQuestResults provides comprehensive support for cancellation tokens, treating `OperationCanceledException` as a cancellation (not a fault) and preserving cancellation state in Result objects.

## Key Concepts

1. **Cancellation is Not a Fault**: `OperationCanceledException` sets `IsFaulted = false` but `IsFailure = true`.
2. **Early Cancellation Checks**: Methods check cancellation tokens before executing operations.
3. **Cancellation Propagation**: Cancellation state propagates through Result chains.

## Cancellation-Aware Wrappers

### WrapCancellationAware

Wraps async operations to handle cancellation functionally:

```csharp
using IndQuestResults.Operations;

var result = await CancellationAwareResult.WrapCancellationAware<int>(
    async ct =>
    {
        await Task.Delay(100, ct);
        return 42;
    },
    cancellationToken: cancellationToken);

if (result.IsCancelled())
{
    // Handle cancellation
}
```

### WrapWithTimeout

Adds timeout support with cancellation:

```csharp
using IndQuestResults.Operations;

var result = await CancellationAwareResult.WrapWithTimeout<int>(
    async ct =>
    {
        await Task.Delay(100, ct);
        return 42;
    },
    timeout: TimeSpan.FromSeconds(5),
    cancellationToken: cancellationToken);

if (result.IsFailure)
{
    if (result.IsCancelled())
    {
        // Handle cancellation
    }
    else if (result.Error.Contains("timed out"))
    {
        // Handle timeout
    }
}
```

## Cancellation in Async Chains

```csharp
using IndQuestResults.Async;

var result = await ResultAsync.BindAsync(
    GetUserAsync(id, cancellationToken),
    async (user, ct) => await LoadProfileAsync(user.Id, ct),
    cancellationToken);

if (result.IsCancelled())
{
    // User cancelled the operation
}
```

## Best Practices

1. **Always pass cancellation tokens** to async operations.
2. **Check `IsCancelled()`** to distinguish cancellation from other failures.
3. **Use `WrapCancellationAware`** for operations that need cancellation support.
4. **Use `WrapWithTimeout`** for operations with time limits.
5. **Early cancellation checks** prevent unnecessary work.

## Example: Service with Cancellation

```csharp
public class UserService
{
    public async Task<Result<User>> GetUserAsync(int id, CancellationToken cancellationToken = default)
    {
        // Early cancellation check
        if (cancellationToken.IsCancellationRequested)
        {
            return ResultExtensions.Cancelled<User>();
        }

        return await CancellationAwareResult.WrapCancellationAware<User>(
            async ct =>
            {
                await Task.Delay(100, ct); // Simulate async work
                return await _repository.GetUserAsync(id, ct);
            },
            cancellationToken);
    }
}
```

---

# Integration Patterns

Service layer

```csharp
using IndQuestResults;
using IndQuestResults.Operations;

public class UserService
{
    public Task<Result<User>> CreateUserAsync(CreateUserRequest request)
    {
        return ValidateRequest(request)
            .ThenAsync(r => CheckEmailUnique(r.Email))
            .ThenAsync(r => CreateUser(r))
            .ThenTap(u => SendWelcomeEmail(u));
    }
}
```

Controller

```csharp
using IndQuestResults;

[ApiController]
public class UsersController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request);
        return result.Match(
            onSuccess: user => Ok(user.ToDto()),
            onFailure: errors => BadRequest(new { Errors = errors })
        );
    }
}
```

---

# Appendix: Behavior Summary (grounded by tests)

- Success constructors set flags and keep `Errors` empty.
- Failure constructors inject a default error when none provided.
- Map/Bind propagate failures without executing delegates.
- Ensure converts success to failure when predicate fails (no-op on failure).
- Combine aggregates errors in order across many results.
- Match calls the correct branch; `Result<T>.Match` wraps return in a success result.
- Nullability rules: for nullable `T`, success operations run even when `Value` is null; for non-nullable `T`, null `Value` maps/binds to a typed failure.
- Async helpers preserve the same semantics and support cancellation.

See tests under `Src/Code/tests/IndQuestResults.Tests.Unit/*` for executable specifications of all behaviors.

---

# Linting & Analyzers

- Package: `IndQuestResults.Analyzers` (optional). Add to your solution to get guidance and code fixes.

## Available Analyzers

### IQR0001: Prefer ResultAsync for Async Chains
- **Purpose**: Encourages consistent async chaining via `ResultAsync.BindAsync` instead of `ResultExtensions.ThenAsync`
- **Severity**: Info (default)
- **Triggers**: When calling `ThenAsync` from `IndQuestResults.Operations.ResultExtensions` on `Task<Result<T>>`
- **Rationale**: `ResultAsync` centralizes async exception/cancellation semantics consistently across chains
- **Code Fix**: Automatically rewrites `task.ThenAsync(next)` to `ResultAsync.BindAsync(task, next)` and adds `using IndQuestResults.Async`
- **Configuration**:
  - `dotnet_diagnostic.IQR0001.severity = suggestion` (default)
  - `dotnet_diagnostic.IQR0001.severity = warning` (stricter)
  - `dotnet_diagnostic.IQR0001.severity = none` (disable)

### IQR201: ROP Violation - ArgumentNullException.ThrowIfNull
- **Purpose**: Detects `ArgumentNullException.ThrowIfNull` usage in extension methods that return `Result<T>`
- **Severity**: Warning (default)
- **Message**: Extension methods should return Result failures instead of throwing ArgumentNullException
- **Suggested Fix**: Replace with `if (parameter is null) { return Result<T>.WithFailure("parameter cannot be null"); }`

### IQR202: ROP Violation - Throw Statement
- **Purpose**: Detects `throw` statements in methods that return `Result<T>` (excluding rethrows in catch blocks)
- **Severity**: Warning (default)
- **Message**: Result-returning methods should return Result failures instead of throwing exceptions
- **Suggested Fix**: Replace `throw` with `return Result<T>.WithFailure("error message")`

### IQR301: Missing Exception Parameter
- **Purpose**: Detects missing exception parameter in `WithFailure` calls within catch blocks
- **Severity**: Warning (default)
- **Message**: WithFailure call in catch block should include exception parameter to preserve stack trace
- **Suggested Fix**: Use `Result<T>.WithFailure("...", default, ex)` to preserve exception

### IQR302: Exception Not Preserved
- **Purpose**: Detects catch blocks where exceptions are used but not passed to `WithFailure`
- **Severity**: Warning (default)
- **Message**: Catch block should preserve exception in Result failure
- **Suggested Fix**: Pass exception to `WithFailure` to preserve stack trace and exception details

## Configuration

All analyzers can be configured in `.editorconfig`:

```ini
# ROP Compliance
dotnet_diagnostic.IQR201.severity = warning
dotnet_diagnostic.IQR202.severity = warning

# Exception Handling
dotnet_diagnostic.IQR301.severity = warning
dotnet_diagnostic.IQR302.severity = warning

# Async Patterns
dotnet_diagnostic.IQR0001.severity = suggestion
```

## CI/CD Integration

Analyzers run automatically during build when `IndQuestResults.Analyzers` package is referenced. ROP violations are detected and reported in CI/CD pipelines.
