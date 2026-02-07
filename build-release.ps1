# Build Release Script for Zapret Ultimate
# This script builds the project and prepares files for the installer

param(
    [string]$Version = "1.0.0",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$ProjectRoot = $PSScriptRoot
$SrcPath = Join-Path $ProjectRoot "src\ZapretUltimate"
$OutputPath = Join-Path $ProjectRoot "release"
$PublishPath = Join-Path $OutputPath "publish"

Write-Host "=== Zapret Ultimate Release Build ===" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Yellow

# Clean previous build
if (Test-Path $OutputPath) {
    Write-Host "Cleaning previous build..." -ForegroundColor Gray
    Remove-Item -Recurse -Force $OutputPath
}

New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null

# Build project
if (-not $SkipBuild) {
    Write-Host "`nBuilding project..." -ForegroundColor Cyan

    Push-Location $SrcPath
    try {
        # Self-contained build for Windows x64
        dotnet publish -c Release -r win-x64 --self-contained -o $PublishPath

        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
    }

    Write-Host "Build completed successfully!" -ForegroundColor Green
}

# Copy required assets
Write-Host "`nCopying assets..." -ForegroundColor Cyan

$AssetsSource = Join-Path $SrcPath "bin\Debug\net8.0-windows"

# Copy bin folder (winws.exe, dlls, etc.)
$BinSource = Join-Path $AssetsSource "bin"
$BinDest = Join-Path $PublishPath "bin"
if (Test-Path $BinSource) {
    Write-Host "  Copying bin folder..." -ForegroundColor Gray
    Copy-Item -Recurse -Force $BinSource $BinDest
} else {
    Write-Host "  WARNING: bin folder not found at $BinSource" -ForegroundColor Yellow
}

# Copy configs folder
$ConfigsSource = Join-Path $AssetsSource "configs"
$ConfigsDest = Join-Path $PublishPath "configs"
if (Test-Path $ConfigsSource) {
    Write-Host "  Copying configs folder..." -ForegroundColor Gray
    Copy-Item -Recurse -Force $ConfigsSource $ConfigsDest
} else {
    Write-Host "  WARNING: configs folder not found at $ConfigsSource" -ForegroundColor Yellow
}

# Copy lists folder
$ListsSource = Join-Path $AssetsSource "lists"
$ListsDest = Join-Path $PublishPath "lists"
if (Test-Path $ListsSource) {
    Write-Host "  Copying lists folder..." -ForegroundColor Gray
    Copy-Item -Recurse -Force $ListsSource $ListsDest
} else {
    Write-Host "  WARNING: lists folder not found at $ListsSource" -ForegroundColor Yellow
}

Write-Host "`nRelease files prepared at: $PublishPath" -ForegroundColor Green

# Check if Inno Setup is installed
$InnoSetupPath = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($InnoSetupPath) {
    Write-Host "`nBuilding installer with Inno Setup..." -ForegroundColor Cyan

    $IssPath = Join-Path $ProjectRoot "installer.iss"

    if (Test-Path $IssPath) {
        & $InnoSetupPath /DAppVersion=$Version $IssPath

        if ($LASTEXITCODE -eq 0) {
            Write-Host "Installer created successfully!" -ForegroundColor Green
        } else {
            Write-Host "Installer build failed!" -ForegroundColor Red
        }
    } else {
        Write-Host "installer.iss not found. Skipping installer build." -ForegroundColor Yellow
    }
} else {
    Write-Host "`nInno Setup not found. Install from: https://jrsoftware.org/isinfo.php" -ForegroundColor Yellow
    Write-Host "Installer build skipped." -ForegroundColor Yellow
}

Write-Host "`n=== Build Complete ===" -ForegroundColor Cyan
