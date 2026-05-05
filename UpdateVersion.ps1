#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Bumps Semantic Version in Snappyup.Aspire.Hosting.Dragonfly.csproj, optionally packs and pushes to NuGet.

.PARAMETER NewVersion
  Explicit package version (e.g. "0.2.0"). If omitted, patch segment is incremented.

.NOTES
  - NuGet defaults: source from $env:NUGET_SOURCE or https://api.nuget.org/v3/index.json
  - API key: $env:NUGET_API_KEY passed to dotnet nuget push when set
  - For private feeds (e.g. Nexus), set NUGET_SOURCE or rely on local nuget.config credentials
  - Pack output directory: ./artifacts (matches CI)
#>

param(
    [string]$NewVersion
)

$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$csprojPath = [System.IO.Path]::Combine($repoRoot, "src", "Snappyup.Aspire.Hosting.Dragonfly", "Snappyup.Aspire.Hosting.Dragonfly.csproj")

if (-not (Test-Path $csprojPath)) {
    Write-Error "Could not find project file: $csprojPath"
}

$newAssemblyVersion = (Get-Date -Format "yyyy.MM.dd.HHmm")

Write-Host "Building project..."
Push-Location $repoRoot
try {
    dotnet build $csprojPath -c Release
}
finally {
    Pop-Location
}

$csprojContent = Get-Content $csprojPath -Raw

if (-not $NewVersion) {
    if ($csprojContent -match '<Version>([0-9]+)\.([0-9]+)\.([0-9]+)</Version>') {
        $major = [int]$Matches[1]
        $minor = [int]$Matches[2]
        $patch = [int]$Matches[3] + 1
        $NewVersion = "$major.$minor.$patch"
    }
    else {
        Write-Error "Could not parse <Version>semver</Version> from $csprojPath"
    }
}

$csprojContent = $csprojContent -replace '<Version>[0-9]+\.[0-9]+\.[0-9]+</Version>', "<Version>$NewVersion</Version>"
$csprojContent = $csprojContent -replace '<AssemblyVersion>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+</AssemblyVersion>', "<AssemblyVersion>$newAssemblyVersion</AssemblyVersion>"
$csprojContent = $csprojContent -replace '<FileVersion>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+</FileVersion>', "<FileVersion>$newAssemblyVersion</FileVersion>"

[System.IO.File]::WriteAllText($csprojPath, $csprojContent, [System.Text.UTF8Encoding]::new($false))

Write-Host "Version updated to NuGet '$NewVersion' and assembly/file '$newAssemblyVersion'"

$nupkgName = "Snappyup.Aspire.Hosting.Dragonfly.$NewVersion.nupkg"
$artifactsDir = "artifacts"
$nupkgPath = [System.IO.Path]::Combine($repoRoot, $artifactsDir, $nupkgName)

$packResponse = Read-Host "Do you want to run 'dotnet pack'? (y/n)"
if ($packResponse -eq 'y') {
    Write-Host "Running 'dotnet pack'..."
    Push-Location $repoRoot
    try {
        New-Item -ItemType Directory -Path (Join-Path $repoRoot $artifactsDir) -Force | Out-Null
        dotnet pack $csprojPath -c Release --output (Join-Path $repoRoot $artifactsDir)
    }
    finally {
        Pop-Location
    }
}

$pushResponse = Read-Host "Do you want to push the package to NuGet? (y/n)"
if ($pushResponse -eq 'y') {
    $nugetSource = $env:NUGET_SOURCE
    if (-not $nugetSource) {
        $nugetSource = "https://api.nuget.org/v3/index.json"
    }

    if (-not (Test-Path $nupkgPath)) {
        Write-Error "Package not found: $nupkgPath (pack first with matching version)"
    }

    Write-Host "Pushing $nupkgPath to '$nugetSource' ..."

    $pushArgs = @(
        "nuget", "push", $nupkgPath,
        "--source", $nugetSource,
        "--skip-duplicate"
    )

    if ($env:NUGET_API_KEY) {
        $pushArgs += @("--api-key", $env:NUGET_API_KEY)
    }

    Push-Location $repoRoot
    try {
        & dotnet @pushArgs

        $snupkg = [System.IO.Path]::Combine($repoRoot, $artifactsDir, "Snappyup.Aspire.Hosting.Dragonfly.$NewVersion.snupkg")
        if (Test-Path $snupkg) {
            Write-Host "Pushing symbol package..."
            $symArgs = @(
                "nuget", "push", $snupkg,
                "--source", $nugetSource,
                "--skip-duplicate"
            )
            if ($env:NUGET_API_KEY) {
                $symArgs += @("--api-key", $env:NUGET_API_KEY)
            }
            & dotnet @symArgs
        }
    }
    finally {
        Pop-Location
    }
}
