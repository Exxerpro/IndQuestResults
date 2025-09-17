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

