@echo off
setlocal enabledelayedexpansion

REM Get local IP address
for /f "tokens=2 delims=:" %%A in ('ipconfig ^| findstr /R "IPv4 Address"') do (
    set "IP=%%A"
    goto :found
)

:found
REM Clean up whitespace
set "IP=!IP: =!"

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║   Wonderland Online - Share This Link With Other Players      ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.
echo 📍 YOUR COMPUTER'S IP ADDRESS:
echo.
echo    %IP%
echo.
echo 🌐 SHARE THIS LINK WITH OTHER PLAYERS:
echo.
echo    http://%IP%:8090
echo.
echo ✓ They can join from any computer on your network
echo ✓ They access the same registration server
echo ✓ Multiple players can play simultaneously
echo.
echo 📋 INSTRUCTIONS FOR OTHER PLAYERS:
echo.
echo    1. Open browser on their computer
echo    2. Go to: http://%IP%:8090
echo    3. Create account (register)
echo    4. Login with credentials
echo    5. Create character
echo    6. Click "Play" to launch game
echo.
echo ⚠️  IMPORTANT:
echo    • Both computers must be on SAME WiFi/Network
echo    • Keep your server running (don't close this window)
echo    • Your computer must stay on while players are playing
echo.
echo 🖥️  MAKE SURE:
echo    • Registration server is running (python registration_server.py)
echo    • Firewall allows port 8090 (check Windows Defender)
echo    • Players use correct IP address (%IP%)
echo.
echo Press any key to continue...
pause >nul
