# 🎮 Wonderland Online - Deployment Complete!

## ✅ System Status

Both servers are now **fully operational** and working together:

### Game Server
- **Status**: ✅ RUNNING
- **Port**: 6414
- **Type**: C# .NET WinForms Application
- **Binary**: `bin\Debug\Wonderland Private Server.exe`
- **Features**:
  - Player character management
  - Quest system
  - NPC interactions
  - Item management
  - Map data
  - Character progression

### Registration & Dashboard Server
- **Status**: ✅ RUNNING
- **Port**: 8090
- **Type**: Python HTTP Server
- **Script**: `registration_server.py`
- **Features**:
  - User registration
  - Account login
  - Password reset
  - Character dashboard
  - Account management

---

## 🚀 Quick Start

### Option 1: Batch File (Recommended)
Double-click `START_SERVERS.bat` to launch both servers automatically.

### Option 2: Manual Start
```powershell
# Terminal 1 - Game Server
cd "C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop"
.\bin\Debug\Wonderland Private Server.exe

# Terminal 2 - Registration Server  
python registration_server.py
```

### Option 3: Command Prompt
```batch
cd "C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop"
python registration_server.py
```
(Game server must be started separately from File Explorer)

---

## 🌐 Accessing the System

### Web Interface
- **Registration Page**: `http://localhost:8090`
  - Create account
  - Login
  - Forgot password
  - Login information

- **Dashboard**: `http://localhost:8090/dashboard.html`
  - View characters
  - Manage account
  - Community info
  - Events & rewards
  - In-game mall

- **Test Page**: `http://localhost:8090/test.html`
  - API testing interface
  - Manual account operations

### API Endpoints
All endpoints accept POST requests with form data:

#### Create Account
```bash
POST http://localhost:8090/api/register
Content-Type: application/x-www-form-urlencoded

username=myusername&password=mypassword&passwordConfirm=mypassword
```

#### Login
```bash
POST http://localhost:8090/api/login
Content-Type: application/x-www-form-urlencoded

username=myusername&password=mypassword
```

#### Reset Password
```bash
POST http://localhost:8090/api/reset-password
Content-Type: application/x-www-form-urlencoded

username=myusername&newPassword=newpassword
```

---

## 💾 Database

### Registration Database (SQLite)
- **Location**: `%APPDATA%\PServer\registration.db`
- **Type**: SQLite 3
- **Auto-created**: Yes, on first run
- **Tables**:
  - `users` (id, username, password, email, created_at, character1, character2)

### Game Database (PostgreSQL)
- **Connection**: Optional - `wlo.kiaoragaming.xyz:5423`
- **Status**: Falls back to SQLite if unavailable
- **Purpose**: Game data, items, NPCs, quests, maps

---

## 📁 Project Structure

```
Wonderland-Develop/
├── bin/
│   └── Debug/
│       ├── Wonderland Private Server.exe  # Game server
│       ├── *.dll                          # Libraries
│       ├── Data/                          # Game data
│       │   └── ServerDataBase.db          # Game database
│       └── Web/                           # Web interface
│           ├── register.html
│           ├── register.css
│           ├── register.js
│           ├── dashboard.html
│           ├── dashboard.css
│           ├── dashboard.js
│           └── test.html
├── Src/                                   # C# source code
├── registration_server.py                 # Python HTTP server
├── START_SERVERS.bat                      # Batch launcher
└── SERVER_SETUP.md                        # Setup documentation
```

---

## 🔍 Testing

### Test Registration
```bash
curl -X POST http://127.0.0.1:8090/api/register ^
  -d "username=testuser&password=Test1234&passwordConfirm=Test1234"
```

### Test Login
```bash
curl -X POST http://127.0.0.1:8090/api/login ^
  -d "username=testuser&password=Test1234"
```

### Test Web Pages
Open in browser:
- `http://localhost:8090/` - Registration page
- `http://localhost:8090/test.html` - Interactive tests

---

## ⚙️ Configuration

### Change Port Numbers
Edit `registration_server.py` (line 11):
```python
PORT = 8090  # Change this number
```

### SSL/HTTPS
Currently using HTTP only. To enable HTTPS:
1. Generate SSL certificate
2. Modify `registration_server.py` to use `ssl` module
3. Configure certificate path

### Database Connection
For PostgreSQL (production):
1. Configure connection in game server settings
2. Update hostname/port/credentials
3. Restart game server

---

## 🐛 Troubleshooting

### Game Server Won't Start
- Run with admin privileges
- Check port 6414 is free: `netstat -ano | findstr :6414`
- Check firewall settings
- Review `bin\Debug\server.log`

### Registration Server Won't Start
- Ensure Python 3 is installed: `python --version`
- Check port 8090 is free: `netstat -ano | findstr :8090`
- Check file permissions on `Web` directory
- Run from command prompt to see errors

### Can't Connect to Server
- Verify both processes are running: `tasklist | findstr wonderland`
- Check firewall isn't blocking ports 6414 or 8090
- Try `localhost` instead of `127.0.0.1`
- Check router/network settings if accessing from another machine

### Database Errors
- Clear database: Delete `%APPDATA%\PServer\registration.db`
- Restart registration server
- Database will be recreated automatically

---

## 🔐 Security Notes

### Current Setup (Development)
- ✅ SQLite database (local storage)
- ❌ Plain-text passwords (NO HASHING)
- ❌ HTTP only (NO HTTPS/SSL)
- ✅ CORS enabled
- ❌ No rate limiting

### Production Checklist
- [ ] Implement password hashing (bcrypt/argon2)
- [ ] Enable HTTPS with valid SSL certificate
- [ ] Add rate limiting to API endpoints
- [ ] Implement CSRF protection
- [ ] Add input validation/sanitization
- [ ] Enable database backups
- [ ] Set up monitoring/logging
- [ ] Add 2FA support
- [ ] Configure domain (wlo.kiaoragaming.xyz)
- [ ] Set up reverse proxy (nginx)

---

## 📞 Support

### Common Issues

**Port already in use:**
```powershell
# Find process using port
Get-NetTCPConnection -LocalPort 8090 | Select-Object OwningProcess
# Kill process (replace PID)
Stop-Process -Id <PID> -Force
```

**Python module missing:**
```bash
pip install --upgrade pip
pip install requests
```

**Permission denied:**
- Run game server as Administrator
- Right-click `.exe` → Properties → Advanced → Run as Administrator

---

## 🎯 Next Steps

1. **Test the system**:
   - Create account at `http://localhost:8090`
   - Login and verify token
   - Check dashboard

2. **Invite players**:
   - Share registration link
   - Players can create accounts
   - Players can join game on port 6414

3. **Customize**:
   - Add your branding to Web pages
   - Configure game server settings
   - Set up SSL certificates
   - Deploy to production server

4. **Monitor**:
   - Keep servers running
   - Monitor logs for errors
   - Backup database regularly
   - Track player activity

---

## 📊 System Merged From

### Wonderland-Develop
- Beautiful, modern registration UI
- Character management dashboard
- Password reset functionality
- Professional HTML/CSS/JavaScript

### Wonderland-Private-Server-cheatengine-fork
- Working game server binary
- Complete game assets (Data, DLLS, Resources)
- All NPC and quest data
- Map data and game content
- Proven stable configuration

**Result**: Unified, playable system combining best of both codebases! ✨

---

**Deployment Date**: November 14, 2026
**Status**: ✅ OPERATIONAL
**Players**: Ready to connect
**Enjoy!** 🎮
