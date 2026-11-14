# Wonderland Online - Merged Server Setup

## Overview
The Wonderland Online private server is now fully operational with both the game server and registration/dashboard system running together.

## System Architecture

### 1. Game Server (Port 6414)
- **Binary**: `bin\Debug\Wonderland Private Server.exe`
- **Type**: C# / .NET Framework
- **Function**: Handles player connections, quests, maps, NPCs, characters
- **Status**: ✓ Running and listening on port 6414

### 2. Registration & Dashboard Server (Port 8090)
- **Type**: Python HTTP Server
- **File**: `registration_server.py`
- **Function**: User registration, login, password reset, character management dashboard
- **Database**: SQLite at `%APPDATA%\PServer\registration.db`
- **Status**: ✓ Running and listening on port 8090

## Starting the Servers

### Automatic Start
Run the startup script in PowerShell:
```powershell
cd "C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop"

# Terminal 1: Start Game Server
Start-Process -FilePath ".\bin\Debug\Wonderland Private Server.exe" -Verb RunAs

# Terminal 2: Start Registration Server
python registration_server.py
```

### Manual Start
1. **Game Server**: Double-click `bin\Debug\Wonderland Private Server.exe` (requires admin)
2. **Registration Server**: Run `python registration_server.py` in command prompt

## API Endpoints

### Registration API (Port 8090)

#### Create Account
```bash
POST /api/register
Content-Type: application/x-www-form-urlencoded

username=myusername&password=mypassword&passwordConfirm=mypassword

Response: {"ok": true, "message": "Account created!"}
```

#### Login
```bash
POST /api/login
Content-Type: application/x-www-form-urlencoded

username=myusername&password=mypassword

Response: {"ok": true, "message": "Login successful!", "token": "token_..."}
```

#### Reset Password
```bash
POST /api/reset-password
Content-Type: application/x-www-form-urlencoded

username=myusername&newPassword=newpassword

Response: {"ok": true, "message": "Password reset successful!"}
```

## Web Interface

### Registration Page
- **URL**: `http://localhost:8090/`
- **Features**:
  - Create new account
  - Login to existing account
  - Forgot password recovery
  - Login information display

### Dashboard (After Login)
- **URL**: `http://localhost:8090/dashboard.html`
- **Features**:
  - View character slots (2 slots per account)
  - Create/delete characters
  - Manage account settings
  - Community information
  - Events and rewards
  - In-game mall/shop

## Database

### Registration Database
- **Location**: `%APPDATA%\PServer\registration.db`
- **Type**: SQLite 3
- **Tables**:
  - `users` - Account information (username, password, email, created_at, character slots)

### Game Database
- **Type**: PostgreSQL (remote server)
- **Connection**: Can be configured to connect to `wlo.kiaoragaming.xyz:5423`
- **Contents**: Game data, items, maps, NPCs, quests

## File Structure

```
Wonderland-Develop/
├── bin/Debug/
│   ├── Wonderland Private Server.exe    # Game server binary
│   ├── Data/                            # Game data files
│   ├── Web/                             # Web interface files
│   │   ├── register.html
│   │   ├── register.css
│   │   ├── register.js
│   │   ├── dashboard.html
│   │   ├── dashboard.css
│   │   └── dashboard.js
│   └── *.dll                            # Required libraries
├── registration_server.py               # Registration server
└── Src/                                 # C# source code
```

## Merged Features

### From Wonderland-Develop
- Beautiful registration page with modern UI
- Dashboard for character management
- Password reset functionality
- CORS support for cross-origin requests

### From Wonderland-Private-Server-cheatengine-fork
- Working game server binary
- All game data and assets
- NPC data and quest systems
- Map data
- Item systems

## Configuration

### Network Settings
- **Game Server**: 0.0.0.0:6414 (all interfaces)
- **Registration Server**: 0.0.0.0:8090 (all interfaces, Python serves localhost)

### Database Settings
Game server attempts to connect to:
- PostgreSQL on `wlo.kiaoragaming.xyz:5423` (if available)
- Falls back to local SQLite for registration

### SSL/TLS
- Registration server currently uses HTTP (not HTTPS)
- To enable HTTPS, configure SSL certificate in registration_server.py

## Troubleshooting

### Game Server Won't Start
- Ensure you run with administrator privileges
- Check that port 6414 is not already in use
- Review server.log in bin/Debug/ directory

### Registration Server Won't Start
- Check that port 8090 is not already in use
- Ensure Python 3 is installed and in PATH
- Run in command prompt to see any error messages

### Database Connection Issues
- The registration server uses SQLite (local file storage)
- Game server will attempt PostgreSQL but will continue with SQLite fallback
- To use PostgreSQL, configure connection string in game server settings

## Security Notes

1. **Default Setup**: Uses plain-text passwords in SQLite database
2. **Production Deployment**: 
   - Implement password hashing (bcrypt/argon2)
   - Enable HTTPS with valid SSL certificate
   - Implement rate limiting on API endpoints
   - Add CSRF protection

## Future Enhancements

- [ ] Integrate with wlo.kiaoragaming.xyz domain
- [ ] HTTPS/SSL support
- [ ] Password hashing
- [ ] Email verification
- [ ] 2FA support
- [ ] Character creation from dashboard
- [ ] Account recovery
- [ ] Admin panel

## Support

For issues or questions, check:
1. Server logs in `bin/Debug/server.log`
2. Registration database in `%APPDATA%\PServer\registration.db`
3. Python console output for registration server errors
