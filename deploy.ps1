#!/usr/bin/env powershell
<#
.SYNOPSIS
    Deployment script for Wonderland Private Server
    
.DESCRIPTION
    Packages the built server for deployment to local or remote machines.
    Creates a clean package with all required files and configuration.
    
.PARAMETER Configuration
    Build configuration to deploy: 'Debug' or 'Release' (default: Release)
    
.PARAMETER OutputPath
    Where to place the deployment package (default: .\Deploy)
    
.PARAMETER IncludeSource
    Include source code in the deployment package
    
.PARAMETER CreateZip
    Create a ZIP file instead of folder structure
    
.EXAMPLE
    .\deploy.ps1 -Configuration Release
    .\deploy.ps1 -OutputPath "C:\Deploy\WonderlandServer" -CreateZip
    .\deploy.ps1 -Configuration Release -IncludeSource
#>

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [string]$OutputPath = '.\Deploy',
    
    [switch]$IncludeSource,
    [switch]$CreateZip
)

$ErrorActionPreference = 'Stop'

# Color output helpers
function Write-Success { Write-Host $args[0] -ForegroundColor Green }
function Write-Error_ { Write-Host $args[0] -ForegroundColor Red }
function Write-Info { Write-Host $args[0] -ForegroundColor Cyan }
function Write-Warn { Write-Host $args[0] -ForegroundColor Yellow }

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Info "═══════════════════════════════════════════════════"
Write-Info "  Wonderland Private Server Deployment Script"
Write-Info "═══════════════════════════════════════════════════"
Write-Info ""

# Check if build exists
$binDir = Join-Path $ScriptDir "bin" $Configuration
if (-not (Test-Path $binDir)) {
    Write-Error_ "❌ Build output not found: $binDir"
    Write-Info "   Please run .\build.ps1 -Configuration $Configuration first"
    exit 1
}

Write-Success "✓ Build found: $binDir"
Write-Info ""

# Verify main executable exists
$mainExe = Join-Path $binDir "Wonderland Private Server.exe"
if (-not (Test-Path $mainExe)) {
    Write-Error_ "❌ Main executable not found: $mainExe"
    exit 1
}

Write-Success "✓ Main executable verified"
Write-Info ""

# Create output directory
Write-Info "Creating deployment package..."

# Clean up previous deployment
if (Test-Path $OutputPath) {
    Remove-Item -Path $OutputPath -Recurse -Force -ErrorAction SilentlyContinue
    Write-Info "  ✓ Cleaned previous deployment"
}

New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null

$deployRoot = Resolve-Path $OutputPath

# Copy build output
Write-Info "Copying application files..."
Copy-Item -Path "$binDir\*" -Destination $deployRoot -Recurse -Force

# Copy essential configuration files
Write-Info "Copying configuration files..."
foreach ($file in @("App.config")) {
    $srcPath = Join-Path $ScriptDir $file
    if (Test-Path $srcPath) {
        Copy-Item -Path $srcPath -Destination $deployRoot -Force
        Write-Info "  ✓ $file"
    }
}

# Create Data directory structure
Write-Info "Creating data directory structure..."
$dataDir = Join-Path $deployRoot "Data"
New-Item -ItemType Directory -Path $dataDir -Force | Out-Null

# Copy database template
$dbTemplate = Join-Path $ScriptDir "Data" "ServerDataBase.db.temp"
if (Test-Path $dbTemplate) {
    Copy-Item -Path $dbTemplate -Destination $dataDir -Force
    Write-Info "  ✓ Database template copied"
}

# Create Web directory for registration
Write-Info "Copying web files..."
$webDir = Join-Path $deployRoot "Web"
$srcWebDir = Join-Path $ScriptDir "Web"
if (Test-Path $srcWebDir) {
    Copy-Item -Path $srcWebDir -Destination $deployRoot -Recurse -Force
    Write-Info "  ✓ Web resources copied"
}

# Include source code if requested
if ($IncludeSource) {
    Write-Info "Copying source code..."
    $srcDir = Join-Path $deployRoot "Src"
    $srcSrcDir = Join-Path $ScriptDir "Src"
    if (Test-Path $srcSrcDir) {
        Copy-Item -Path $srcSrcDir -Destination $deployRoot -Recurse -Force
        Write-Info "  ✓ Source code included"
    }
}

# Copy documentation
Write-Info "Copying documentation..."
foreach ($docFile in @("README.md", "BUILD_AND_DEPLOY.md", "LICENSE", "LICENSE.txt")) {
    $srcPath = Join-Path $ScriptDir $docFile
    if (Test-Path $srcPath) {
        Copy-Item -Path $srcPath -Destination $deployRoot -Force
        Write-Info "  ✓ $docFile"
    }
}

Write-Info ""

# Create deployment information file
$deployInfoPath = Join-Path $deployRoot "DEPLOYMENT_INFO.txt"
@"
Wonderland Private Server - Deployment Package
===============================================

Build Date: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Configuration: $Configuration
.NET Framework Required: 4.5 or higher

SERVER STARTUP
--------------
To run the server, execute:
  Wonderland Private Server.exe

DATABASE
--------
Default location: %APPDATA%\PServer\LocalServer.sqlite

The server will automatically create the SQLite database on first run if it
doesn't exist. You can also configure a MySQL database in App.config.

WEB REGISTRATION
----------------
When using the default SQLite database, the registration page is available at:
  http://127.0.0.1:8090/

This page is only accessible from the local machine. To expose it to other
players, configure a reverse proxy with HTTPS and rate limiting.

CONFIGURATION
-------------
Edit App.config to customize:
- Database connection strings
- Server port and address
- Logging settings
- Plugin directories

DEPLOYMENT STRUCTURE
--------------------
$(Get-ChildItem -Path $deployRoot -Depth 1 | ForEach-Object { "  " + $_.Name })

FILES INCLUDED
--------------
- All compiled binaries and dependencies
- Web registration interface (HTML/CSS/JS)
- Database template
- Configuration file (App.config)
- Documentation and licenses

SYSTEM REQUIREMENTS
-------------------
- Windows 7 SP1 or later
- .NET Framework 4.5 or higher
- 512 MB minimum RAM (1 GB recommended)
- 100 MB free disk space (more for databases)

TROUBLESHOOTING
---------------
1. If the server won't start:
   - Verify .NET Framework 4.5 is installed
   - Check App.config for valid database settings
   - Review application event log for errors

2. If registration page doesn't load:
   - Ensure port 8090 is not blocked by firewall
   - Check that you're accessing from localhost

3. Database issues:
   - Ensure write permissions to %APPDATA%\PServer\
   - Verify SQLite DLLs are present in the bin directory

For more information, see BUILD_AND_DEPLOY.md or README.md
"@ | Out-File -Path $deployInfoPath -Encoding ASCII

Write-Success "✓ Deployment info file created"

Write-Info ""

# Create batch launcher for convenience
$launcherPath = Join-Path $deployRoot "StartServer.bat"
@"
@echo off
REM Wonderland Private Server Launcher
REM This batch file starts the server with proper error handling

title Wonderland Private Server
cd /d "%~dp0"

if not exist "Wonderland Private Server.exe" (
    echo Error: Wonderland Private Server.exe not found!
    echo Please ensure this file is in the same directory as StartServer.bat
    pause
    exit /b 1
)

echo Starting Wonderland Private Server...
echo.
"Wonderland Private Server.exe"

if errorlevel 1 (
    echo.
    echo Error: Server exited with error code %errorlevel%
    echo Please check the server logs for details.
    pause
)
"@ | Out-File -Path $launcherPath -Encoding ASCII

Write-Success "✓ StartServer.bat launcher created"

Write-Info ""

# Create PowerShell launcher
$psLauncherPath = Join-Path $deployRoot "StartServer.ps1"
@"
# Wonderland Private Server Launcher (PowerShell)
`$scriptDir = Split-Path -Parent `$MyInvocation.MyCommand.Path
Push-Location `$scriptDir

`$exe = "Wonderland Private Server.exe"

if (-not (Test-Path `$exe)) {
    Write-Host "Error: `$exe not found!" -ForegroundColor Red
    Write-Host "Please ensure this file is in the same directory as StartServer.ps1"
    exit 1
}

Write-Host "Starting Wonderland Private Server..." -ForegroundColor Cyan
Write-Host ""

& ".\\`$exe"

if (`$LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Server exited with error code: `$LASTEXITCODE" -ForegroundColor Red
    Write-Host "Check the logs for details."
}

Pop-Location
"@ | Out-File -Path $psLauncherPath -Encoding UTF8

Write-Success "✓ StartServer.ps1 launcher created"

Write-Info ""

# Create ZIP if requested
if ($CreateZip) {
    Write-Info "Creating ZIP archive..."
    
    $zipPath = "$deployRoot.zip"
    
    # Remove existing ZIP
    if (Test-Path $zipPath) {
        Remove-Item -Path $zipPath -Force
    }
    
    # Create ZIP
    if ((Get-Command Compress-Archive -ErrorAction SilentlyContinue)) {
        Compress-Archive -Path $deployRoot -DestinationPath $zipPath -CompressionLevel Optimal
        Write-Success "✓ ZIP created: $zipPath"
    } else {
        Write-Warn "⚠ Compress-Archive not available (requires PowerShell 5.0+)"
        Write-Warn "  Manual ZIP creation required"
    }
    
    Write-Info ""
}

# Summary
Write-Info ""
Write-Success "═══════════════════════════════════════════════════"
Write-Success "  Deployment Package Created!"
Write-Success "═══════════════════════════════════════════════════"
Write-Info ""
Write-Info "Package Location:"
Write-Info "  $deployRoot"
Write-Info ""
Write-Info "Quick Start:"
Write-Info "  1. Copy the entire folder to your server/target machine"
Write-Info "  2. Run: StartServer.bat  (or StartServer.ps1 on PowerShell)"
Write-Info "  3. Access registration: http://127.0.0.1:8090/"
Write-Info ""
Write-Info "Package Size:"
$size = (Get-ChildItem -Path $deployRoot -Recurse | Measure-Object -Property Length -Sum).Sum
Write-Info "  $([math]::Round($size / 1MB, 2)) MB"
Write-Info ""

# Show deployment structure
Write-Info "Contents:"
Get-ChildItem -Path $deployRoot -Depth 2 | ForEach-Object {
    $indent = "  " * ((($_.FullName | Split-Path -Parent).Length - $deployRoot.Length) / 50)
    if ($_.PSIsContainer) {
        Write-Info "$indent📁 $($_.Name)/"
    } else {
        Write-Info "$indent📄 $($_.Name) ($([math]::Round($_.Length / 1KB, 2)) KB)"
    }
}

Write-Info ""
