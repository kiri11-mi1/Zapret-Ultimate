# Build script for Zapret Ultimate
param(
    [switch]$Release,
    [switch]$Publish,
    [switch]$CopyAssets
)

$ErrorActionPreference = "Stop"
$ProjectPath = "$PSScriptRoot\src\ZapretUltimate\ZapretUltimate.csproj"
$SourceProject = "C:\Users\kirill\pet-projects\Smart-Zapret-Launcher"

Write-Host "=== Zapret Ultimate Build Script ===" -ForegroundColor Cyan

# Check dotnet
try {
    $dotnetVersion = dotnet --version
    Write-Host "Using .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

# Restore packages
Write-Host "`nRestoring packages..." -ForegroundColor Yellow
dotnet restore $ProjectPath

if ($Release -or $Publish) {
    $Configuration = "Release"
} else {
    $Configuration = "Debug"
}

# Build
Write-Host "`nBuilding ($Configuration)..." -ForegroundColor Yellow
dotnet build $ProjectPath -c $Configuration

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

$OutputPath = "$PSScriptRoot\src\ZapretUltimate\bin\$Configuration\net8.0-windows"

# Copy assets if requested
if ($CopyAssets) {
    Write-Host "`nCopying assets from Smart-Zapret-Launcher..." -ForegroundColor Yellow

    # Create directories
    $dirs = @(
        "$OutputPath\bin",
        "$OutputPath\configs\discord",
        "$OutputPath\configs\youtube_twitch",
        "$OutputPath\configs\gaming",
        "$OutputPath\configs\universal",
        "$OutputPath\lists",
        "$OutputPath\Resources"
    )

    foreach ($dir in $dirs) {
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
    }

    # Copy files
    Copy-Item "$SourceProject\bin\*" "$OutputPath\bin\" -Recurse -Force
    Copy-Item "$SourceProject\configs\discord\*" "$OutputPath\configs\discord\" -Force
    Copy-Item "$SourceProject\configs\youtube_twitch\*" "$OutputPath\configs\youtube_twitch\" -Force
    Copy-Item "$SourceProject\configs\gaming\*" "$OutputPath\configs\gaming\" -Force
    Copy-Item "$SourceProject\configs\universal\*" "$OutputPath\configs\universal\" -Force
    Copy-Item "$SourceProject\lists\*" "$OutputPath\lists\" -Force

    Write-Host "Assets copied successfully!" -ForegroundColor Green
}

# Publish
if ($Publish) {
    Write-Host "`nPublishing self-contained application..." -ForegroundColor Yellow

    $PublishPath = "$PSScriptRoot\publish"
    dotnet publish $ProjectPath -c Release -r win-x64 --self-contained -o $PublishPath

    if ($LASTEXITCODE -eq 0) {
        # Copy assets to publish folder
        $dirs = @(
            "$PublishPath\bin",
            "$PublishPath\configs\discord",
            "$PublishPath\configs\youtube_twitch",
            "$PublishPath\configs\gaming",
            "$PublishPath\configs\universal",
            "$PublishPath\lists"
        )

        foreach ($dir in $dirs) {
            if (-not (Test-Path $dir)) {
                New-Item -ItemType Directory -Path $dir -Force | Out-Null
            }
        }

        Copy-Item "$SourceProject\bin\*" "$PublishPath\bin\" -Recurse -Force
        Copy-Item "$SourceProject\configs\discord\*" "$PublishPath\configs\discord\" -Force
        Copy-Item "$SourceProject\configs\youtube_twitch\*" "$PublishPath\configs\youtube_twitch\" -Force
        Copy-Item "$SourceProject\configs\gaming\*" "$PublishPath\configs\gaming\" -Force
        Copy-Item "$SourceProject\configs\universal\*" "$PublishPath\configs\universal\" -Force
        Copy-Item "$SourceProject\lists\*" "$PublishPath\lists\" -Force

        Write-Host "`nPublished to: $PublishPath" -ForegroundColor Green
    }
}

Write-Host "`nBuild completed!" -ForegroundColor Green
Write-Host "Output: $OutputPath" -ForegroundColor Cyan

if (-not $CopyAssets) {
    Write-Host "`nNote: Run with -CopyAssets to copy Zapret binaries and configs" -ForegroundColor Yellow
}
