# Changelog

## [1.1.0] - 2025-01-XX

### Major Improvements

#### ROP (Railway-Oriented Programming) Compliance
- **Breaking Change**: All extension methods now return `Result` failures instead of throwing `ArgumentNullException` for null parameters
- Replaced 128+ instances of `ArgumentNullException.ThrowIfNull` with ROP-compliant null checks
- Methods in `ResultTiming`, `ResultSubscriptionsCore`, and `ResultObservableBridge` now handle null parameters gracefully
- Added `NoOpDisposable` class for ROP-compliant subscription handling

#### Exception Support
- Added `IsFaulted` and `Exception?` properties to `Result` and `Result<T>` types
- Exception preservation in async methods - exceptions are now captured and stored in Result objects
- `OperationCanceledException` is handled separately and does not set `IsFaulted = true`
- All catch blocks now preserve exception objects for better debugging

#### Discoverable Validation Extensions
- Added new `ResultValidationExtensions` class with discoverable validation methods
- New extension methods: `result.EnsureNotNull(...)`, `value.EnsureNotNull(...)`
- Static methods: `ValidateNotNull(...)`, `CreateIfValid(...)`
- Maintains full backward compatibility with existing validation APIs

#### Code Quality
- Fixed all IDE0046 warnings with proper brace usage and pragma suppressions
- All null parameter checks now use early return pattern with braces
- Improved exception handling consistency across all async operations
- Enhanced test coverage with 892 passing unit tests

### Technical Details
- All methods now follow ROP principles - no exceptions for control flow
- Null parameters return appropriate failure states or no-op implementations
- Exception stack traces are preserved for better error diagnostics
- Backward compatibility maintained for all public APIs

### Testing
- Updated 14 tests to match new ROP-compliant behavior
- All 892 unit tests passing
- Enhanced test coverage for exception preservation and null parameter handling

## [Unreleased] - Code Review and Quality Enhancement Plan

### Phase 0: Critical Discoverability Issue - Validation Extensions
- **Problem**: Validation extensions are not discoverable and not being used consistently
- **Solution**: Add new extension methods on `Result` and `Result<T>` types for discoverable validation
  - New extension methods: `result.ValidateNotNull(...)`, `value.EnsureNotNull(...)`
  - Maintains full backward compatibility with existing APIs
  - Future: Analyzer guidance to suggest new API (informational message, later warning)
- **Impact**: Improves discoverability and adoption of validation APIs

### Phase 1: Best Practices Compliance Review
- **Exception Handling**: Review and fix exception preservation in async methods
- **ROP Compliance**: Replace 128 instances of `ArgumentNullException.ThrowIfNull` with Result returns
- **Cancellation Token Handling**: Verify proper handling of `OperationCanceledException`
- **Null Parameter Validation**: Standardize all public APIs to return Result failures

### Phase 2: Code Quality Issues
- **Exception Preservation**: Fix missing exception parameters in `WithFailure` calls
- **ROP Violations**: Replace throw statements with Result returns
- **Inconsistent Exception Handling**: Preserve exceptions in `ResultTryExtensions`

### Phase 3: Test Coverage Analysis
- Expand test coverage for exception preservation, ROP compliance, and validation extensions
- Target: 90%+ line/branch coverage for core modules
- Add integration tests for complex ROP chains

### Phase 4: Static Analysis
- Run static analysis tools to identify code smells, performance issues, and security vulnerabilities
- Perform git diff analysis of recent exception support changes

### Phase 5: Documentation Updates
- Update Result-Manual.md with exception support, ROP best practices, and new validation extensions
- Document backward compatibility and migration paths

### Phase 6: Proactive Measures
- Create custom Roslyn analyzers to detect ROP violations
- Add CI/CD checks for exception handling compliance
- Future: Analyzer for old validation API usage

### Phase 7: Release Planning
- Priority: Fix critical bugs first (exception preservation, ROP violations)
- Maintain backward compatibility throughout
- Comprehensive testing and documentation updates

**Note**: This is a comprehensive quality enhancement plan. All changes maintain backward compatibility. See full plan details in project documentation.

## 1.0.7 - 2025-09-21
- Build: Renamed solution to `IndQuestResults.sln` and aligned scripts/docs
- Packaging: Bumped package and assembly version to 1.0.7
- Docs: Updated README badges, folder structure, and internal guides

## 1.0.6 - 2025-09-21
- Docs: Consolidated documentation under `docs/` with a central index; archived drafts under `docs/archive/`
- Build/Tests: Added coverage testing behavior and refined test configuration
- Packaging: Bumped package and assembly version to 1.0.6; updated README badges and release pointers

## 1.0.5 - 2025-09-19
- Core: Added async, LINQ, and error-handling extensions; enhanced `Result<T>` with warnings metadata
- Analyzers: Refactored analyzers and improved diagnostics coverage
- Tests: Expanded testing coverage for new extensions and behaviors
- Docs: General documentation updates

## 1.0.4 - 2025-09-19
- Core: Result<T>.WithWarnings now exposes warnings via Errors for backward compatibility
- Docs: Updated README to .NET 10.0+, version badges, and release pointers
- Tests: Added coverage for Tap, Error property, and default ctor; stabilized metrics smoke test timing
- Packaging: Bumped package to 1.0.4

## 1.0.1 - 2025-09-18
- Docs: Added comprehensive Result manual and test-derived specs (result-spec, mutation-spec)
- Build: .gitignore hardened; removed tracked artifacts; Release folder policy established
- Tests: Validation unit tests added; coverage reviewed
- Packaging: Version bumped to 1.0.1; nupkg available under `Release/`

Note: 1.0.0 remains available in `Release/` for consumers who need extra time to transition. New installations are encouraged to use 1.0.1.

## 1.0.0 - 2025-09-18
- Initial stable release
