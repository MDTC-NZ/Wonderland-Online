# 🌐 Wonderland Online - Network Access Guide

## For Other Players on Your Network

Your Wonderland Online server is now accessible to other players on your local network!

### Server Address

**Replace `192.168.1.8` with your actual computer's IP address**

```
http://192.168.1.8:8090
```

## How to Find Your Computer's IP Address

### On Your Server Computer (Windows)

**Method 1: Command Prompt**
```bash
ipconfig
```
Look for: `IPv4 Address: 192.168.x.x`

**Method 2: Settings**
1. Open Settings
2. Click "Network & Internet"
3. Click "WiFi" or "Ethernet"
4. Scroll down to "Properties"
5. Find "IPv4 address"

**Method 3: Check Server Startup Output**
When you start the registration server, it will display:
```
📌 Share this link with other players:
   http://192.168.1.8:8090
```

## How Other Players Can Join

### Step 1: Get Your IP Address
Ask the server admin or check the server startup message for your computer's IP.

### Step 2: Open Browser
On another computer/device on the same network, open:
```
http://192.168.1.8:8090
```
(Replace 192.168.1.8 with your actual IP)

### Step 3: Register Account
- Click "Create Account"
- Enter username and password
- Click "Create Account"

### Step 4: Login
- Click "Login" tab
- Enter credentials
- Click "Login"

### Step 5: Create Character
- Click "Create Character"
- Fill in character details
- Click "Create Character"

### Step 6: Play!
- Click "Play" button
- Game launches
- Connect to game server

## Network Requirements

✅ **Same WiFi/Network**
- Players must be on the same local network
- Or connected via VPN to your network

❌ **NOT on Same Network**
- Can't access at 192.168.1.8:8090
- Need to deploy to actual domain
- Use wlo.kiaoragaming.xyz:5423 instead

## Troubleshooting Network Access

### "Can't connect to 192.168.1.8:8090"

**Problem 1: Wrong IP Address**
- Solution: Double-check your actual IP with `ipconfig`
- Ensure you're using the correct IP

**Problem 2: Firewall Blocking**
- Solution: Allow Python through Windows Firewall
  1. Open Windows Defender Firewall
  2. Click "Allow an app through firewall"
  3. Find Python (python.exe)
  4. Check both Private and Public boxes
  5. Click OK

**Problem 3: Port 8090 Blocked**
- Solution: Use Windows Firewall settings
  1. Inbound Rules → New Rule
  2. Port → Next
  3. TCP → Specific Ports: 8090 → Next
  4. Allow the connection → Finish

**Problem 4: Not on Same Network**
- Solution: Connect to same WiFi or VPN
- Ensure both computers are on 192.168.1.x network

**Problem 5: Server Not Running**
- Solution: Start server first
  ```bash
  python registration_server.py
  ```
- Keep the window open while playing

## Sharing With Friends

### Option 1: Local Network (Same WiFi)
```
✓ Easy - Just share your IP address
✓ Fast - No internet needed
✓ Simple - Works immediately
✗ Only works on same network
```

**Steps:**
1. Get your IP (192.168.1.8)
2. Share: "http://192.168.1.8:8090"
3. They join and create accounts

### Option 2: Remote Access (Different Network)

**For friends NOT on your network:**
- Need to deploy to actual server
- Use: `https://wlo.kiaoragaming.xyz:5423/`
- Or set up VPN/port forwarding

### Option 3: Port Forwarding (Advanced)
```
⚠️  Not recommended for security reasons
    Only if you know what you're doing
```

If you want to expose your server to the internet:
1. Configure port forwarding on router
2. Forward port 8090 to your computer
3. Get your public IP from: https://whatismyipaddress.com/
4. Share: `http://[your_public_ip]:8090`

⚠️  **Security Warning**: This exposes your computer to the internet!
- Use firewall to limit access
- Consider HTTPS/SSL
- Monitor connections
- Consider using VPN instead

## Testing Network Access

### From Your Computer
```
✓ http://localhost:8090
✓ http://127.0.0.1:8090
✓ http://192.168.1.8:8090
```

### From Another Computer on Network
```
✓ http://192.168.1.8:8090
✗ http://localhost:8090 (won't work!)
✗ http://127.0.0.1:8090 (won't work!)
```

## Server Status Commands

### Check if server is running
```bash
netstat -ano | findstr :8090
```

### Find listening process
```bash
tasklist | findstr python
```

### Stop server
- Press Ctrl+C in the server window

### Restart server
- Close server window
- Run: `python registration_server.py`

## Multiple Players Simultaneously

Your server can handle **multiple players at once**:

✓ 10+ players registering
✓ 10+ players playing simultaneously  
✓ 2 characters per player (20 total)
✓ All sharing one server

The SQLite database grows as you add users, but handles local networks well.

## Performance Tips

### For Best Performance

1. **Use Ethernet** (if possible)
   - WiFi can be slower
   - Ethernet more stable

2. **Close Other Apps**
   - Free up network bandwidth
   - Reduce server load

3. **Don't Overload Network**
   - Too many simultaneous streams = lag
   - Reasonable for 10-20 players
   - Beyond that: consider dedicated server

4. **Restart Server Daily**
   - Clears memory
   - Prevents memory leaks
   - Keeps performance optimal

## Production Deployment

When ready for real players:

### Local Network Version
```
http://192.168.1.8:8090
✓ Easy to set up
✓ No costs
✗ Only works locally
✗ Only on your network
```

### Production Version
```
https://wlo.kiaoragaming.xyz:5423/
✓ Works worldwide
✓ Professional
✓ Scalable
✗ Requires deployment
✗ Requires domain
✗ Requires SSL certificate
```

## For Server Admin

### Monitor Active Players
1. Check registration_server.py console
2. Look for login/launch-game requests
3. Each line = player action

### View Database
```bash
# List registered users
sqlite3 "%APPDATA%\PServer\registration.db" "SELECT username FROM users;"

# See all user data
sqlite3 "%APPDATA%\PServer\registration.db" "SELECT * FROM users;"
```

### Backup Database
```bash
# Copy database to safe location
copy "%APPDATA%\PServer\registration.db" "backup_$(date /t).db"
```

### Restart for Maintenance
1. Close registration_server.py window
2. Do maintenance
3. Restart: `python registration_server.py`

## Common Questions

**Q: How many players can join?**
A: SQLite handles 100+ concurrent users fine on local network.

**Q: Does my computer need to stay on?**
A: Yes - server runs on your computer. Turn it off = server down.

**Q: Can we play from outside the network?**
A: Not with current setup. Need to deploy to actual server.

**Q: Is it secure?**
A: No - passwords are plain text. OK for local testing only.

**Q: Can we backup our data?**
A: Yes - database is at %APPDATA%\PServer\registration.db

**Q: What if we get hacked?**
A: Delete database, restart server. Recreate accounts.

**Q: Can we run multiple servers?**
A: Use different ports (8091, 8092, etc.)

## Network Diagram

```
                    ┌─ Player 1's Computer
                    │  (192.168.1.100)
                    │  http://192.168.1.8:8090
                    │
         ┌──────────┴────────┐
         │                   │
    Router (192.168.1.1)   Your Computer
         │                (192.168.1.8) ← Server Here
         │
         ├─ Player 2 Phone
         │  http://192.168.1.8:8090
         │
         └─ Player 3 Laptop
            http://192.168.1.8:8090

         ✓ All connected to same WiFi
         ✓ All can access 192.168.1.8:8090
         ✓ All can create accounts & play
```

## Summary

✅ **Your server is ready for local network play**
- Other players on your WiFi can connect
- Share your IP address (192.168.1.8)
- They access: http://192.168.1.8:8090
- Multiple players can play simultaneously

🚀 **When ready for production**
- Deploy to real server (wlo.kiaoragaming.xyz)
- Set up SSL/HTTPS
- Allow players from anywhere

---

**Network Status**: ✅ Ready for Local Multiplayer
**Players Supported**: 10-100+ (local network)
**Access Method**: LAN only (Same WiFi)
**Security Level**: Development (not production-ready)

For production deployment, see SERVER_CONFIGURATION.md
