#!/usr/bin/env powershell
<#
.SYNOPSIS
    Automated build script for Wonderland Private Server
    
.DESCRIPTION
    Builds the Wonderland Private Server solution with automatic dependency restoration.
    Supports both Debug and Release configurations.
    
.PARAMETER Configuration
    Build configuration: 'Debug' or 'Release' (default: Release)
    
.PARAMETER Clean
    Perform a clean build (delete obj/ and bin/ first)
    
.PARAMETER NoBuild
    Only restore packages without building
    
.EXAMPLE
    .\build.ps1 -Configuration Release
    .\build.ps1 -Configuration Debug -Clean
    .\build.ps1 -NoBuild
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [switch]$Clean,
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'

# Color output helpers
function Write-Success { Write-Host $args[0] -ForegroundColor Green }
function Write-Error_ { Write-Host $args[0] -ForegroundColor Red }
function Write-Info { Write-Host $args[0] -ForegroundColor Cyan }

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Info "═══════════════════════════════════════════════════"
Write-Info "  Wonderland Private Server Build Script"
Write-Info "═══════════════════════════════════════════════════"
Write-Info ""

# Check prerequisites
Write-Info "Checking prerequisites..."

# Check for NuGet
$nuget = Get-Command nuget -ErrorAction SilentlyContinue
if (-not $nuget) {
    Write-Error_ "❌ NuGet not found in PATH"
    Write-Info "   Download from: https://www.nuget.org/downloads"
    Write-Info "   Or install via: choco install nuget.commandline -y"
    exit 1
}
Write-Success "✓ NuGet found: $($nuget.Source)"

# Check for MSBuild
$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
if (-not $msbuild) {
    # Try to find MSBuild in common VS locations
    $vsPaths = @(
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"
    )
    
    $msbuildPath = $null
    foreach ($path in $vsPaths) {
        if (Test-Path $path) {
            $msbuildPath = $path
            break
        }
    }
    
    if (-not $msbuildPath) {
        Write-Error_ "❌ MSBuild not found"
        Write-Info "   Install Visual Studio 2017 or higher:"
        Write-Info "   https://visualstudio.microsoft.com/downloads/"
        Write-Info ""
        Write-Info "   Or install Build Tools for Visual Studio:"
        Write-Info "   https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022"
        exit 1
    }
} else {
    $msbuildPath = $msbuild.Source
}
Write-Success "✓ MSBuild found: $msbuildPath"

# Verify solution file exists
$slnFile = Join-Path $ScriptDir "Wonderland Private Server.sln"
if (-not (Test-Path $slnFile)) {
    Write-Error_ "❌ Solution file not found: $slnFile"
    exit 1
}
Write-Success "✓ Solution file found"

Write-Info ""

# Clean if requested
if ($Clean) {
    Write-Info "Cleaning previous builds..."
    $binPath = Join-Path $ScriptDir "bin"
    $objPath = Join-Path $ScriptDir "obj"
    
    if (Test-Path $binPath) {
        Remove-Item -Path $binPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "  ✓ Cleaned bin directory"
    }
    if (Test-Path $objPath) {
        Remove-Item -Path $objPath -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "  ✓ Cleaned obj directory"
    }
    
    # Also clean project-specific directories
    foreach ($subDir in @("wlo.pserver.core", "wlo.pserver.maps", "wlo.pserver.Bot")) {
        $subBin = Join-Path $ScriptDir $subDir "bin"
        $subObj = Join-Path $ScriptDir $subDir "obj"
        
        if (Test-Path $subBin) {
            Remove-Item -Path $subBin -Recurse -Force -ErrorAction SilentlyContinue
        }
        if (Test-Path $subObj) {
            Remove-Item -Path $subObj -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    Write-Info ""
}

# Restore NuGet packages
Write-Info "Restoring NuGet packages..."
& nuget restore $slnFile

if ($LASTEXITCODE -ne 0) {
    Write-Error_ "❌ NuGet restore failed with exit code: $LASTEXITCODE"
    exit 1
}
Write-Success "✓ NuGet packages restored"
Write-Info ""

if ($NoBuild) {
    Write-Success "✓ Build skipped (NoBuild flag set)"
    exit 0
}

# Build solution
Write-Info "Building solution ($Configuration configuration)..."
Write-Info "This may take a moment..."
Write-Info ""

$buildArgs = @(
    "`"$slnFile`"",
    "/p:Configuration=$Configuration",
    "/p:Platform=AnyCPU",
    "/p:WarningLevel=4",
    "/m",  # Use all processors
    "/nologo"
)

& $msbuildPath @buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error_ "❌ Build failed with exit code: $LASTEXITCODE"
    exit 1
}

Write-Success "✓ Build completed successfully!"
Write-Info ""

# Show output location
$outputDir = Join-Path $ScriptDir "bin" $Configuration
if (Test-Path $outputDir) {
    Write-Info "Build output:"
    Write-Info "  Location: $outputDir"
    
    $exePath = Get-ChildItem -Path $outputDir -Filter "*.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($exePath) {
        Write-Info "  Executable: $($exePath.Name)"
        Write-Info ""
        Write-Info "To run the server:"
        Write-Info "  & `"$($exePath.FullName)`""
    }
}

Write-Info ""
Write-Success "═══════════════════════════════════════════════════"
Write-Success "  Build Complete!"
Write-Success "═══════════════════════════════════════════════════"
