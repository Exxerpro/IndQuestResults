#!/usr/bin/env pwsh

param(
  [int]$MaxIterations = 5,
  [int]$Concurrency = 4,
  [string]$SolutionPath = "Src/Code/IndQuestResults.sln",
  [string]$TestProject = "Src/Code/tests/IndQuestResults.Tests.Unit/IndQuestResults.Tests.Unit.csproj",
  [string]$ProjectUnderTest = "IndQuestResults.csproj"
)

$ErrorActionPreference = 'Stop'

function Invoke-StrykerRun {
  param([string]$SolutionPath, [string]$TestProject, [string]$ProjectUnderTest, [int]$Concurrency)
  $args = @(
    'stryker',
    '-s', $SolutionPath,
    '-tp', $TestProject,
    '-p', $ProjectUnderTest,
    '-r', 'Progress', '-r', 'Html', '-r', 'Json',
    '-c', $Concurrency
  )
  & dotnet @args | Write-Host
}

function Get-LatestReportJson {
  param([string]$Root)
  $reports = Get-ChildItem -Path $Root -Recurse -Filter mutation-report.json -ErrorAction SilentlyContinue
  $reports | Sort-Object LastWriteTime -Descending | Select-Object -First 1
}

function New-MutationKillerTest {
  param(
    [string]$OutDir,
    [string]$FilePath,
    [int]$Line,
    [string]$Mutator,
    [string]$Id
  )

  if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }
  $safeName = ($Id -replace '[^A-Za-z0-9_]', '_')
  $className = "KillMutant_" + $safeName
  $testFile = Join-Path $OutDir ($className + '.cs')
  $relFile = ($FilePath -replace "^.*Src\\Code\\src\\", "src\\")
  $content = @"
using Xunit;
using Shouldly;
using IndQuestResults;
using IndQuestResults.Operations;

namespace IndQuestResults.Tests.Unit.MutationKillers;

// Auto-generated placeholder to kill surviving mutant.
// Mutator: $Mutator
// Location: ${relFile}:${Line}
public class $className
{
    [Fact(Skip = "TODO: Implement assertion to kill mutant: ${Mutator} at ${relFile}:${Line}")]
    public void Kill_Mutant_$safeName()
    {
        // Arrange
        // TODO: Setup inputs that expose the mutated behavior

        // Act
        // TODO: Invoke API under test (Result/Result<T> methods)

        // Assert
        // TODO: Add precise assertion that fails on the mutant
        true.ShouldBeTrue();
    }
}
"@
  Set-Content -Path $testFile -Value $content -Encoding UTF8
  return $testFile
}

function Get-SurvivorsFromReport {
  param([string]$ReportPath)
  $json = Get-Content $ReportPath -Raw | ConvertFrom-Json
  $survivors = @()
  foreach ($file in $json.files.PSObject.Properties.Name) {
    $fileEntry = $json.files.$file
    foreach ($mutant in $fileEntry.mutants) {
      if ($mutant.status -eq 'Survived') {
        $survivors += [PSCustomObject]@{
          File = $file
          Line = $mutant.location.start.line
          Mutator = $mutant.mutatorName
          Id = $mutant.id
        }
      }
    }
  }
  $survivors
}

$iteration = 0
$testsRoot = "Src/Code/tests/IndQuestResults.Tests.Unit/MutationKillers"
if (Test-Path $testsRoot) { Remove-Item $testsRoot -Recurse -Force }

do {
  $iteration++
  Write-Host "=== Stryker Iteration $iteration ===" -ForegroundColor Cyan
  Invoke-StrykerRun -SolutionPath $SolutionPath -TestProject $TestProject -ProjectUnderTest $ProjectUnderTest -Concurrency $Concurrency

  $report = Get-LatestReportJson -Root (Split-Path $SolutionPath -Parent)
  if (-not $report) { throw 'No Stryker JSON report found.' }
  $survivors = Get-SurvivorsFromReport -ReportPath $report.FullName

  if (-not $survivors -or $survivors.Count -eq 0) {
    Write-Host "No surviving mutants." -ForegroundColor Green
    break
  }

  Write-Host ("Survivors: {0}" -f $survivors.Count) -ForegroundColor Yellow
  $created = @()
  foreach ($s in $survivors) {
    $created += New-MutationKillerTest -OutDir $testsRoot -FilePath $s.File -Line $s.Line -Mutator $s.Mutator -Id $s.Id
  }

  Write-Host ("Created {0} placeholder tests at {1}" -f $created.Count, $testsRoot) -ForegroundColor Yellow

  dotnet build $TestProject -c Debug --nologo | Out-Null
  if ($LASTEXITCODE -ne 0) { throw 'Build failed after generating killers.' }

} while ($iteration -lt $MaxIterations)

Write-Host "Mutation loop completed." -ForegroundColor Green
