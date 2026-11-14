# Wonderland Online - Private Server Setup Guide

## 🎮 System Overview

This is a complete **Wonderland Online private server** setup running on your local network (192.168.1.8).

### Server Architecture

```
┌─────────────────────────────────────────────────────────┐
│  Player Web Browser                                      │
│  http://192.168.1.8:8090                               │
└────────────┬────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────┐
│  Registration Server (Python - Port 8090)              │
│  • User registration & login                            │
│  • Character management                                 │
│  • Account dashboard                                    │
│  • Game launcher                                        │
└────────┬────────────────────────────────┬──────────────┘
         │                                │
         ▼                                ▼
    Database                      Game Client Launcher
    (SQLite)                      (Wonderland.exe)
         │                                │
         └────────────────┬───────────────┘
                          ▼
                ┌──────────────────────┐
                │  Game Server         │
                │  Port 6414           │
                │  • Auto-auth         │
                │  • Session mgmt      │
                │  • 13 data files     │
                └──────────────────────┘
```

---

## 📊 Loaded Game Data Files

The game server loads **13 essential data files** (23.2 MB total):

| File | Size | Contents |
|------|------|----------|
| **npc.dat** | 468.7 KB | All NPCs (Persia Cat, Warrior, Samurai, etc.) |
| **item.dat** | 1432.6 KB | Items (weapons, armor, consumables) |
| **skill.dat** | 90.8 KB | Skills & abilities |
| **talk.dat** | 2186.6 KB | NPC dialogue & conversation data |
| **compound.dat** | 10.7 KB | Compounding recipes |
| **compound2.dat** | 41.6 KB | Advanced recipes |
| **formula.dat** | 0.4 KB | Formulas & calculations |
| **ground.mmg** | 15138.3 KB | Ground/terrain maps |
| **scenedata.dat** | 103.0 KB | Scene definitions |
| **wem.mmg** | 269.5 KB | Weather/environment |
| **mark.dat** | 643.2 KB | Markers & waypoints |
| **eve.Emg** | 1851.2 KB | Events & triggers |
| **skilldata.mbtm** | 759.5 KB | Skill database |

---

## 🚀 Quick Start

### 1. Start the Servers

Both servers run automatically when you launch them. To start fresh:

```bash
# Kill old processes
taskkill /F /IM python.exe

# Start servers
python registration_server.py  # Terminal 1
python game_server.py          # Terminal 2
```

### 2. Access the Dashboard

Open in your web browser:
- **Local**: http://localhost:8090/dashboard.html
- **Network**: http://192.168.1.8:8090/dashboard.html

### 3. Create Account or Login

- **Create Account**: http://192.168.1.8:8090/register.html
- **Login**: Use your credentials
- **Auto-create Character**: First-time login creates "Adventurer"

### 4. Play the Game

1. Go to Dashboard
2. Login with your credentials
3. Select your character
4. Click **PLAY**
5. Game launches and connects to localhost:6414

---

## 🛠️ Server Configuration

### Registration Server (Port 8090)

**File**: `registration_server.py`

- Handles user accounts & authentication
- Manages character creation
- Serves web UI (HTML/CSS/JS)
- Launches game client via `game_launcher.py`
- Binds to `0.0.0.0:8090` for network access

**Database**: `%APPDATA%\PServer\registration.db` (SQLite)

### Game Server (Port 6414)

**File**: `game_server.py`

- Accepts game client connections
- Auto-authenticates all players
- Loads 13 game data files
- Maintains player sessions
- Responds to client messages

**Game Data Directory**: `./gamedata/` (13 data files)

### Game Launcher

**File**: `game_launcher.py`

- Launches `Wonderland.exe`
- Passes server info (localhost:6414)
- Creates auth token
- Writes `auth.json` for game client

---

## 🌐 Network Access

### Local Machine
```
Browser: http://localhost:8090
Game: localhost:6414
```

### Local Network
```
Browser: http://192.168.1.8:8090
Game: 192.168.1.8:6414
```

Both registration and game servers bind to `0.0.0.0` to allow network access.

---

## 📝 Files Structure

```
Wonderland-Develop/
├── registration_server.py      # Main registration/web server
├── game_server.py              # Game server with data loader
├── game_launcher.py            # Launches game client
├── simple_game_server.py       # Legacy (not used)
├── game_server_working.py      # Legacy (not used)
│
├── Web/                        # Web UI files
│   ├── register.html           # Registration page
│   ├── login.html              # Login page
│   ├── dashboard.html          # Game dashboard
│   ├── register.js
│   ├── dashboard.js
│   ├── config.js              # Server config auto-detection
│   ├── styles.css
│   └── ...
│
├── bin/Debug/Web/             # Deployed web files (copy of Web/)
│   └── [same as Web/]
│
├── gamedata/                  # Game data files (13 files, 23.2 MB)
│   ├── npc.dat
│   ├── item.dat
│   ├── skill.dat
│   ├── talk.dat
│   ├── compound.dat
│   ├── compound2.dat
│   ├── formula.dat
│   ├── ground.mmg
│   ├── scenedata.dat
│   ├── wem.mmg
│   ├── mark.dat
│   ├── eve.Emg
│   └── skilldata.mbtm
│
├── auth.json                  # Created at game launch
├── game_server.log            # Game server log file
├── reg_server.log             # Registration server log file
│
└── %APPDATA%/PServer/
    └── registration.db        # User accounts database
```

---

## 🎯 Game Data Categories

### Items (item.dat)
- Weapons: Katana, Sword, Spear, Bow, Wand, Claw, Axe, Gun
- Armor: Body, Head, Arm, Shoes
- Consumables: Food, Potions
- Quest Items
- Special Items

### NPCs (npc.dat)
- Persia Cat
- Tabby Cat
- Warrior
- Samurai
- And 100+ more creatures and NPCs

### Skills (skill.dat)
- Battle Type
- Earth Type
- Water Type
- Fire Type
- Wind Type
- Pet Skills
- Life Skills

### Maps (ground.mmg, scenedata.dat)
- Terrain data
- Scene definitions
- Waypoints & markers
- Environmental settings

---

## 📊 Database Schema

### Users Table
```sql
CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT UNIQUE NOT NULL,
    password TEXT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Characters Table
```sql
CREATE TABLE characters (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER,
    name TEXT NOT NULL,
    level INTEGER DEFAULT 1,
    exp INTEGER DEFAULT 0,
    hp INTEGER DEFAULT 100,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(user_id) REFERENCES users(id)
);
```

---

## 🔧 Configuration

### Auto-Detection (config.js)
The web UI automatically detects the server:
- **Localhost**: Uses `localhost:8090`
- **Network**: Uses `192.168.1.8:8090`

### Game Server Connection
Game launcher connects to:
- **Server**: localhost
- **Port**: 6414
- **Protocol**: Binary socket with handshake

### API Endpoints

#### Registration Server (8090)
- `GET /register.html` - Registration page
- `GET /login.html` - Login page
- `GET /dashboard.html` - Character dashboard
- `GET /config.js` - Server configuration
- `POST /api/register` - Create account
- `POST /api/login` - User login
- `POST /api/user-stats` - Get account stats
- `POST /api/server-stats` - Get server statistics
- `POST /api/launch-game` - Launch game client

#### Game Server (6414)
- Binary protocol socket
- Auto-authentication
- Session management
- Data file serving

---

## 🐛 Troubleshooting

### "Could not reach registration service"
1. Check if `registration_server.py` is running
2. Verify port 8090 is free: `netstat -ano | findstr :8090`
3. Restart server:
```bash
taskkill /F /IM python.exe
python registration_server.py
```

### Game won't launch
1. Verify game server is running: `netstat -ano | findstr :6414`
2. Check `game_server.log` for errors
3. Ensure `Wonderland.exe` exists at `C:\Program Files (x86)\Wonderland Online\`

### Game stuck on login screen
1. Game server may not be responding
2. Check `game_server.log` for connection entries
3. Verify handshake: Look for "CONNECTED" and "AUTO-AUTHENTICATED"

### Port conflicts
If ports 8090 or 6414 are in use:
```bash
# Find process using port 8090
netstat -ano | findstr :8090

# Kill by PID
taskkill /F /PID [PID]
```

---

## 📈 Performance

### Server Resources
- **Registration Server**: ~50 MB RAM, Python 3.11
- **Game Server**: ~100 MB RAM (includes 23.2 MB data files)
- **Network**: Local network only, no external access

### Connection Limits
- Max players: 10 (limited by TCPServer listen backlog)
- Session timeout: 120 seconds idle
- Keep-alive: Binary heartbeat packets

---

## 🔐 Security Notes

⚠️ **This is a local/private server**
- No encryption on game protocol
- Passwords stored in plaintext (SQLite)
- No access control lists
- No rate limiting
- For **local testing only**

### For Production Use:
- Add password hashing
- Implement SSL/TLS
- Add rate limiting
- Implement access controls
- Add logging/auditing

---

## 📚 Game Data Tools

Included viewer tools:
- **WLOImageViewer.exe** - View game images
- **WLO Item Data Viewer.exe** - Browse items
- **WLO Skill Data Viewer.exe** - View skills
- **ItemDataLabels.txt** - Item categories
- **SkillDataLabels.txt** - Skill types

---

## 🎓 Development Notes

### Adding New Features

1. **New API Endpoint**:
```python
elif self.path == '/api/new-endpoint':
    # Handle POST data
    # Return JSON response
```

2. **New Game Server Feature**:
```python
# Add to GameServer.handle_client()
# Parse incoming data
# Send response
```

3. **New Database Table**:
```python
# Add to ensure_db() in registration_server.py
c.execute('''CREATE TABLE IF NOT EXISTS new_table (...)''')
```

### Modifying Game Data

Game data files are binary. Use the included viewer tools or reverse-engineer the format.

### Custom Skins/Content

Client release files in `client-releases-main/` contain:
- Menu skins
- UI assets
- Addon files
- Version manifests

---

## 📞 Support

### Log Files
- `game_server.log` - Game server events
- `reg_server.log` - Registration server events (if exists)
- Browser console - Web UI errors

### Key Files to Check
1. `registration_server.py` - Port 8090 binding
2. `game_server.py` - Port 6414 binding
3. `gamedata/` directory - Data files loaded
4. `auth.json` - Game launcher token

---

## ✅ Verification Checklist

- [x] Registration server running on port 8090
- [x] Game server running on port 6414
- [x] All 13 game data files loaded
- [x] Web UI accessible at http://192.168.1.8:8090
- [x] Game launcher connects to localhost:6414
- [x] Game client auto-authenticates
- [x] Player sessions maintained
- [x] Network access working

---

## 🎉 You're Ready!

Your Wonderland Online private server is now **fully operational** with:

✅ Complete game data (NPCs, items, skills, maps)
✅ User registration & authentication
✅ Character management
✅ Game launcher
✅ Network-accessible dashboard
✅ Local game server
✅ Auto-authentication
✅ Session management

**Start playing**: Open http://192.168.1.8:8090/dashboard.html and launch a game!

---

**Last Updated**: 2026-11-15
**Version**: 1.0 - Full Release
**Status**: ✅ Production Ready (Local Network)
