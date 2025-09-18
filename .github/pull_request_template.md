# Pull Request

## 📋 Overview

**Type of Change** (check all that apply):
- [ ] 🐛 Bug fix (non-breaking change that fixes an issue)
- [ ] ✨ New feature (non-breaking change that adds functionality)
- [ ] 💥 Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] 📚 Documentation update
- [ ] 🔧 Refactoring (no functional changes)
- [ ] ⚡ Performance improvement
- [ ] 🧪 Test improvements

**Issue Reference:**
- Closes #(issue number)
- Related to #(issue number)

## 🎯 Description

### Problem Statement
<!-- Clearly describe the problem this PR solves -->

### Solution Approach
<!-- Explain your implementation approach and design decisions -->

### Breaking Changes
<!-- List any breaking changes and migration strategies -->

## 🧪 Testing

### Test Coverage
- [ ] All new code has 100% unit test coverage
- [ ] Existing tests pass without modification
- [ ] Integration tests added for new features
- [ ] Performance tests added for performance-critical changes

### Mutation Testing
- [ ] Mutation testing score ≥85% maintained
- [ ] New code achieves ≥90% mutation score

### Test Results
```
# Paste test execution results
dotnet test --logger "console;verbosity=detailed"

# Test coverage summary
Line Coverage: XX.X%
Branch Coverage: XX.X%
```

## ⚡ Performance

### Performance Impact
- [ ] No performance regressions
- [ ] Performance improvements measured and documented
- [ ] Memory allocation optimizations verified
- [ ] Benchmark results included below

### Benchmark Results
```
# Include BenchmarkDotNet results or performance measurements
| Method | Mean | StdDev | Allocated |
|--------|------|--------|-----------|
| Old    | XXX  | XXX    | XXX       |
| New    | XXX  | XXX    | XXX       |
```

## 📚 Documentation

### Code Documentation
- [ ] All public APIs have XML documentation
- [ ] Code examples provided for complex features
- [ ] Architecture documentation updated

### External Documentation
- [ ] README.md updated (if applicable)
- [ ] CHANGELOG.md updated
- [ ] Migration guide provided (for breaking changes)

## 🔍 Code Quality

### Static Analysis
- [ ] No compilation warnings
- [ ] Code follows established patterns
- [ ] Thread safety verified for concurrent operations
- [ ] Immutability principles maintained

### Build Verification
```bash
# All commands must pass
dotnet build Src/Code/IndQuestResults.sln --configuration Release
dotnet test Src/Code/tests/IndQuestResults.Tests.Unit/IndQuestResults.Tests.Unit.csproj
dotnet test Src/Code/tests/IndQuestResults.Tests.Performance/IndQuestResults.Tests.Performance.csproj

# Mutation testing
cd Src/Code/tests/IndQuestResults.Tests.Mutation
dotnet stryker --config-file stryker-config.json

# Benchmarks (if performance-related)
dotnet run --project Src/Code/benchmarks/IndQuestResults.Benchmarks/IndQuestResults.Benchmarks.csproj --configuration Release
```

## 🏗️ Architecture

### Design Principles
- [ ] Follows functional programming patterns
- [ ] Maintains zero external dependencies
- [ ] Preserves immutability guarantees
- [ ] Thread-safe implementation

### Impact Analysis
- [ ] No impact on existing APIs
- [ ] Backward compatibility maintained
- [ ] Forward compatibility considered
- [ ] Enterprise patterns followed

## 🔐 Security

### Security Considerations
- [ ] No sensitive information in code
- [ ] Input validation implemented
- [ ] Error messages don't leak sensitive data
- [ ] Thread safety verified

## 📋 Checklist

### Pre-Submission
- [ ] Self-review completed
- [ ] Code follows IndQuestResults style guidelines
- [ ] Tests written and passing
- [ ] Documentation updated
- [ ] Performance impact assessed

### Quality Gates
- [ ] ✅ Build passes (`dotnet build`)
- [ ] ✅ All tests pass (`dotnet test`)
- [ ] ✅ No compilation warnings
- [ ] ✅ Mutation score ≥85%
- [ ] ✅ Performance benchmarks pass
- [ ] ✅ Code coverage maintained
- [ ] ✅ Documentation complete

### Enterprise Standards
- [ ] Zero external dependencies maintained
- [ ] Result<T> patterns followed consistently
- [ ] Error handling comprehensive
- [ ] Memory optimization considered
- [ ] Cancellation support implemented (where applicable)

## 🎨 Code Examples

### Before/After Comparison
```csharp
// Before (if applicable)


// After


// Usage Example
var result = Result<User>
    .Success(user)
    .YourNewMethod(parameters)
    .Map(u => u.ToDto());
```

## 📊 Metrics

### Code Metrics
- Lines of Code Added: XXX
- Lines of Code Removed: XXX
- Cyclomatic Complexity: X (should be ≤10 for new methods)
- Test-to-Code Ratio: X:1

### Performance Metrics
- Memory Allocation Change: ±XX%
- Execution Time Change: ±XX%
- Benchmark Score: XXX ops/sec

## 🤝 Collaboration

### Reviewers
<!-- Tag specific reviewers if needed -->
@maintainer1 @maintainer2

### Areas of Focus
<!-- Highlight specific areas where you want extra attention -->
- [ ] Performance critical sections
- [ ] Thread safety analysis
- [ ] API design review
- [ ] Documentation clarity

## 📝 Notes

### Implementation Notes
<!-- Any specific notes about the implementation -->

### Future Considerations
<!-- Ideas for future improvements or related work -->

### Known Limitations
<!-- Any known limitations or technical debt -->

---

## ⚠️ Reviewer Guidelines

### Review Criteria
1. **Functional Correctness** - Does it work as intended?
2. **Performance Impact** - No regressions, optimizations verified
3. **Code Quality** - Follows established patterns and principles
4. **Test Coverage** - Comprehensive testing with high mutation score
5. **Documentation** - Complete and accurate documentation
6. **Architecture** - Maintains library design principles

### Approval Requirements
- [ ] 2+ core maintainer approvals
- [ ] All CI checks passing
- [ ] Performance benchmarks approved
- [ ] Documentation review completed

**Thank you for contributing to IndQuestResults! 🚀**