# IndQuestResults - Enterprise-Grade Result<T> Library

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](#)
[![Mutation Score](https://img.shields.io/badge/mutation%20score-85%25-brightgreen)](#)
[![Coverage](https://img.shields.io/badge/coverage-74.5%25-green)](#)
[![License](https://img.shields.io/badge/license-MIT-blue)](#)
[![.NET Version](https://img.shields.io/badge/.NET-10.0+-blue)](#)
[![NuGet](https://img.shields.io/badge/NuGet-v1.7.0-blue)](#release-notes)
[![Contributors](https://img.shields.io/badge/contributors-welcome-brightgreen)](#contributing)
[![Code of Conduct](https://img.shields.io/badge/code%20of%20conduct-enforced-blue)](#code-of-conduct) [![Docs](https://img.shields.io/badge/docs-available-brightgreen)](docs/README.md)

A battle-tested, enterprise-grade Result<T> library for functional error handling in .NET applications. Provides type-safe, performant, and expressive ways to represent operation outcomes without exceptions.

> ⚠️ **.NET 9 support notice** — **v1.6.0 is the last release to support .NET 9.**
> As .NET 9 approaches end of life, subsequent releases (v1.7.0+) will target **.NET 10 and later only**.
> This version multi-targets `net9.0` and `net10.0`; if you are still on .NET 9, pin to `1.6.0` and plan your upgrade to .NET 10.

## Key Features

- Type safety: eliminate null-reference exceptions in control flow
- Performance: Span-based optimizations and reduced allocations
- Functional: Map, Bind, Match, Recover with fluent composition
- Thread-safe: immutable design, no shared mutable state
- JSON serializable: API responses and persistence ready (System.Text.Json)
- Warnings on success: diagnostics with quality metadata
- Quality: mutation-tested, high coverage

## Quick Start

### Installation

```bash
dotnet add package IndQuestResults
```

### Basic Usage

```csharp
using IndQuestResults;

// Success results
var ok = Result.Success();
var okWithValue = Result<string>.Success("Hello World");

// Failure results
var fail = Result.WithFailure("Operation failed");
var failWithValue = Result<int>.WithFailure("Parse error", value: 0);

// Multiple errors
var many = Result.WithFailure(new[] { "Error 1", "Error 2" });

// Warnings (successful with diagnostics)
var withWarnings = Result<string>.WithWarnings(
    new[] { "Performance warning" },
    "Operation completed"
);

// Warnings with quality metadata (confidence + missing data ratio)
var withWarnsAndMeta = Result<string>.WithWarnings(
    warnings: new[] { "Heuristic fill for missing fields", "Low signal period" },
    value: "Computed",
    confidence: 0.82,            // clamped to [0,1]
    missingDataRatio: 0.25       // clamped to [0,1]
);
Console.WriteLine(withWarnsAndMeta.Confidence);        // 0.82
Console.WriteLine(withWarnsAndMeta.MissingDataRatio);  // 0.25
Console.WriteLine(string.Join(", ", withWarnsAndMeta.Warnings));
```

### Functional Programming (Sync)

```csharp
using IndQuestResults;

Result<UserDto> CreateUser(string userId)
{
    return Result<string>.Success(userId)
        .Ensure(id => !string.IsNullOrWhiteSpace(id), "empty id")
        .Bind(id => LoadUser(id))
        .Map(user => user.ToDto())
        .Tap(dto => _logger.LogInformation($"User {dto.Id} processed"))
        .Recover(() => Result<UserDto>.Success(UserDto.Default));
}
```

## Async

Use the async API in `IndQuestResults.Async.ResultAsync` for fluent async composition.

```csharp
using IndQuestResults;
using IndQuestResults.Async;

var dto = await ResultAsync
    .BindAsync(GetUserAsync(userId), user => ValidateUserAsync(user))
    .MapAsync(valid => valid.ToDtoAsync())
    .TapAsync(dto => CacheAsync(dto))
    .RecoverAsync(() => GetDefaultUserDtoAsync());

// Collections
var users = await ResultAsync.TraverseParallelAsync(
    userIds,
    id => GetUserAsync(id),
    maxDegreeOfParallelism: 4
);
```

Full API and patterns: docs/Result-Manual.md

More docs: docs/README.md

Note: Optional analyzers are available in `IndQuestResults.Analyzers` to guide async usage. Rule IQR0001 suggests using `ResultAsync` for async chaining and includes a one-click code fix.

## Package Information

- Target Framework: .NET 10.0+
- Dependencies: none (zero external dependencies)
- Package ID: IndQuestResults
- License: MIT
- Coverage: 74.5% with comprehensive test suite
- Quality: 85%+ mutation testing score

### Release Notes & Download

- Recommended: `Release/IndQuestResults.1.0.7.nupkg`
- Previous: `Release/IndQuestResults.1.0.5.nupkg`, `Release/IndQuestResults.1.0.4.nupkg`, `Release/IndQuestResults.1.0.3.nupkg`, `Release/IndQuestResults.1.0.2.nupkg`, `Release/IndQuestResults.1.0.1.nupkg`
- See CHANGELOG.md for details

## Contributing

We welcome high-quality contributions! See CONTRIBUTING.md and CODE_OF_CONDUCT.md.

- 100% unit test coverage for new code
- Mutation testing score ≥ 85%
- Zero compilation warnings (warnings as errors)
- XML documentation for public APIs
- Benchmarks for performance-sensitive changes

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support & Community

- Discussions: GitHub Discussions
- Issues: GitHub Issues
- Documentation: docs/Result-Manual.md
- Examples: Src/Code/samples/

---

Quality First: We maintain enterprise-grade standards because this library powers production applications. Every contribution makes the .NET ecosystem stronger!
