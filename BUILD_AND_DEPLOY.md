# Wonderland Private Server - Build & Deployment Guide

## Overview

This is a **Windows Forms-based game server** for Wonderland Online targeting **.NET Framework 4.5**. The project uses MSBuild and NuGet for dependency management.

**Key Components:**
- **Main Server**: Wonderland Private Server (WinForms GUI)
- **Core Library**: wlo.pserver.core (shared game logic)
- **Bot Project**: wlo.pserver.Bot
- **Maps Plugin**: wlo.pserver.maps

---

## Prerequisites

### Option 1: Using Visual Studio (Recommended)

**Visual Studio 2012 or higher** is required. Steps:

1. **Download & Install Visual Studio**
   - Visual Studio 2015, 2017, 2019, or 2022 all support .NET Framework 4.5 projects
   - Install the ".NET desktop development" workload during setup
   - Include "NuGet Package Manager"

2. **Verify .NET Framework 4.5+ is installed**
   ```powershell
   # Check installed .NET Frameworks
   Get-ChildItem "HKLM:\Software\Microsoft\NET Framework Setup\NDP" -Recurse | 
     Where-Object { $_.PSChildName -like "v*" } | Select-Object PSChildName
   ```

### Option 2: Using Command Line (MSBuild + .NET Framework SDK)

Ensure you have:
- **Visual Studio Build Tools** (for MSBuild)
- **.NET Framework 4.5 Developer Pack**
- **NuGet CLI** (optional, for manual package restore)

---

## Building the Project

### Method A: Using Visual Studio GUI

1. **Open the Solution**
   ```
   C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop\Wonderland Private Server.sln
   ```

2. **Restore NuGet Packages**
   - Right-click solution → "Restore NuGet Packages"
   - Or: Tools → NuGet Package Manager → Manage NuGet Packages for Solution

3. **Build the Solution**
   - Build → Build Solution (Ctrl+Shift+B)
   - Or select **Release** configuration for production builds
   - Output goes to: `bin\Debug\` or `bin\Release\`

### Method B: Using Command Line (MSBuild)

```powershell
# Navigate to project directory
cd "C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop"

# Restore NuGet packages first
nuget restore "Wonderland Private Server.sln"

# Build the solution (Debug configuration)
msbuild "Wonderland Private Server.sln" /p:Configuration=Debug /p:Platform=AnyCPU

# Or build Release configuration
msbuild "Wonderland Private Server.sln" /p:Configuration=Release /p:Platform=AnyCPU /p:WarningLevel=4
```

### Method C: Using the Build.proj Script

```powershell
# Custom MSBuild project included
msbuild Build.proj /p:BuildConfiguration=Release
```

**Build Output:**
- **Debug**: `bin\Debug\Wonderland Private Server.exe`
- **Release**: `bin\Release\Wonderland Private Server.exe`

---

## Project Structure

```
Wonderland-Develop/
├── Wonderland Private Server.sln          # Main solution file
├── Wonderland Private Server.csproj       # Main server project
├── Build.proj                             # Custom build configuration
├── packages.config                        # NuGet dependencies
├── Src/
│   ├── Program.cs                         # Entry point
│   ├── Gui/                               # Windows Forms UI
│   ├── Network/                           # Client/Server networking
│   ├── Server/                            # Game server logic
│   │   ├── LoginServer.cs
│   │   ├── WorldServer.cs
│   │   ├── RegistrationServer.cs
│   │   ├── Plugin/
│   │   └── Config/
│   └── ...
├── wlo.pserver.core/                      # Core game library
├── wlo.pserver.maps/                      # Map plugins
├── wlo.pserver.Bot/                       # Bot implementation
├── DLLS/                                  # Pre-built dependencies
├── Data/                                  # Local SQLite database template
├── bin/                                   # Build output
└── packages/                              # NuGet packages
```

---

## Dependencies

### NuGet Packages (Restored Automatically)
- **EntityFramework** 6.0.0 - ORM for database access
- **log4net** 2.0.3 - Logging framework
- **MySql.Data** 6.9.7 - MySQL connector (optional)
- **System.Data.SQLite** 1.0.97.0 - Local SQLite support
- **System.Data.SQLite.EF6** 1.0.97.0 - EF6 SQLite provider
- **Octokit** 0.4.1 - GitHub API integration

### Custom DLLs (Pre-built in `DLLS/` folder)
- GupdtSrv.dll
- PhoenixData.dll
- PhoenixCore.dll
- RCLibrary.dll
- Wlo.Core.dll

---

## Configuration

### Database Setup

**Default**: Uses local SQLite database at:
```
%APPDATA%\PServer\LocalServer.sqlite
```

The template database is copied from: `Data\ServerDataBase.db.temp`

**To use MySQL instead**:
- Modify database configuration in the server settings
- Ensure MySql.Data 6.9.7 is available

### Server Registration Page

When using the default SQLite database:
- **URL**: `http://127.0.0.1:8090/`
- **Location**: Local machine only
- **Web files**: `Web/register.html`, `Web/register.css`, `Web/register.js`

---

## Running the Server

### From Visual Studio

1. Open the solution in Visual Studio
2. Set "Wonderland Private Server" as startup project (if not already)
3. Press **F5** (Debug) or **Ctrl+F5** (Release)
4. The GUI window will launch with game server controls

### From Command Line

```powershell
cd "C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop"

# Run the built executable
.\bin\Debug\"Wonderland Private Server.exe"

# Or for Release
.\bin\Release\"Wonderland Private Server.exe"
```

### As a Windows Service (Future Implementation)

The current WinForms GUI runs as a console/GUI application. To run as a service:
- Create a Windows Service wrapper around the core server logic
- Use the `wlo.pserver.core` library as the service base

---

## Testing

### Manual Testing

1. **Start the server** - The GUI will show server status
2. **Launch the game client** - Connect to `localhost` or the server's IP
3. **Create an account** - Use the web registration page at `http://127.0.0.1:8090/`
4. **Test gameplay** - Verify logins, character creation, and world connectivity

### Known Limitations (From README)

- Map plugins are incomplete (development snapshot)
- Login and world functionality are partial
- This is **not yet a fully playable game**

### Map Loading

At startup, the server preloads map records from `gamedata\eve.Emg`. Plugin-defined maps take precedence; other EVE records use the generic map implementation and use names from `Data\SceneData.Dat` when available. This loads map records and supported warp entries, but does not provide missing NPC, item, or other gameplay content.

---

## Deployment Options

### Option 1: Local Deployment (Development)

Simply run the executable from `bin\Release\`:
```powershell
.\bin\Release\"Wonderland Private Server.exe"
```

Database: Local SQLite at `%APPDATA%\PServer\LocalServer.sqlite`

### Option 2: Network Deployment

1. **Copy the Release Build**
   ```powershell
   robocopy bin\Release\ \\<server-ip>\shared\ /E /COPY:DAT
   ```

2. **Install Dependencies**
   - .NET Framework 4.5+ on the target machine
   - Ensure SQLite interop DLLs are available (`x86\SQLite.Interop.dll`, `x64\SQLite.Interop.dll`)

3. **Run the Server**
   ```
   \\<server-ip>\shared\Wonderland Private Server.exe
   ```

### Option 3: Cloud Deployment (Advanced)

For Azure or similar:
1. Refactor WinForms GUI to a console/service application
2. Deploy to a Windows VM or App Service
3. Use Azure SQL Database or managed MySQL instead of SQLite
4. Implement proper logging and monitoring

---

## Troubleshooting

### Build Errors

| Error | Solution |
|-------|----------|
| "MSBuild not found" | Install Visual Studio Build Tools or Visual Studio 2015+ |
| "NuGet packages missing" | Run `nuget restore "Wonderland Private Server.sln"` |
| ".NET Framework 4.5 not found" | Install .NET Framework 4.5 Developer Pack |
| "SQLite.Interop.dll not found" | Copy `x86\SQLite.Interop.dll` to build output directory |

### Runtime Errors

| Error | Solution |
|-------|----------|
| "Cannot connect to database" | Check `%APPDATA%\PServer\` permissions; verify SQLite is installed |
| "Port 8090 already in use" | Change server port in configuration settings |
| "Registration page not accessible" | Ensure firewall allows local HTTP on port 8090 |

---

## Next Steps

1. **Install Prerequisites** - Get Visual Studio 2015+ and .NET Framework 4.5
2. **Build the Solution** - Compile using MSBuild or Visual Studio
3. **Run Locally** - Test on your development machine
4. **Configure Settings** - Customize server port, database, and game settings
5. **Deploy** - Move to production server following Option 2 or 3 above

---

## Additional Resources

- [Visual Studio Download](https://visualstudio.microsoft.com/downloads/)
- [.NET Framework 4.5 Developer Pack](https://www.microsoft.com/en-us/download/details.aspx?id=42637)
- [Entity Framework 6 Documentation](https://learn.microsoft.com/en-us/ef/ef6/)
- [SQLite.NET Documentation](https://www.sqlite.org/index.html)

---

## Support

For issues with:
- **Build process**: Check compiler output and NuGet restore logs
- **Game logic**: Review code in `Src/Server/` and `wlo.pserver.core/`
- **Database schema**: See database configuration in `Src/Server/Config/DB/`

