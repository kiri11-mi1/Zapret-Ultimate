@echo off
chcp 65001 >nul
setlocal

echo === Zapret Ultimate Build Script ===

:: Check dotnet
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo ERROR: .NET SDK not found!
    echo.
    echo Please install .NET 8.0 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

echo.
echo Restoring packages...
dotnet restore src\ZapretUltimate\ZapretUltimate.csproj

echo.
echo Building...
dotnet build src\ZapretUltimate\ZapretUltimate.csproj -c Debug

if %errorlevel% neq 0 (
    echo.
    echo Build failed!
    pause
    exit /b 1
)

set OUTPUT=src\ZapretUltimate\bin\Debug\net8.0-windows
set SOURCE=C:\Users\kirill\pet-projects\Smart-Zapret-Launcher

echo.
echo Copying assets from Smart-Zapret-Launcher...

:: Create directories
if not exist "%OUTPUT%\bin" mkdir "%OUTPUT%\bin"
if not exist "%OUTPUT%\configs\discord" mkdir "%OUTPUT%\configs\discord"
if not exist "%OUTPUT%\configs\youtube_twitch" mkdir "%OUTPUT%\configs\youtube_twitch"
if not exist "%OUTPUT%\configs\gaming" mkdir "%OUTPUT%\configs\gaming"
if not exist "%OUTPUT%\configs\universal" mkdir "%OUTPUT%\configs\universal"
if not exist "%OUTPUT%\lists" mkdir "%OUTPUT%\lists"

:: Copy files
xcopy "%SOURCE%\bin\*" "%OUTPUT%\bin\" /E /Y /Q
xcopy "%SOURCE%\configs\discord\*" "%OUTPUT%\configs\discord\" /Y /Q
xcopy "%SOURCE%\configs\youtube_twitch\*" "%OUTPUT%\configs\youtube_twitch\" /Y /Q
xcopy "%SOURCE%\configs\gaming\*" "%OUTPUT%\configs\gaming\" /Y /Q
xcopy "%SOURCE%\configs\universal\*" "%OUTPUT%\configs\universal\" /Y /Q
xcopy "%SOURCE%\lists\*" "%OUTPUT%\lists\" /Y /Q

echo.
echo ========================================
echo Build completed!
echo Output: %OUTPUT%
echo ========================================
echo.
echo To run: %OUTPUT%\ZapretUltimate.exe
echo.
pause
