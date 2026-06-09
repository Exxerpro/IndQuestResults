# Stryker & xUnit Compatibility Verification Report

## Your Statements - Verification Results

### ✅ Statement 1: "We have two projects - one running xUnit v2 for mutation testing and one on xUnit v3 for speed"

**VERIFIED: PARTIALLY CORRECT**

**Actual Situation:**
1. **`IndQuestResults.Analyzers.Tests`** - ✅ Uses **xUnit v2** (Version 2.9.0)
   - Has `stryker-config.json` 
   - Has StrykerOutput folders (evidence of successful runs)
   - Purpose: Mutation testing with Stryker

2. **`IndQuestResults.Tests.Unit`** - ✅ Uses **xUnit v3** (Version 3.0.1)
   - Has `stryker-config.json` (but may not work)
   - Has MTP enabled (Microsoft Testing Platform)
   - Purpose: Main unit tests with advanced features

3. **`IndQuestResults.Tests.V3Minimal`** - Uses xUnit v3 (minimal example project)

**Conclusion:** You have **xUnit v2 project for Stryker**, but `IndQuestResults.Tests.Unit` uses xUnit v3, not a separate mutation project.

---

### ✅ Statement 2: "Stryker doesn't work with MTP (Microsoft Testing Platform)"

**VERIFIED: CORRECT**

**Evidence:**
- `docs/MigrationGuideXUnitV3.md` line 122: "Microsoft.Testing.Platform: Not supported by Stryker (see https://github.com/stryker-mutator/stryker-net/issues/3094)"
- `StrykerOverrides.props` disables MTP for Stryker runs:
  ```xml
  <UseMicrosoftTestingPlatformRunner>false</UseMicrosoftTestingPlatformRunner>
  <TestingPlatformDotnetTestSupport>false</TestingPlatformDotnetTestSupport>
  <TestingPlatformServer>false</TestingPlatformServer>
  ```

**Source:** Official Stryker.NET issue #3094

---

### ⚠️ Statement 3: "Stryker doesn't work with xUnit v3"

**VERIFIED: UNCLEAR - NEEDS CLARIFICATION**

**What We Know:**
- xUnit v3 **requires** Microsoft Testing Platform (MTP) by default
- Stryker **doesn't work** with MTP
- Therefore: xUnit v3 + MTP = ❌ Not compatible with Stryker

**What We DON'T Know:**
- Can xUnit v3 run **without** MTP? (Unlikely, as it's a requirement)
- Is there a workaround? (The docs only mention disabling MTP, but xUnit v3 may require it)

**Current State:**
- `IndQuestResults.Tests.Unit` uses xUnit v3 **with** MTP enabled
- `StrykerOverrides.props` tries to disable MTP, but xUnit v3 may not work without it
- This explains the build errors we're seeing

---

### ❌ Statement 4: "No workaround until Q3 2026"

**VERIFIED: NOT FOUND IN REPOSITORY**

**What I Found:**
- Docs say "as of January 2025" - no mention of Q3 2026 timeline
- No roadmap or timeline mentioned in the codebase
- GitHub issue #3094 exists but timeline not verified

**Recommendation:** Check the actual GitHub issue for official timeline.

---

## Current Architecture Summary

### Test Projects:

1. **`IndQuestResults.Analyzers.Tests`** (xUnit v2)
   - ✅ Works with Stryker
   - Uses xUnit 2.9.0
   - No MTP dependencies
   - **Purpose:** Mutation testing

2. **`IndQuestResults.Tests.Unit`** (xUnit v3)
   - ⚠️ May NOT work with Stryker (xUnit v3 + MTP requirement)
   - Uses xUnit v3.0.1
   - MTP enabled by default
   - `StrykerOverrides.props` tries to disable MTP, but may break xUnit v3
   - **Purpose:** Main unit tests with advanced features

3. **`IndQuestResults.Tests.V3Minimal`** (xUnit v3)
   - Example/demo project
   - MTP enabled

---

## Recommendation for Metadata Mutation Testing

**Option 1: Use Analyzers Test Project (xUnit v2)**
- Copy metadata tests to `IndQuestResults.Analyzers.Tests`
- Run Stryker from there (already configured)
- ✅ Guaranteed to work

**Option 2: Create Separate Mutation Project (xUnit v2)**
- Create `IndQuestResults.Tests.Mutation` (referenced in docs but missing)
- Use xUnit v2 for Stryker compatibility
- Copy tests there for mutation testing

**Option 3: Try xUnit v3 Without MTP** (Experimental)
- Disable MTP in `IndQuestResults.Tests.Unit`
- May break xUnit v3 (needs testing)
- ⚠️ Risky approach

---

## Next Steps

1. **Verify GitHub Issue #3094** for official timeline
2. **Test if xUnit v3 works without MTP** (experimental)
3. **Use Analyzers project** for metadata mutation testing (safest)
4. **Create dedicated Mutation project** if needed (cleanest long-term)

