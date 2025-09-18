## Migration Guide: xUnit v3 for Test Projects

This guide explains how to migrate your test projects to xUnit v3 using Microsoft Testing Platform and central package management.

### Prerequisites
- .NET SDK aligned with your target framework (e.g., net10.0)
- Central package management enabled (ManagePackageVersionsCentrally=true)

### Central Package Versions
Ensure these are present in `Directory.Packages.props` (already included in this repo):
- `xunit.v3`, `xunit.v3.core`, `xunit.runner.visualstudio`
- `Microsoft.NET.Test.Sdk`
- `Microsoft.Testing.Platform`, `Microsoft.Testing.Platform.MSBuild`
- `coverlet.collector`
- Optional logging: `Meziantou.Extensions.Logging.Xunit.v3`

### Project File (csproj)
Use this baseline as reference:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <IsPackable>false</IsPackable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>

    <!-- Microsoft Testing Platform -->
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
    <TestingPlatformServer>true</TestingPlatformServer>
    <ImplicitUsings>disable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup Label="Core Testing">
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.v3.core" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup Label="Coverage">
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup Label="Logging">
    <PackageReference Include="Meziantou.Extensions.Logging.Xunit.v3" />
    <PackageReference Include="Microsoft.Extensions.Logging" />
  </ItemGroup>
</Project>
```

### Source Updates
- Attributes: `[Fact]`, `[Theory]`, `[InlineData]` unchanged
- Fixtures: `IClassFixture<T>`, `ICollectionFixture<T>` unchanged
- Output: `ITestOutputHelper` continues to work
- Parallelization (assembly-level):
```csharp
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = false, MaxParallelThreads = -1)]
```

### Optional: Logging in Tests
```csharp
using Meziantou.Extensions.Logging.Xunit;
using Xunit.Abstractions;

public class MyTests
{
  private readonly ILogger<MyTests> _logger;
  public MyTests(ITestOutputHelper output)
  {
    var factory = LoggerFactory.Create(b => b.AddXunit(output));
    _logger = factory.CreateLogger<MyTests>();
  }
  [Fact]
  public void Logs() => _logger.LogInformation("hello");
}
```

### Coverage: RunSettings (recommended)
Create `test.runsettings` next to the csproj/solution:
```xml
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <Format>Cobertura</Format>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```
Run:
```bash
dotnet test -c Release --settings test.runsettings
```

Alternatively (msbuild properties):
```bash
dotnet test -c Release /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

### Common Gotchas
- Remove legacy xunit v2 runners if present
- Keep analyzers aligned (e.g., Shouldly.Analyzers, NSubstitute analyzers)
- Use collections/fixtures to control parallelism with shared state
- Prefer `Microsoft.Extensions.TimeProvider.Testing` for time-sensitive tests

### Checklist
- [ ] TFM set to net8.0+ (e.g., net10.0)
- [ ] csproj references: Microsoft.NET.Test.Sdk, xUnit v3 packages, coverlet.collector
- [ ] Microsoft Testing Platform enabled (properties + package refs)
- [ ] Global usings updated, no v2 runner leftovers
- [ ] Optional `test.runsettings` added
- [ ] dotnet test -c Release runs and reports coverage

### CI
- Use `dotnet test -c Release --settings test.runsettings`
- Publish TRX and Cobertura artifacts
- Optionally merge multi-project coverage with a report tool


### Exact runners and package matrix (must match)
Use these exact packages/versions via central management (already present in `Directory.Packages.props`). Do not change versions.

- Test platform and SDK
  - Microsoft.NET.Test.Sdk 17.14.1
  - Microsoft.Testing.Platform 1.8.4
  - Microsoft.Testing.Platform.MSBuild 1.8.4
  - Microsoft.Testing.Extensions.TrxReport 1.8.4
  - Microsoft.Testing.Extensions.CodeCoverage 17.14.2
  - Microsoft.Testing.Extensions.Telemetry 1.8.2
  - Microsoft.Testing.Extensions.VSTestBridge 1.8.4

- xUnit v3
  - xunit.v3 3.0.1
  - xunit.v3.core 3.0.1
  - xunit.runner.visualstudio 3.1.4

- Coverage
  - coverlet.collector 6.0.4

- Logging & helpers
  - Meziantou.Extensions.Logging.Xunit.v3 1.1.13
  - Microsoft.Extensions.TimeProvider.Testing 9.9.0

- Assertions & mocking (as used in sample)
  - Shouldly 4.3.0
  - Shouldly.Analyzers 0.34.1
  - NSubstitute 5.3.0
  - NSubstitute.Analyzers.CSharp 1.0.17
  - Chill 4.1.0

- ASP.NET & SignalR (if referenced by tests)
  - Microsoft.AspNetCore.Mvc.Testing 10.0.0-rc.1.25451.107
  - Microsoft.AspNetCore.SignalR.Client.Core 10.0.0-rc.1.25451.107
  - Microsoft.AspNet.SignalR.Client 2.4.3

- EF Core (if referenced by tests)
  - Microsoft.EntityFrameworkCore 10.0.0-rc.1.25451.107
  - Microsoft.EntityFrameworkCore.Relational 10.0.0-rc.1.25451.107
  - Microsoft.EntityFrameworkCore.InMemory 10.0.0-rc.1.25451.107
  - Microsoft.EntityFrameworkCore.Sqlite 10.0.0-rc.1.25451.107

Notes
- Keep PackageReference entries without Version in the csproj; resolve centrally to the versions above.
- If a test project needs only a subset, still keep all runner/platform packages to preserve the working combination.
- When adding new test projects, copy the same property flags and PackageReference groups to avoid discovery/execution issues.
