## Result and Result<T> Specification (Test-Derived)

This document defines the normative behavior of `IndQuestResults.Operations.Result` and `IndQuestResults.Operations.Result<T>` as enforced by unit tests in `Src/Code/tests/IndQuestResults.Tests.Unit/Operations`.

Scope: synchronous APIs covered by tests; see `docs/specs/mutation-spec.md` for additional mutation-hardening rules.

---

## Terminology

- **Success**: A state indicating operation completed successfully. For `Result<T>`, success can coexist with a null `Value` when `T` is nullable.
- **Failure**: A state indicating operation failed; contains one or more error messages.
- **Warnings**: Non-fatal diagnostic messages stored in `Errors` on successful `Result<T>`, set by `WithWarnings`.

---

## Non-generic Result

### Construction

- **Success**
  - `Result.Success()` MUST produce success with `Errors` empty and `Error == null`.
  - `ToString()` MUST equal the success prefix: `ResultConstants.SuccessPrefix`.

- **Failure**
  - `Result.WithFailure(string error)` MUST produce failure with exactly that error.
  - `Result.WithFailure(IEnumerable<string> errors)` MUST produce failure with all provided errors.
  - If `errors` is null or empty, it MUST produce failure with a single `ResultConstants.DefaultErrorMessage`.

### State

- **IsSuccess/IsFailure**
  - `IsSuccess` MUST be true IFF the result represents success.
  - `IsFailure` MUST be `!IsSuccess`.

- **Errors/Error**
  - `Errors` MUST be empty for success; non-empty for failure.
  - `Error` MUST be the first non-empty message, or null when `Errors` is empty.

### Functional operations

- **OnSuccess(Action)**
  - MUST invoke the action exactly once when success; MUST NOT invoke for failure. MUST return same instance.

- **OnFailure(Action<IEnumerable<string>>)**
  - MUST invoke the action exactly once with all errors when failure; MUST NOT invoke for success. MUST return same instance.

- **Map<T>(Func<T>) : Result<T>**
  - On success: MUST return `Result<T>.Success(func())`.
  - On failure: MUST propagate the same `Errors` and return failure.

- **Bind<T>(Func<Result<T>>) : Result<T>**
  - On success: MUST return the `Result<T>` produced by `func()`.
  - On failure: MUST propagate the same `Errors` and return failure.

- **Ensure(Func<bool> condition, string errorMessage) : Result**
  - On success and `condition()` true: MUST return same instance.
  - On success and `condition()` false: MUST return failure with exactly `errorMessage`.
  - On failure: MUST return same instance.

- **Tap(Action)**
  - On success: MUST invoke action and return same instance.
  - On failure: MUST be a no-op and return same instance.

- **Combine(params Result[]) : Result**
  - MUST aggregate all errors from `this` and input results that are failures.
  - MUST return success when no aggregated errors exist.

- **Match<T>(Func<T> onSuccess, Func<IEnumerable<string>, T> onFailure) : T**
  - On success: MUST execute `onSuccess()` and return its value.
  - On failure: MUST execute `onFailure(Errors)` and return its value.

- **Recover(Func<Result>) : Result**
  - On success: MUST return the original instance.
  - On failure: MUST return the result produced by `recoverFunc()`.

### Formatting

- **ToString()**
  - Success: MUST equal `ResultConstants.SuccessPrefix`.
  - Failure: MUST start with `ResultConstants.FailurePrefix` and contain all error messages.

- **CombineErrors(IEnumerable<string>? primary, IEnumerable<string>? secondary) : Result**
  - When both inputs null or empty: MUST be failure containing `ResultConstants.NoErrorsFoundMessage`.
  - Otherwise: MUST be failure with concatenated errors in order: primary then secondary.

- **IsCancelled (extension)**
  - `IsCancelled(this Result)` MUST return true only when `Errors` contains `ResultErrors.OperationCancelled`.
  - MUST return false when the input result is null, `Errors` is null/empty, or contains other messages.

---

## Generic Result<T>

### Construction

- **Success**
  - `Result<T>.Success(T value)` MUST be a success with `Errors` empty.
  - `WithSuccess(T value)` MUST behave identically to `Success`.

- **Failure**
  - `WithFailure(string error, T? value = default)` MUST be failure with the single error and the optional `value` preserved.
  - `WithFailure(IEnumerable<string>? errors, T? value = default)` MUST be failure with all provided errors; when null/empty, MUST use `ResultConstants.DefaultErrorMessage`.
  - `WithFailure(string[] errors, T? value = default)` MUST follow the same rules as the `IEnumerable<string>` overload.

- **Warnings**
  - `WithWarnings(IEnumerable<string> warnings, T value)` MUST be success with `HasWarnings == true`, `HasErrors == true` (warnings included), and `Value == value`.

- **Implicit conversions**
  - From `T` to `Result<T>`: `Result<T> r = someT;` MUST be equivalent to `Success(someT)`.
  - From `Result<T>` to `Result`:
    - If source is success: MUST become `Result.Success()`.
    - If source is failure: MUST become `Result.WithFailure(source.Errors)`.

- **Deconstruction**
  - `(bool succeeded, T? data, IEnumerable<string> errors)` MUST return success state, value, and errors.

### State flags

Given `r : Result<T>`:

- **Value**: MAY be null when `T` is nullable.
- **IsSuccessMayBeNull**: MUST be true iff the operation was marked successful (regardless of `Value` nullness).
- **IsSuccess / IsSuccessNotNull**: MUST be true iff success AND `Value` is not null.
- **IsSuccessValueNull**: MUST be true iff success AND `Value` is null.
- **IsFailure**: MUST be false for successful results (even when `Value` is null), true for failures.
- **HasErrors**: MUST be true when there are warnings or failure errors; false otherwise.
- **HasWarnings**: MUST be true only for successes with non-empty `Errors` (warnings).
- **IsRecoverable**: MUST equal success state (may include warnings).

### Functional operations (nullability-aware)

- **OnSuccess(Action<T>)**
  - When success and `T` is nullable (reference or `Nullable<TValue>`): MUST invoke action even if `Value` is null.
  - When success and `T` is non-nullable value type with null `Value`: MUST NOT invoke action.
  - When failure: MUST NOT invoke action. MUST return same instance in all cases.

- **OnFailure(Action<IEnumerable<string>>)**
  - On failure: MUST invoke action with `Errors` if non-empty, otherwise with a single `ResultConstants.DefaultErrorMessage`.
  - On success: MUST NOT invoke. MUST return same instance.

- **Map<TOut>(Func<T, TOut>) : Result<TOut>**
  - On failure: MUST propagate errors unchanged.
  - On success and `T` is nullable: MUST invoke `func(Value)` even when `Value` is null.
  - On success and `T` is non-nullable but `Value` is null: MUST produce failure with a specific message indicating mapping null for non-nullable types.

- **Bind<TOut>(Func<T, Result<TOut>>) : Result<TOut>**
  - MUST follow the same nullability rules as `Map` and propagate failures accordingly.

- **Ensure(Func<T, bool> condition, string errorMessage) : Result<T>**
  - On failure: MUST return same instance.
  - On success and `Value` is null: MUST return failure containing `ResultConstants.ConditionEvaluationWithNullValue`.
  - On success and condition false: MUST return failure with exactly `errorMessage`.
  - On success and condition true: MUST return same instance.

- **Tap(Action<T>)**
  - MUST mirror `OnSuccess` invocation rules and always return same instance.

- **Combine(params Result[]) : Result<T>**
  - MUST aggregate errors from `this` (when failed) and all failed input results.
  - If any errors aggregated: MUST return `Result<T>.WithFailure(allErrors)`.
  - If no errors aggregated: MUST return the current successful result (preserving `Value` as-is, including nulls).

- **Match<TOut>(Func<T, TOut> onSuccess, Func<IEnumerable<string>, TOut> onFailure) : Result<TOut>**
  - On failure: MUST call `onFailure(errorsOrDefault)` where `errorsOrDefault` is `Errors` when non-empty, otherwise an array containing `ResultConstants.DefaultErrorMessage`. The return value MUST be wrapped as `Result<TOut>.Success(...)`.
  - On success: MUST call `onSuccess(Value)` (even when `Value` is null for nullable `T`) and wrap as `Result<TOut>.Success(...)`.

- **Recover(Func<Result<T>>) : Result<T>**
  - On success: MUST return original instance.
  - On failure: MUST return the result of `recoverFunc()`.

- **RecoverWith<TOut>(Func<Result<TOut>>) : Result<TOut>**
  - On failure: MUST return `recoverFunc()` result.
  - On success: MUST attempt safe conversion of `Value` to `TOut`.
    - If convertible: MUST return `Result<TOut>.Success(converted)`.
    - If not convertible: MUST return failure with message indicating conversion cannot be performed.

### Formatting

- **ToString()**
  - On success: MUST start with `ResultConstants.SuccessPrefix` and include value text when present.
  - On failure: MUST start with `ResultConstants.FailurePrefix` and include all error messages.

- **IsCancelled (extension)**
  - `IsCancelled<T>(this Result<T>)` MUST return true only when `Errors` contains `ResultErrors.OperationCancelled`.
  - MUST return false when result is null, `Errors` is null/empty, or contains other messages.

---

## Derived Invariants (cross-cutting)

- **Default error when none present**
  - Any failure path that would expose `Errors` to a consumer branch (e.g., `Match` failure branch for `Result<T>`) MUST provide at least one error: either existing `Errors` or `[ResultConstants.DefaultErrorMessage]`.

- **Aggregation order**
  - Where multiple error lists are combined, resulting error order MUST preserve input order (primary then secondary; for `Combine`, original result’s errors followed by each failed input in parameter order).

- **String formatting equivalence**
  - `FormatErrorsString` MUST produce identical formatted output regardless of internal optimization path; for any inputs, output MUST equal `"{prefix}: err1, err2, ..."`.

---

## Conformance Examples (non-normative)

```csharp
// Non-generic
var ok = Result.Success()
    .OnSuccess(() => DoSomething())
    .Ensure(() => true, "fail")
    .Tap(() => Log("ok"));

var bad = Result.WithFailure(new[] {"E1", "E2"})
    .OnFailure(errs => Log(string.Join(", ", errs)));

// Generic
var len = Result<string>.Success("hello")
    .Map(s => s.Length) // Success(5)
    .Ensure(i => i > 0, "zero");

var chained = Result<string>.Success("42")
    .Bind(s => int.TryParse(s, out var n)
        ? Result<int>.Success(n)
        : Result<int>.WithFailure("Parse failed"));

// Match guarantees success-wrapped return in Result<T>
var m = Result<string>.WithFailure("E").Match(
    onSuccess: s => $"S:{s}",
    onFailure: errs => $"F:{string.Join(", ", errs)}");
// m is Result<string> with Value == "F:E" and IsSuccess == true
```

---

## References

- Tests: `IndQuestResults.Tests.Unit.Operations.ResultTests`, `IndQuestResults.Tests.Unit.Operations.ResultGenericTests`
- Related: `docs/specs/mutation-spec.md`
