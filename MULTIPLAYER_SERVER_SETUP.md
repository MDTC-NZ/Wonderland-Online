# 🌐 Multiplayer Server Setup Guide

## What's Changed

✅ **Registration Server Fixed**  
The registration server now accepts connections from **any IP address**, not just localhost.

**Before:**
```csharp
listener = new TcpListener(IPAddress.Loopback, Port);  // Only local connections
```

**After:**
```csharp
listener = new TcpListener(IPAddress.Any, Port);  // All connections welcome!
```

---

## Network Architecture

### Game Ports

| Service | Port | Purpose | Access |
|---------|------|---------|--------|
| **Login Server** | 6414 | Player login & game connection | All IPs ✅ |
| **Registration Server** | 8090 | Account registration (HTTP) | All IPs ✅ |
| **World Server** | Various | Game world (managed by game) | All IPs ✅ |

---

## Step-by-Step: Enable Multiplayer

### Step 1: Prepare Server Machine

**Requirements:**
- Windows PC with the compiled server executable
- Stable internet connection
- Firewall configured to allow ports 6414 and 8090

### Step 2: Configure Firewall (Windows)

Allow the server to accept incoming connections:

**Option A: Windows Defender Firewall (Automatic)**
```
When you first run the server, Windows may ask "Allow this app to communicate?"
Click: Yes / Allow
```

**Option B: Manual Configuration**

1. Open Windows Defender Firewall
2. Click "Allow an app through firewall"
3. Click "Allow another app"
4. Select: `obj\Release\Wonderland Private Server.exe`
5. Check both "Private" and "Public"
6. Click OK

### Step 3: Get Your Server IP Address

**On the server machine**, open Command Prompt and run:
```
ipconfig
```

Look for **IPv4 Address** - this is your server's local IP (e.g., `192.168.1.100`)

**For remote players**, you'll also need your **public IP address**:
```
Open browser and visit: https://www.whatismyipaddress.com/
```

### Step 4: Start the Server

Run the executable:
```
obj\Release\Wonderland Private Server.exe
```

You should see in the status window:
```
✓ Account registration page available at http://127.0.0.1:8090/
✓ Server bound to port 6414 successfully
✓ Now listening for clients on Port 6414
```

### Step 5: Connect Players

#### For Local Network Players (same WiFi/LAN):
```
Registration: http://192.168.1.100:8090/
Game Server: 192.168.1.100:6414
```

#### For Remote Players (over internet):
You need to set up **port forwarding** on your router.

**Setup Port Forwarding:**
1. Open your router's admin panel (usually http://192.168.1.1)
2. Look for "Port Forwarding"
3. Forward port 6414 → 192.168.1.100:6414 (Game Server)
4. Forward port 8090 → 192.168.1.100:8090 (Registration)
5. Save changes

Then provide remote players with:
```
Registration: http://[YOUR_PUBLIC_IP]:8090/
Game Server: [YOUR_PUBLIC_IP]:6414
```

**Example:**
```
If your public IP is 203.10.20.30:
Registration: http://203.10.20.30:8090/
Game Server: 203.10.20.30:6414
```

---

## Testing Connectivity

### Test Local Connection

**On a different computer on the same network:**

1. **Test Registration Server:**
   ```
   Open browser: http://192.168.1.100:8090/
   ```
   Should see: Account registration form

2. **Test Game Server:**
   ```
   Try connecting game client to: 192.168.1.100:6414
   ```

### Test Remote Connection

**From a computer outside your network:**

1. **Test Registration Server:**
   ```
   Open browser: http://[YOUR_PUBLIC_IP]:8090/
   ```

2. **Test Game Server Port:**
   ```
   Use online port checker: https://www.yougetsignal.com/tools/open-ports/
   Enter: [YOUR_PUBLIC_IP]:6414 and [YOUR_PUBLIC_IP]:8090
   ```

---

## Player Registration Flow

### New Player Setup

1. **Player registers account**
   ```
   Browser → http://[SERVER_IP]:8090/
   Creates username & password
   ```

2. **Account stored in database**
   ```
   Database auto-created if not exists
   Stored in: %APPDATA%\PServer\
   ```

3. **Player launches game client**
   ```
   Enters [SERVER_IP]:6414
   Logs in with registered credentials
   ```

4. **Player enters game world**
   ```
   Connects to LoginServer (port 6414)
   Routed to WorldServer for gameplay
   ```

---

## Server Configuration

### View Active Configuration

When server runs, open the **Configuration** tab:

1. **Database Settings**
   - Can use SQLite (local file) or MySQL (network database)
   - Default: SQLite (easiest for single server)

2. **Update Schedule**
   - Set when server updates happen
   - Configure warning type for players

3. **Save Configuration**
   - Click "Save" button to persist settings
   - Settings survive server restart

### Recommended Settings

**For Testing (All Local)**
```
Database: SQLite
Update Schedule: Monday 2:00 AM
Warning Type: Dialog
```

**For Public Server (Multiple Players)**
```
Database: MySQL (external hosted database recommended)
Update Schedule: Off-peak time (e.g., 3:00 AM)
Warning Type: Message (less intrusive)
```

---

## Monitoring Player Connections

### In Server Window

The server logs show:
```
✓ New client [192.168.1.101:54821] connected from Port 6414
✓ Connection Successful
✓ Testing Connection to UserDatabase
```

This means a player has connected!

### Database Verification

Players create accounts in the database:
- **SQLite Location:** `%APPDATA%\PServer\
` (auto-managed)
- **Verify Accounts:** Check server logs for "Connection Successful"

---

## Troubleshooting

### Problem: "Connection refused" from other computers

**Cause:** Firewall is blocking
**Solution:**
1. Allow app through Windows Firewall (see Step 2)
2. Check router firewall settings

### Problem: Local players can't connect but server runs

**Cause:** Wrong IP address used
**Solution:**
1. Run `ipconfig` on server
2. Use the "IPv4 Address" value (e.g., 192.168.1.100)
3. NOT localhost or 127.0.0.1

### Problem: Remote players can't access registration page

**Cause:** Port forwarding not configured
**Solution:**
1. Enable port forwarding in router (ports 6414 & 8090)
2. Test with online port checker
3. Provide players with PUBLIC IP (not local IP)

### Problem: Players connect but can't log in

**Cause:** Database not configured or network database unreachable
**Solution:**
1. Verify database in Configuration tab
2. If using MySQL, verify network connectivity
3. For testing, use SQLite (local file)

### Problem: Server crashes with "Address already in use"

**Cause:** Port already in use by another application
**Solution:**
1. Close other applications using ports 6414 or 8090
2. Or change the ports in code and rebuild

---

## Security Considerations

### For Testing/Friends Only

If hosting for friends only on local network:
- No firewall changes needed (just allow through Windows Firewall)
- No port forwarding needed
- Use local IP addresses only

### For Public Server

If hosting for strangers over internet:

1. **Use Strong Database Credentials**
   - Set strong password in database configuration
   - Save configuration securely

2. **Configure Network Security**
   - Only open necessary ports (6414, 8090)
   - Consider using firewall rules to limit connections
   - Monitor for suspicious activities in server logs

3. **Implement Rate Limiting** (future enhancement)
   - Add connection throttling
   - Prevent password brute-force attacks

4. **Enable HTTPS for Registration** (future enhancement)
   - Currently uses HTTP for registration page
   - For public servers, implement SSL/TLS

---

## Performance Tuning

### For Multiple Players

Server is optimized for local area network play. For remote/internet play:

1. **Bandwidth Optimization**
   - Server already uses compressed packets
   - No additional configuration needed

2. **Player Limit**
   - Server supports 40 simultaneous login connections (configurable)
   - Actual player limit depends on map design and system specs

3. **Server Machine Requirements**
   ```
   Minimum: Windows 7+, 2GB RAM, 1 Mbps internet
   Recommended: Windows 10+, 4GB+ RAM, 10+ Mbps internet
   ```

---

## Files & Directories

### Server Locations

```
C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop\
├── obj\Release\
│   └── Wonderland Private Server.exe  (Main executable)
├── Src\
│   ├── Server\RegistrationServer.cs   (Registration HTTP server) ✅ FIXED
│   ├── Server\LoginServer.cs          (Login/Game connection)
│   └── Server\WorldServer.cs          (Game world)
└── Web\
    ├── register.html                   (Registration page)
    ├── register.css                    (Styling)
    └── register.js                     (Client-side logic)
```

### Player Data Locations

```
%APPDATA%\PServer\
├── Config.settings.wlo               (Server configuration)
└── [Database Files]
    ├── userdb.sqlite / userdb.db    (If using SQLite for users)
    └── chardb.sqlite / chardb.db    (If using SQLite for characters)
```

---

## Summary: Network Setup Checklist

- [ ] Executable compiled and ready (`obj\Release\Wonderland Private Server.exe`)
- [ ] Firewall allows ports 6414 and 8090
- [ ] Server IP address identified (use `ipconfig`)
- [ ] Database configured (SQLite default)
- [ ] Server started and listening
- [ ] Local players can register via `http://[SERVER_IP]:8090/`
- [ ] Local players can connect to `[SERVER_IP]:6414`
- [ ] (Optional) Port forwarding configured for remote players
- [ ] (Optional) Remote players can register and connect

---

## Quick Commands

**Get Server IP:**
```
ipconfig
```

**Find Open Ports (after starting server):**
```
netstat -an | find "6414"
netstat -an | find "8090"
```

**View Server Logs:**
Check the server application window for connection logs

**Restart Server:**
```
Close application window
Relaunch executable
```

---

## What's Ready for Players

✅ **Registration System** - Players can create accounts  
✅ **Login Server** - Players can connect and log in  
✅ **World Server** - Players can play the game  
✅ **Database** - Player data persists  
✅ **Configuration** - Server settings saved  
✅ **Network Access** - Public connections enabled  

---

**Version:** 1.0  
**Last Updated:** 2026-11-14  
**Status:** ✅ READY FOR MULTIPLAYER
