Benchmarks: IndQuestResults
===========================

How to run
- Interactive menu:
  - `dotnet run --project Src/Code/benchmarks/IndQuestResults.Benchmarks -c Release`
- Direct suite:
  - `dotnet run --project Src/Code/benchmarks/IndQuestResults.Benchmarks -c Release -- comparison`
  - Other options: `concurrency`, `errorformatting`, `fluentapi`, `creation`, `span`, `memory`, `all`

Artifacts
- Results are written under `BenchmarkDotNet.Artifacts/results`.

Optional: Compare with other libraries
- We ship an optional benchmark class `ExternalComparisonBenchmarks`.
- By default it compiles only IndQuestResults cases. To include others:
  1) Add packages to the benchmarks project (not the library):
     - FluentResults: `dotnet add Src/Code/benchmarks/IndQuestResults.Benchmarks package FluentResults`
     - CSharpFunctionalExtensions (optional): `dotnet add Src/Code/benchmarks/IndQuestResults.Benchmarks package CSharpFunctionalExtensions`
  2) Define symbols so conditional benchmarks compile:
     - For FluentResults: `-p:DefineConstants=FLUENTRESULTS`
     - For CSFE: `-p:DefineConstants=CSFEXT`
  3) Run with define(s), for example:
     - `dotnet run --project Src/Code/benchmarks/IndQuestResults.Benchmarks -c Release -p:DefineConstants=FLUENTRESULTS -- comparison`

Notes
- External comparisons are for sanity/perf calibration only. The library remains dependency-free.

