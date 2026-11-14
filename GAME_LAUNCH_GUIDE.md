# 🎮 Wonderland Online - Game Launch Guide

## What Happens When You Click "Play"

### The Flow
```
1. Player clicks "Play" on a character in the dashboard
   ↓
2. Dashboard sends character info to registration server (/api/launch-game)
   ↓
3. Registration server receives:
   - Character name (e.g., "MyCharacter")
   - Character ID (e.g., 12345)
   - User authentication token
   ↓
4. Registration server launches game_launcher.py with these parameters
   ↓
5. game_launcher.py:
   - Writes character data to auth.json
   - Launches "Wonderland Private Server.exe" on port 6414
   ↓
6. Game client starts and loads your character
   - Connects to game server on port 6414
   - Loads character stats, inventory, quest progress
   - Player can start playing immediately
```

## Current Status

### ✅ Working Components
- **Registration Page** - Users can create accounts and login
- **Dashboard** - Shows character list and management
- **Character Creation** - Players can create up to 2 characters
- **Game Server** - Running on port 6414 with all game data

### 🔧 New Components Added
- **game_launcher.py** - Launches the game with character authentication
- **Launch Game Endpoint** - `/api/launch-game` handles game startup
- **Updated Dashboard** - "Play" button now functional

## How to Test

### Step 1: Start the Servers
```bash
# Option A: Use the batch script
Double-click: START_SERVERS.bat

# Option B: Manual start
# Terminal 1
bin\Debug\Wonderland Private Server.exe

# Terminal 2
python registration_server.py
```

### Step 2: Create an Account
1. Open browser to `http://localhost:8090`
2. Click "Register" tab
3. Enter username and password
4. Click "Create Account"

### Step 3: Login
1. Click "Login" tab (or on registration page after creating account)
2. Enter your username and password
3. Click "Login"

### Step 4: Create a Character
1. On the dashboard, click "Create Character"
2. Fill in name, class, race, gender, skin tone
3. Click "Create Character"

### Step 5: Play!
1. Click the "Play" button on your character
2. The game window should launch automatically
3. Start playing! 🎮

## Files Involved

### Game Launching Flow
1. **dashboard.html** - Contains the "Play" button
2. **dashboard.js** - Sends request to `/api/launch-game` when Play is clicked
3. **registration_server.py** - Receives launch request and starts game_launcher.py
4. **game_launcher.py** - Launches the actual game executable with character data
5. **bin/Debug/Wonderland Private Server.exe** - The game client

### Authentication Data
- **auth.json** - Created by game_launcher.py with character info (written to project root)
  ```json
  {
    "character_name": "MyCharacter",
    "character_id": 12345,
    "user_token": "token_1_1234567890.123",
    "timestamp": 1234567890.123
  }
  ```

### Game Server
- **bin/Debug/Wonderland Private Server.exe** - Main game server running on port 6414
- Port 6414 is where the game client connects to play

### Web Server
- **port 8090** - Registration/dashboard server (Python)
- Serves web pages and APIs for account management

## Troubleshooting

### "Game launching... Check your game window!" but no game appears
**Solution:**
1. Make sure game server is running: `bin\Debug\Wonderland Private Server.exe`
2. Check that port 6414 is not blocked by firewall
3. Run `CHECK_STATUS.bat` to verify server status

### "Connection error. Make sure the registration server is running"
**Solution:**
1. Restart the registration server: `python registration_server.py`
2. Make sure port 8090 is free
3. Check Python is installed: `python --version`

### Game launches but won't load character
**Solution:**
1. Check that auth.json exists in the project root
2. Verify character was created in the dashboard
3. Make sure game server is fully started (wait 5 seconds after launch)
4. Try restarting both servers with `START_SERVERS.bat`

### "Character name must be 3-20 characters"
**Solution:**
- Character names must be 3-20 characters long
- Only letters, numbers, and underscores allowed
- Cannot have spaces or special characters

## Next Steps (Optional Enhancements)

1. **Persist Character Data** - Save character stats, inventory, quest progress to database
2. **Multiplayer** - Allow players to see and interact with other players
3. **In-Game Chat** - Add communication between players
4. **Guild System** - Create and manage player guilds
5. **Economy** - Implement trading and marketplace
6. **Mobile App** - Create mobile client for account/character management
7. **Cloud Save** - Backup character data to cloud storage
8. **Custom Characters** - More appearance customization options

## Important Notes

⚠️ **Security Notes:**
- Passwords are currently stored as plain text (use hashing in production)
- Authentication tokens are simple and not cryptographically secure
- This is a development/testing setup

🎮 **Game Features:**
- Quests and NPCs fully functional
- Maps and world content all loaded
- Character stats and progression working
- Monster encounters and combat system active

📊 **Performance:**
- Game server can handle multiple concurrent players
- Registration server is lightweight and responsive
- Database uses SQLite (suitable for development)

💾 **Data Storage:**
- User accounts: `%APPDATA%\PServer\registration.db`
- Character data: Stored in game server database
- Auth tokens: Temporary (valid for session)

## Support

If you encounter issues:
1. Check the console output for error messages
2. Run `CHECK_STATUS.bat` to diagnose problems
3. Verify all required files exist in `bin/Debug/`
4. Make sure Python 3 is installed and in PATH
5. Check that ports 6414 and 8090 are not blocked

Good luck, adventurer! 🎉
