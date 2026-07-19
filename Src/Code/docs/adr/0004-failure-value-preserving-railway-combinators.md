# ADR 0004: Failure-Value-Preserving Railway Combinators (ThenStep / MatchAsync / ElseWithValue / TapErrorAsync / sync RequireValue)

## Status
Accepted — shipped in 1.5.0

## Context

### The consumer guard-clause problem
IndTraceV2025 (and other consumers) write command handlers full of this guard:

```csharp
var loadResult = await loader.LoadAsync(request, ct);
if (loadResult.IsFailure || loadResult.Value is null)
{
    _logger.LogError("Failed: {Errors}", string.Join(", ", loadResult.Errors));
    var dto = BuildFailureDto(loadResult.Errors.FirstOrDefault(), command.MachineId);
    return Result<TaskGatewayResponseDto>.WithFailure(loadResult.Errors, dto);
}
```

166 occurrences across ~147 files in IndTraceV2025 alone. Refactoring these to fluent railway
chains stalls on two library semantics:

1. **The tri-state.** `Result<T>.IsSuccess` is `IsRecoverable && Value is not null` while
   `IsFailure` is `!IsRecoverable` — a "success carrying a null Value" is *neither* success nor
   failure. The `IsFailure || Value is null` guard is exactly `!IsSuccess`; any replacement
   combinator must route on `IsSuccess`, not `IsFailure`, or null-success slips through.

2. **Failure values are dropped.** Every type-transitioning combinator (`Bind`, `Map`, `Then`,
   `ThenAsync`, `BindAsync`, …) rebuilds a propagated failure as
   `Result<TOut>.WithFailure(Errors)` — the failure's carried `Value` and `Exception` are lost.
   But the handlers' failure paths *carry a projected DTO* (`WithFailure(errors, dto)`) that the
   PLC gateway contract requires in the response. No existing combinator lets a mid-chain step
   attach a failure DTO and have it survive to the end of the chain.

Additionally `MatchValue` routes on `IsRecoverable`, so its success branch receives `null!` for a
success-with-null — unusable as a safe terminal for this refactor. And the failure-side tap family
(`TapError`, `ThenLogErrors`) is sync-only, while the handlers' failure paths perform async audits.

## Decision

Add five small combinator families to `IndQuestResults.Operations` (already globally imported by
consumers), in `ResultStepExtensions.cs` except where noted:

| Member | Shape | Purpose |
|---|---|---|
| `ThenStep<T>` | `Result<T> → Func<T, Result<T>> → Result<T>` (+ `Task` source with sync/async step) | Same-type railway step. Runs iff `IsSuccess`; **any short-circuit returns the original instance**, so value-carrying failures (and success-with-null) pass through untouched. |
| `MatchAsync<T,TOut>` | `Task<Result<T>> → (T → TOut, (errors, T?) → TOut) → Task<TOut>` (+ async branches) | Terminal that exposes the carried failure value. Routes on `IsSuccess`; success branch can never observe null; success-with-null routes to `onFailure(empty errors, null)`. |
| `ElseWithValue<T>` | `Result<T> → (errors → T?) → Result<T>` (+ `Task` source) | Chainable failure-value projection: attaches a value to a value-LESS failure (preserving `Errors` + `Exception`); a null projection or an existing failure value leaves the original instance. |
| `TapErrorAsync<T>` | `Task<Result<T>> → (errors → Task) → Task<Result<T>>` (in `ResultErrorExtensions.cs`) | Async mirror of `TapError`: fires only on `IsFailure`, returns the original instance, swallows nothing. |
| `RequireValue<T>` (sync) | `Result<T?> → string → Result<T>` (in `ResultExtensions.cs`) | Sync twin of the existing `Task<Result<T?>>` overload: failure propagates first, success-null becomes a failure with the given message. |

### Design rules (uniform across all five)
- **No `catch` blocks.** Exceptions and cancellation propagate to the caller. Handlers keep their
  own `try/catch` producing byte-identical exception DTOs. This deliberately differs from
  `ThenAsync`/`BindAsync`, which wrap exceptions into `"Async bind operation failed: …"` failures.
- **No cancellation-token pre-checks.** Handlers pass tokens to steps via closure (the existing
  `ThenAsyncCancellable` remains available when a pre-check is wanted). A pre-check would inject
  `Cancelled<T>()` errors where current handler behavior returns nothing extra.
- **Short-circuits return the same instance** — `ReferenceEquals`-verifiable, locked by tests —
  so `Value`, `Exception`, warnings and metadata on failures survive the rest of the chain.
- Null-argument handling is `ArgumentNullException.ThrowIfNull`, matching the current
  `MatchValue`/`TapErrorAsync`-era house style.

### Naming
A same-type `Then<T>` overload is impossible: it would be CS0121-ambiguous with the existing
`Then<TIn, TOut>` whenever `TIn == TOut`. Hence the new verb `ThenStep`. `MatchAsync` is distinct
from `MatchValue` because its contract (route on `IsSuccess`, expose the failure value) is
different, not merely async.

### 1.4.1 API parity restoration (same release)
The shipped 1.4.1 package was built from a source state ahead of the `Kat3` branch tip (the
nuspec's commit claim is stale). The members present in the shipped DLL but missing from source —
`Result.Success<T>`, the five `Then` bridging overloads, `Ensure` on `Task<Result<T>>`,
`ToResult` (×2), `RequireValue` on `Task<Result<T?>>`, and chainable `ValidateNotNull(selector)` —
are restored in this release so 1.5.0 is a strict superset of the shipped 1.4.1 surface.
Without this, upgrading consumers (e.g. `CreateBarCodeCommandHandler`'s existing
`ValidateNotNull(...).Ensure(...).ThenAsync(...)` chain) would fail to compile.

## Consequences

**Positive**
- The consumer guard collapses to a chain; the target shape for a handler becomes:
  ```csharp
  return await LoadAsync(cmd, ct)
      .ThenStep(s => Validate(s))                    // failure DTO attached inside the step
      .ThenStep(s => ExecuteAsync(s, ct))
      .ThenTap(s => AuditAsync(s, ct))
      .MatchAsync(
          s => Result<Dto>.Success(Project(s)),
          (errors, s) => Result<Dto>.WithFailure(errors, s?.FailureDto));
  ```
- Failure DTOs, exceptions and warnings survive chains without hand-rolled guards.
- 1.5.0 is additive: no existing member changed semantics; consumers recompile without edits.

**Negative / accepted trade-offs**
- Two exception philosophies now coexist (`ThenAsync` wraps; `ThenStep` propagates). Documented
  in XML docs; the difference is the point — `ThenStep` serves handlers that own their `try/catch`.
- `ThenStep` is same-type only. Type transitions still go through `Then`/`ThenAsync` and lose
  failure values — by design, since a `TOut`-typed failure cannot carry a `TIn` value.
- The tri-state (`IsSuccess`/`IsFailure` both false for success-with-null) remains; these
  combinators codify rather than fix it. A future major version may revisit the tri-state itself.

## Verification
- 69 new unit tests (`ResultStepExtensionsTests`, `ResultTerminalMatchAsyncTests`,
  `ResultElseWithValueTests`, `ResultTapErrorAsyncTests`, `ResultRequireValueSyncTests`,
  `ResultPipelineParityTests`) with mutation-killing assertions: `ReferenceEquals` on every
  short-circuit, invocation counters on non-taken branches, exact default-error-substitution
  message, exception propagation asserted un-wrapped.
- Public-API diff against the shipped 1.4.1 DLL (ilspycmd decompilation) confirms 1.5.0 ⊇ 1.4.1.
