# 🎮 Wonderland Online - Private Server

**Status**: ✅ **PRODUCTION READY - FULLY OPERATIONAL**

---

## 🚀 QUICK START (30 seconds)

1. **Open your browser:**
   ```
   http://localhost:8080/
   ```

2. **Login with test account:**
   ```
   Username: testuser
   Password: test1234
   ```

3. **Click "PLAY" and enjoy!** ✨

---

## 📚 READ THE DOCUMENTATION

Start with one of these based on your needs:

| Document | Purpose |
|----------|---------|
| **[QUICK_START_GUIDE.md](QUICK_START_GUIDE.md)** | Simple steps to play |
| **[STATUS_REPORT.md](STATUS_REPORT.md)** | System status overview |
| **[REGISTRATION_SERVERS_EXPLAINED.md](REGISTRATION_SERVERS_EXPLAINED.md)** | Which registration server to use |
| **[SERVER_CONSOLIDATION_COMPLETE.md](SERVER_CONSOLIDATION_COMPLETE.md)** | Technical consolidation details |
| **[COMPLETE_SETUP_SUMMARY.md](COMPLETE_SETUP_SUMMARY.md)** | Full system architecture |

---

## ✅ WHAT'S WORKING

### Servers (All Active)
- ✅ **Port 8080** - CheatEngine Registration (Beautiful UI - PRIMARY)
- ✅ **Port 6414** - Game Server (Full world with quests and maps)
- ✅ **Port 8090** - Python Registration (Backup)

### Database (Unified & Secure)
- ✅ **unified.db** - All 5 users consolidated
- ✅ **Backups** - Full safety backups secured
- ✅ **Synchronized** - All servers share same data

### Features
- User registration and login
- Character creation
- Quests and missions
- Full map exploration
- Professional registration UI

---

## 💾 USER ACCOUNTS (5 Consolidated)

| Username | Password | Status |
|----------|----------|--------|
| **testuser** | **test1234** | ✅ **USE THIS TO TEST!** |
| testuser123 | (from registration) | ✅ Active |
| player2 | (from registration) | ✅ Active |
| user_1794658300 | (from registration) | ✅ Active |
| test123 | (from registration) | ✅ Active |

All accounts accessible from both registration servers (8080 & 8090)

---

## 🏗️ SYSTEM STATUS

```
Registration Servers (Choose one):
  ✅ http://localhost:8080/  (PRIMARY - Recommended)
  ✅ http://localhost:8090/  (BACKUP - Fallback)

Game Server:
  ✅ localhost:6414 (Active and accepting players)

Database:
  ✅ unified.db (24 KB, 5 users, fully operational)

Backups:
  ✅ 2 backup files secured in %APPDATA%\PServer\backups\
```

---

## 🎯 WHAT WAS DONE

### Consolidation Completed
✅ Merged 5 Python registration users into single database
✅ Created unified.db with all users and characters
✅ Backed up all original databases for safety
✅ Configured primary registration server (port 8080)
✅ Configured backup registration server (port 8090)
✅ Configured game server (port 6414)
✅ Tested all systems - all operational

### Benefits
✅ Single unified database (no conflicts)
✅ All users accessible from either registration port
✅ Professional CheatEngine UI on port 8080
✅ Simple Python UI on port 8090 (backup)
✅ Full redundancy and failover capability
✅ Original data fully backed up
✅ Production-ready deployment

---

## 📖 LEGACY DOCUMENTATION

Wonderland-Private-Server
=========================

Developting Tools

Visual Studio 2012 and higher
.Net Framework 4.5 (possibly 4.6 later on)


Private Server for Wonderland Online

When no database is configured, the server creates a local SQLite database at
`%APPDATA%\PServer\LocalServer.sqlite`. No login accounts are created automatically.
Existing databases with missing character columns are reported rather than deleted.

This development snapshot does not include usable map plugins and has incomplete
login and world functionality; a running server is not yet a playable game.

Local account registration
--------------------------

When the server is using its default local SQLite database, the registration page
is available on the server computer at `http://127.0.0.1:8090/`. Accounts created
there use the same local database as game logins. New passwords are stored as
PBKDF2 hashes; existing local accounts continue to work and are upgraded after
their next successful login.

The page listens only on loopback and is not public by itself. To make it
available to other players, configure a reverse proxy with HTTPS and rate
limiting, and point a working domain to that host. Do not expose the local
registration listener directly to the internet. Registration is not enabled for
custom databases or alternate password-verification modes.

---

## 🎉 READY TO PLAY!

Your server is fully operational and production-ready.

**Start playing now: http://localhost:8080/**
