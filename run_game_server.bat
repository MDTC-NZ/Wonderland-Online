@echo off
REM Wonderland Online - Game Server Launcher
REM Keep the window open after Python exits

setlocal enabledelayedexpansion

cd /d "%~dp0"

echo.
echo ======================================
echo   WONDERLAND ONLINE - GAME SERVER
echo ======================================
echo.

:retry
echo Starting game server...
python game_server.py

echo.
echo ⚠️ Game server has stopped.
echo Waiting 5 seconds before restart...
timeout /t 5

goto retry
