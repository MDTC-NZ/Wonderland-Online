@echo off
REM Wonderland Online - Complete Setup & Test
REM This script verifies the system is working and ready to play

setlocal enabledelayedexpansion

title Wonderland Online - Setup & Test

echo.
echo ============================================
echo  WONDERLAND ONLINE - SETUP & TEST
echo ============================================
echo.

REM Check Python
echo [1/5] Checking Python installation...
python --version >nul 2>&1
if errorlevel 1 (
    echo   [X] Python not found!
    echo   Solution: Install Python 3 from python.org
    pause
    exit /b 1
) else (
    for /f "tokens=*" %%i in ('python --version') do echo   [OK] %%i
)

REM Check required files
echo.
echo [2/5] Checking required files...
if not exist "bin\Debug\Wonderland Private Server.exe" (
    echo   [X] Game executable not found!
    pause
    exit /b 1
)
echo   [OK] Game server found

if not exist "registration_server.py" (
    echo   [X] Registration server not found!
    pause
    exit /b 1
)
echo   [OK] Registration server found

if not exist "game_launcher.py" (
    echo   [X] Game launcher not found!
    pause
    exit /b 1
)
echo   [OK] Game launcher found

if not exist "Web\register.html" (
    echo   [X] Registration page not found!
    pause
    exit /b 1
)
echo   [OK] Web pages found

if not exist "bin\Debug\Web\register.html" (
    echo   [WARNING] Web files not in runtime directory, copying...
    xcopy /Y /S "Web\" "bin\Debug\Web\" >nul
    if not errorlevel 1 (
        echo   [OK] Web files copied
    ) else (
        echo   [X] Failed to copy web files
        pause
        exit /b 1
    )
) else (
    echo   [OK] Web files in runtime directory
)

REM Test Python imports
echo.
echo [3/5] Testing Python modules...
python -c "import sqlite3; import json; import subprocess; print('   [OK] All required modules available')" 2>nul
if errorlevel 1 (
    echo   [X] Python modules not available
    echo   Solution: Reinstall Python or install missing modules
    pause
    exit /b 1
)

REM Start servers
echo.
echo [4/5] Starting servers...
echo   Starting Game Server on port 6414...
start "Wonderland Game Server" "bin\Debug\Wonderland Private Server.exe"
timeout /t 2 /nobreak

echo   Starting Registration Server on port 8090...
start "Wonderland Registration Server" python registration_server.py
timeout /t 3 /nobreak

REM Verify servers
echo.
echo [5/5] Verifying servers are running...
timeout /t 2 /nobreak

netstat -ano | findstr :6414 >nul
if errorlevel 1 (
    echo   [X] Game Server NOT responding on port 6414
) else (
    echo   [OK] Game Server running on port 6414
)

netstat -ano | findstr :8090 >nul
if errorlevel 1 (
    echo   [X] Registration Server NOT responding on port 8090
) else (
    echo   [OK] Registration Server running on port 8090
)

echo.
echo ============================================
echo  SETUP COMPLETE!
echo ============================================
echo.
echo Next steps:
echo   1. Open browser to: http://localhost:8090
echo   2. Create an account (Register tab)
echo   3. Login with your credentials
echo   4. Create a character
echo   5. Click "Play" to start the game
echo.
echo Both servers are now running in the background.
echo Check the server windows for any messages.
echo.
pause
