# Wonderland Private Server - Complete Build & Deployment System

Welcome! I've created a comprehensive build and deployment automation system for your Wonderland Private Server project.

## 🎯 Start Here

**For a quick overview**: Read [IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md) *(5 min read)*

**For step-by-step instructions**: Follow the Quick Start section below *(5 min to execute)*

**For detailed documentation**: See individual documentation files listed below

## 🚀 Quick Start (Copy & Paste)

Open PowerShell in your project directory and run these three commands:

```powershell
# 1. Validate your system has all prerequisites
.\setup.ps1

# 2. Build the server
.\build.ps1 -Configuration Release

# 3. Create deployment package
.\deploy.ps1 -Configuration Release -CreateZip
```

That's it! You'll have a working server built and packaged for deployment.

## 📚 Documentation Files

### Essential Reading

| File | Purpose | Read Time |
|------|---------|-----------|
| **[IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md)** | Quick overview, project structure, next steps | 5 min |
| **[BUILD_AND_DEPLOY.md](./BUILD_AND_DEPLOY.md)** | Complete build guide with all details | 15 min |
| **[SCRIPTS_README.md](./SCRIPTS_README.md)** | Detailed guide to each automation script | 10 min |

### Automation Scripts

| Script | Purpose | When to Use |
|--------|---------|------------|
| **[setup.ps1](./setup.ps1)** | Validate environment & prerequisites | Before first build |
| **[build.ps1](./build.ps1)** | Compile project from source | Every time you change code |
| **[deploy.ps1](./deploy.ps1)** | Create distribution package | Before deployment |

## 📋 What's Included

### 🛠️ Build Automation
- **setup.ps1** - Validates .NET Framework, Visual Studio, NuGet, and project files
- **build.ps1** - Automated compilation with automatic dependency restoration
- **deploy.ps1** - Package creation with launchers and documentation

### 📖 Documentation
- **BUILD_AND_DEPLOY.md** - Comprehensive guide (9 KB)
- **SCRIPTS_README.md** - Script documentation with examples (12 KB)
- **IMPLEMENTATION_SUMMARY.md** - Quick reference (9 KB)
- **README.md** - Original project documentation

### 🎮 Game Server Features
- Windows Forms GUI for server management
- SQLite database (default) or MySQL support
- Web-based account registration at `http://127.0.0.1:8090/`
- Multi-component architecture (core, maps, bots)
- Modular plugin system

## 🎯 Common Tasks

### Build for Development
```powershell
.\build.ps1 -Configuration Debug
```

### Build for Production
```powershell
.\build.ps1 -Configuration Release
```

### Create Deployment ZIP
```powershell
.\deploy.ps1 -Configuration Release -CreateZip
```

### Test the Build
```powershell
.\bin\Release\Wonderland Private Server.exe
```

### Deploy to Different Location
```powershell
.\deploy.ps1 -Configuration Release -OutputPath "C:\ServerBuilds\Prod"
```

### Clean Build (Troubleshooting)
```powershell
.\build.ps1 -Configuration Release -Clean
```

## 📦 Deployment Package

When you run `deploy.ps1`, you get a complete, ready-to-run package:

```
Deploy/
├── Wonderland Private Server.exe    ← Main executable
├── *.dll                            ← All dependencies
├── App.config                       ← Configuration
├── StartServer.bat                  ← Launch script (Windows)
├── StartServer.ps1                  ← Launch script (PowerShell)
├── Data/ServerDataBase.db.temp      ← Database template
├── Web/                             ← Registration interface
├── DEPLOYMENT_INFO.txt              ← Quick start guide
└── [documentation files]
```

Simply copy this folder to your server and run `StartServer.bat`.

## 🔧 System Requirements

**Development Machine:**
- Windows 7 SP1 or later
- Visual Studio 2012+ or Build Tools 2013+
- .NET Framework 4.5 or higher

**Production Server:**
- Windows Server 2012 R2 or later
- .NET Framework 4.5 or higher
- 512 MB RAM minimum (1 GB recommended)

## 📊 Build Configurations

| Config | Size | Speed | Use For |
|--------|------|-------|---------|
| **Debug** | ~200 MB | Slower | Development |
| **Release** | ~50 MB | Faster | Production |

## 🆘 Troubleshooting

### "MSBuild not found"
→ Install Visual Studio 2015+ from https://visualstudio.microsoft.com/downloads/

### ".NET Framework 4.5 not found"
→ Install from https://www.microsoft.com/download/details.aspx?id=42637

### "NuGet packages missing"
→ Run: `nuget restore "Wonderland Private Server.sln"`

### Build fails
→ Try: `.\build.ps1 -Configuration Release -Clean`

For more help, see the **Troubleshooting** section in [BUILD_AND_DEPLOY.md](./BUILD_AND_DEPLOY.md).

## 📖 Next Steps

1. **Understand Your System**
   - Read [IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md)

2. **Validate Environment**
   ```powershell
   .\setup.ps1
   ```
   This will tell you if you need to install any prerequisites.

3. **Build the Server**
   ```powershell
   .\build.ps1 -Configuration Release
   ```

4. **Test Locally**
   ```powershell
   .\bin\Release\Wonderland Private Server.exe
   ```

5. **Create Deployment Package**
   ```powershell
   .\deploy.ps1 -Configuration Release -CreateZip
   ```

6. **Deploy to Server**
   - Copy the `Deploy` folder (or `Deploy.zip`) to your server
   - Extract if needed
   - Run `StartServer.bat`

## 🎓 Learning Resources

### About the Project
- See [README.md](./README.md) for project overview
- Review `Src/` folder for source code structure
- Check `wlo.pserver.core/` for game logic

### About Building
- [BUILD_AND_DEPLOY.md](./BUILD_AND_DEPLOY.md) - Complete build guide
- [SCRIPTS_README.md](./SCRIPTS_README.md) - Script documentation

### About Deployment
- [IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md) - Deployment overview
- `Deploy/DEPLOYMENT_INFO.txt` - Generated deployment documentation

## 💡 Pro Tips

1. **First build is slow** - NuGet packages download the first time (5-10 min)
2. **Subsequent builds are fast** - Only changed files recompile (30 sec - 2 min)
3. **Use Release for production** - Better performance, smaller size
4. **Keep deployments fresh** - Run `deploy.ps1` each time before deploying
5. **Test locally first** - Always run on your development machine first

## 🔐 Security

- Registration page is **local-only** by default (127.0.0.1)
- For public access, use a reverse proxy with HTTPS
- Passwords stored as PBKDF2 hashes
- Enable proper firewall rules for production

## 📞 Support

All documentation is self-contained in these files:

- **General questions** → [IMPLEMENTATION_SUMMARY.md](./IMPLEMENTATION_SUMMARY.md)
- **Build issues** → [BUILD_AND_DEPLOY.md](./BUILD_AND_DEPLOY.md)
- **Script usage** → [SCRIPTS_README.md](./SCRIPTS_README.md)
- **Project details** → [README.md](./README.md)

## ✅ Success Indicators

You'll know everything is working when:

- ✅ `setup.ps1` completes with "All Critical Checks Passed"
- ✅ `build.ps1` shows "Build completed successfully!"
- ✅ Server executable runs and shows GUI window
- ✅ Registration page accessible at `http://127.0.0.1:8090/`
- ✅ `deploy.ps1` creates a complete `Deploy/` folder

---

## 🚀 Ready? Let's Go!

**Start here:**
```powershell
.\setup.ps1
```

This single command will tell you exactly what you need to do next.

Good luck! 🎮

---

*Last updated: 2025*  
*Wonderland Private Server - Complete Build & Deployment System*  
*Automated scripts created by Copilot*
