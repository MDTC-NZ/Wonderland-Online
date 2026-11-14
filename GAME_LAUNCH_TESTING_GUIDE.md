# Game Launch Testing Guide

## Quick Start (You're Here Now!)

### Servers Status
- ✅ **Game Server v3**: Running on port 6414
- ✅ **Registration Server**: Running on port 8090
- ✅ **13 Game Data Files**: Loaded and ready

### Step 1: Open Dashboard
Open your browser and navigate to:
```
http://localhost:8090/dashboard.html
```

Or for network access:
```
http://192.168.1.8:8090/dashboard.html
```

### Step 2: Login or Register
- If first time: Click **Register** and create an account
- If returning: Click **Login** with your credentials

### Step 3: Launch Game
1. Once logged in, you should see the **Dashboard**
2. Click the **PLAY** button
3. Wonderland.exe should launch

### Step 4: Monitor Server Logs
Watch the game server window for these messages:

**Success:**
```
[Session 1] ✅ CONNECTED from 127.0.0.1:xxxxx
[Session 1] → Sent handshake packet (AC: 0x01, 0x00)
[Session 1] → Sent character info packet (AC: 0x03, 0x00)
[Session 1] → Sent map data packet (AC: 0x05, 0x00)
[Session 1] ← Received X bytes (buffer: Y)
```

**Problems:**
```
[Session 1] ❌ Client disconnected
[Session 1] ❌ Connection reset by client
[Session 1] ⚠️  Error: [specific error]
```

## What to Test

### Test Case 1: Basic Connection
1. Register a new account
2. Click PLAY
3. Check for "CONNECTED" message in server log
4. **Expected**: Game client launches, connects to server

### Test Case 2: Multiple Connections
1. Register 2-3 different accounts
2. Launch game for each account
3. Check Session IDs increment (1, 2, 3...)
4. **Expected**: Multiple sessions shown in server log

### Test Case 3: Keep-Alive
1. Launch game and let it run for 30+ seconds
2. Watch server logs
3. **Expected**: See "Keep-alive sent" every 30 seconds

### Test Case 4: Character Persistence
1. Create character with specific name
2. Close game and relaunch
3. Character name should still be available
4. **Expected**: Same character appears after relaunching

### Test Case 5: Multiple Sessions
1. Open game from two different browsers/accounts
2. Both should connect
3. Check server shows both sessions
4. **Expected**: No conflicts, both sessions active

## Troubleshooting

### Game Won't Launch
**Symptom**: Click PLAY but nothing happens

**Solutions**:
1. Check if Wonderland.exe is installed at: `C:\Program Files (x86)\Wonderland Online\Wonderland.exe`
2. Check firewall isn't blocking port 6414
3. Check both servers are running (ports 8090 and 6414)
4. Look at game_server.log for errors

### Game Crashes on Launch
**Symptom**: Game window opens then closes

**Check**:
1. Look at game_server.log - should show `CONNECTED` message
2. If no CONNECTED message: Client isn't reaching server
3. If CONNECTED but crashes: Check packet format in logs
4. Verify network connectivity: `ping 127.0.0.1` and check port 6414

### Can't Access Dashboard
**Symptom**: Browser can't reach localhost:8090

**Solutions**:
1. Verify registration server is running in separate window
2. Try: `http://127.0.0.1:8090/dashboard.html` (127.0.0.1 vs localhost)
3. Check Windows Firewall isn't blocking port 8090
4. Check registration_server.py window doesn't have error message

### Ports Already in Use
**Symptom**: Error "Port already in use" or "Address in use"

**Solutions**:
1. Close any old server windows
2. Run: `netstat -ano | findstr :6414` - see what's using it
3. Kill the process: Find PID and close it
4. Wait 30 seconds for OS to release port
5. Restart server

## Monitoring Commands

### Check Server Status
```powershell
netstat -ano | findstr :6414  # Game server
netstat -ano | findstr :8090  # Registration server
```

### View Server Logs
```powershell
# Watch live (if server running in PowerShell)
Get-Content -Path "game_server.log" -Tail 20 -Wait

# View last 50 lines
Get-Content -Path "game_server.log" -Tail 50
```

### Check for Errors
```powershell
# Search logs for errors
Select-String "ERROR" game_server.log
Select-String "FATAL" game_server.log
Select-String "❌" game_server.log
```

## Expected Log Output

### Startup
```
[02:45:29] ======================================================================
[02:45:29] 🎮 WONDERLAND ONLINE - GAME SERVER v3
[02:45:29] ======================================================================
[02:45:29] Loading game data...
[02:45:29] ✓ Loaded npc.dat (468.7 KB)
[02:45:29] ✓ Loaded item.dat (1432.6 KB)
... (11 more files) ...
[02:45:29] ✅ Loaded 13 data files
[02:45:29] ✅ Listening on 0.0.0.0:6414
[02:45:29] Waiting for connections...
```

### Client Connects
```
[02:46:00] [Session 1] ✅ CONNECTED from 127.0.0.1:58074
[02:46:00] [Session 1] → Sent handshake packet (AC: 0x01, 0x00)
[02:46:00] [Session 1] → Sent character info packet (AC: 0x03, 0x00)
[02:46:00] [Session 1] → Sent map data packet (AC: 0x05, 0x00)
```

### Keep-Alive Running
```
[02:46:30] [Session 1] → Keep-alive sent
[02:47:00] [Session 1] → Keep-alive sent
[02:47:30] [Session 1] → Keep-alive sent
```

### Graceful Disconnect
```
[02:48:00] [Session 1] ❌ Client disconnected
[02:48:00] [Session 1] 🔌 Connection closed
```

## Known Issues & Limitations

### Issue 1: Protocol Incomplete
- Only basic handshake implemented
- Game world objects/NPCs may not display
- Player movement might be limited
- Chat/trading not implemented

**Workaround**: This is expected - full protocol would require complete reverse engineering

### Issue 2: No Persistence
- Characters don't save world state
- Inventory/equipment not stored
- Every session starts fresh

**Workaround**: Create database integration (future enhancement)

### Issue 3: Single-Server Only
- No inter-server communication
- No player-to-player trading
- No guild functions

**Workaround**: Scale up by adding multiple game servers (future)

## Next Steps

### If Connection Works:
1. ✅ Celebrate! The protocol is correct
2. 📊 Analyze packet captures
3. 🔧 Extend with more action codes
4. 💾 Add database persistence

### If Connection Fails:
1. Check game_server.log for errors
2. Verify Wonderland.exe installation
3. Test network connectivity
4. Review protocol documentation
5. Check firewall settings

## Support Files

- **Protocol Reference**: `GAME_SERVER_V3_PROTOCOL.md`
- **Detailed Fix Summary**: `SESSION_CRASH_FIX_SUMMARY.md`
- **Server Logs**: `game_server.log`
- **Registration Logs**: `registration_server.log`

## Quick Reference

| Component | Status | Port | Log File |
|-----------|--------|------|----------|
| Game Server v3 | ✅ Running | 6414 | game_server.log |
| Registration Server | ✅ Running | 8090 | reg_server.log |
| Game Data Files | ✅ Loaded | - | in game_server.log |
| Dashboard | ✅ Available | 8090 | - |
| Database | ✅ SQLite | - | AppData\PServer\ |

