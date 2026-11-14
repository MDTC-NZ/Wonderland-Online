# 🎮 Wonderland Online Server - Crash Diagnosis

## Current Status
The .NET game server (**Wonderland Private Server.exe**) is crashing on startup. Here's what we've discovered:

## Root Cause
The server is a **Windows Forms GUI application**, not a headless console server. It attempts to:
1. Load item data files (itemDat.wpdat)
2. Connect to the local SQLite database
3. Initialize various game systems
4. Display a GUI window

When running in a non-admin context or with GUI disabled, it crashes before showing any error messages.

## Requirements
✅ **Administrator Rights** - Must run as Administrator  
✅ **Database** - Located at: `%APPDATA%\PServer\LocalServer.sqlite`  
✅ **Data Files** - Located at: `bin\Debug\Data\`  
✅ **GUI Support** - Needs a display (cannot run headless)  
✅ **Port 6414** - Must be available

## Solutions

### Solution 1: Clean Restart (Recommended)
1. Right-click: `bin\Debug\START_SERVER_CLEAN.bat`
2. Select: "Run as Administrator"
3. This will:
   - Kill any old Python processes
   - Clear old port bindings
   - Start the server fresh

### Solution 2: Manual Admin Startup
1. Open Command Prompt
2. Right-click and select: "Run as Administrator"
3. Navigate to the bin\Debug folder:
   ```
   cd C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop\bin\Debug
   ```
4. Run the server:
   ```
   "Wonderland Private Server.exe"
   ```

### Solution 3: Create Scheduled Task
Create a Windows Task Scheduler task to run the server at startup with admin rights:
1. Open Task Scheduler
2. Create New Task
3. Set:
   - Trigger: At startup
   - Action: Run `Wonderland Private Server.exe`
   - Options: Run with highest privileges

## Troubleshooting

### Issue: "The requested operation requires elevation"
**Solution:** Right-click the batch file and select "Run as Administrator"

### Issue: "Port 6414 is already in use"
**Solution:** 
1. Kill Python processes:
   ```
   taskkill /IM python.exe /F
   ```
2. Wait 5 seconds
3. Start server again

### Issue: "Database file not found"
**Solution:**
1. Create directory if missing:
   ```
   mkdir %APPDATA%\PServer
   ```
2. Server will create database on first run

### Issue: "itemDat.wpdat not found"
**Solution:**
- Ensure `bin\Debug\Data\` folder has all required files
- Check that `itemDat.wpdat` (218 KB) is present

## Servers Configuration

| Server | Port | Type | Status |
|--------|------|------|--------|
| **Game Server** | 6414 | .NET GUI App | Requires Admin |
| **Registration** | 8090 | Python Script | Regular User |

## Next Steps

**Try this:**
```
1. Right-click: bin\Debug\START_SERVER_CLEAN.bat
2. Select: "Run as Administrator"
3. Watch the window - it should NOT crash
4. Open: http://192.168.1.8:8090/dashboard.html
5. Click PLAY
```

---

**If it still crashes after these steps, we may need to:**
- Rebuild the .NET project from source
- Use the Python game server instead (fallback option)
- Debug with Visual Studio

Let me know what happens when you try the clean start!
