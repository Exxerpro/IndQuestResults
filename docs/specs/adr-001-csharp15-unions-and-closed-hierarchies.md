# ADR-001: Opt-Out of C# 15 / .NET 11 `union` Types and `closed` Hierarchies for Core `Result` Types

- **Status:** Accepted (Opted Out — Deferred for Future Re-evaluation if CLR/Language Lowering Changes)
- **Date:** 2026-11-15
- **Evaluated via:** BMAD v6.12.4 Party Mode Architecture Review
- **Target Frameworks Evaluated:** `net10.0` (LTS) and `net11.0` (STS) with C# 15 (`SDK 11.0.100`)

---

## Context

With the release of **.NET 11** and **C# 15**, two headline algebraic data type (ADT) features were introduced to the language:
1. **Union Types (`public union U(A, B)`)** and the custom `[Union]` / `IUnion` pattern.
2. **Closed Hierarchies (`public closed abstract class` / `public closed record`)**, restricting direct inheritance to the declaring assembly and enabling compiler-verified exhaustive `switch` expressions without a discard (`_ =>`) arm.

At first glance, a functional `Result` / `Result<T>` library appears to be the textbook candidate for discriminated unions and closed class hierarchies. We conducted due diligence and a full BMAD architectural evaluation to determine whether `IndQuestResults` should adopt these features.

---

## Decision

**We are opting out of introducing C# 15 `union` types and `closed` hierarchies into `IndQuestResults` at this time.**

`Result` and `Result<T>` will remain `public sealed class` implementations, and all future evolutions on the `1.x` line will continue to honor our **strictly additive, non-breaking API promise** to downstream consumers.

---

## Technical Rationale

### 1. Silent Heap Boxing in Standard C# 15 `union` Declarations (`IUnion.Value`)
When declaring a nominal union using the concise C# 15 syntax:
```csharp
public union Outcome<T>(T, Error);
```
The Roslyn compiler lowers the union into a `struct` implementing `System.Runtime.CompilerServices.IUnion`, which stores its payload inside a single **`object? Value`** backing field:
```csharp
public interface IUnion
{
    object? Value { get; }
}
```
- **Performance Penalty:** Every value-type `T` (`int`, `double`, `bool`, `Guid`, `DateTime`, `ReadOnlyMemory<T>`, or custom domain `struct`s) passed into a standard `union` is **boxed onto the managed heap** upon construction.
- **Misalignment with Library Goals:** `IndQuestResults` is heavily engineered around zero-allocation fast paths, `Span<char>` stack-allocated formatting, and minimal GC pressure in high-throughput pipelines. Introducing hidden boxing on every value-type `Result<T>` violates our core performance guarantees.

### 2. Prohibitive Ceremony and Type-Collision Flaws in Non-Boxing Custom `[Union]` Structs
To avoid the `object?` boxing penalty in C# 15, a type must opt out of the `union` keyword and manually implement the `[System.Runtime.CompilerServices.Union]` attribute pattern:
```csharp
[Union]
public readonly struct CustomResultUnion<T> : IUnion
{
    private readonly T? _value;
    private readonly string[]? _errors;
    private readonly byte _tag;

    // Still forced by IUnion to expose a boxing object? getter:
    public object? Value => _tag switch { 1 => _value, 2 => _errors, _ => null };

    public bool HasValue => _tag != 0;
    public bool TryGetValue(out T value) { ... }
    public bool TryGetValue(out string[] value) { ... }
}
```
- **Ugly, Boilerplate-Heavy Syntax:** The non-boxing pattern requires manual `_tag` management, nullable backing fields for every case simultaneously (bloating struct size to the sum of all cases rather than a true overlapped C-style union), and repetitive `TryGetValue(out ...)` methods.
- **Generic Type-Parameter Ambiguity:** In a generic `Result<T>`, if a consumer instantiates `Result<string[]>` or `Result<Error>`, the compiler-required `TryGetValue(out T)` and `TryGetValue(out Error)` overloads collide with identical signatures (`CS0111`), making a general-purpose non-boxing generic union brittle or impossible without wrapper types at every call site.

### 3. Violation of Our Additive-Only Compatibility Promise
`IndQuestResults` is the foundational functional library across multiple production repositories targeting both `net10.0` (LTS) and `net11.0`. We have committed to **strictly additive changes** for our consumers:
- Converting `public sealed class Result<T>` into a `public closed abstract class Result<T>` (with derived `Success<T>` / `Failure<T>` subclasses) or a `union` struct is a **binary and source breaking change**.
- Unsealing `Result<T>` breaks existing `[JsonConstructor]` deserialization contracts, public constructors, and JIT devirtualization optimizations that rely on `Result` and `Result<T>` being `sealed`.
- Multi-targeting `net10.0;net11.0` would require either polyfilling .NET 11 compiler attributes or splitting the public type surface across target frameworks.

### 4. Rich Product-State Semantics vs. Pure Sum-Type Constraints
Unlike naive textbook `Ok<T> | Error<E>` types, `IndQuestResults.Result<T>` intentionally models rich real-world operational telemetry across state boundaries:
- **Diagnostic Warnings & Telemetry on Success:** `WithWarnings(warnings, value, confidence, missingDataRatio)` carries `Warnings`, `Confidence`, and `MissingDataRatio` alongside a valid `Value`.
- **Value-Carrying Failures:** `WithFailure(errors, defaultValue, exception)` and `EnsureOrFault` intentionally preserve a fallback or diagnostic `T? Value` alongside `Errors` and `Exception?` even when `IsFailure == true`.
- Forcing `Result<T>` into a mutually exclusive sum type (`Success` vs. `Failure`) either strips away carried failure values and warning telemetry or explodes the union into 4+ cases, ruining pattern-matching ergonomics.

---

## Conditions for Future Re-evaluation

This decision is documented so that we can revisit it in a future .NET / C# release (e.g., C# 16+ / .NET 12+) **if and only if**:
1. **CLR / Language Support for True Non-Boxing Generic Unions:** The runtime introduces layout-optimized, zero-boxing generic unions without requiring manual `[Union]` + `TryGetValue` ceremony or `object? Value` boxing under `IUnion`.
2. **Additive Extension Unions / Active Patterns:** C# introduces active patterns or extension-based exhaustive matching that can project `Result<T>` into an exhaustive `switch` **additively** without unsealing `Result<T>` or altering its memory layout.
3. **Major Version Window:** A planned major version (`v2.0`+) where all downstream consumers have migrated past `net10.0` LTS.
