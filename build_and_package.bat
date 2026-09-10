@echo off
echo ===================================================
echo Building and Packaging Announcement Utility
echo ===================================================

echo [1/3] Publishing Announcement project for Windows x64...
dotnet publish "%~dp0Announcement.csproj" -c Release -r win-x64 --self-contained false
if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] dotnet publish failed. Build aborted!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/3] Compressing and packaging assets...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0package.ps1"

echo.
echo [3/3] Done! The deployable packages have been created at:
echo 1. Production Package: %~dp0bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\EnergySavingAlert_Package.zip
echo 2. Test Package:       %~dp0bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\EnergySavingAlert_Test_Package.zip
echo 3. Standalone App:     %~dp0bin\Release\net8.0-windows10.0.17763.0\win-x64\publish\Announcement.zip
echo.
