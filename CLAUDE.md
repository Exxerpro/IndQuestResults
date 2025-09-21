# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

IndQuestResults is an enterprise-grade Result<T> library for functional error handling in .NET applications. The library is unified into a single package containing core domain functionality and all extension capabilities with zero external dependencies.

### Unified Architecture

**Single Package (Zero Dependencies):**
- `IndQuestResults` - Complete Result<T> library with all functionality unified:
  - **Core Operations** - Result<T> and Result classes, validation, performance optimizations
  - **Extensions.Async** - Async/await integration (Task<Result<T>> patterns)
  - **Extensions.Collections** - Collection operations (Sequence, Traverse, Partition)
  - **Extensions.Functional** - Advanced functional patterns (Applicative functors)
  - **Extensions.Observables** - Observable/reactive patterns with subscription management

This unified architecture eliminates dependency complexity while providing powerful functional programming patterns across all paradigms.

## Essential Commands

### Building and Testing
```bash
# Build the entire solution (primary command)
dotnet build Src/Code/IndQuestResults.sln

# Build the main project only
dotnet build Src/Code/src/IndQuestResults/IndQuestResults.csproj

# Run all unit tests (comprehensive test suite)
dotnet test Src/Code/tests/IndQuestResults.Tests.Unit/IndQuestResults.Tests.Unit.csproj

# Run performance tests with benchmarking
dotnet test Src/Code/tests/IndQuestResults.Tests.Performance/IndQuestResults.Tests.Performance.csproj

# Run analyzers tests
dotnet test Src/Code/tests/IndQuestResults.Analyzers.Tests/IndQuestResults.Analyzers.Tests.csproj

# Run minimal v3 compatibility tests
dotnet test Src/Code/tests/IndQuestResults.Tests.V3Minimal/IndQuestResults.Tests.V3Minimal.csproj

# Run benchmarks (Release configuration required)
dotnet run --project Src/Code/benchmarks/IndQuestResults.Benchmarks/IndQuestResults.Benchmarks.csproj --configuration Release

# Create NuGet package
dotnet pack Src/Code/src/IndQuestResults/IndQuestResults.csproj --configuration Release
```

### Mutation Testing
```bash
# Run mutation testing with Stryker (from unit test directory)
cd Src/Code/tests/IndQuestResults.Tests.Unit
dotnet stryker --config-file stryker-config.json

# Global Stryker installation (if needed)
dotnet tool install -g dotnet-stryker

# View mutation test reports (after running tests)
# Reports are generated in StrykerOutput folder within test directory
```

### Development Environment
- Target Framework: .NET 9.0
- Language Version: Latest C#
- Nullable Reference Types: Enabled
- Treat Warnings as Errors: Enabled

## Core Architecture

### Main Components

**Core Domain (IndQuestResults):**
- `Result.cs` - Non-generic result for operations without return values
- `ResultGeneric.cs` - Generic result for operations with strongly-typed return values  
- `ResultConstants.cs` - Error message constants and defaults
- `ResultErrors.cs` - Error handling and messaging utilities
- `Operations/ResultExtensions.cs` - Core validation and utility methods (cancellation, null checks)
- `Operations/CancellationAwareResult.cs` - Cancellation token integration
- `Performance/MemoryOptimizations.cs` - Memory allocation optimizations
- `Performance/SpanOptimizations.cs` - Span<T> optimizations for 70% reduction in allocations
- `Performance/ResultMetrics.cs` - Performance monitoring and metrics collection
- `Performance/ResultTiming.cs` - Timing utilities for performance analysis
- `Validation/NullArgumentValidation.cs` - Null argument checking utilities
- `Validation/NullArgumentError.cs` - Null argument error definitions
- `Validation/MultipleNullArgumentsError.cs` - Multi-parameter validation errors
- Both Result classes are immutable, thread-safe, and JSON serializable

**Extension Components (Unified Package):**
- `Async/ResultAsync.cs` - Async patterns (BindAsync, MapAsync, TapAsync, RecoverAsync)
- `Collections/ResultCollections.cs` - Collection operations (Sequence, Traverse, Partition, Collect)
- `Functional/ResultApplicative.cs` - Applicative functors for validation error accumulation
- `Reactive/` - Observable/reactive patterns with subscription management
  - `ResultObservableBridge.cs` - Observable integration utilities
  - `ResultSubscriptionsCore.cs` - Core subscription management

### Design Principles

1. **Immutability** - All Result objects are immutable after creation for thread safety
2. **Performance First** - Span<T> optimizations, stack allocation for small collections, zero allocations for successful operations
3. **Railway-Oriented Programming** - Fluent API for chaining operations with automatic error propagation
4. **Enterprise Features** - Warning system, cancellation support, comprehensive validation

### State Management
- **IsSuccess** - Operation succeeded with non-null value
- **IsSuccessMayBeNull** - Operation succeeded (value may be null for nullable types)
- **HasWarnings** - Successful operation with diagnostic messages
- **IsFailure** - Operation failed
- **IsRecoverable** - Alias for IsSuccess

## Testing Strategy

### Test Structure
- **Unit Tests** - Comprehensive coverage for all operations and edge cases
- **Performance Tests** - Benchmarking for memory optimizations and performance validation
- **Mutation Testing** - 85% mutation score threshold with Stryker for code quality assurance

### Mutation Testing Configuration
- High threshold: 90%
- Low threshold: 75%
- Break threshold: 0%
- Coverage analysis: per test in isolation
- Timeout: 30 seconds per test
- Current mutation score target: 85%+ for enterprise quality

## Development Patterns

### Extension Usage Guide
```csharp
// Core domain - available in base package
using IndQuestResults;
var result = Result<User>.Success(user);

// Async operations - no additional using required (unified package)
var result = await GetUserAsync(id).BindAsync(LoadProfileAsync);

// Collection operations - no additional using required (unified package)  
var results = userIds.TraverseResults(LoadUser);

// Validation with error accumulation - no additional using required (unified package)
var userResult = ResultApplicative.Apply(nameResult, emailResult, (n, e) => new User(n, e));

// Observable/reactive patterns - no additional using required (unified package)
var subject = ResultSubscriptionsCore.CreateResultSubject<User>();

// Performance monitoring
var timedResult = ResultTiming.Time(() => expensiveOperation());
var metrics = ResultMetrics.CollectMetrics(results);
```

### Functional Composition
```csharp
// Core functional patterns (available in base package)
return _dataService
    .GetUser(userId)
    .Bind(user => _validator.ValidateUser(user))
    .Map(user => user.ToDto())
    .Match(
        onSuccess: dto => Ok(dto),
        onFailure: errors => BadRequest(errors)
    );

// Async functional patterns (unified package)
return await _dataService
    .GetUserAsync(userId)
    .BindAsync(user => _validator.ValidateUserAsync(user))
    .MapAsync(user => user.ToDtoAsync())
    .TapAsync(dto => _logger.LogSuccessAsync($"User {dto.Id} processed"))
    .RecoverAsync(errors => CreateDefaultUserDtoAsync());
```

### Validation Patterns
```csharp
// Multi-parameter null validation
var validation = ResultExtensions.ValidateNotNull(
    (user, nameof(user)),
    (user.Email, nameof(user.Email))
);
```

### Performance Considerations
- Small error collections (≤16 items) use Span<T> with stack allocation
- String formatting uses pre-calculated capacity estimation
- Minimal LINQ usage to reduce allocations
- Copy-on-write semantics for transformations

## File Organization

**Root Structure:**
```
F:\Dynamic\IndFusion\IndQuestResults\
├── CLAUDE.md                  # Project guidance for Claude Code
├── README.md                  # Project documentation
├── LICENSE                    # Project license
├── Src\Code\                  # Main source code directory
├── docs\                      # Documentation files
└── build\                     # Build artifacts and scripts
```

**Main Project (Src\Code\src\):**
- `IndQuestResults/` - Unified library with all functionality (zero dependencies)
  - Root files: `Result.cs`, `ResultGeneric.cs`, `ResultConstants.cs`, `ResultErrors.cs`
  - `Operations/` - Core extensions, cancellation-aware utilities
  - `Performance/` - Memory optimizations, Span optimizations, metrics, timing
  - `Validation/` - Null argument validation utilities and error types
  - `Async/` - Async patterns and utilities
  - `Collections/` - Collection operations and utilities
  - `Functional/` - Advanced functional patterns (Applicative functors)
  - `Reactive/` - Observable/reactive patterns and subscription management

**Supporting Projects (Src\Code\):**
- `src/IndQuestResults.Analyzers/` - Code analyzers for Result patterns and performance
- `tests/` - All test projects
  - `IndQuestResults.Tests.Unit/` - Comprehensive unit tests (includes mutation testing config)
  - `IndQuestResults.Tests.Performance/` - Performance benchmarking tests
  - `IndQuestResults.Analyzers.Tests/` - Tests for code analyzers
  - `IndQuestResults.Tests.V3Minimal/` - XUnit v3 compatibility tests
- `samples/` - Usage examples
  - `IndQuestResults.Samples.Basic/` - Basic usage patterns
  - `IndQuestResults.Samples.Advanced/` - Advanced usage scenarios
- `benchmarks/IndQuestResults.Benchmarks/` - Performance benchmarking code
- Build configuration files: `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `Common.props`

**Documentation:**
- `docs/architecture.md` - Detailed architecture documentation
- `docs/getting-started.md` - Getting started guide

## Build Configuration

The solution uses centralized build configuration via `Directory.Build.props`:
- Consistent targeting of .NET 9.0
- Shared package references for test projects
- Mutation testing tools for quality assurance projects
- Benchmarking tools for performance projects
- Unified packaging for single NuGet distribution