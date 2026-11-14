@echo off
REM Wonderland Online - Start Complete Game System
REM This launches both the working game server and registration server

setlocal enabledelayedexpansion

cls
echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║     WONDERLAND ONLINE - STARTING COMPLETE SYSTEM           ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

REM Get the directory where this batch file is located
set SCRIPT_DIR=%~dp0

echo Launching Game Server (.NET - Wonderland Private Server.exe)...
echo.

REM Start the working .NET game server
if exist "%SCRIPT_DIR%bin\Debug\Wonderland Private Server.exe" (
    echo ✓ Found working server at: bin\Debug\Wonderland Private Server.exe
    start "" cmd /k "cd /d "%SCRIPT_DIR%bin\Debug" && "Wonderland Private Server.exe""
    echo ✓ Game Server started
) else (
    echo ✗ Error: Wonderland Private Server.exe not found!
    echo   Expected: %SCRIPT_DIR%bin\Debug\Wonderland Private Server.exe
    pause
    exit /b 1
)

echo.
timeout /t 2 /nobreak

echo.
echo Launching Registration Server (Python)...
echo.

REM Start registration server
if exist "%SCRIPT_DIR%registration_server.py" (
    echo ✓ Found registration server
    start "" cmd /k "cd /d "%SCRIPT_DIR%" && python registration_server.py"
    echo ✓ Registration Server started
) else (
    echo ✗ Error: registration_server.py not found!
    pause
    exit /b 1
)

echo.
echo ════════════════════════════════════════════════════════════
echo ✅ BOTH SERVERS STARTED SUCCESSFULLY!
echo ════════════════════════════════════════════════════════════
echo.
echo 🎮 Game Server:       port 6414 (running in separate window)
echo 📡 Registration Server: port 8090 (running in separate window)
echo.
echo 🌐 Open browser: http://localhost:8090/dashboard.html
echo.
echo Close any window to stop that server.
echo Press Ctrl+C in each server window to stop it.
echo.
pause
