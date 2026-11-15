@echo off
REM Build Script for Wonderland Online Private Server
REM This script builds the project when Build Tools are installed

setlocal enabledelayedexpansion

echo.
echo ╔════════════════════════════════════════════════════════════╗
echo ║  Wonderland Online - Build Script                          ║
echo ║  Compiling with Visual Studio Build Tools...              ║
echo ╚════════════════════════════════════════════════════════════╝
echo.

REM Try to find MSBuild
set MSBUILD_PATHS=^
  "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ^
  "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" ^
  "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" ^
  "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"

set MSBUILD=

for %%p in (%MSBUILD_PATHS%) do (
  if exist %%p (
    set MSBUILD=%%p
    goto found_msbuild
  )
)

:found_msbuild
if "%MSBUILD%"=="" (
  echo.
  echo ❌ ERROR: MSBuild not found!
  echo.
  echo Visual Studio Build Tools may not be installed yet.
  echo Please install: https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022
  echo.
  echo Or install Visual Studio Community Edition:
  echo https://visualstudio.microsoft.com/vs/community/
  echo.
  pause
  exit /b 1
)

echo ✓ Found MSBuild: %MSBUILD%
echo.

REM Find NuGet
if not exist "nuget.exe" (
  echo ❌ ERROR: nuget.exe not found!
  echo Please ensure nuget.exe is in the current directory.
  pause
  exit /b 1
)

echo ✓ Found NuGet: nuget.exe
echo.

REM Restore NuGet packages
echo Restoring NuGet packages...
call nuget.exe restore "Wonderland Private Server.sln"
if %ERRORLEVEL% neq 0 (
  echo ❌ NuGet restore failed!
  pause
  exit /b 1
)
echo ✓ Packages restored
echo.

REM Build solution
echo Building solution...
echo (This may take several minutes)
echo.

"%MSBUILD%" "Wonderland Private Server.sln" ^
  /p:Configuration=Release ^
  /p:Platform=AnyCPU ^
  /m /nologo

if %ERRORLEVEL% neq 0 (
  echo.
  echo ❌ Build failed!
  echo Please check the error messages above.
  pause
  exit /b 1
)

echo.
echo ✓ Build completed successfully!
echo.

REM Display output location
if exist "bin\Release" (
  echo Build output:
  echo   Location: bin\Release
  echo.
  echo Next steps:
  echo   1. The server application is ready in bin\Release
  echo   2. Configure DNS for wlo.kiaoragaming.xyz
  echo   3. Set up firewall rules for ports 8080 and 7777
  echo   4. Run the server application
  echo   5. Visit http://localhost:8080 to access registration and dashboard
  echo.
)

pause
