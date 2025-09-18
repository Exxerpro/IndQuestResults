# Repository Guidelines

This AGENTS.md applies to the entire repository. Keep changes focused, small, and consistent with the existing style.

## Project Structure & Module Organization
- Root: `README.md`, `LICENSE`, `docs/`, `build/`, `Src/`.
- Library: `Src/src/IndQuestResults/`; solution at `Src/IndQuestResults.sln`.
- Tests: `Src/tests/` (e.g., `IndQuestResults.Tests.Unit/`, `IndQuestResults.Tests.Benchmarks/`).
- Benchmarks: `Src/benchmarks/IndQuestResults.Benchmarks/`.
- CI: GitHub Actions in `.github/workflows/`.

## Build, Test, and Development Commands
- Build all: `pwsh ./build/build.ps1 -Configuration Release` (restore, build, test, benchmarks, pack).
- Skip phases: `pwsh ./build/build.ps1 -SkipTests -SkipPack`.
- Direct dotnet: `dotnet restore Src/IndQuestResults.sln`; `dotnet build Src/IndQuestResults.sln -c Release`; `dotnet test Src/IndQuestResults.sln -c Release`; `dotnet pack Src/src/IndQuestResults/IndQuestResults.csproj -c Release`.
- Benchmarks: `dotnet run --project Src/benchmarks/IndQuestResults.Benchmarks -c Release`.

## Coding Style & Naming Conventions
- Language: C# (.NET 8+). Enforced by `stylecop.json` and `.editorconfig`.
- Indentation 4 spaces; UTF-8; Unix line endings for source.
- Naming: PascalCase (public), camelCase (locals/parameters), `_camelCase` (private fields).
- Files/folders mirror type names (e.g., `Result.cs`, `Validation/NullArgumentError.cs`). Prefer immutable, thread-safe APIs.

## Testing Guidelines
- Framework: xUnit; assertions via Shouldly or FluentAssertions.
- Layout: mirror library namespaces under `Src/tests/*`; use `*.Tests.Unit` and `*.Tests.Benchmarks`.
- Coverage: target 90%+ lines/branches for core modules.
- Run: `dotnet test Src/IndQuestResults.sln -c Release`.
- Mutation (optional): `dotnet tool run dotnet-stryker` in `Src/tests/IndQuestResults.Tests.Mutation`.

## Commit & Pull Request Guidelines
- Commits: Conventional Commits (e.g., `feat(result): add Recover overload`, `fix(errors): correct null check`).
- PRs include: clear description/rationale/scope, linked issue (e.g., `Closes #123`), tests and relevant docs (`docs/`), and passing CI.

## Security & Configuration Tips
- Pin SDK via `Src/global.json` (use LTS). Avoid new runtime dependencies without discussion.
- Validate performance using benchmarks before/after changes.

## Agent Notes
- These instructions apply to any file in this repo. Keep patches minimal; do not reformat unrelated code or add license headers. Run build and tests locally before opening a PR.

