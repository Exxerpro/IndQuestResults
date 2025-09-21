# Folder Structure

A high-level overview of the repository structure.

For a visual, historic draft see `docs/archive/FOLDER_STRUCTURE.md`.

- Root files: `README.md`, `LICENSE`, `CHANGELOG.md`, `CONTRIBUTING.md`
- CI/CD: `.github/workflows/`
- Build scripts: `build/`
- Documentation: `docs/` (this directory)
  - Architecture, Manual, Specs, Migration guides
  - Archive for older drafts: `docs/archive/`
- Source code: `Src/Code/`
  - Solution: `IndQuestResults.sln`
  - Library: `src/IndQuestResults/`
  - Analyzers: `src/IndQuestResults.Analyzers/`
  - Tests: `tests/*`
  - Benchmarks: `benchmarks/`
  - Samples: `samples/`
