# Repository Guidelines

This AGENTS.md applies to the entire repository. Keep changes small, focused, and consistent with the existing style.

## Project Structure & Module Organization
- Root: `README.md`, `LICENSE`, `build/`, `.github/workflows/`.
- Solution: `Src/Code/IndQuestResults.All.sln`.
- Library: `Src/Code/src/IndQuestResults/` (primary package) and `Src/Code/src/IndQuestResults.Analyzers/`.
- Tests: `Src/Code/tests/` (e.g., `IndQuestResults.Tests.Unit/`, `IndQuestResults.Tests.Performance/`).
- Benchmarks: `Src/Code/benchmarks/IndQuestResults.Benchmarks/`.
- Samples: `Src/Code/samples/*`.

## Build, Test, and Development Commands
- Build all: `pwsh ./build/build.ps1 -Configuration Release` (restore, build, test, benchmarks, pack).
- Skip phases: `pwsh ./build/build.ps1 -SkipTests -SkipPack`.
- Direct dotnet: `dotnet restore Src/Code/IndQuestResults.All.sln`; `dotnet build Src/Code/IndQuestResults.All.sln -c Release`; `dotnet test Src/Code/IndQuestResults.All.sln -c Release`.
- Pack library: `dotnet pack Src/Code/src/IndQuestResults/IndQuestResults.csproj -c Release`.
- Run benchmarks: `dotnet run --project Src/Code/benchmarks/IndQuestResults.Benchmarks -c Release`.

## Coding Style & Naming Conventions
- Language: C# targeting .NET 8+. Analyzer rules configured via `.editorconfig` and analyzer packages.
- Formatting: 4-space indent, UTF-8, Unix line endings for source.
- Naming: PascalCase (public), camelCase (locals/params), `_camelCase` (private fields).
- Structure: Files/folders mirror type names (e.g., `Result.cs`, `Validation/NullArgumentError.cs`). Favor immutable, thread-safe APIs.

## Testing Guidelines
- Framework: xUnit with Shouldly assertions (tests under `Src/Code/tests/*`).
- Coverage: aim for 90%+ lines/branches in core modules.
- Commands: `dotnet test Src/Code/IndQuestResults.All.sln -c Release`.
- Mutation (optional): use Stryker locally if installed; check `build/mutation-loop.ps1`.

## Commit & Pull Request Guidelines
- Commits: Conventional Commits (e.g., `feat(result): add Recover overload`, `fix(errors): correct null check`).
- PRs: clear description/rationale/scope, linked issue (e.g., `Closes #123`), tests and relevant docs, green CI.

## Security & Configuration Tips
- Use .NET LTS (8+). Package versions centralized in `Directory.Packages.props`.
- Avoid adding new runtime dependencies without discussion. Validate perf with benchmarks before/after changes.

## Agent Notes
- These rules apply repo-wide. Keep patches minimal; avoid unrelated reformatting or license headers. Run build and tests locally before opening a PR.
