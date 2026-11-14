# 🎮 GAME CLIENT CONNECTION GUIDE

## ✅ Current Status

### Server Status
- **Game Server**: ✅ RUNNING (Wonderland Private Server.exe)
- **Port**: ✅ 6414 (listening)
- **Protocol**: ✅ WLO (0x44F4)
- **Data Files**: ✅ 13 files loaded
- **Registration Server**: ✅ RUNNING (Python, port 8090)

### Test User
```
Username:  testuser
Password:  test1234
Character: TestCharacter
```

## 🚀 How to Connect Game Client

### Step-by-Step Connection Process

1. **Open Dashboard**
   - URL: http://localhost:8090/dashboard.html
   - Browser: Chrome, Firefox, Edge, Safari

2. **Login to Account**
   - Username: `testuser`
   - Password: `test1234`
   - Click LOGIN button

3. **Select Character**
   - Character: "TestCharacter"
   - Click PLAY button

4. **Game Client Launches**
   - Wonderland.exe will start automatically
   - Should connect to: localhost:6414
   - Loading screen appears

5. **Enter Game World**
   - Character appears in game world
   - Controls: WASD to move, mouse to look around
   - See NPCs, items, and map

## 🔧 Troubleshooting

### Issue: Game Client Won't Connect

**Symptom**: Game launches but displays black screen or "Connecting..." message

**Solutions**:
1. Check if game server is running on port 6414
   ```bash
   netstat -ano | findstr 6414
   ```
   Should show: `TCP 0.0.0.0:6414 0.0.0.0:0 LISTENING`

2. Verify localhost resolves to 127.0.0.1
   ```bash
   ping localhost
   ```

3. Check Windows Firewall isn't blocking port 6414
   - Open Windows Defender Firewall
   - Go to "Allow an app through firewall"
   - Ensure port 6414 is open

4. Try different connection method:
   - Edit game_launcher.py
   - Change `server_domain` from "localhost" to "127.0.0.1"

### Issue: "Connection Refused" Error

**Symptom**: Game client fails to connect immediately

**Solutions**:
1. Start the game server:
   ```bash
   cd Wonderland-Develop\Wonderland-Develop\bin\Debug
   "Wonderland Private Server.exe"
   ```

2. Verify registration server is running:
   ```bash
   netstat -ano | findstr 8090
   ```

3. Check if multiple servers are listening on 6414:
   ```bash
   netstat -ano | findstr 6414
   ```
   If multiple processes, kill extras

### Issue: Game Client Crashes/Closes Immediately

**Symptom**: Game launches but closes within 1-2 seconds

**Possible Causes**:
1. Missing DirectX or graphics drivers
2. Invalid character data
3. Database connection error
4. Server sending corrupted data

**Solutions**:
1. Check character exists in database:
   ```bash
   python create_test_user.py
   ```

2. Check game_server.log for errors:
   ```
   tail -f game_server.log
   ```

3. Try creating new character:
   - Edit create_test_user.py
   - Change character name
   - Run: `python create_test_user.py`

4. Check Windows Event Viewer for crash details

### Issue: Can't Login to Dashboard

**Symptom**: "Invalid username or password"

**Solutions**:
1. Create test user:
   ```bash
   python create_test_user.py
   ```

2. Check registration database:
   ```bash
   python -c "import sqlite3; c = sqlite3.connect('%APPDATA%\\PServer\\registration.db'); print(c.execute('SELECT * FROM users').fetchall())"
   ```

3. Reset password:
   - Use create_test_user.py
   - It will create testuser with password test1234

## 📊 Server Architecture

### Game Server (Port 6414)
- **Type**: Wonderland Private Server.exe (.NET)
- **Data**: ServerDataBase.db (SQLite)
- **Data Files**:
  - npc.dat (NPCs and their properties)
  - item.dat (Items and equipment)
  - skill.dat (Skills and abilities)
  - talk.dat (NPC dialogues)
  - ground.mmg (Map terrain)
  - scenedata.dat (Map objects and structures)
  - And 7 more files

### Registration Server (Port 8090)
- **Type**: registration_server.py (Python)
- **Database**: %APPDATA%\PServer\registration.db (SQLite)
- **Functions**:
  - User registration
  - User login
  - Character management
  - Game launcher
  - Web dashboard

### Game Client
- **Type**: Wonderland.exe (Compiled binary)
- **Location**: C:\Program Files (x86)\Wonderland Online\
- **Launched by**: game_launcher.py
- **Connection**: localhost:6414

## 🎮 In-Game Controls

| Key | Action |
|-----|--------|
| W/A/S/D | Move character |
| Mouse | Look around |
| Left Click | Interact/Attack |
| Right Click | Context menu |
| Space | Jump |
| I | Inventory |
| C | Character stats |
| Q | Quest log |
| M | Map |
| Esc | Menu |

## 📁 File Structure

```
Wonderland-Develop/
├── bin/Debug/
│   ├── Wonderland Private Server.exe    (Game server)
│   ├── wlo.pserver.core.dll             (Core game logic)
│   ├── ServerDataBase.db                (Game database)
│   ├── Web/                             (Web files)
│   └── [28 DLL dependencies]
│
├── Data/                                 (Game data files)
│   ├── npc.dat
│   ├── item.dat
│   ├── skill.dat
│   └── [13 total files]
│
├── registration_server.py               (Login/register server)
├── game_launcher.py                     (Game launcher)
├── game_server.log                      (Server log)
└── Web/                                 (Dashboard HTML/CSS/JS)
```

## 🔐 Security Notes

### Database Locations
- **Game Database**: `bin\Debug\ServerDataBase.db`
- **Registration Database**: `%APPDATA%\PServer\registration.db`
- **Backups**: Keep copies of .db files for safety

### Default Credentials
- Username: testuser
- Password: test1234
- ⚠️ CHANGE THESE IN PRODUCTION!

## 📝 Logs and Debugging

### Game Server Log
**File**: `game_server.log`

**View Latest Entries**:
```bash
tail -f game_server.log
```

**Common Log Messages**:
- `✅ CONNECTED` - Client connected successfully
- `✅ AUTO-AUTHENTICATED` - Client authenticated
- `⌛ Timeout` - Connection idle for too long
- `⌚ DISCONNECTED` - Client disconnected
- `FATAL` - Fatal error occurred

### Registration Server Log
**Console Output**: Shows web request logs

**View HTTP Requests**:
- Open browser developer tools (F12)
- Go to Network tab
- Make requests in dashboard

## 🎯 Next Steps

### To Create More Characters
1. Edit `create_test_user.py`
2. Change username and character name
3. Run: `python create_test_user.py`
4. Login with new credentials

### To Customize Game
1. Modify quest/map source in `wlo.pserver.core\Game\`
2. Rebuild: `dotnet build "Wonderland Private Server.sln"`
3. New executable in `bin\Debug\`

### To Deploy on Network
1. Share your PC's IP address (e.g., 192.168.1.8)
2. Other players connect to: http://192.168.1.8:8090/dashboard.html
3. Game server will be on: 192.168.1.8:6414

## ✨ Summary

**Your game server is fully operational!**

✅ Game Server Running on port 6414
✅ Registration Server Running on port 8090
✅ Test User Ready (testuser / test1234)
✅ Database Configured
✅ All 13 Data Files Loaded

**Just open http://localhost:8090/dashboard.html and click PLAY!** 🎮

---

**For more help**, check:
- game_server.log - Server debug info
- Dashboard console - Game client messages
- Windows Event Viewer - System errors
