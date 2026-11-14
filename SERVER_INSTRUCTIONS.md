# 🎮 Wonderland Online - How to Run the Game Server

## ⚠️ IMPORTANT: Administrator Rights Required

The game server (**Wonderland Private Server.exe**) requires Administrator privileges to run properly.

## 🚀 Quick Start (3 Steps)

### Step 1: Start the Game Server (Administrator)
```
Right-click: bin\Debug\run_with_admin.bat
Select: Run as Administrator
```

The game server will start on **port 6414**.

### Step 2: Start the Registration Server (Regular)
```
Double-click: run_registration_server.bat
```

The registration server will start on **port 8090**.

### Step 3: Play the Game
```
Open browser: http://192.168.1.8:8090/dashboard.html
Login or Create Account
Click PLAY
```

---

## 📍 Server Details

| Server | Port | Technology | How to Run |
|--------|------|-----------|-----------|
| **Game Server** | 6414 | .NET (Wonderland Private Server.exe) | As Administrator |
| **Registration** | 8090 | Python (registration_server.py) | Normal User |

---

## 🔧 Batch Files Available

### Game Server
- **run_with_admin.bat** - Auto-elevates to admin and starts game server ✅ RECOMMENDED
- **start_server_admin.bat** - Requires manual "Run as Administrator"

### Registration Server  
- **run_registration_server.bat** - Starts on port 8090

### Game Launcher (Alternative)
- **run_game_server.bat** - Python version (not recommended, use .NET instead)

---

## ❓ Troubleshooting

**Q: "The game server still crashes"**  
A: Make sure you right-click and select "Run as Administrator". This is critical!

**Q: "Connection refused"**  
A: Both servers must be running:
1. Game Server (admin, port 6414)
2. Registration Server (normal, port 8090)

**Q: "Game client won't connect"**  
A: Check that port 6414 is listening:
```
netstat -ano | findstr "6414"
```

---

## 📊 System Status Check

To verify servers are running:
```
Registration: http://localhost:8090/dashboard.html
Game Server: Port 6414 (netstat check)
```

---

**Ready to play? Follow the Quick Start above!** 🎮
