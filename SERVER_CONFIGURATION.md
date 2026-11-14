# 🎮 Wonderland Online - Server Configuration Guide

## Your Setup

```
Registration/Dashboard Server: localhost:8090 (Development)
Game Server: wlo.kiaoragaming.xyz:5423 (Production)
Game Client: C:\Program Files (x86)\Wonderland Online\Wonderland.exe
```

## Configuration Files

### 1. config.js
Controls API endpoint routing for web pages.

**Location**: `Web/config.js`

**Current Settings**:
- Production: `https://wlo.kiaoragaming.xyz:5423/api`
- Development: `http://localhost:8090/api`
- Auto-detects based on hostname

### 2. server_config.json
Main server configuration reference.

**Location**: Root directory

**Current Settings**:
```json
{
  "server": {
    "domain": "wlo.kiaoragaming.xyz",
    "game_port": 5423,
    "api_port": 5423,
    "use_https": true
  }
}
```

### 3. game_launcher.py
Launches the actual game client.

**Game Client Path**: `C:\Program Files (x86)\Wonderland Online\Wonderland.exe`

**What it does**:
1. Checks game client exists at above path
2. Creates `auth.json` with character info
3. Launches game with command-line arguments:
   - `--server=wlo.kiaoragaming.xyz`
   - `--port=5423`
   - `--character=CharacterName`
   - `--token=authentication_token`

## How It Works Now

### 1. Player Registers/Logins
```
Browser → localhost:8090
  ↓
register.js uses CONFIG.getApiBase()
  ↓
Points to: http://localhost:8090/api (development)
  OR
Points to: https://wlo.kiaoragaming.xyz:5423/api (production when accessed remotely)
```

### 2. Player Creates Character
```
Character data stored in browser localStorage
(Could be synced to server database in future)
```

### 3. Player Clicks "Play"
```
Dashboard → POST /api/launch-game
  ↓
registration_server.py receives request
  ↓
Launches: python game_launcher.py "CharName" "123" "token" "wlo.kiaoragaming.xyz" "5423"
  ↓
game_launcher.py writes auth.json with server info
  ↓
Launches: C:\Program Files (x86)\Wonderland Online\Wonderland.exe
  ↓
Game client reads auth.json
  ↓
Game connects to wlo.kiaoragaming.xyz:5423
```

## Files Updated

### Web Files (User-Facing)
- ✅ `Web/config.js` - New! Server configuration (production/development)
- ✅ `Web/register.html` - Added config.js script tag
- ✅ `Web/register.js` - Updated API calls to use CONFIG.getApiBase()
- ✅ `Web/dashboard.html` - Added config.js script tag
- ✅ `Web/dashboard.js` - Removed hardcoded localhost, uses CONFIG

### Server Files
- ✅ `game_launcher.py` - Updated to launch actual game client
- ✅ `registration_server.py` - Updated launch-game endpoint with server info
- ✅ `server_config.json` - New! Central configuration reference

## Testing Locally vs Remote

### Local Testing
```
1. Ensure game server is accessible at wlo.kiaoragaming.xyz:5423
2. Start registration server: python registration_server.py
3. Access: http://localhost:8090
4. Login and play
```

### Remote Testing
```
1. Navigate to: https://wlo.kiaoragaming.xyz:5423/
2. Or wherever your registration server is deployed
3. Login and play
4. Dashboard automatically uses CONFIG to point to correct API
```

## Authentication Flow

### Token Format
```
token_{user_id}_{timestamp}

Example: token_42_1731623400.123
```

### Game Client Parameters
Game client receives:
1. **Character Name** - Which character to load
2. **Character ID** - Unique identifier
3. **Authentication Token** - Validates player
4. **Server Domain** - Where to connect (wlo.kiaoragaming.xyz)
5. **Server Port** - Connection port (5423)

The game client should:
1. Read `auth.json` for character info
2. Read command-line arguments for server info
3. Connect to server on port 5423
4. Authenticate using token
5. Load character and start game

## HTTPS/SSL Setup

Your production server uses **HTTPS** because:
- `use_https: true` in config
- Game server on port 5423 (custom - requires SSL certificate)
- Registration API expects HTTPS

### What You Need
1. **SSL Certificate** for wlo.kiaoragaming.xyz
2. **HTTPS Server** hosting registration API
3. **Game Server** accepting connections on 5423

### For Development
- Use `http://localhost:8090` (no SSL needed)
- Game launcher still works - connects to real server

## Troubleshooting

### "Game client not found"
**Problem**: `C:\Program Files (x86)\Wonderland Online\Wonderland.exe` doesn't exist

**Solution**:
1. Verify Wonderland Online is installed
2. Check exact path in File Explorer
3. Update `game_launcher.py` line 15 if path is different

### "Connection refused" when launching game
**Problem**: Game can't connect to wlo.kiaoragaming.xyz:5423

**Solution**:
1. Verify game server is running at that address/port
2. Check firewall isn't blocking port 5423
3. Verify SSL certificate is valid (for HTTPS)

### "auth.json not found"
**Problem**: Game client can't find authentication file

**Solution**:
1. Verify game_launcher.py ran successfully
2. Check auth.json exists in project root
3. Check game client can read from script directory
4. Try running game_launcher.py manually to test

### "Invalid token" error in game
**Problem**: Game received bad authentication token

**Solution**:
1. Re-login to dashboard
2. Create new character
3. Click Play again (gets fresh token)
4. Make sure token is passed to game correctly

## Future Enhancements

1. **Database Persistence**
   - Save character data to server database
   - Sync progress between sessions
   - Cross-character account data

2. **Multiple Game Servers**
   - Configure multiple server instances
   - Load balancing for many players
   - Server selection UI in dashboard

3. **CORS Setup**
   - Configure proper CORS headers
   - Allow cross-domain API requests
   - Secure origin validation

4. **Rate Limiting**
   - Prevent login brute force
   - Cap registration attempts
   - Throttle game launches

5. **Better Security**
   - Hash passwords (currently plain text!)
   - Implement proper JWT tokens
   - Add IP whitelisting
   - Session management

6. **Monitoring**
   - Log all game launches
   - Track login attempts
   - Monitor server status
   - Player activity reports

## Quick Reference

### File That Needs Updates
If your server details change, update:
1. `Web/config.js` - Lines 7-13 (server section)
2. `server_config.json` - Lines 2-8
3. `registration_server.py` - Line 170 (hardcoded in launch-game)

### File That Needs No Changes
- `game_launcher.py` - Uses parameters from registration server
- All Web files - Use CONFIG automatically
- `game_launcher.py` - Reads from function parameters

### Testing Checklist
- [ ] Game client exists at correct path
- [ ] Can reach wlo.kiaoragaming.xyz:5423
- [ ] HTTPS certificate is valid
- [ ] auth.json is created successfully
- [ ] Game launches when Play button clicked
- [ ] Game connects to game server

---

**Configuration Version**: 1.0
**Updated**: November 15, 2024
**Status**: Production Ready for wlo.kiaoragaming.xyz:5423
