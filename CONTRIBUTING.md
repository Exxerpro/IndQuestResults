# Contributing to IndQuestResults

## 🚀 Welcome Contributors

IndQuestResults is an enterprise-grade Result<T> library that maintains the highest standards of code quality, performance, and reliability. We welcome contributions but enforce strict quality requirements to maintain our 85% mutation testing threshold and production-ready stability.

## 📋 Prerequisites

Before contributing, you **MUST** meet these requirements:

### Development Environment
- **.NET 9.0 SDK** or later
- **Visual Studio 2022** or **JetBrains Rider** (latest version)
- **Git** for version control
- **Global tools**: Stryker.NET for mutation testing

### Required Reading
- Complete understanding of **functional programming** principles
- Experience with **Result<T> patterns** and railway-oriented programming
- Familiarity with **enterprise-grade error handling**
- Knowledge of **.NET performance optimization** and memory management

## 🔥 Contribution Requirements

### ⚠️ MANDATORY: All contributions MUST include:

1. **100% Unit Test Coverage** for new code
2. **XML Documentation** for all public APIs
3. **Performance Benchmarks** for performance-critical changes
4. **Mutation Testing Score ≥85%** for modified code
5. **Zero Compilation Warnings** (warnings treated as errors)
6. **Thread Safety Analysis** for concurrent operations

### ⛔ Automatic Rejection Criteria

Contributions will be **immediately closed** without review if they:

- Lack unit tests for any new public API
- Introduce compilation warnings
- Reduce mutation testing score below 85%
- Add external dependencies (zero-dependency policy)
- Include performance regressions
- Lack proper XML documentation
- Use non-standard C# formatting
- Violate immutability principles

## 🛡️ Quality Gates

### Build Requirements
```bash
# All commands MUST pass without warnings
dotnet build Src/Code/IndQuestResults.sln --configuration Release
dotnet test Src/Code/tests/IndQuestResults.Tests.Unit/IndQuestResults.Tests.Unit.csproj
dotnet test Src/Code/tests/IndQuestResults.Tests.Performance/IndQuestResults.Tests.Performance.csproj
```

### Performance Requirements
```bash
# Benchmarks MUST show no regressions
dotnet run --project Src/Code/benchmarks/IndQuestResults.Benchmarks/IndQuestResults.Benchmarks.csproj --configuration Release
```

### Mutation Testing Requirements
```bash
# Mutation score MUST be ≥85%
cd Src/Code/tests/IndQuestResults.Tests.Mutation
dotnet stryker --config-file stryker-config.json
```

## 📝 Issue Requirements

### 🐛 Bug Reports

**All bug reports MUST include:**

1. **Minimal Reproducible Repository** 
   - Public GitHub repository with complete reproduction case
   - Include all dependencies and build scripts
   - Repository MUST compile and demonstrate the issue

2. **Complete Environment Details**
   - .NET version (exact)
   - Operating system and version
   - IndQuestResults version
   - Hardware specifications (for performance issues)

3. **Detailed Analysis**
   - **Expected Behavior**: Specific, measurable outcomes
   - **Actual Behavior**: Exact error messages, stack traces, performance metrics
   - **Steps to Reproduce**: Numbered, actionable steps
   - **Impact Assessment**: Business impact, severity level
   - **Attempted Solutions**: What you've already tried

### 💡 Feature Requests

**Feature requests MUST include:**

1. **Business Justification**
   - Clear business value proposition
   - Performance impact analysis
   - Compatibility considerations

2. **Technical Design**
   - API design proposal
   - Performance implications
   - Thread safety considerations
   - Memory allocation impact

3. **Implementation Plan**
   - Proposed implementation approach
   - Test strategy
   - Documentation plan
   - Migration strategy (if breaking)

### ⚠️ Issue Policies

- **Non-reproducible issues** will be closed immediately
- **Issues without repositories** will be closed immediately  
- **Incomplete issue reports** will be closed immediately
- **Feature requests without justification** will be closed immediately
- **Issues violating guidelines** will be closed immediately

## 🔄 Pull Request Process

### Before Submitting

1. **Fork and Branch**
   ```bash
   git checkout -b feature/your-feature-name
   git checkout -b fix/issue-number-description
   ```

2. **Validate Your Changes**
   ```bash
   # Build and test
   dotnet build Src/Code/IndQuestResults.sln --configuration Release
   dotnet test Src/Code/tests/IndQuestResults.Tests.Unit/IndQuestResults.Tests.Unit.csproj
   
   # Run mutation testing
   cd Src/Code/tests/IndQuestResults.Tests.Mutation
   dotnet stryker --config-file stryker-config.json
   
   # Run benchmarks
   dotnet run --project Src/Code/benchmarks/IndQuestResults.Benchmarks/IndQuestResults.Benchmarks.csproj --configuration Release
   ```

3. **Code Quality Checklist**
   - [ ] All new public APIs have XML documentation
   - [ ] All new code has 100% unit test coverage
   - [ ] Mutation testing score ≥85% maintained
   - [ ] No performance regressions in benchmarks
   - [ ] Zero compilation warnings
   - [ ] Thread safety verified for concurrent operations
   - [ ] Immutability principles maintained
   - [ ] No external dependencies added

### Pull Request Requirements

**Every PR MUST include:**

1. **Comprehensive Description**
   - Clear problem statement
   - Detailed solution approach
   - Breaking changes analysis
   - Performance impact assessment

2. **Testing Evidence**
   - Unit test results
   - Mutation testing scores
   - Performance benchmark comparisons
   - Thread safety verification

3. **Documentation Updates**
   - XML documentation for new APIs
   - Code examples for complex features
   - Architecture documentation updates
   - README updates if applicable

## 🎯 Code Standards

### C# Style Requirements

```csharp
// ✅ CORRECT: Immutable design with proper validation
public static Result<User> CreateUser(string name, string email)
{
    var validation = ResultExtensions.ValidateNotNull(
        (name, nameof(name)),
        (email, nameof(email))
    );
    if (!validation.IsSuccess)
    {
        return Result<User>.WithFailure(validation.Errors);
    }
    
    return Result<User>.Success(new User(name, email));
}

// ❌ INCORRECT: Mutable design without validation
public static Result<User> CreateUser(string name, string email)
{
    return Result<User>.Success(new User { Name = name, Email = email });
}
```

### Performance Requirements

```csharp
// ✅ CORRECT: Span<T> optimization for small collections
private static string FormatErrors(ReadOnlySpan<string> errors)
{
    if (errors.Length <= 16)
    {
        Span<char> buffer = stackalloc char[capacity];
        // Use stack allocation
    }
    // Fallback to heap allocation
}

// ❌ INCORRECT: Always using heap allocation
private static string FormatErrors(IEnumerable<string> errors)
{
    return string.Join(", ", errors);
}
```

### Thread Safety Requirements

```csharp
// ✅ CORRECT: Thread-safe with proper locking
private readonly Lock _lock = new();
private readonly Dictionary<int, T> _items = new();

public void Add(T item)
{
    using var _ = _lock.EnterScope();
    _items[GetNextId()] = item;
}

// ❌ INCORRECT: Non-thread-safe operations
private readonly Dictionary<int, T> _items = new();
public void Add(T item) => _items[GetNextId()] = item;
```

## 🧪 Testing Standards

### Unit Test Requirements

```csharp
[Fact]
public void OperationName_InputCondition_ExpectedBehavior()
{
    // Arrange - Set up test data
    var input = CreateValidInput();
    
    // Act - Perform the operation
    var result = SystemUnderTest.Operation(input);
    
    // Assert - Verify all aspects
    result.IsSuccess.ShouldBeTrue();
    result.Value.ShouldNotBeNull();
    result.Value.Property.ShouldBe(expectedValue);
    result.HasWarnings.ShouldBeFalse();
}

[Fact]
public void OperationName_InvalidInput_ReturnsFailure()
{
    // Test failure scenarios
    var result = SystemUnderTest.Operation(null);
    
    result.IsFailure.ShouldBeTrue();
    result.Errors.ShouldContain(expectedErrorMessage);
}

[Fact]
public async Task AsyncOperation_CancellationToken_ProperlyCancels()
{
    // Test cancellation scenarios
    using var cts = new CancellationTokenSource();
    cts.Cancel();
    
    var result = await SystemUnderTest.OperationAsync(input, cts.Token);
    
    result.IsFailure.ShouldBeTrue();
    result.Error.ShouldBe(ResultErrors.OperationCancelled);
}
```

### Performance Test Requirements

```csharp
[Fact]
public void OperationName_LargeDataSet_MeetsPerformanceTargets()
{
    // Arrange
    var largeDataSet = GenerateTestData(100000);
    
    // Act & Assert
    var stopwatch = Stopwatch.StartNew();
    var result = SystemUnderTest.ProcessLargeDataSet(largeDataSet);
    stopwatch.Stop();
    
    // Performance assertions
    stopwatch.ElapsedMilliseconds.ShouldBeLessThan(1000);
    GC.GetTotalMemory(false).ShouldBeLessThan(initialMemory + 1024 * 1024); // Max 1MB allocation
}
```

## 🏗️ Architecture Principles

### Immutability Requirements
- All Result objects MUST be immutable after creation
- Use readonly fields and properties
- Implement copy-on-write semantics for transformations

### Performance Principles
- Optimize for the success path (minimal allocations)
- Use Span<T> for small collections (≤16 items)
- Implement stack allocation where appropriate
- Measure and benchmark all performance-critical paths

### Error Handling Patterns
- Never throw exceptions in normal operation flows
- Use Result<T> patterns for all error-prone operations
- Provide detailed, actionable error messages
- Support error aggregation and validation scenarios

## 📊 Metrics and Monitoring

### Required Metrics
- **Code Coverage**: ≥95% line coverage
- **Mutation Score**: ≥85% mutation testing score
- **Performance**: No regressions in benchmark suite
- **Memory**: Zero allocations for success path operations
- **Thread Safety**: Concurrent operation verification

### Continuous Integration
All PRs automatically run:
- Full test suite execution
- Mutation testing analysis
- Performance benchmark comparison
- Static code analysis
- Documentation validation

## 🚫 Rejection Criteria

Contributions will be **immediately rejected** for:

1. **Missing unit tests** for any new functionality
2. **Compilation warnings** of any kind
3. **Performance regressions** without justification
4. **Mutation score reduction** below 85%
5. **Breaking immutability** principles
6. **Adding external dependencies**
7. **Thread safety violations**
8. **Missing XML documentation**
9. **Non-standard formatting**
10. **Incomplete issue descriptions**

## 💬 Communication

### Getting Help
- **Discussions**: Use GitHub Discussions for questions
- **Documentation**: Check existing documentation first
- **Examples**: Review samples and tests for patterns

### Response Times
- **Issue Triage**: Within 48 hours
- **PR Review**: Within 1 week for complete submissions
- **Bug Fixes**: Priority based on severity and reproduction quality

## 📈 Recognition

### Contributor Levels
- **Bronze**: First merged contribution
- **Silver**: 5+ merged contributions with sustained quality
- **Gold**: 20+ contributions with architectural improvements
- **Platinum**: Core maintainer with mutation testing expertise

### Hall of Fame
Outstanding contributors who maintain our quality standards will be recognized in our documentation and release notes.

---

## ⚖️ Final Notes

**Quality over Quantity**: We prefer fewer, high-quality contributions over numerous low-quality submissions.

**Educational Value**: We aim to maintain this library as a reference implementation for enterprise-grade functional programming in .NET.

**Zero Tolerance**: Our quality standards are non-negotiable. They exist to ensure this library remains production-ready for enterprise applications.

**Welcome Aboard**: If you meet our standards and share our commitment to excellence, we'd love to have your contributions!