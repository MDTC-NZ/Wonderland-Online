@echo off
REM Wonderland Online - Registration Server Launcher
REM Keep the window open after Python exits

setlocal enabledelayedexpansion

cd /d "%~dp0"

echo.
echo ======================================
echo   WONDERLAND ONLINE - REGISTRATION
echo ======================================
echo.

:retry
echo Starting registration server...
python registration_server.py

echo.
echo ⚠️ Registration server has stopped.
echo Waiting 5 seconds before restart...
timeout /t 5

goto retry
