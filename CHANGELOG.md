# Changelog

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
