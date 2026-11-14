# Wonderland Private Server - Build & Deployment Workflow

## Complete System Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    WONDERLAND PRIVATE SERVER                            │
│                   Complete Build & Deployment System                   │
└─────────────────────────────────────────────────────────────────────────┘

                           DOCUMENTATION
                                 │
                    ┌────────────┼────────────┐
                    │            │            │
                    ▼            ▼            ▼
                INDEX.md    IMPLEMENTATION  BUILD_AND
                           SUMMARY.md       DEPLOY.md
                    │            │            │
                    └────────────┼────────────┘
                                 │
                        SCRIPTS_README.md


                      AUTOMATION SCRIPTS
                                 │
            ┌────────────────────┼────────────────────┐
            │                    │                    │
            ▼                    ▼                    ▼
        setup.ps1           build.ps1            deploy.ps1
            │                    │                    │
            │                    │                    │
        Validate          Restore NuGet        Package for
        Environment       Compile Code         Distribution
            │                    │                    │
            └────────────────────┴────────────────────┘
                                 │
                         BUILD OUTPUT
                                 │
                    ┌────────────┴────────────┐
                    │                         │
                    ▼                         ▼
              bin\Debug\               bin\Release\
              (200 MB)                 (50 MB)
                    │                         │
                    │                         │
                    └────────────┬────────────┘
                                 │
                          Wonderland Private
                          Server.exe + DLLs
                                 │
                    ┌────────────┴────────────┐
                    │                         │
                    ▼                         ▼
                 Deploy/              Deploy.zip
                 (Folder)             (Archive)
                    │                         │
                    └────────────┬────────────┘
                                 │
                      Ready for Distribution


═══════════════════════════════════════════════════════════════════════════
                            BUILD WORKFLOW
═══════════════════════════════════════════════════════════════════════════

Step 1: VALIDATE ENVIRONMENT
┌─────────────────────────────────────────────────────────────────────────┐
│ Command: .\setup.ps1                                                    │
│                                                                         │
│ Checks:                                                                 │
│  ✓ .NET Framework 4.5+                                                 │
│  ✓ Visual Studio 2015+ or Build Tools                                 │
│  ✓ NuGet package manager                                               │
│  ✓ Project files and structure                                         │
│  ✓ Database templates                                                  │
│  ✓ Web resources                                                       │
│                                                                         │
│ Output: ✓ All Critical Checks Passed                                   │
└─────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
Step 2: BUILD PROJECT
┌─────────────────────────────────────────────────────────────────────────┐
│ Command: .\build.ps1 -Configuration Release                            │
│                                                                         │
│ Actions:                                                                │
│  1. Find MSBuild.exe                                                    │
│  2. Restore NuGet packages from packages.config                         │
│  3. Compile Wonderland Private Server (main project)                   │
│  4. Compile wlo.pserver.core (library)                                 │
│  5. Copy all DLL dependencies                                          │
│  6. Copy database template                                             │
│                                                                         │
│ Output: ✓ Build completed successfully!                                │
│ Location: bin\Release\Wonderland Private Server.exe                    │
└─────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
Step 3: TEST LOCALLY (Optional)
┌─────────────────────────────────────────────────────────────────────────┐
│ Command: .\bin\Release\Wonderland Private Server.exe                   │
│                                                                         │
│ Verify:                                                                 │
│  • Server GUI window appears                                            │
│  • No errors in event log                                              │
│  • Registration page loads: http://127.0.0.1:8090/                     │
│  • Database creates at: %APPDATA%\PServer\LocalServer.sqlite           │
└─────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
Step 4: CREATE DEPLOYMENT PACKAGE
┌─────────────────────────────────────────────────────────────────────────┐
│ Command: .\deploy.ps1 -Configuration Release -CreateZip               │
│                                                                         │
│ Actions:                                                                │
│  1. Copy build output to Deploy\ folder                                │
│  2. Copy configuration files                                            │
│  3. Create StartServer.bat launcher                                     │
│  4. Create StartServer.ps1 launcher                                     │
│  5. Copy database template                                              │
│  6. Copy web registration interface                                     │
│  7. Copy documentation                                                  │
│  8. Create ZIP archive (optional)                                       │
│  9. Generate DEPLOYMENT_INFO.txt                                        │
│                                                                         │
│ Output: ✓ Deployment Package Created!                                  │
│ Location: Deploy\ folder (~50-100 MB) or Deploy.zip                    │
└─────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
Step 5: DEPLOY TO PRODUCTION
┌─────────────────────────────────────────────────────────────────────────┐
│ Actions:                                                                │
│  1. Copy Deploy\ folder to target server                                │
│  2. Extract if using Deploy.zip                                         │
│  3. Run StartServer.bat (or StartServer.ps1)                            │
│  4. Access registration: http://server-ip:8090/                        │
│                                                                         │
│ That's it! Server is now running.                                       │
└─────────────────────────────────────────────────────────────────────────┘


═══════════════════════════════════════════════════════════════════════════
                         DEPLOYMENT PACKAGE
═══════════════════════════════════════════════════════════════════════════

Deploy/
│
├── Wonderland Private Server.exe          ← Main executable
├── *.dll                                  ← All dependencies
│   ├── EntityFramework.dll
│   ├── log4net.dll
│   ├── MySql.Data.dll
│   ├── System.Data.SQLite.dll
│   ├── System.Data.SQLite.EF6.dll
│   ├── PhoenixData.dll
│   ├── RCLibrary.dll
│   ├── Wlo.Core.dll
│   └── ...
│
├── *.config                               ← Configuration
│   └── App.config
│
├── StartServer.bat                        ← Windows launcher
├── StartServer.ps1                        ← PowerShell launcher
│
├── Data/                                  ← Database
│   └── ServerDataBase.db.temp             (Template, creates on first run)
│
├── Web/                                   ← Registration interface
│   ├── register.html
│   ├── register.css
│   └── register.js
│
├── DEPLOYMENT_INFO.txt                    ← Quick start guide
├── README.md                              ← Project documentation
├── BUILD_AND_DEPLOY.md                    ← Build guide
├── LICENSE                                ← License files
└── LICENSE.txt


═══════════════════════════════════════════════════════════════════════════
                      CONFIGURATION OPTIONS
═══════════════════════════════════════════════════════════════════════════

After deployment, customize App.config for:

├── Database Connection
│   ├── SQLite (default)
│   │   └── %APPDATA%\PServer\LocalServer.sqlite
│   └── MySQL
│       └── server=localhost;database=wonderland;...
│
├── Server Settings
│   ├── Game server port (default: 2106)
│   ├── Registration port (default: 8090)
│   ├── Login server host/port
│   └── World server host/port
│
├── Logging
│   ├── Log level (DEBUG, INFO, WARN, ERROR)
│   ├── Log file location
│   └── Rolling file policy
│
└── Plugin Directories
    ├── Maps plugin path
    ├── Bot plugin path
    └── Custom plugin paths


═══════════════════════════════════════════════════════════════════════════
                      FILE ORGANIZATION GUIDE
═══════════════════════════════════════════════════════════════════════════

Your Project Directory:
┌────────────────────────────────────────────────────────────────┐
│ Wonderland-Develop/                                            │
│                                                                │
│ ⬜ Source Code                                                 │
│   └─ Src/                    (Source code)                     │
│   └─ wlo.pserver.core/       (Core library)                    │
│   └─ wlo.pserver.maps/       (Map plugins)                     │
│   └─ wlo.pserver.Bot/        (Bot system)                      │
│                                                                │
│ ⬜ Configuration                                               │
│   └─ Wonderland Private Server.sln    (Solution)               │
│   └─ Wonderland Private Server.csproj (Project)                │
│   └─ App.config                       (Configuration)          │
│   └─ packages.config                  (Dependencies)           │
│                                                                │
│ ⬜ Build Output                                                │
│   └─ bin/Debug/     (Debug build - 200 MB)                    │
│   └─ bin/Release/   (Release build - 50 MB)                   │
│   └─ obj/           (Temporary build files)                    │
│                                                                │
│ ⬜ Dependencies                                                │
│   └─ packages/      (NuGet packages)                           │
│   └─ DLLS/          (Custom DLLs)                              │
│                                                                │
│ ⬜ Data                                                        │
│   └─ Data/          (Database templates)                       │
│   └─ Web/           (Registration interface)                   │
│                                                                │
│ ⬜ Documentation & Scripts (NEW)                               │
│   └─ INDEX.md                (Navigation hub)                  │
│   └─ IMPLEMENTATION_SUMMARY.md (Quick start)                   │
│   └─ BUILD_AND_DEPLOY.md     (Complete guide)                 │
│   └─ SCRIPTS_README.md        (Script docs)                    │
│   └─ setup.ps1               (Validation)                      │
│   └─ build.ps1               (Build)                           │
│   └─ deploy.ps1              (Deploy)                          │
│   └─ Deploy/                 (Package output)                  │
│                                                                │
└────────────────────────────────────────────────────────────────┘


═══════════════════════════════════════════════════════════════════════════
                        COMMAND CHEAT SHEET
═══════════════════════════════════════════════════════════════════════════

SETUP & VALIDATION
  .\setup.ps1                          Validate all prerequisites
  .\setup.ps1 -CheckOnly               Check without fixes

BUILD PROJECT
  .\build.ps1                          Build Release (default)
  .\build.ps1 -Configuration Debug     Build Debug
  .\build.ps1 -Configuration Release -Clean  Clean rebuild
  .\build.ps1 -NoBuild                 Restore packages only

TEST
  .\bin\Release\Wonderland Private Server.exe   Run server

DEPLOYMENT
  .\deploy.ps1                         Create folder package
  .\deploy.ps1 -CreateZip              Create ZIP archive
  .\deploy.ps1 -IncludeSource           Include source code
  .\deploy.ps1 -OutputPath "C:\Builds"  Custom location

TROUBLESHOOTING
  nuget restore "Wonderland Private Server.sln"  Manual restore
  .\build.ps1 -Configuration Release -Clean      Full rebuild


═══════════════════════════════════════════════════════════════════════════
                          SUCCESS CRITERIA
═══════════════════════════════════════════════════════════════════════════

✅ setup.ps1 output shows:
   "═══════════════════════════════════════════════════════════════"
   "  ✓ All Critical Checks Passed!"

✅ build.ps1 output shows:
   "✓ Build completed successfully!"
   "  Executable: Wonderland Private Server.exe"

✅ Server starts:
   Wonderland Online GUI window appears
   No error messages in console

✅ Registration page loads:
   Browser shows http://127.0.0.1:8090/ with form

✅ Database created:
   File appears at %APPDATA%\PServer\LocalServer.sqlite

✅ Deployment package ready:
   Deploy folder contains all files
   Deploy.zip created (if requested)


═══════════════════════════════════════════════════════════════════════════
                      ESTIMATED TIME BREAKDOWN
═══════════════════════════════════════════════════════════════════════════

Activity                              Time        Notes
────────────────────────────────────────────────────────────────
Reading INDEX.md                      3 min       Quick overview
Reading IMPLEMENTATION_SUMMARY.md     5 min       Full picture
Running setup.ps1                     1 min       Validation
First build.ps1 (with NuGet restore)  10-15 min   One-time, slow
Second build.ps1 (incremental)        30-120 sec  Fast
Testing server locally                5 min       Verify it runs
Running deploy.ps1                    3 min       Package creation
────────────────────────────────────────────────────────────────
TOTAL: First time = 30-40 minutes
       Subsequent = 5-10 minutes


═══════════════════════════════════════════════════════════════════════════
                        DOCUMENT ROADMAP
═══════════════════════════════════════════════════════════════════════════

                            START HERE
                                 │
                            INDEX.md
                           (This file)
                                 │
                 ┌────────────────┴────────────────┐
                 │                                 │
          Quick Overview                    Detailed Guides
                 │                                 │
        IMPLEMENTATION_SUMMARY.md          BUILD_AND_DEPLOY.md
        (5-10 min read)                     (Comprehensive)
                 │                                 │
                 │                          SCRIPTS_README.md
                 │                          (Script details)
                 │                                 │
                 └────────────────┬────────────────┘
                                  │
                         Ready to Execute
                                  │
                      .\setup.ps1 ──────┐
                                        │
                      .\build.ps1 ──────┤
                                        │
                      .\deploy.ps1 ─────┘
                                  │
                         Deployment Ready!
```

---

## Getting Started

1. **Read**: [INDEX.md](./INDEX.md)
2. **Execute**: `.\setup.ps1`
3. **Build**: `.\build.ps1 -Configuration Release`
4. **Package**: `.\deploy.ps1 -Configuration Release -CreateZip`

That's it! You now have a production-ready server package.
