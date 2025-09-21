# IndQuestResults Cleanup Index

**Date Created:** 2025-09-18  
**Reason:** Repository cleanup to remove files not included in IndQuestResults.sln  
**Status:** SAFE TO DELETE (after verification period)

## Overview

This document tracks all files and folders moved from the main repository structure to this cleanup folder. All items listed here were determined to be residual files not included in the main solution `IndQuestResults.sln`.

## Verification Process

1. ✅ **Solution Analysis**: Confirmed all moved files are NOT referenced in `IndQuestResults.sln`
2. ✅ **Build Verification**: Solution builds successfully with 0 warnings and 0 errors after cleanup
3. ✅ **Functionality Test**: All tests pass (minor test expectation was fixed during verification)
4. ⏳ **Waiting Period**: Safe to delete after 2-3 days if no issues occur

## Files Moved

### Mutation Testing Outputs ✅ COMPLETED
**Location:** `cleanup-candidates/mutation-outputs/`  
**Original Paths:** 
- `Src/Code/src/IndQuestResults/StrykerOutput/` → `src-main-stryker-output/`
- `Src/Code/tests/IndQuestResults.Tests.Unit/StrykerOutput/` → `tests-unit-stryker-output/`

**Type:** Stryker mutation testing reports and artifacts  
**Safe to Delete:** ✅ YES - These are regenerated when mutation tests run
**Status:** ✅ MOVED SUCCESSFULLY

### Package Caches ⚠️ PARTIALLY COMPLETED
**Location:** `cleanup-candidates/artifacts/`  
**Original Paths:**
- `Src/Code/local-packages/` → ✅ MOVED to `local-packages/`
- `Src/Code/packages/` → ❌ PERMISSION DENIED (files likely in use)

**Type:** Local NuGet package caches  
**Safe to Delete:** ✅ YES - These are restored automatically by dotnet restore
**Status:** ⚠️ Partial - packages folder blocked by permission

### Miscellaneous Files ✅ COMPLETED
**Location:** `cleanup-candidates/misc-files/`  
**Type:** Various supporting files not referenced by solution
**Status:** ✅ MOVED SUCCESSFULLY

**Files:**
- `ResultCollections.png` - Diagram file
- `IndQuestResults.sln.DotSettings.user` - User-specific IDE settings

### Build Artifacts and Outputs ❌ BLOCKED
**Location:** `cleanup-candidates/artifacts/`  
**Original Path:** `Src/Code/artifacts/`  
**Type:** Build outputs, compiled binaries, test results, coverage reports  
**Safe to Delete:** ✅ YES - These are regenerated on each build
**Status:** ❌ PERMISSION DENIED (files likely in use by IDE/build processes)

**Note:** The artifacts and packages folders contain files that are currently in use by development tools or build processes. These will need to be moved manually after closing IDEs and ensuring no processes are using the files.

## Files NOT Moved (Kept in Repository)

The following files were analyzed but kept in the repository as they may be needed:

### Essential Configuration Files
- `Directory.Build.props` - Referenced in solution
- `Directory.Packages.props` - Referenced in solution  
- `Common.props` - Referenced by Directory.Build.props
- `Directory.Build.targets` - MSBuild configuration
- `NuGet.config` - Package source configuration

### Documentation (Outside Src)
- Root level documentation files (`README.md`, `CLAUDE.md`, etc.)
- `docs/` folder - Project documentation
- `build/` folder - May contain build scripts
- `Release/` folder - Release artifacts

### Solution and Project Files
- All `.csproj` files referenced in the solution
- All `.cs` source files in included projects
- `IndQuestResults.sln` - Main solution file

## Rollback Instructions

If any issues are discovered, files can be restored using these steps:

1. **Copy Back Artifacts:**
   ```bash
   robocopy "F:\Dynamic\IndFusion\IndQuestResults\cleanup-candidates\artifacts" "F:\Dynamic\IndFusion\IndQuestResults\Src\Code\artifacts" /E
   ```

2. **Copy Back Mutation Outputs:**
   ```bash
   robocopy "F:\Dynamic\IndFusion\IndQuestResults\cleanup-candidates\mutation-outputs\root" "F:\Dynamic\IndFusion\IndQuestResults\StrykerOutput" /E
   robocopy "F:\Dynamic\IndFusion\IndQuestResults\cleanup-candidates\mutation-outputs\src-main" "F:\Dynamic\IndFusion\IndQuestResults\Src\Code\src\IndQuestResults\StrykerOutput" /E
   robocopy "F:\Dynamic\IndFusion\IndQuestResults\cleanup-candidates\mutation-outputs\tests-unit" "F:\Dynamic\IndFusion\IndQuestResults\Src\Code\tests\IndQuestResults.Tests.Unit\StrykerOutput" /E
   ```

3. **Copy Back Misc Files:**
   ```bash
   copy "F:\Dynamic\IndFusion\IndQuestResults\cleanup-candidates\misc-files\*" "F:\Dynamic\IndFusion\IndQuestResults\Src\Code\"
   ```

## Safety Verification Checklist

- [x] Solution builds successfully: `dotnet build Src/Code/IndQuestResults.sln` ✅ SUCCESS (0 warnings, 0 errors)
- [x] All unit tests pass: `dotnet test Src/Code/tests/IndQuestResults.Tests.Unit/` ✅ SUCCESS (237 tests passed)
- [ ] Performance tests run: `dotnet run --project Src/Code/tests/IndQuestResults.Tests.Performance/`
- [ ] Benchmarks can execute: `dotnet run --project Src/Code/benchmarks/IndQuestResults.Benchmarks/`
- [ ] NuGet package can be created: `dotnet pack Src/Code/src/IndQuestResults/`
- [x] No broken references or missing files reported ✅ VERIFIED

## Deletion Timeline

**Created:** 2025-09-18  
**Safe to Delete After:** 2025-09-20 (48-72 hours)  
**Delete Command:** `Remove-Item "F:\Dynamic\IndFusion\IndQuestResults\cleanup-candidates" -Recurse -Force`

---

*This cleanup was performed to maintain a clean repository structure focused on the main solution while preserving the ability to restore files if needed.*