@echo off
REM Start the Wonderland Online Merged Server

cd /d "%~dp0"

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║  Wonderland Online - Merged Server                         ║
echo ║  Starting with game assets and registration system...      ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

echo Launching server...
echo.

cd /d "%~dp0bin\Debug"
"Wonderland Private Server.exe"

if %ERRORLEVEL% neq 0 (
    echo.
    echo ERROR: Server failed to start with error code: %ERRORLEVEL%
    echo.
)

pause
