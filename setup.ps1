#!/usr/bin/env powershell
<#
.SYNOPSIS
    Setup and validation script for Wonderland Private Server
    
.DESCRIPTION
    Checks system requirements, validates the build environment, and prepares
    the project for building and deployment.
    
.PARAMETER CheckOnly
    Only check prerequisites without attempting to fix issues
    
.PARAMETER InstallDotNet
    Attempt to download and install .NET Framework 4.5
    
.EXAMPLE
    .\setup.ps1
    .\setup.ps1 -CheckOnly
#>

param(
    [switch]$CheckOnly,
    [switch]$InstallDotNet
)

$ErrorActionPreference = 'Continue'

# Color output helpers
function Write-Success { Write-Host $args[0] -ForegroundColor Green }
function Write-Error_ { Write-Host $args[0] -ForegroundColor Red }
function Write-Info { Write-Host $args[0] -ForegroundColor Cyan }
function Write-Warn { Write-Host $args[0] -ForegroundColor Yellow }

Write-Info "═══════════════════════════════════════════════════"
Write-Info "  Wonderland Private Server - Setup Wizard"
Write-Info "═══════════════════════════════════════════════════"
Write-Info ""

$allChecksPass = $true

# Check .NET Framework 4.5
Write-Info "Checking .NET Framework..."
$dotnetVersion = $null
try {
    $dotnetVersion = [Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
    Write-Success "  ✓ .NET Runtime: $dotnetVersion"
} catch {
    # Try alternate method
    $regPath = "HKLM:\Software\Microsoft\NET Framework Setup\NDP\v4\Full"
    if (Test-Path $regPath) {
        $version = Get-ItemProperty -Path $regPath -Name Version -ErrorAction SilentlyContinue
        if ($version) {
            Write-Success "  ✓ .NET Framework 4.5 (or newer) detected"
        } else {
            Write-Error_ "  ✗ .NET Framework 4.5 not found or not properly installed"
            $allChecksPass = $false
        }
    } else {
        Write-Warn "  ⚠ Could not verify .NET Framework"
        Write-Warn "    .NET Framework 4.5 or higher is required"
        Write-Info "    Download: https://www.microsoft.com/en-us/download/details.aspx?id=42637"
        $allChecksPass = $false
    }
}

Write-Info ""

# Check Visual Studio or Build Tools
Write-Info "Checking Visual Studio / Build Tools..."

$vsInstalled = $false
$vsPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022",
    "C:\Program Files\Microsoft Visual Studio\2019",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019",
    "C:\Program Files (x86)\Microsoft Visual Studio\2017"
)

foreach ($path in $vsPaths) {
    if (Test-Path $path) {
        $edition = Split-Path -Leaf $path
        Write-Success "  ✓ Visual Studio found: $edition"
        $vsInstalled = $true
        break
    }
}

# Check for MSBuild in PATH
$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuild) {
    Write-Success "  ✓ MSBuild available: $($msbuild.Source)"
    $vsInstalled = $true
} elseif (-not $vsInstalled) {
    Write-Error_ "  ✗ MSBuild not found"
    Write-Warn "    Install Visual Studio 2015 or higher"
    Write-Info "    Download: https://visualstudio.microsoft.com/downloads/"
    $allChecksPass = $false
}

Write-Info ""

# Check NuGet
Write-Info "Checking NuGet..."
$nuget = Get-Command nuget -ErrorAction SilentlyContinue
if ($nuget) {
    Write-Success "  ✓ NuGet found: $($nuget.Source)"
} else {
    Write-Warn "  ⚠ NuGet not in PATH (can still build with Visual Studio)"
    Write-Info "    Download: https://www.nuget.org/downloads"
}

Write-Info ""

# Check project files
Write-Info "Checking project files..."
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$requiredFiles = @(
    "Wonderland Private Server.sln",
    "Wonderland Private Server.csproj",
    "packages.config"
)

foreach ($file in $requiredFiles) {
    $path = Join-Path $scriptDir $file
    if (Test-Path $path) {
        Write-Success "  ✓ $file"
    } else {
        Write-Error_ "  ✗ $file not found"
        $allChecksPass = $false
    }
}

Write-Info ""

# Check project dependencies
Write-Info "Checking project structure..."
$requiredDirs = @(
    "Src",
    "wlo.pserver.core",
    "packages",
    "DLLS"
)

foreach ($dir in $requiredDirs) {
    $path = Join-Path $scriptDir $dir
    if (Test-Path $path) {
        Write-Success "  ✓ $dir/"
    } else {
        Write-Warn "  ⚠ $dir/ not found (may be needed)"
    }
}

Write-Info ""

# Check for pre-built DLLs
Write-Info "Checking DLL dependencies..."
$dllsDir = Join-Path $scriptDir "DLLS"
if (Test-Path $dllsDir) {
    $dlls = @(
        "GupdtSrv.dll",
        "PhoenixData.dll",
        "RCLibrary.dll",
        "Wlo.Core.dll"
    )
    
    foreach ($dll in $dlls) {
        $path = Join-Path $dllsDir $dll
        if (Test-Path $path) {
            Write-Success "  ✓ $dll"
        } else {
            Write-Warn "  ⚠ $dll not found"
        }
    }
}

Write-Info ""

# Check database template
Write-Info "Checking database files..."
$dataDir = Join-Path $scriptDir "Data"
if (Test-Path $dataDir) {
    $dbTemplate = Join-Path $dataDir "ServerDataBase.db.temp"
    if (Test-Path $dbTemplate) {
        $size = (Get-Item $dbTemplate).Length
        Write-Success "  ✓ Database template found ($([math]::Round($size / 1KB, 2)) KB)"
    } else {
        Write-Warn "  ⚠ Database template not found"
    }
} else {
    Write-Warn "  ⚠ Data directory not found"
}

Write-Info ""

# Check web files
Write-Info "Checking web resources..."
$webDir = Join-Path $scriptDir "Web"
if (Test-Path $webDir) {
    $webFiles = Get-ChildItem -Path $webDir -Filter "*.html", "*.css", "*.js" -ErrorAction SilentlyContinue
    if ($webFiles) {
        Write-Success "  ✓ Web resources found ($($webFiles.Count) files)"
    } else {
        Write-Warn "  ⚠ No web files found in Web/"
    }
} else {
    Write-Warn "  ⚠ Web directory not found"
}

Write-Info ""
Write-Info ""

# Summary
if ($allChecksPass) {
    Write-Success "═══════════════════════════════════════════════════"
    Write-Success "  ✓ All Critical Checks Passed!"
    Write-Success "═══════════════════════════════════════════════════"
    Write-Info ""
    Write-Info "Next steps:"
    Write-Info "  1. Run the build script:"
    Write-Info "     .\build.ps1 -Configuration Release"
    Write-Info ""
    Write-Info "  2. Start the server:"
    Write-Info "     .\bin\Release\Wonderland Private Server.exe"
    Write-Info ""
    Write-Info "  3. Create deployment package:"
    Write-Info "     .\deploy.ps1 -Configuration Release -CreateZip"
    Write-Info ""
} else {
    Write-Warn "═══════════════════════════════════════════════════"
    Write-Warn "  ⚠ Some Checks Failed"
    Write-Warn "═══════════════════════════════════════════════════"
    Write-Info ""
    Write-Warn "Please install missing components:"
    Write-Info ""
    Write-Info "1. Install Visual Studio 2015 or higher:"
    Write-Info "   https://visualstudio.microsoft.com/downloads/"
    Write-Info "   (Select '.NET desktop development' workload)"
    Write-Info ""
    Write-Info "2. Ensure .NET Framework 4.5 is installed:"
    Write-Info "   https://www.microsoft.com/en-us/download/details.aspx?id=42637"
    Write-Info ""
    Write-Info "3. (Optional) Install NuGet CLI for command-line builds:"
    Write-Info "   https://www.nuget.org/downloads"
    Write-Info ""
    
    if (-not $CheckOnly) {
        Write-Info "After installing missing components, run setup.ps1 again."
    }
}

Write-Info ""
Write-Info "For detailed information, see: BUILD_AND_DEPLOY.md"
Write-Info ""

exit ($allChecksPass ? 0 : 1)
