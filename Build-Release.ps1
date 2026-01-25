# DNS Agent - Build and Release Script
# This script automates the publishing and packaging of DNS Agent v1.2

$ErrorActionPreference = "Stop"
$Version = "2.1.0"
$ReleaseName = "DNSAgent_V2.1"
$ProjectRoot = Get-Location
$ReleasePath = Join-Path $ProjectRoot "Release"
$DistPath = Join-Path $ReleasePath "Dist"
$TempService = Join-Path $ReleasePath "TempService"
$TempTray = Join-Path $ReleasePath "TempTray"

Write-Host "--- DNS Agent v$Version Build & Release ---" -ForegroundColor Cyan

# 1. Cleanup old release folders
if (Test-Path $ReleasePath) {
    Write-Host "Cleaning up old release folder..." -ForegroundColor Yellow
    Remove-Item $ReleasePath -Recurse -Force
}
New-Item -Path $DistPath -ItemType Directory -Force | Out-Null
New-Item -Path $TempService -ItemType Directory -Force | Out-Null
New-Item -Path $TempTray -ItemType Directory -Force | Out-Null

# 2. Build and Publish DNSAgent.Service
Write-Host "Publishing DNSAgent.Service..." -ForegroundColor Yellow
dotnet publish "DNSAgent.Service\DNSAgent.Service.csproj" -c Release -o "$TempService" --self-contained false
Copy-Item "$TempService\*" -Destination "$DistPath\" -Recurse -Force

# 3. Build and Publish DNSAgent.Tray
Write-Host "Publishing DNSAgent.Tray..." -ForegroundColor Yellow
dotnet publish "DNSAgent.Tray\DNSAgent.Tray.csproj" -c Release -o "$TempTray" --self-contained false
Copy-Item "$TempTray\*" -Destination "$DistPath\" -Recurse -Force

# 4. Copy Setup Scripts to Dist
Write-Host "Copying setup scripts..." -ForegroundColor Yellow
Copy-Item "Setup-DNSAgent.ps1" -Destination "$DistPath\"
Copy-Item "Start-Setup.bat" -Destination "$DistPath\"

if (Test-Path "DNSAgent.Service\install-service.ps1") {
    Copy-Item "DNSAgent.Service\install-service.ps1" -Destination "$DistPath\"
}

# 5. Copy Extension
Write-Host "Copying Browser Extension..." -ForegroundColor Yellow
$ExtDistPath = Join-Path $DistPath "extension"
New-Item -Path $ExtDistPath -ItemType Directory -Force | Out-Null
Copy-Item "extension\*" -Destination "$ExtDistPath\" -Recurse -Exclude ".git*"

# 6. Cleanup Temp Folders
Remove-Item $TempService -Recurse -Force
Remove-Item $TempTray -Recurse -Force

# 6. Create ZIP Archive
Write-Host "Creating $ReleaseName.zip..." -ForegroundColor Green
$ZipFile = Join-Path $ReleasePath "$ReleaseName.zip"
Compress-Archive -Path "$DistPath\*" -DestinationPath $ZipFile -Force

Write-Host "`nRelease package created successfully!" -ForegroundColor Green
Write-Host "Folder: $ReleasePath" -ForegroundColor Cyan
Write-Host "Archive: $ZipFile" -ForegroundColor Cyan

Write-Host "`nBuild complete. Ready for distribution!" -ForegroundColor Green
