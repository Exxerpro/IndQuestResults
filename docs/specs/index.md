## IndQuestResults Specifications Index

This is the entry point for all Result-related specifications and documentation.

### Documents

- [Result Manual](../Result-Manual.md)
- [Result and Result<T> Specification](./result-spec.md)
- [Mutation Hardening Specs](./mutation-spec.md)

### Recommended Reading Order

1. [Result Manual](../Result-Manual.md): Conceptual overview, API tour, examples
2. [Result and Result<T> Specification](./result-spec.md): Normative, test-backed behaviors (MUST/SHOULD)
3. [Mutation Hardening Specs](./mutation-spec.md): Guardrails and invariants for mutation testing

### Inline ToC

- Result Manual
  - [Overview](../Result-Manual.md#overview)
  - [Non-generic Result](../Result-Manual.md#non-generic-result)
  - [Generic Result<T>](../Result-Manual.md#generic-resultt)
  - [Extensions (Validation & Cancellation)](../Result-Manual.md#extensions-validation--cancellation)
  - [Async Extensions](../Result-Manual.md#async-extensions)
  - [Behavior Summary](../Result-Manual.md#behavior-summary-from-unit-tests)
  - [Practical Patterns](../Result-Manual.md#practical-patterns)
  - [Notes and Guarantees](../Result-Manual.md#notes-and-guarantees)
  - [See Also](../Result-Manual.md#see-also)

- Result and Result<T> Specification
  - [Terminology](./result-spec.md#terminology)
  - [Non-generic Result](./result-spec.md#non-generic-result)
  - [Generic Result<T>](./result-spec.md#generic-resultt)
  - [Derived Invariants](./result-spec.md#derived-invariants-cross-cutting)
  - [Conformance Examples](./result-spec.md#conformance-examples-non-normative)
  - [References](./result-spec.md#references)

- Mutation Hardening Specs
  - [Cancellation Semantics](./mutation-spec.md#cancellation-semantics)
  - [Formatting/Combining Thresholds](./mutation-spec.md#formattingcombining-thresholds-functional-invariants)
  - [Result<T> Null/Coalescing Behavior](./mutation-spec.md#resultt-nullcoalescing-behavior)
  - [IsCancelled Guards](./mutation-spec.md#iscancelled-guards)

### Traceability

Spec statements are derived from unit tests in:
- `Src/Code/tests/IndQuestResults.Tests.Unit/Operations/ResultTests.cs`
- `Src/Code/tests/IndQuestResults.Tests.Unit/Operations/ResultGenericTests.cs`

When tests change, update the specification accordingly to maintain 1:1 coverage.

### Contributing

- Keep edits minimal and aligned with existing style.
- Use MUST/SHOULD/MAY language for normative clarity.
- Cross-reference test names when adding new behaviors, where helpful.
