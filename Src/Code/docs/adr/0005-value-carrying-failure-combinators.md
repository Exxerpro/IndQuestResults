# ADR 0005: Value-Carrying Failure Combinators (EnsureOrFault / value-aware TapError)

## Status
Accepted — targeted for 1.7.0

## Context

ADR 0004 (shipped in 1.5.0) established a family of *failure-value-preserving* railway combinators
— `ThenStep`, `MatchAsync`, `ElseWithValue`, `TapErrorAsync`, and the sync `RequireValue` — so that a
mid-chain failure could carry a projected diagnostic value (typically a response DTO) all the way to a
terminal, instead of having it dropped by the type-transitioning `Bind`/`Map`/`Then` rebuild
(`Result<TOut>.WithFailure(Errors)`). 1.6.0 continued that line. Refactoring IndTraceV2025's command
handlers (166 guard-clause occurrences across ~147 files) onto those chains is now underway
(Exxerpro/IndTraceV2025#174, #176).

Two gaps remain in that surface that keep specific handlers on hand-rolled guards:

1. **Validation that fails *with a value*.** The existing `Result<T>.Ensure(predicate, errorMessage)`
   can only fail with a *string*. But the handlers' validation steps must fail carrying a projected
   failure DTO (`WithFailure(errors, dto)`) required by the PLC gateway response contract. There is no
   `Ensure` whose failure branch is caller-supplied and receives the current value, so handlers fall
   back to an explicit `if (!predicate) return WithFailure(errors, BuildDto(value));`.

2. **A failure-side tap that can read the carried value.** `TapError(Action<IEnumerable<string>>)`
   exposes only the errors. When the handler's failure audit/log needs the carried DTO (or other
   failure value) it cannot get it from the tap and must re-open the result by hand.

## Decision

Add two combinator families to `IndQuestResults.Operations.ResultExtensions` (already globally imported
by consumers). Both follow the ADR 0004 contract: short-circuits return the **original instance** so
value-carrying failures survive, and nothing is swallowed.

| Member | Shape | Purpose |
|---|---|---|
| `EnsureOrFault<T>` | `Result<T> → (Func<T,bool>, Func<T,Result<T>>) → Result<T>` (+ `Task` source) | Value-carrying `Ensure`. On a success with a non-null value: predicate true returns the original success; predicate false returns `onFalse(value)` — a caller-supplied, typically value-carrying `WithFailure`. Upstream failures **and** success-with-null short-circuit unchanged. |
| `TapError<T>` (value-aware) | `Result<T> → Action<IReadOnlyList<string>, T?> → Result<T>` (+ `Task` source) | Failure-side tap that receives the errors **and** the failure's carried value (may be null); fires only on `IsFailure`; returns the original instance; swallows nothing. |

### Design rules (consistent with ADR 0004)
- **Short-circuits return the same instance.** `EnsureOrFault` routes on `IsSuccess && Value is not null`
  (mirroring `ThenStep`), so both an upstream failure and a success-with-null pass through the original
  instance untouched — `Value`, `Exception`, warnings and metadata all survive. The predicate is never
  evaluated against a null value.
- **The failure branch is caller-owned.** Unlike `Ensure`, `EnsureOrFault` does not synthesize the
  failure; `onFalse` receives the current value and decides the errors and carried value. This is what
  lets a validation step attach a failure DTO that later chain steps preserve.
- **Value-aware `TapError` complements, never shadows, the string-only overload.** The two-argument
  `Action<IReadOnlyList<string>, T?>` has a different delegate arity than the existing
  `Action<IEnumerable<string>>`, so overload resolution is unambiguous: a one-argument lambda still binds
  to the ADR-era string-only `TapError`, a two-argument lambda binds to the new one. No existing call
  site changes meaning. `IReadOnlyList<string>` is used for the error parameter so callers get indexed
  access; the materialization reuses the result's backing array when it already implements the interface.
- **Exceptions propagate.** Neither verb has a `catch`; exceptions from the predicate, `onFalse`, or the
  tap action reach the caller. Handlers keep their own `try/catch` producing byte-identical exception
  DTOs, exactly as with the ADR 0004 family.

### Naming
`EnsureOrFault` is a new verb rather than an `Ensure` overload: an
`Ensure<T>(this Result<T>, Func<T,bool>, Func<T,Result<T>>)` would compile, but the name `Ensure` already
signals "fail with a message"; `EnsureOrFault` makes the value-carrying failure branch explicit at the
call site. The value-aware `TapError` reuses the `TapError` name deliberately — it is the same verb
(a non-mutating failure-side side effect), differentiated only by the richer delegate.

## Consequences

**Positive**
- Handlers express validation-that-fails-with-a-DTO as one fluent step:
  ```csharp
  return await LoadAsync(cmd, ct)
      .EnsureOrFault(
          s => s.IsValid,
          s => Result<State>.WithFailure(s.ValidationErrors, s with { FailureDto = BuildDto(s) }))
      .ThenStep(s => ExecuteAsync(s, ct))
      .TapError((errors, s) => _logger.LogError("Failed: {Errors} dto={Dto}", errors, s?.FailureDto))
      .MatchAsync(
          s => Result<Dto>.Success(Project(s)),
          (errors, s) => Result<Dto>.WithFailure(errors, s?.FailureDto));
  ```
- Closes the two gaps blocking the IndTraceV2025 command-handler refactor
  (Exxerpro/IndTraceV2025#174, #176) — validation with a carried DTO, and a failure tap that can read it.
- Additive: no existing member changes semantics; consumers recompile without edits, and the string-only
  `TapError` continues to resolve for existing one-argument call sites.

**Negative / accepted trade-offs**
- `EnsureOrFault` is same-type only (`Result<T> → Result<T>`), like `ThenStep`; a type-transitioning
  failure cannot carry a `T` value by construction.
- The tri-state (`IsSuccess`/`IsFailure` both false for success-with-null) remains; `EnsureOrFault`
  codifies it by passing success-with-null through untouched rather than treating it as a validation
  failure — deliberately unlike `Result<T>.Ensure`, which fails a null-valued success.
- Two `TapError` overloads now coexist; the arity difference keeps resolution unambiguous but authors
  must pick the delegate that matches their need.

## Verification
- New unit tests (`ResultValueCarryingCombinatorsTests`) with mutation-killing assertions:
  `ReferenceEquals` on every short-circuit (upstream failure, success-with-null, success-passthrough,
  tap-returns-original); invocation counters proving the predicate/tap do not fire on the non-taken
  branch; the carried failure value asserted on the `onFalse` result and on the value-aware `TapError`
  callback (including the null-value case for a value-less failure); async/`Task`-source coverage for
  both verbs; and an explicit test that a one-argument lambda still binds to the string-only `TapError`
  overload (no overload-resolution regression), with exceptions asserted to propagate un-swallowed.
- Package version is intentionally **not** bumped here; the 1.7.0 release handles versioning and the
  public-API diff.
