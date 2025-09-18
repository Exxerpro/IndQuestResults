## IndQuestResults Result Manual

This manual documents the `Result` and `Result<T>` types and their extensions in the IndQuestResults library, with precise behavior specifications and runnable examples. All behaviors are backed by unit tests in `Src/Code/tests/IndQuestResults.Tests.Unit`.

### Overview

- **Purpose**: Provide a functional, type-safe approach to handling success/failure without exceptions in normal control flow.
- **Core types**:
  - `IndQuestResults.Operations.Result` (non-generic)
  - `IndQuestResults.Operations.Result<T>` (generic)
- **Key capabilities**: success/failure modeling, error aggregation, warnings on success, functional composition (Map/Bind/Match), recovery, and async counterparts.

---

## Non-generic `Result`

Namespace: `IndQuestResults.Operations`

### Construction

- `Result.Success()`
  - Returns a successful `Result` with empty `Errors`.
  - ToString() -> `"Success"`.

- `Result.WithFailure(string error)`
  - Returns a failed `Result` with one error.

- `Result.WithFailure(IEnumerable<string> errors)` / `Result.WithFailure(string[] errors)`
  - Returns a failed `Result` with all provided errors.
  - If `errors` is null or empty, a default error message is used: `ResultConstants.DefaultErrorMessage`.

### State

- `IsSuccess` (bool): true when successful and there are no errors.
- `IsFailure` (bool): logical negation of `IsSuccess`.
- `Errors` (IEnumerable<string>): all error messages (empty when successful).
- `Error` (string?): first non-empty error or null.

### Formatting

- `ToString()`
  - Success: `"Success"`.
  - Failure: `"WithFailure: <err1>, <err2>, ..."` (prefix configurable internally).

### Functional API

- `OnSuccess(Action action)`
  - Executes `action` only if success; returns same instance.

- `OnFailure(Action<IEnumerable<string>> action)`
  - Executes `action(errors)` only if failure; returns same instance.

- `Map<T>(Func<T> func) : Result<T>`
  - If success: returns `Result<T>.Success(func())`.
  - If failure: returns `Result<T>.WithFailure(Errors)`.

- `Bind<T>(Func<Result<T>> func) : Result<T>`
  - If success: returns `func()`.
  - If failure: returns `Result<T>.WithFailure(Errors)`.

- `Ensure(Func<bool> condition, string errorMessage) : Result`
  - If success and `condition()` is false: returns failure with `errorMessage`.
  - If failure: returns original instance.

- `Tap(Action action) : Result`
  - If success: runs side effect `action`; returns same instance.

- `Combine(params Result[] results) : Result`
  - Aggregates all errors from `this` and `results` that failed.
  - Returns success if no errors found.

- `Match<T>(Func<T> onSuccess, Func<IEnumerable<string>, T> onFailure) : T`
  - Executes the relevant branch based on state and returns its value.

- `Recover(Func<Result> recoverFunc) : Result`
  - If failure: returns the result of `recoverFunc()`; otherwise returns original.

- `CombineErrors(IEnumerable<string>? primary, IEnumerable<string>? secondary) : Result`
  - Produces a failed `Result` containing all provided errors.
  - If both are null or both empty: returns failure with `ResultConstants.NoErrorsFoundMessage`.

### Usage Examples

```csharp
var r1 = Result.Success()
    .Ensure(() => true, "must be true")
    .Tap(() => Console.WriteLine("ok"));

var r2 = Result.WithFailure(new[]{"E1","E2"})
    .OnFailure(errs => Console.WriteLine(string.Join(", ", errs)));

var mapped = Result.Success().Map(() => 42); // Success 42
```

---

## Generic `Result<T>`

Namespace: `IndQuestResults.Operations`

### Construction

- `Result<T>.Success(T value)` / `Result<T>.WithSuccess(T value)`
  - Success with `value`.
  - Note: `value` may be null for nullable `T`. See state flags below.

- `Result<T>.WithFailure(IEnumerable<string>? errors, T? value = default)`
- `Result<T>.WithFailure(T? value = default, IEnumerable<string>? errors = default)`
- `Result<T>.WithFailure(string[] errors, T? value = default)`
- `Result<T>.WithFailure(string error, T? value = default)`
  - Create failure; null/empty `errors` replaced with `DefaultErrorMessage`.

- `Result<T>.WithWarnings(IEnumerable<string> warnings, T value)`
  - Success + diagnostics (warnings are stored in `Errors`).

- Implicit conversions
  - `public static implicit operator Result<T>(T value)` => `Success(value)`.
  - `public static implicit operator Result(Result<T> result)` => preserves success/failure + errors.

- Deconstruction: `(bool succeeded, T? data, IEnumerable<string> errors)`

### State

- `Value` (T?)
- `IsSuccessMayBeNull`: true if operation succeeded (value may be null).
- `IsSuccess`: true if succeeded AND `Value` is not null.
- `IsSuccessNotNull`: same as `IsSuccess`.
- `IsSuccessValueNull`: true if succeeded AND `Value` is null.
- `HasErrors`: true if there are any messages (warnings or errors).
- `HasWarnings`: true if success AND there are messages.
- `IsRecoverable`: alias of success (may include warnings).
- `IsFailure`: logical negation of success flag.
- `Errors`/`Error`: same semantics as non-generic.

### Functional API

- `OnSuccess(Action<T> action) : Result<T>`
  - Executes `action(Value)` when successful.
  - Behavior across nullability:
    - If `T` is nullable (reference type or nullable value type), `OnSuccess` executes even when `Value` is null.
    - If `T` is non-nullable value type and `Value` is null, treated as failure for composition APIs.

- `OnFailure(Action<IEnumerable<string>> action) : Result<T>`
  - Executes only when failed; passes either existing errors or a default error if none.

- `Map<TOut>(Func<T, TOut> func) : Result<TOut>`
  - If failed: propagate errors.
  - If success and `T` nullable: executes even when `Value` is null.
  - If success and `T` is non-nullable but `Value` unexpectedly null: returns failure with a specific message.

- `Bind<TOut>(Func<T, Result<TOut>> func) : Result<TOut>`
  - Same nullability semantics as `Map`.

- `Ensure(Func<T, bool> condition, string errorMessage) : Result<T>`
  - If failed: returns original.
  - If success and `Value` is null: returns failure with `ResultConstants.ConditionEvaluationWithNullValue`.
  - If condition false: returns failure with `errorMessage`.

- `Tap(Action<T> action) : Result<T>`
  - Same nullability semantics as `OnSuccess`.

- `Combine(params Result[] results) : Result<T>`
  - Aggregates errors from `results` and `this` (if failed). If any errors: returns failed `Result<T>`; otherwise, returns current successful result (may preserve null value).

- `Match<TOut>(Func<T, TOut> onSuccess, Func<IEnumerable<string>, TOut> onFailure) : Result<TOut>`
  - If failed: wraps the result of `onFailure(errorsOrDefault)` as `Success(TOut)`.
  - If success: wraps `onSuccess(Value)` as `Success(TOut)`. The success branch is invoked even if `Value` is null (consistent with tests).

- Recovery
  - `Recover(Func<Result<T>> recoverFunc) : Result<T>`
  - `RecoverWith<TOut>(Func<Result<TOut>> recoverFunc) : Result<TOut>`
    - When already successful: tries to convert `Value` to `TOut`; if incompatible, returns failure with conversion error.

### Usage Examples

```csharp
Result<string> r = Result<string>.Success("hello");
var length = r.Map(s => s.Length); // Success(5)

var parsed = Result<string>.Success("42").Bind(s =>
    int.TryParse(s, out var n)
        ? Result<int>.Success(n)
        : Result<int>.WithFailure("Parse failed"));

var ensured = Result<string>.Success("hello").Ensure(s => s.Length > 0, "empty"); // success
```

Nullability examples (from tests):

```csharp
Result<int?> ri = new Result<int?>(true, errors: null, value: null);
ri.OnSuccess(_ => /* runs */);
ri.Map(i => (i ?? 0) + 1); // Success(1)
ri.Bind(i => Result<string>.Success((i ?? 0).ToString())); // Success("0")
```

---

## Extensions (Validation & Cancellation)

Namespace: `IndQuestResults.Operations`

- `ResultExtensions.Cancelled()` / `Cancelled<T>()`
  - Convenience helpers that return a failed Result with a well-known cancelled message `ResultErrors.OperationCancelled`.

- `IsCancelled(this Result result)` / `IsCancelled<T>(this Result<T> result)`
  - True if errors contain `ResultErrors.OperationCancelled`. Null-safe.

- Validation helpers
  - `FailForNullArgument<T>(string parameterName, string? message = null)` → failed `Result<T>` with `NullArgumentError`.
  - `FailForNullArguments<T>(params string[] parameterNames)` → failed `Result<T>` with `MultipleNullArgumentsError`.
  - Non-generic counterparts: `FailForNullArgument`, `FailForNullArguments`.
  - `EnsureNotNull<T>(T? value, string parameterName)` overloads for class and nullable structs.
  - `ValidateNotNull(params (object? value, string parameterName)[] validations)`
    - Returns `Result.Success()` if all values are non-null; otherwise a failed `Result` with the appropriate errors.
  - `CreateIfValid<T>(Func<T> factory, params (object? value, string parameterName)[] validations)`
    - Returns `Result<T>.Success(factory())` if all values are non-null; else failed `Result<T>` with appropriate error(s).

### Examples

```csharp
var created = ResultExtensions.CreateIfValid(
    factory: () => new User("abc"),
    (value: "abc", parameterName: nameof(User.Id))
);

var validated = ResultExtensions.ValidateNotNull(
    (user, nameof(user)),
    (order, nameof(order))
);
```

---

## Async Extensions

Namespace: `IndQuestResults.Extensions.Async`

- `BindAsync<TIn,TOut>(this Task<Result<TIn>>, Func<TIn, Task<Result<TOut>>>, CancellationToken)`
  - If cancellation requested or `OperationCanceledException` thrown: returns cancelled.
  - Otherwise applies same semantics as sync `Bind`.

- `MapAsync<TIn,TOut>(this Task<Result<TIn>>, Func<TIn, Task<TOut>>, CancellationToken)`
  - Same success/failure propagation as sync `Map`.

- `TapAsync<T>(this Task<Result<T>>, Func<T, Task>, CancellationToken)`
  - Executes side effect only on success and returns original result.

- `RecoverAsync<T>(this Task<Result<T>>, Func<Task<Result<T>>>, CancellationToken)`
  - On failure: invokes recovery function.

- `TraverseAsync<TIn,TOut>(IEnumerable<TIn>, Func<TIn, Task<Result<TOut>>>, CancellationToken)`
- `TraverseParallelAsync<TIn,TOut>(..., int maxDegreeOfParallelism = 4, CancellationToken)`
  - Turns many `Task<Result<TOut>>` into `Result<IEnumerable<TOut>>` by sequencing and aggregating errors via `ResultCollections.Sequence`.

- `SequenceAsync<T>(IEnumerable<Task<Result<T>>>, CancellationToken)`

### Async Example

```csharp
Task<Result<User>> user = GetUserAsync(id);
var profile = await user
    .BindAsync(u => LoadProfileAsync(u.Id), cancellationToken)
    .TapAsync(p => CacheAsync(p), cancellationToken);
```

---

## Behavior Summary (from unit tests)

- Success constructors set success flags and keep `Errors` empty.
- Failure constructors handle null/empty errors by injecting a default message.
- `Map`/`Bind` propagate failures without executing functions.
- `Ensure` on success converts to failure when predicate fails; is a no-op on failures.
- `Combine` aggregates errors across many results; returns success only if none have errors.
- `Match` calls the correct branch; for `Result<T>`, returns a successful `Result<TOut>` with the branch value.
- Nullability rules allow operations to proceed for nullable `T` even when `Value` is null.
- Async helpers preserve cancellation and failure semantics.

---

## Practical Patterns

- Validation first, then creation
```csharp
var userResult = ResultExtensions.CreateIfValid(
    factory: () => new User(input.Id!),
    (input.Id, nameof(input.Id))
);
```

- Pipelining
```csharp
var answer =
    from s in Result<string>.Success("42")
    from n in Result<int>.Success(int.Parse(s))
    select n; // using Map/Bind equivalents
```

- Aggregating
```csharp
var combined = Result.Success()
    .Combine(Result.WithFailure("E1"), Result.Success(), Result.WithFailure("E2"));
```

---

## Notes and Guarantees

- Immutable, thread-safe results; operations create new instances.
- High-performance error string formatting for small collections.
- `Result<T>.Match` guarantees the failure branch receives at least one error (default if none present).
- `RecoverWith<TOut>` attempts safe conversion; returns typed failure on incompatibility.

---

## See Also

- Unit tests: `IndQuestResults.Tests.Unit.Operations.ResultTests`, `IndQuestResults.Tests.Unit.Operations.ResultGenericTests`
- Async extensions: `IndQuestResults.Extensions.Async.ResultAsync`
- Validation extensions and errors: `IndQuestResults.Operations.ResultExtensions`, `IndQuestResults.Validation.*`
