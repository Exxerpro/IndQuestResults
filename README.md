# IndQuestResults - Enterprise-Grade Result<T> Library

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](#)
[![Mutation Score](https://img.shields.io/badge/mutation%20score-85%25-brightgreen)](#)
[![Coverage](https://img.shields.io/badge/coverage-74.5%25-green)](#)
[![License](https://img.shields.io/badge/license-MIT-blue)](#)
[![.NET Version](https://img.shields.io/badge/.NET-10.0+-blue)](#)
[![NuGet](https://img.shields.io/badge/NuGet-v1.0.4-blue)](#release-notes)
[![Contributors](https://img.shields.io/badge/contributors-welcome-brightgreen)](#contributing)
[![Code of Conduct](https://img.shields.io/badge/code%20of%20conduct-enforced-blue)](#code-of-conduct)

A battle-tested, enterprise-grade Result<T> library for functional error handling in .NET applications. Provides type-safe, performant, and expressive ways to represent operation outcomes without exceptions.

## ✨ Key Features

- **🛡️ Type Safety**: Eliminates null reference exceptions and runtime errors
- **⚡ Performance Optimized**: 70% reduction in allocations with Span<T> optimizations
- **🔄 Functional Programming**: Full monadic operations (Map, Bind, Match, Recover)
- **🧵 Thread-Safe**: Immutable design with readonly fields
- **📊 JSON Serializable**: Built-in support for API responses and data persistence
- **⚠️ Warning Support**: Distinguish between errors and diagnostic warnings
- **🎯 Enterprise Ready**: Extensive mutation testing with 85% quality threshold

## 🚀 Quick Start

### Installation

```bash
dotnet add package IndQuestResults
```

### Basic Usage

```csharp
using IndQuestResults;

// Success results
var success = Result.Success();
var successWithValue = Result<string>.Success("Hello World");

// Failure results
var failure = Result.WithFailure("Operation failed");
var failureWithValue = Result<int>.WithFailure("Parse error", defaultValue: 0);

// Multiple errors
var multipleErrors = Result.WithFailure(new[] { "Error 1", "Error 2" });

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

### Functional Programming

```csharp
return await _dataService
    .GetUserAsync(userId)
    .Bind(user => _validator.ValidateUser(user))
    .Map(user => user.ToDto())
    .Tap(dto => _logger.LogSuccess($"User {dto.Id} processed"))
    .Recover(errors => CreateDefaultUserDto())
    .Match(
        onSuccess: dto => Ok(dto),
        onFailure: errors => BadRequest(errors)
    );
```

## 📚 Package Information

- **Target Framework**: .NET 10.0+
- **Dependencies**: None (zero external dependencies)
- **Package ID**: IndQuestResults
- **License**: MIT
- **Source**: Battle-tested patterns from enterprise codebase
- **Coverage**: 74.5% with comprehensive test suite (353 tests)
- **Quality**: 85%+ mutation testing score

### Release Notes & Download
- Recommended: `Release/IndQuestResults.1.0.4.nupkg`
- Previous: `Release/IndQuestResults.1.0.3.nupkg`, `Release/IndQuestResults.1.0.2.nupkg`, `Release/IndQuestResults.1.0.1.nupkg`
- See [CHANGELOG.md](CHANGELOG.md) for details

## 🤝 Contributing

We welcome high-quality contributions! IndQuestResults maintains strict quality standards:

### Quick Start for Contributors
1. **Read our [Contributing Guidelines](CONTRIBUTING.md)** (mandatory)
2. **Check our [Code of Conduct](CODE_OF_CONDUCT.md)**
3. **Review existing [Issues](../../issues)** and [Discussions](../../discussions)

### Quality Requirements
- ✅ **100% unit test coverage** for new code
- ✅ **Mutation testing score ≥85%**
- ✅ **Zero compilation warnings** (warnings as errors)
- ✅ **XML documentation** for all public APIs
- ✅ **Performance benchmarks** for performance-critical changes

### 🚨 Before Opening Issues
- **Bug reports** MUST include a reproducible repository
- **Feature requests** MUST include business justification and technical design
- **Performance issues** MUST include benchmark data and profiling results

[📋 Full Contributing Guidelines →](CONTRIBUTING.md)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🏆 Recognition

IndQuestResults serves as a reference implementation for enterprise-grade functional programming in .NET. Contributors who maintain our quality standards are recognized in release notes and project documentation.

### Contributor Levels
- **Bronze**: First merged contribution
- **Silver**: 5+ contributions with sustained quality  
- **Gold**: 20+ contributions with architectural improvements
- **Platinum**: Core maintainer with mutation testing expertise

## 📞 Support & Community

- 💬 **[GitHub Discussions](../../discussions)** - Questions and community support
- 🐛 **[GitHub Issues](../../issues)** - Bug reports and feature requests  
- 📚 **[Documentation](README.md)** - Comprehensive usage guide
- 🎯 **[Examples](Src/Code/samples/)** - Practical implementation samples

---

**Quality First**: We maintain enterprise-grade standards because this library powers production applications. Every contribution makes the .NET ecosystem stronger! 🚀