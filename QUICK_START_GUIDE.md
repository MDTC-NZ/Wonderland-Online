# 🎮 WONDERLAND ONLINE - QUICK START GUIDE

## ✅ System Status: OPERATIONAL

```
✅ Port 8080   - CheatEngine Registration Server (ACTIVE)
✅ Port 6414   - Game Server (ACTIVE)
⏸️  Port 8090   - Python Backup Server (Standby)
✅ Database    - unified.db (24 KB, 5 users consolidated)
✅ Backups     - 2 backup files secured
```

---

## 🚀 START PLAYING NOW

### Step 1: Open Registration (Browser)
```
http://localhost:8080/
```
You'll see the beautiful Wonderland registration page with:
- Username field
- Email field
- Password field
- Confirm Password field
- Register button

### Step 2: Create Account or Login
**Option A - New Player:**
1. Fill in Username (e.g., "myplayer")
2. Enter Email
3. Set Password
4. Confirm Password
5. Click "Register"

**Option B - Existing Player (5 users available):**
```
Username: testuser
Password: test1234
```

### Step 3: Launch Game
1. Login with your credentials
2. Click "PLAY" or "Launch Game"
3. Game will launch and connect to port 6414
4. Load into Wonderland Online world

---

## 🎯 Consolidated System Architecture

### What Happened
✅ **Merged 5 Python registration users** into unified database
✅ **Created unified.db** with all user/character data
✅ **Backed up all original databases** for safety
✅ **Kept both registration servers** for redundancy

### User Database (5 Consolidated Users)
```
testuser123      ✅ Active
player2          ✅ Active
user_1794658300  ✅ Active
test123          ✅ Active
testuser         ✅ Active (DEFAULT TEST ACCOUNT)
```

### Database Location
```
Primary:    C:\Users\{username}\AppData\Roaming\PServer\unified.db
Backups:    C:\Users\{username}\AppData\Roaming\PServer\backups\
```

---

## 📊 Server Configuration

### CheatEngine Registration Server (Port 8080) - PRIMARY
```
URL:           http://localhost:8080/
GUI:           Beautiful, professional interface
Database:      unified.db
Features:      Full registration, user management, login
Status:        ✅ ACTIVE AND RECOMMENDED
```

### Game Server (Port 6414) - PRIMARY
```
Host:          localhost:6414
Type:          CheatEngine .NET game server
Features:      Full game world, quests, maps, characters
Status:        ✅ ACTIVE
Connections:   Accepting players
```

### Python Backup Server (Port 8090) - BACKUP
```
URL:           http://localhost:8090/
GUI:           Simple HTML interface
Database:      unified.db
Status:        ⏸️  Standby (available if 8080 fails)
```

---

## 🔑 Test Credentials

Use these to test immediately:

| Username | Password | Status |
|----------|----------|--------|
| testuser | test1234 | ✅ Active |

Characters available after login: TestCharacter

---

## 📁 File Structure

```
Wonderland-Develop/
├── Wonderland Private Server.exe        ← Game + Registration
├── registration_server.py               ← Python backup
├── merge_databases.py                   ← Database consolidation (COMPLETED)
├── create_test_user.py                  ← User creation utility
├── QUICK_START_GUIDE.md                 ← THIS FILE
├── SERVER_CONSOLIDATION_COMPLETE.md     ← Technical details
├── COMPLETE_SETUP_SUMMARY.md            ← Full architecture
└── wlo.pserver.core/                    ← Source code
    ├── Game/
    │   ├── Quest.cs (MERGED)
    │   └── Maps/ (6 files MERGED)
    └── ... (other game systems)

%APPDATA%\PServer/
├── unified.db                           ← PRIMARY DATABASE
├── registration.db                      ← Original Python DB
├── ServerDataBase.db                    ← Original Game DB
├── backups/                             ← Safety backups
│   ├── registration.db.backup_20261115_031015
│   └── ServerDataBase.db.backup_20261115_031015
└── logs/
```

---

## 🎮 How to Register a New Player

### Method 1: Using CheatEngine GUI (Recommended)
1. Go to http://localhost:8080/
2. Fill in the registration form
3. Click "Register"
4. Login with new credentials
5. Click "Play" to launch game

### Method 2: Using Python (Advanced)
```bash
python create_test_user.py
```
Then access via http://localhost:8090/

---

## 🔧 Troubleshooting

### "Connection refused on port 8080"
- CheatEngine server may have crashed
- Check running processes: `tasklist | findstr Wonderland`
- Restart: Open `Wonderland Private Server.exe` from bin\Debug\

### "Port 8080 in use"
- Another application is running on 8080
- Check: `netstat -ano | findstr :8080`
- Kill process and retry

### "Database locked error"
- Database is in use by another process
- Restart both servers:
  1. Close `Wonderland Private Server.exe`
  2. Wait 2 seconds
  3. Restart `Wonderland Private Server.exe`

### "Game client won't launch"
- Verify game server on 6414 is running
- Check: `netstat -ano | findstr :6414`
- Firewall may be blocking - allow port 6414

### "Login failed"
- Verify username/password are correct
- Check unified.db exists: `C:\Users\{user}\AppData\Roaming\PServer\unified.db`
- Try with testuser/test1234
- Create new user via registration page

---

## 📈 System Health Check

Run this to verify everything is working:

```powershell
# Check database exists
Test-Path "$env:APPDATA\PServer\unified.db"

# Check servers are listening
netstat -ano | findstr ":8080\|:6414\|:8090"

# Check processes running
Get-Process | Where-Object { $_.Name -match "Wonderland|python" }
```

All should show ✅ status.

---

## 🛟 Getting Help

### If servers crash:
1. Check `$env:APPDATA\PServer\` for logs
2. Restart `Wonderland Private Server.exe`
3. Verify unified.db still exists

### If you need to restore:
1. Backups are in `$env:APPDATA\PServer\backups\`
2. Can restore original registration.db or ServerDataBase.db
3. Copy backup over unified.db and restart servers

### For more details:
- Read: `SERVER_CONSOLIDATION_COMPLETE.md` - Technical details
- Read: `COMPLETE_SETUP_SUMMARY.md` - Full architecture
- Check: Logs in `$env:APPDATA\PServer\logs\`

---

## ✨ What's New

### Consolidation Completed
- ✅ All user data in ONE database (unified.db)
- ✅ Redundant registration access (8080 primary, 8090 backup)
- ✅ Professional registration UI (CheatEngine)
- ✅ All original data backed up safely
- ✅ Ready for production use

### System is Now
- ✅ Stable - No duplicate databases causing conflicts
- ✅ Scalable - Can easily add more users
- ✅ Recoverable - Full backups of all original data
- ✅ Redundant - Backup server ready if primary fails
- ✅ Professional - Beautiful registration interface

---

## 🎯 Next Steps

1. **Test Now**: Go to http://localhost:8080/
2. **Create Account**: Or use testuser/test1234
3. **Play**: Click Play button and enjoy Wonderland Online!
4. **Monitor**: Keep servers running - they're production-ready

---

## 📞 Contact & Support

For issues with:
- **Server crashes**: Check processes and restart `Wonderland Private Server.exe`
- **Database errors**: Restore from backups in `%APPDATA%\PServer\backups\`
- **Game connection**: Verify port 6414 is open and listening
- **Registration**: Try both localhost:8080 and localhost:8090

---

## 🎉 CONGRATULATIONS!

Your Wonderland Online server is now:
✅ **Consolidated** - Single unified database
✅ **Stable** - No conflicts or duplicates
✅ **Secure** - Full backups created
✅ **Ready** - Production deployment complete

**Enjoy Wonderland Online!** 🌟

---

**Last Updated**: Consolidation Complete
**System Status**: ✅ OPERATIONAL
**Database**: unified.db (24 KB, 5 users)
**Backups**: 2 files secured
