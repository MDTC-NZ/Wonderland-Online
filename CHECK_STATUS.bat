@echo off
REM Wonderland Online - Server Status & Diagnostics
REM Run this script to check if both servers are working

setlocal enabledelayedexpansion

title Wonderland Server Status

echo.
echo =====================================
echo  WONDERLAND ONLINE - SERVER STATUS
echo =====================================
echo.

REM Check Game Server
echo Checking Game Server (Port 6414)...
netstat -ano | findstr :6414 >nul
if errorlevel 1 (
    echo   [X] Game Server NOT RUNNING
) else (
    echo   [OK] Game Server is listening on port 6414
)

REM Check Registration Server  
echo.
echo Checking Registration Server (Port 8080)...
netstat -ano | findstr :8080 >nul
if errorlevel 1 (
    echo   [X] Registration Server NOT RUNNING
) else (
    echo   [OK] Registration Server is listening on port 8080
)

REM Check Python
echo.
echo Checking Python installation...
python --version >nul 2>&1
if errorlevel 1 (
    echo   [!] Python not found in PATH
    echo   Solution: Install Python 3 or add to PATH
) else (
    for /f "tokens=*" %%i in ('python --version') do echo   [OK] %%i
)

REM Check network connectivity
echo.
echo Testing HTTP connectivity...
powershell -Command "try { Invoke-WebRequest -Uri 'http://localhost:8080/' -TimeoutSec 2 -ErrorAction Stop | Out-Null; Write-Host '   [OK] Registration server responding to HTTP'; exit 0 } catch { Write-Host '   [X] Cannot connect to registration server'; exit 1 }"

echo.
echo =====================================
echo  SOLUTIONS
echo =====================================

echo.
echo IF Game Server is NOT running:
echo   1. Start Windows Command Prompt (as Administrator)
echo   2. Navigate to: C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop
echo   3. Run: bin\Debug\Wonderland Private Server.exe
echo.
echo IF Registration Server is NOT running:
echo   1. Start Windows Command Prompt
echo   2. Navigate to: C:\Users\hayde\Downloads\Wonderland-Develop\Wonderland-Develop
echo   3. Run: python registration_server.py
echo.
echo IF Python is not found:
echo   1. Download Python 3 from python.org
echo   2. Run installer (check "Add Python to PATH")
echo   3. Restart Command Prompt
echo.
echo FASTEST WAY TO START BOTH:
echo   Double-click: START_SERVERS.bat
echo.
echo =====================================

pause
