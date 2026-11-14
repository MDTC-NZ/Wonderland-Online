# Wonderland Private Server - Automation Scripts

This directory contains PowerShell automation scripts to simplify building, deploying, and managing the Wonderland Private Server.

## Quick Start

```powershell
# 1. Check prerequisites (one-time setup)
.\setup.ps1

# 2. Build the server
.\build.ps1

# 3. Create deployment package
.\deploy.ps1 -CreateZip

# 4. Run the server
.\bin\Release\Wonderland Private Server.exe
```

## Scripts Overview

### 1. `setup.ps1` - Environment Validation

**Purpose**: Validates your system has all required prerequisites for building the Wonderland server.

**What it checks**:
- .NET Framework 4.5+ is installed
- Visual Studio 2015+ or Build Tools are available
- NuGet package manager is accessible
- Project files and dependencies exist
- Database templates and web resources are present

**Usage**:
```powershell
# Full setup check with recommendations
.\setup.ps1

# Only check prerequisites (don't attempt fixes)
.\setup.ps1 -CheckOnly

# Attempt to install .NET Framework (if missing)
.\setup.ps1 -InstallDotNet
```

**Output**:
- ✓ = All checks passed, ready to build
- ⚠ = Non-critical warnings (optional dependencies)
- ✗ = Critical issues that block building

**Example Output**:
```
═══════════════════════════════════════════════════
  Wonderland Private Server - Setup Wizard
═══════════════════════════════════════════════════

Checking .NET Framework...
  ✓ .NET Runtime: .NET Framework 4.8

Checking Visual Studio / Build Tools...
  ✓ Visual Studio found: 2022

Checking NuGet...
  ✓ NuGet found: C:\Program Files\NuGet\nuget.exe

Checking project files...
  ✓ Wonderland Private Server.sln
  ✓ Wonderland Private Server.csproj
  ✓ packages.config

═══════════════════════════════════════════════════
  ✓ All Critical Checks Passed!
═══════════════════════════════════════════════════
```

---

### 2. `build.ps1` - Project Compilation

**Purpose**: Compiles the Wonderland Private Server from source code.

**What it does**:
1. Validates MSBuild and NuGet are available
2. Restores NuGet packages from packages.config
3. Compiles all projects (main server + libraries)
4. Produces executable in `bin\Debug\` or `bin\Release\`

**Usage**:
```powershell
# Build Release configuration (optimized, smaller)
.\build.ps1 -Configuration Release

# Build Debug configuration (with symbols, larger)
.\build.ps1 -Configuration Debug

# Clean previous build first
.\build.ps1 -Configuration Release -Clean

# Restore packages only (no compilation)
.\build.ps1 -NoBuild
```

**Parameters**:
- `-Configuration`: `Debug` (default) or `Release`
  - **Debug**: Slower startup, larger binary, easier debugging
  - **Release**: Faster startup, optimized, recommended for deployment
  
- `-Clean`: Deletes `bin/` and `obj/` directories before building
  
- `-NoBuild`: Restores NuGet packages but skips compilation

**Example Output**:
```
═══════════════════════════════════════════════════
  Wonderland Private Server Build Script
═══════════════════════════════════════════════════

Checking prerequisites...
✓ NuGet found: C:\Program Files\NuGet\nuget.exe
✓ MSBuild found: C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe
✓ Solution file found

Cleaning previous builds...
  ✓ Cleaned bin directory
  ✓ Cleaned obj directory

Restoring NuGet packages...
✓ NuGet packages restored

Building solution (Release configuration)...
This may take a moment...

✓ Build completed successfully!

Build output:
  Location: C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop\bin\Release
  Executable: Wonderland Private Server.exe

═══════════════════════════════════════════════════
  Build Complete!
═══════════════════════════════════════════════════
```

**Build Artifacts**:
- Main executable: `bin\Release\Wonderland Private Server.exe`
- All dependencies: DLLs copied to `bin\Release\`
- Database template: `bin\Release\Data\ServerDataBase.db.temp`

---

### 3. `deploy.ps1` - Packaging for Distribution

**Purpose**: Creates a clean deployment package ready to run on any machine.

**What it does**:
1. Packages all files from the build output
2. Includes configuration files and databases
3. Includes web registration interface
4. Creates launcher scripts (.bat and .ps1)
5. Optionally creates a ZIP archive
6. Generates deployment documentation

**Usage**:
```powershell
# Create deployment folder (default output)
.\deploy.ps1 -Configuration Release

# Create deployment to custom location
.\deploy.ps1 -Configuration Release -OutputPath "C:\Deploy\WonderlandServer"

# Create deployment and ZIP archive
.\deploy.ps1 -Configuration Release -CreateZip

# Include source code in deployment
.\deploy.ps1 -Configuration Release -IncludeSource

# Full package with everything
.\deploy.ps1 -Configuration Release -CreateZip -IncludeSource -OutputPath "C:\Deploy\WonderlandServer-Full"
```

**Parameters**:
- `-Configuration`: `Debug` or `Release` (default: `Release`)
- `-OutputPath`: Target deployment directory (default: `.\Deploy`)
- `-IncludeSource`: Copy all source code to deployment
- `-CreateZip`: Create a ZIP archive of the deployment

**Deployment Package Contents**:
```
Deploy/
├── Wonderland Private Server.exe    # Main executable
├── *.dll                             # All dependencies
├── App.config                        # Configuration file
├── Data/
│   └── ServerDataBase.db.temp        # Database template
├── Web/
│   ├── register.html
│   ├── register.css
│   └── register.js
├── StartServer.bat                   # Windows batch launcher
├── StartServer.ps1                   # PowerShell launcher
├── DEPLOYMENT_INFO.txt               # Quick reference
├── README.md                         # Project readme
├── BUILD_AND_DEPLOY.md               # Build instructions
└── LICENSE                           # License files
```

**Deployment Size**: ~50-100 MB (depending on configuration)

---

## Complete Build & Deployment Workflow

### Initial Setup (One-Time)

```powershell
# 1. Validate environment
.\setup.ps1

# 2. If setup fails, install missing tools:
#    - Visual Studio 2015+ from https://visualstudio.microsoft.com/downloads/
#    - .NET Framework 4.5 from https://www.microsoft.com/download/details.aspx?id=42637

# 3. Re-run setup to verify
.\setup.ps1
```

### Build & Deploy Workflow

```powershell
# 1. Build the server
.\build.ps1 -Configuration Release

# 2. Test locally
.\bin\Release\Wonderland Private Server.exe

# 3. Create deployment package
.\deploy.ps1 -Configuration Release

# 4. Create ZIP for distribution
.\deploy.ps1 -Configuration Release -CreateZip

# 5. Deploy to server
#    Copy Deploy folder to target machine
#    Or extract Deploy.zip on target machine
```

### Rapid Development Cycle

```powershell
# Build and test quickly
.\build.ps1 -Configuration Debug
.\bin\Debug\Wonderland Private Server.exe

# After making code changes, just rebuild
.\build.ps1 -Configuration Debug

# Once ready for release
.\build.ps1 -Configuration Release
.\deploy.ps1 -Configuration Release -CreateZip
```

### Clean Build (Troubleshooting)

```powershell
# Start from scratch
.\build.ps1 -Configuration Release -Clean

# If still having issues, clear NuGet cache
nuget locals all -clear

# Then rebuild
.\build.ps1 -Configuration Release
```

---

## Advanced Usage

### Building Specific Configurations

```powershell
# Debug with source debugging enabled
.\build.ps1 -Configuration Debug

# Release for production
.\build.ps1 -Configuration Release
```

### Custom Deployment Locations

```powershell
# Deploy to shared network folder
.\deploy.ps1 -OutputPath "\\server\share\WonderlandServer" -CreateZip

# Deploy to cloud storage staging
.\deploy.ps1 -OutputPath "D:\CloudSync\Wonderland" -CreateZip
```

### CI/CD Integration

```powershell
# In CI/CD pipeline
$buildResult = & ".\build.ps1" -Configuration Release
if ($LASTEXITCODE -eq 0) {
    & ".\deploy.ps1" -Configuration Release -CreateZip -OutputPath ".\artifacts"
}
```

---

## Troubleshooting

### Issue: "MSBuild not found"

**Solution**:
```powershell
# Install Visual Studio 2022 Community Edition
# https://visualstudio.microsoft.com/downloads/

# Or install Build Tools only
# https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022
```

### Issue: "NuGet packages missing"

**Solution**:
```powershell
# Manually restore packages
nuget restore "Wonderland Private Server.sln"

# Then build
.\build.ps1 -Configuration Release
```

### Issue: ".NET Framework 4.5 not found"

**Solution**:
```powershell
# Check installed .NET Frameworks
Get-ChildItem "HKLM:\Software\Microsoft\NET Framework Setup\NDP" -Recurse

# Install from
# https://www.microsoft.com/en-us/download/details.aspx?id=42637
```

### Issue: "Folder already exists" during deployment

**Solution**:
```powershell
# Remove old deployment
Remove-Item -Path ".\Deploy" -Recurse -Force

# Create new deployment
.\deploy.ps1 -Configuration Release
```

### Issue: Build takes very long

**Solution**:
```powershell
# First build is slower due to package restoration
# Subsequent builds are much faster

# To force incremental rebuild
.\build.ps1 -Configuration Release

# To force full rebuild
.\build.ps1 -Configuration Release -Clean
```

---

## Script Parameters Reference

### setup.ps1
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-CheckOnly` | Switch | - | Only validate, don't attempt fixes |
| `-InstallDotNet` | Switch | - | Attempt .NET Framework installation |

### build.ps1
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Configuration` | String | Release | `Debug` or `Release` |
| `-Clean` | Switch | - | Delete previous build first |
| `-NoBuild` | Switch | - | Restore packages only |

### deploy.ps1
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `-Configuration` | String | Release | `Debug` or `Release` |
| `-OutputPath` | String | .\Deploy | Target deployment folder |
| `-IncludeSource` | Switch | - | Include source code |
| `-CreateZip` | Switch | - | Create ZIP archive |

---

## Performance Tips

1. **First build is slow** (package restoration)
   - Subsequent builds are much faster
   - Network speed affects NuGet restoration

2. **Use Release configuration**
   - Smaller binary size
   - Faster startup time
   - Optimized code

3. **Incremental builds**
   - Only rebuild changed files
   - Faster than full rebuilds
   - Use `-Clean` only when necessary

4. **Keep packages cached**
   - Don't clear NuGet cache unless necessary
   - Use `nuget restore` sparingly

---

## Support & Documentation

- **Build Details**: See [BUILD_AND_DEPLOY.md](./BUILD_AND_DEPLOY.md)
- **Project Info**: See [README.md](./README.md)
- **Architecture**: See source code in `Src/` directory
- **Configuration**: Edit `App.config` or `Src\App.config`

---

## PowerShell Requirements

- **Minimum**: PowerShell 3.0 (Windows 7 SP1+)
- **Recommended**: PowerShell 5.0+ (Windows 10+)
- **Execution Policy**: May need to run as Administrator to execute scripts

### Enabling Scripts (if needed)

```powershell
# Check current execution policy
Get-ExecutionPolicy

# Allow scripts in current user scope
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

---

Last Updated: 2025
For questions, refer to the project documentation or Visual Studio project properties.
