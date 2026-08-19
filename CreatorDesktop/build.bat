@echo off
echo Building CreatorDesktop.exe (self-contained, single-file)...
echo.

REM Kill any running instances that would lock the EXE
taskkill /F /IM CreatorDesktop.exe >nul 2>&1

REM Clean old build artifacts in case they're stale
if exist bin\Release rmdir /S /Q bin\Release 2>nul
if exist obj\Release rmdir /S /Q obj\Release 2>nul

dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
if errorlevel 1 (
    echo.
    echo BUILD FAILED. Stelle sicher, dass .NET 8 SDK installiert ist:
    echo   https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)
echo.
echo FERTIG. Die EXE liegt unter:
echo   bin\Release\net8.0-windows\win-x64\publish\CreatorDesktop.exe
echo.
echo Diese Datei kannst du auf den Desktop kopieren und doppelklicken.
pause
