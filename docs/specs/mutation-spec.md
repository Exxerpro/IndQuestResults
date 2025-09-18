# Mutation Hardening Specs (TDD)

This document defines explicit, testable behavior for boundary and guard cases to guide TDD and mutation testing.

Cancellation Semantics
- WrapWithTimeout<T> returns Timeout failure only when the timeout token cancels AND the external token did not cancel.
- If the external token cancels (regardless of timeout), the operation returns Cancelled (not Timeout).
- Non-generic and generic Wrap* success cases must not be reported as Cancelled when no token is cancelled.

Formatting/Combining Thresholds (functional invariants)
- FormatErrorsString output must be identical across small/large paths; for any inputs, exact string equals "{prefix}: err1, err2, ...".
- CombineErrors returns NoErrorsFoundMessage only when both inputs are null or empty; order of errors is primary then secondary.

Result<T> Null/Coalescing Behavior
- Match failure branch MUST supply DefaultErrorMessage when Errors is null OR empty.
- Map/Bind/OnSuccess execute when T is nullable (T?) even if Value is null; for non-nullable T, null Value does not execute and maps/binds fail with explicit messages.

IsCancelled Guards
- IsCancelled returns true only when Errors contains OperationCancelled; returns false when result is null, Errors is null/empty, or contains other messages.



Concurrency and Race Conditions
- Immutability/Thread-Safety: Result and Result<T> instances MUST be immutable; concurrent calls to composition APIs MUST NOT mutate internal state or flip flags after construction.
- Single-invocation guarantee: OnSuccess/Tap and async counterparts (TapAsync) MUST invoke the supplied delegate at most once per call site, even under concurrent evaluation.
- Error collections visibility: Errors exposed by Result/Result<T> MUST be stable snapshots; concurrent reads MUST observe consistent contents (no interleaving or partial updates).
- Async cancellation precedence (extended): If an external CancellationToken is canceled, the operation MUST return Cancelled (not Timeout), and MUST NOT later be reported as Timeout. If both external and timeout tokens cancel, external cancellation MUST win.
- Async side effects: TapAsync MUST NOT run the side-effect when cancellation has been requested; partial side effects MUST be avoided or compensated. A failed/canceled side-effect MUST NOT result in a success state.
- Async bind/map atomicity: MapAsync/BindAsync MUST either propagate failure/cancellation or produce exactly one consistent success. No duplicate downstream invocations are permitted under concurrency.
- Determinism in aggregation: Combine and CombineErrors MUST be deterministic with respect to input order (original, then parameters in order). Concurrency MUST NOT reorder or duplicate errors.
- Match stability under concurrency: Result<T>.Match failure branch MUST always receive at least one error (defaulted when empty or null); this MUST hold under concurrent access to Errors.
