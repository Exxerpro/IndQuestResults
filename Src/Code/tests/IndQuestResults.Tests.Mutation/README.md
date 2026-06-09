# IndQuestResults.Tests.Mutation

Mutation testing project using **xUnit v2** for Stryker.NET compatibility.

## Purpose

This project exists specifically for mutation testing with Stryker.NET, which currently:
- ✅ Works with xUnit v2
- ❌ Does NOT work with xUnit v3 + Microsoft Testing Platform (MTP)
- ❌ Does NOT work with MTP (see GitHub issue #3094)

## Project Structure

- **xUnit v2** (Version 2.9.0) - Compatible with Stryker
- **No MTP** - Disabled for Stryker compatibility
- **Target Framework**: net10.0
- **Tests**: Focused mutation tests designed to kill specific mutants

## Running Mutation Tests

### Full Mutation Test Suite
```bash
cd Src/Code/tests/IndQuestResults.Tests.Mutation
dotnet stryker --config-file stryker-config.json
```

### Metadata-Focused Mutation Tests
```bash
cd Src/Code/tests/IndQuestResults.Tests.Mutation
dotnet stryker --config-file stryker-config-metadata.json
```

## Configuration Files

- **`stryker-config.json`** - Full mutation testing configuration
- **`stryker-config-metadata.json`** - Focused on metadata code slice only

## Test Files

- **`ResultMetadataMutationTests.cs`** - Mutation tests for metadata functionality

## Notes

- This project uses xUnit v2 specifically for Stryker compatibility
- The main `IndQuestResults.Tests.Unit` project uses xUnit v3 for advanced features
- Mutation tests are designed to kill specific mutants, not just verify functionality

