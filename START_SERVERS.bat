@echo off
REM Wonderland Online - Start Both Servers
REM Uses Python servers (reliable and working)

cd /d "%~dp0"

echo.
echo ======================================
echo   WONDERLAND ONLINE
echo   Game + Registration Servers
echo ======================================
echo.

echo Starting Registration Server (port 8090)...
start "WLO Registration" cmd /k python registration_server.py

echo Starting Game Server (port 6414)...
start "WLO Game Server" cmd /k python game_server.py

echo.
echo ✓ Both servers started!
echo ✓ Two new windows should open
echo.
echo Next: Open your browser
echo   http://192.168.1.8:8090/dashboard.html
echo.
echo Then login and click PLAY to launch game!
echo.

pause
