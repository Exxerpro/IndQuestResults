# ADR 0003: Add Metadata Dictionary to Result<T>

## Status
Proposed

## Context
Multiple teams using IndQuestResults have independently created custom Result subclasses or wrapper types to attach metadata to operation results. Common use cases include:

1. **Quality metrics**: OCR confidence, image quality scores, validation success rates
2. **Processing context**: Source system, processing time, transformation steps applied
3. **Audit information**: User ID, timestamp, correlation IDs
4. **Multi-source reconciliation**: Source reliability scores, conflict markers, fusion decisions

This pattern proliferation leads to:
- **Inconsistency**: Each team implements their own metadata attachment mechanism
- **Duplication**: Similar code repeated across projects
- **Lost type safety**: Metadata patterns not standardized or reusable
- **Integration friction**: Difficult to compose Results from different systems

### Real-world example
The ExxerCube.Prisma project needs to attach `ExtractionMetadata` (OCR confidence, image quality, pattern validation results) to extracted `Expediente` entities for multi-source data fusion. Without built-in metadata support, they considered:
- Creating custom `ExtractionResult<T>` wrapper (duplicates Result semantics)
- Adding metadata to domain entities (violates separation of concerns)
- Extension properties (not fully supported in .NET 10 yet)

## Decision
Add an optional `Metadata` property to `Result<T>` as a `Dictionary<string, object?>` with type-safe accessor extension methods.

### API Design

**Core modification to Result<T>:**
```csharp
public class Result<T>
{
    public T Value { get; }
    public bool IsSuccess { get; }
    public string Error { get; }

    /// <summary>
    /// Optional metadata dictionary for attaching supplementary information to results.
    /// Common uses: quality metrics, processing context, audit info, source reliability.
    /// Use nameof() for type-safe keys: SetMetadata(nameof(MyMetadata), value)
    /// </summary>
    public Dictionary<string, object?>? Metadata { get; set; }
}
```

**Extension methods for type-safe access:**
```csharp
public static class ResultMetadataExtensions
{
    /// <summary>
    /// Sets metadata value using nameof() pattern for type-safe keys.
    /// Example: result.SetMetadata(nameof(ExtractionMetadata), metadata);
    /// </summary>
    public static void SetMetadata<T>(this Result<T> result, string key, object? value)
    {
        result.Metadata ??= new Dictionary<string, object?>();
        result.Metadata[key] = value;
    }

    /// <summary>
    /// Gets strongly-typed metadata value.
    /// Returns default(TValue) if key not found or type mismatch.
    /// </summary>
    public static TValue? GetMetadata<T, TValue>(this Result<T> result, string key)
    {
        if (result.Metadata?.TryGetValue(key, out var value) == true && value is TValue typed)
            return typed;
        return default;
    }

    /// <summary>
    /// Tries to get strongly-typed metadata value.
    /// Returns false if key not found or type mismatch.
    /// </summary>
    public static bool TryGetMetadata<T, TValue>(this Result<T> result, string key, out TValue? value)
    {
        if (result.Metadata?.TryGetValue(key, out var obj) == true && obj is TValue typed)
        {
            value = typed;
            return true;
        }
        value = default;
        return false;
    }
}
```

### Usage pattern (type-as-key):
```csharp
// Producer side (extractor/parser):
public class ExtractionMetadata
{
    public double Confidence { get; set; }
    public int PatternMatches { get; set; }
}

var result = Result<Document>.Success(document);
// Type inferred from parameter - typeof(ExtractionMetadata).FullName used as key
result.SetMetadata(new ExtractionMetadata
{
    Confidence = 0.87,
    PatternMatches = 25
});

// Consumer side (fusion/validation):
// Type IS the key - no string literals needed
var metadata = result.GetMetadata<Document, ExtractionMetadata>();
if (metadata?.Confidence > 0.85)
{
    // High confidence, auto-process
}

// Safe pattern with TryGetMetadata:
if (result.TryGetMetadata<Document, ExtractionMetadata>(out var meta))
{
    Console.WriteLine($"Confidence: {meta.Confidence}");
}
```

## Consequences

### Positive
- ✅ **Single implementation**: All teams use same metadata mechanism
- ✅ **Backward compatible**: Metadata is optional (nullable), existing code unaffected
- ✅ **Type-safe**: nameof() pattern prevents magic strings, enables refactoring
- ✅ **Flexible**: Dictionary allows any metadata type without generic explosion
- ✅ **Discoverable**: Extension methods show up in IntelliSense
- ✅ **Prevents proliferation**: Stops custom Result subclasses/wrappers
- ✅ **Serializable**: Dictionary<string, object?> works with common serializers

### Negative
- ⚠️ **Runtime type safety only**: Type mismatch detected at runtime, not compile time
- ⚠️ **Nullable reference**: Metadata is optional, null checks required
- ⚠️ **Boxing for value types**: Value type metadata gets boxed in object?
- ⚠️ **Serialization complexity**: object? requires serializer configuration for polymorphism

### Mitigation strategies
1. **Convention over magic strings**: Document nameof() pattern in all examples
2. **Extension method guidance**: Provide GetMetadata<T, TValue>() for type safety
3. **Null safety**: Extension methods handle null metadata gracefully
4. **Serialization docs**: Provide guidance for JSON/XML serialization with metadata
5. **Roslyn analyzer** (future enhancement): Create analyzer to warn on string literals in SetMetadata/GetMetadata calls, encouraging nameof() usage

## Implementation checklist

### Phase 1: Core Feature (v1.2.0)
- [ ] Add `Metadata` property to Result and Result<T> classes
- [ ] Add ResultMetadataExtensions with SetMetadata/GetMetadata/TryGetMetadata
- [ ] Write unit tests for metadata operations
- [ ] Write unit tests for type safety (correct/incorrect types)
- [ ] Write unit tests for null metadata handling
- [ ] Update XML documentation with nameof() usage examples
- [ ] Update README with metadata usage patterns
- [ ] Bump version to 1.2.0 (minor version, new feature)
- [ ] Run mutation tests (ensure coverage)
- [ ] Package and publish to NuGet

### Phase 2: Quality Enforcement (v1.3.0 - Future)
- [ ] Create Roslyn analyzer project IndQuestResults.Analyzers
- [ ] Implement analyzer rule: Warn on string literal in SetMetadata(string key, ...) - suggest nameof()
- [ ] Implement analyzer rule: Warn on string literal in GetMetadata<T, TValue>(string key) - suggest nameof()
- [ ] Implement code fix provider: Auto-convert "MyType" → nameof(MyType) where applicable
- [ ] Write analyzer unit tests using Microsoft.CodeAnalysis.Testing
- [ ] Package analyzer as NuGet package with DevelopmentDependency=true
- [ ] Document analyzer rules in README

## Alternatives considered

### Alternative 1: Generic metadata parameter Result<T, TMeta>
```csharp
public class Result<T, TMeta>
{
    public TMeta? Metadata { get; set; }
}
```
**Rejected**: Generic explosion, breaks backward compatibility, forces all callers to specify TMeta.

### Alternative 2: Extension properties (.NET 10)
```csharp
public implicit extension ResultMetadataExtension for Result<T>
{
    public ExtractionMetadata? Metadata { get; set; }
}
```
**Rejected**: Not fully available in .NET 10, project-specific type (not general-purpose).

### Alternative 3: Add to domain entities
```csharp
public class Document
{
    public ExtractionMetadata? Metadata { get; set; }
}
```
**Rejected**: Violates separation of concerns, mixes domain with infrastructure.

### Alternative 4: Custom Result wrapper per project
**Rejected**: Leads to proliferation, the problem we're solving.

## Version bump rationale
- **Current version**: 1.1.x
- **New version**: 1.2.0
- **Reason**: Minor version bump per SemVer (new feature, backward compatible, no breaking changes)

## References
- ExxerCube.Prisma multi-source data fusion requirements
- SemVer specification: https://semver.org/
- .NET Dictionary<TKey, TValue> docs
- Result pattern best practices
