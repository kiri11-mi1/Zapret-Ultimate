# Скрипт для копирования ресурсов из Smart-Zapret-Launcher
$SourcePath = "C:\Users\kirill\pet-projects\Smart-Zapret-Launcher"
$DestPath = "C:\Users\kirill\pet-projects\zapret-ultimate"
$OutputPath = "$DestPath\src\ZapretUltimate\bin\Debug\net8.0-windows"

# Создаем директории
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
        Write-Host "Created: $dir"
    }
}

# Копируем бинарные файлы
Write-Host "Copying bin files..."
Copy-Item "$SourcePath\bin\*" "$OutputPath\bin\" -Recurse -Force

# Копируем конфиги
Write-Host "Copying configs..."
Copy-Item "$SourcePath\configs\discord\*" "$OutputPath\configs\discord\" -Force
Copy-Item "$SourcePath\configs\youtube_twitch\*" "$OutputPath\configs\youtube_twitch\" -Force
Copy-Item "$SourcePath\configs\gaming\*" "$OutputPath\configs\gaming\" -Force
Copy-Item "$SourcePath\configs\universal\*" "$OutputPath\configs\universal\" -Force

# Копируем списки
Write-Host "Copying lists..."
Copy-Item "$SourcePath\lists\*" "$OutputPath\lists\" -Force

Write-Host "Done! Assets copied to $OutputPath"
