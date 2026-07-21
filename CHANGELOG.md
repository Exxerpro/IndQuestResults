# Changelog

## [1.6.0] - 2026-07-21

> **Last release with .NET 9 support.** As .NET 9 nears end of life, v1.7.0+ will
> target .NET 10 and later only. This version multi-targets `net9.0` and `net10.0`.

### Fixed — Reactive: stream errors now surface as exception-preserving failures
- `RouteResultsObserver<T>.OnError` routed a stream error to the failure subject as a
  *success* `Result` carrying the message string. It now routes a **failure** `Result`
  that preserves the original exception (`IsFaulted`), matching `ResultBridgeObserver.OnError`
  and `ResultObserver.OnError`.
- Re-activates two previously-skipped reactive contract tests (`RouteResults_StreamError_PreservesException`,
  `OnNext_ExceptionInHandler_PreservesException`) that now pass against the implemented behavior.

### Added — test coverage
- New `ResultRailwayVerbsTests` suite covering the railway verbs (`Then` bind-vs-map overload
  selection, `Ensure`, `ToResult`, `RequireValue`, `ValidateNotNull`) across sync and async receivers.

## [1.5.1] - 2026-07-19

### Release engineering only — no library code changes
- Identical API and behavior to 1.5.0 (which was published manually while the
  automated pipeline was being repaired)
- First release published end-to-end through the tag-triggered GitHub Actions
  pipeline using NuGet.org Trusted Publishing (OIDC)

## [1.5.0] - 2026-07-19

### Added — failure-value-preserving railway combinators (ADR 0004)
- `ThenStep<T>` (sync source + Task source with sync/async step): same-type railway step whose
  short-circuit returns the ORIGINAL instance, so value-carrying failures (e.g. diagnostic
  response DTOs attached mid-chain) survive to the end of the chain
- `MatchAsync<T, TOut>` (sync + async branches): terminal that routes on `IsSuccess` (success
  branch can never observe a null value) and exposes the carried failure value to the failure branch
- `ElseWithValue<T>` (sync + Task source): chainable failure-value projection — attaches a value to
  a value-less failure, preserving errors and exception
- `TapErrorAsync<T>`: async mirror of `TapError` (fires only on failure, returns the original
  instance, swallows nothing)
- `RequireValue<T>(this Result<T?>, string)`: synchronous twin of the Task overload

### Fixed — API parity with the shipped 1.4.1 package
The 1.4.1 NuGet package was built from a source state ahead of the repository; these shipped
members are restored to source so 1.5.0 is a strict superset of 1.4.1:
- `Result.Success<T>(T)` chain-starting factory
- `Then` bridging overloads (sync bind/map; Task source with async-bind/sync-bind/map)
- `Ensure<T>(this Task<Result<T>>, Func<T,bool>, string)`
- `ToResult<T>(this Task<T?>, string)` and `ToResult<T>(this Task<Result<T>>, string)`
- `RequireValue<T>(this Task<Result<T?>>, string)`
- `ValidateNotNull<T>(this Result<T>, Func<T?, (object?, string)>)`

### Build
- Green under .NET SDK 10.0.3xx (new IDE0370/IDE0390/CA1873 rules; xunit.v3 + Microsoft.Testing
  Platform alignment in the unit test project)
- BMAD v6.10.0 installed (`_bmad/`, `.claude/skills/bmad-*`); legacy v4 `BMAD/` folder retired

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

#### Proactive Code Quality - Custom Roslyn Analyzers
- **New Analyzers**: Added `ROPComplianceAnalyzer` (IQR201, IQR202) to detect ROP violations
  - IQR201: Detects `ArgumentNullException.ThrowIfNull` in extension methods that return `Result<T>`
  - IQR202: Detects `throw` statements in methods that return `Result<T>` (excluding rethrows)
- **New Analyzers**: Added `ExceptionHandlingComplianceAnalyzer` (IQR301, IQR302) for exception preservation
  - IQR301: Detects missing exception parameter in `WithFailure` calls within catch blocks
  - IQR302: Detects catch blocks where exceptions are used but not passed to `WithFailure`
- **Enhanced**: Improved `PreferResultAsyncAnalyzer` (IQR0001) to better detect `ThenAsync` usage
- **Code Fixes**: Added automatic code fix for IQR0001 to rewrite `ThenAsync` to `ResultAsync.BindAsync`
- **CI/CD Integration**: Analyzers run automatically during build and report violations
- All analyzers are included in `IndQuestResults.Analyzers` NuGet package

#### Code Quality
- Fixed all IDE0046 warnings with proper brace usage and pragma suppressions
- All null parameter checks now use early return pattern with braces
- Improved exception handling consistency across all async operations
- Enhanced test coverage with 900+ passing unit tests
- All analyzer tests passing

### Technical Details
- All methods now follow ROP principles - no exceptions for control flow
- Null parameters return appropriate failure states or no-op implementations
- Exception stack traces are preserved for better error diagnostics
- Backward compatibility maintained for all public APIs
- Analyzers provide real-time feedback in IDE and during CI/CD builds

### Testing
- Updated 14 tests to match new ROP-compliant behavior
- All 900+ unit tests passing
- Enhanced test coverage for exception preservation and null parameter handling
- Comprehensive analyzer test suite with 100% pass rate

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
