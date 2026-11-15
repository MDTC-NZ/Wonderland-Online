@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "UPDATER=%SCRIPT_DIR%bin\Release\WloClientUpdater.exe"
set "MANIFEST_URL=http://127.0.0.1:8080/client-updates/manifest.json"
set "GAME_DIR=C:\Program Files (x86)\Wonderland Online"

if not exist "%UPDATER%" (
    echo WloClientUpdater.exe was not found. Build the Release configuration first.
    pause
    exit /b 1
)

"%UPDATER%" --manifest-url "%MANIFEST_URL%" --install-dir "%GAME_DIR%"
if errorlevel 1 pause
