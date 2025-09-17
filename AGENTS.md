# Repository Guidelines

## Project Structure & Module Organization
- Root: `README.md`, `LICENSE`, `docs/`, `build/`, `Src/`.
- Source: `Src/src/IndQuestResults/` (library), solution at `Src/IndQuestResults.sln`.
- Tests: `Src/tests/` (e.g., `IndQuestResults.Tests.Unit/`, `IndQuestResults.Tests.Benchmarks/`).
- Benchmarks: `Src/benchmarks/IndQuestResults.Benchmarks/`.
- CI: GitHub Actions in `.github/workflows/`.

## Build, Test, and Development Commands
- Build all: `pwsh ./build/build.ps1 -Configuration Release`
  - Restores, builds, runs tests and benchmarks, and packs (unless skipped).
- Skip phases: `pwsh ./build/build.ps1 -SkipTests -SkipPack`
- Direct dotnet:
  - Restore: `dotnet restore Src/IndQuestResults.sln`
  - Build: `dotnet build Src/IndQuestResults.sln -c Release`
  - Test: `dotnet test Src/IndQuestResults.sln -c Release`
  - Pack: `dotnet pack Src/src/IndQuestResults/IndQuestResults.csproj -c Release`

## Coding Style & Naming Conventions
- Language: C# (.NET 8+). Enforced by `stylecop.json` and `editorconfig`.
- Indentation: 4 spaces; UTF-8; Unix line endings for source.
- Naming: PascalCase for public types/members; camelCase for locals/parameters; `_camelCase` for private fields.
- Files/folders: match type names (e.g., `Result.cs`, `Validation/NullArgumentError.cs`). Keep APIs immutable and thread-safe.

## Testing Guidelines
- Framework: xUnit; assertions via Shouldly or FluentAssertions.
- Layout: mirror library namespaces under `Src/tests/*`. Use `*.Tests.Unit` and `*.Tests.Benchmarks` naming.
- Coverage: target 90%+ lines/branches for core modules.
- Run: `dotnet test Src/IndQuestResults.sln -c Release`.
- Mutation (optional): `dotnet tool run dotnet-stryker` in `Src/tests/IndQuestResults.Tests.Mutation`.

## Commit & Pull Request Guidelines
- Commits: follow Conventional Commits (`feat:`, `fix:`, `perf:`, `docs:`, `test:`, `refactor:`). Keep messages imperative and scoped (e.g., `feat(result): add Recover overload`).
- PRs must include:
  - Clear description, rationale, and scope.
  - Linked issue (e.g., `Closes #123`).
  - Tests for new behavior; update docs under `docs/` when applicable.
  - Passing CI (build, unit, formatting, pack check).

## Security & Configuration Tips
- SDK pinning via `Src/global.json` (if present). Use LTS .NET SDK.
- No external runtime dependencies; avoid introducing packages without discussion.
- Run benchmarks before/after performance changes: `dotnet run --project Src/benchmarks/IndQuestResults.Benchmarks -c Release`.

