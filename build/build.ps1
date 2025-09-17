#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Build script for IndQuestResults
.DESCRIPTION
    Builds, tests, and packages the IndQuestResults library
.PARAMETER Configuration
    Build configuration (Debug/Release)
.PARAMETER SkipTests
    Skip running tests
.PARAMETER SkipPack
    Skip creating NuGet packages
.PARAMETER Verbosity
    MSBuild verbosity level
#>

param(
    [string]$Configuration = "Release",
    [switch]$SkipTests,
    [switch]$SkipPack,
    [string]$Verbosity = "minimal"
)

$ErrorActionPreference = "Stop"

# Paths
$RootPath = Split-Path $PSScriptRoot -Parent
$SrcPath = Join-Path $RootPath "Src"
$ArtifactsPath = Join-Path $RootPath "artifacts"
# Projects live under Src/Code; point solution accordingly
$SolutionFile = Join-Path (Join-Path $SrcPath "Code") "IndQuestResults.sln"

Write-Host "🏗️ Building IndQuestResults" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Root Path: $RootPath" -ForegroundColor Cyan

# Clean artifacts
Write-Host "🧹 Cleaning artifacts..." -ForegroundColor Yellow
if (Test-Path $ArtifactsPath) {
    Remove-Item $ArtifactsPath -Recurse -Force
}

# Restore packages
Write-Host "📦 Restoring packages..." -ForegroundColor Yellow
dotnet restore $SolutionFile --verbosity $Verbosity
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Build solution
Write-Host "🔨 Building solution..." -ForegroundColor Yellow
dotnet build $SolutionFile --configuration $Configuration --no-restore --verbosity $Verbosity
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipTests) {
    # Run unit tests
    Write-Host "🧪 Running unit tests..." -ForegroundColor Yellow
    dotnet test $SolutionFile --configuration $Configuration --no-build --verbosity $Verbosity --logger "trx" --results-directory "$ArtifactsPath/test-results"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # Run performance tests
    Write-Host "⚡ Running performance benchmarks..." -ForegroundColor Yellow
    $BenchmarkProject = Join-Path (Join-Path $SrcPath "Code") "benchmarks/IndQuestResults.Benchmarks/IndQuestResults.Benchmarks.csproj"
    dotnet run --project $BenchmarkProject --configuration $Configuration -- --exporters json --artifacts "$ArtifactsPath/benchmarks"
    
    # Run mutation tests (if Stryker is available)
    if (Get-Command "dotnet-stryker" -ErrorAction SilentlyContinue) {
        Write-Host "🧬 Running mutation tests..." -ForegroundColor Yellow
        Push-Location (Join-Path (Join-Path $SrcPath "Code") "tests/IndQuestResults.Tests.Mutation")
        dotnet stryker --output "$ArtifactsPath/mutation" --reporter "html" --reporter "json"
        Pop-Location
    } else {
        Write-Host "⚠️ Stryker not found, skipping mutation tests" -ForegroundColor Yellow
    }
}

if (-not $SkipPack) {
    # Create NuGet packages
    Write-Host "📦 Creating NuGet packages..." -ForegroundColor Yellow
    $ProjectFile = Join-Path (Join-Path $SrcPath "Code") "src/IndQuestResults/IndQuestResults.csproj"
    dotnet pack $ProjectFile --configuration $Configuration --no-build --output "$ArtifactsPath/packages" --verbosity $Verbosity
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "✅ Build completed successfully!" -ForegroundColor Green
Write-Host "📁 Artifacts location: $ArtifactsPath" -ForegroundColor Cyan
