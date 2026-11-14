@echo off
REM Wonderland Online - Quick Start Guide
REM This script helps you start the server and share it with other players

setlocal enabledelayedexpansion

cls
color 0B
title Wonderland Online - Quick Start

echo.
echo ╔════════════════════════════════════════════════════════════════╗
echo ║        🎮 WONDERLAND ONLINE - QUICK START GUIDE 🎮             ║
echo ╚════════════════════════════════════════════════════════════════╝
echo.
echo Choose an option:
echo.
echo   1) START SERVER (for your computer & network)
echo   2) SHARE SERVER INFO (show IP address to share)
echo   3) CHECK SERVER STATUS (verify if it's working)
echo   4) OPEN DASHBOARD (http://localhost:8090)
echo   5) SETUP FIREWALL (allow port 8090 through firewall)
echo   6) VIEW CONFIGURATION (see server details)
echo   7) EXIT
echo.

set /p choice="Enter your choice (1-7): "

if "%choice%"=="1" (
    echo.
    echo Starting Wonderland Registration Server...
    echo Launching: python registration_server.py
    echo.
    python registration_server.py
    goto :end
)

if "%choice%"=="2" (
    echo.
    SHARE_SERVER_INFO.bat
    goto :end
)

if "%choice%"=="3" (
    echo.
    echo Checking if registration server is running on port 8090...
    netstat -ano | findstr :8090 >nul
    if errorlevel 1 (
        echo.
        echo [X] Server NOT RUNNING on port 8090
        echo.
        echo Start the server with option 1
    ) else (
        echo.
        echo [OK] Server is RUNNING on port 8090
        echo.
        echo Access it at:
        echo   • http://localhost:8090 (on this computer)
        echo   • http://192.168.1.8:8090 (from network)
        echo.
    )
    pause
    goto :menu
)

if "%choice%"=="4" (
    echo.
    echo Opening: http://localhost:8090
    start http://localhost:8090
    goto :end
)

if "%choice%"=="5" (
    echo.
    echo Opening Windows Firewall settings...
    echo Please allow Python (python.exe) through the firewall
    echo.
    pause
    wf.msc
    goto :end
)

if "%choice%"=="6" (
    echo.
    echo ╔════════════════════════════════════════════════════════════════╗
    echo ║              SERVER CONFIGURATION DETAILS                      ║
    echo ╚════════════════════════════════════════════════════════════════╝
    echo.
    echo Registration Server:
    echo   • Address: 0.0.0.0 (all interfaces)
    echo   • Port: 8090
    echo   • URL: http://localhost:8090 (local)
    echo   • Network: http://192.168.1.8:8090 (other computers)
    echo.
    echo Database:
    echo   • Type: SQLite
    echo   • Location: %%APPDATA%%\PServer\registration.db
    echo.
    echo Game Server:
    echo   • Address: wlo.kiaoragaming.xyz
    echo   • Port: 5423
    echo   • Protocol: HTTPS
    echo.
    echo Game Client:
    echo   • Path: C:\Program Files (x86)\Wonderland Online\Wonderland.exe
    echo   • Launcher: game_launcher.py
    echo.
    echo Features:
    echo   • Account registration & login
    echo   • Character creation (up to 2 per account)
    echo   • Password reset
    echo   • Dashboard & account management
    echo   • One-click game launch
    echo.
    pause
    goto :menu
)

if "%choice%"=="7" (
    goto :end
)

echo.
echo Invalid choice. Please try again.
echo.
pause
goto :menu

:menu
cls
color 0B
title Wonderland Online - Quick Start
goto :start

:end
color 07
echo.
echo Thank you for playing Wonderland Online!
echo.
exit /b 0
