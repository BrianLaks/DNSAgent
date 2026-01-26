# DNS Agent - Build and Release Script
# This script automates the publishing and packaging of DNS Agent

$ErrorActionPreference = "Stop"
$Version = "2.3.8"
$ReleaseName = "DNSAgent_V$Version"
$ProjectRoot = Get-Location
$ReleasePath = Join-Path $ProjectRoot "Release"
$DistPath = Join-Path $ReleasePath "Dist"
$TempService = Join-Path $ReleasePath "TempService"
$TempTray = Join-Path $ReleasePath "TempTray"
$TempWeb = Join-Path $ReleasePath "TempWeb"

Write-Host "--- DNS Agent v$Version Build & Release ---" -ForegroundColor Cyan

# 0. Forced Zombie Extermination (Build Safety)
Write-Host "Clearing build locks..." -ForegroundColor Yellow
try {
    Get-Process -Name "DNSAgent*" -ErrorAction SilentlyContinue | Stop-Process -Force
    & taskkill.exe /F /IM "DNSAgent.Service.exe" /T 2>$null
    & taskkill.exe /F /IM "DNSAgent.Tray.exe" /T 2>$null
    & taskkill.exe /F /IM "dotnet.exe" /T 2>$null
}
catch {}
Start-Sleep -Seconds 1

# 1. Cleanup old release folders
if (Test-Path $ReleasePath) {
    Write-Host "Cleaning up old release folders (keeping artifacts)..." -ForegroundColor Yellow
    # Only remove Dist and Temp folders, keep existing .zip files
    Get-ChildItem $ReleasePath -Directory | Remove-Item -Recurse -Force
}
New-Item -Path $DistPath -ItemType Directory -Force | Out-Null
New-Item -Path $TempService -ItemType Directory -Force | Out-Null
New-Item -Path $TempTray -ItemType Directory -Force | Out-Null
New-Item -Path $TempWeb -ItemType Directory -Force | Out-Null

# 2. Build and Publish DNSAgent.Web (UI Dashboard)
Write-Host "Publishing DNSAgent.Web..." -ForegroundColor Yellow
dotnet publish "DNSAgent.Web\DNSAgent.Web.csproj" -c Release -o "$TempWeb" --self-contained false
# Assets from Web go into Dist folder
Copy-Item "$TempWeb\*" -Destination "$DistPath\" -Recurse -Force

# 3. Build and Publish DNSAgent.Service
Write-Host "Publishing DNSAgent.Service..." -ForegroundColor Yellow
dotnet publish "DNSAgent.Service\DNSAgent.Service.csproj" -c Release -o "$TempService" --self-contained false
Copy-Item "$TempService\*" -Destination "$DistPath\" -Recurse -Force

# 4. Build and Publish DNSAgent.Tray
Write-Host "Publishing DNSAgent.Tray..." -ForegroundColor Yellow
dotnet publish "DNSAgent.Tray\DNSAgent.Tray.csproj" -c Release -o "$TempTray" --self-contained false
Copy-Item "$TempTray\*" -Destination "$DistPath\" -Recurse -Force

# 5. Copy Scripts to Dist
Write-Host "Copying setup scripts..." -ForegroundColor Yellow
Copy-Item "Setup-DNSAgent.ps1" -Destination "$DistPath\"
Copy-Item "Start-Setup.bat" -Destination "$DistPath\"
Copy-Item "Toggle-DNS.bat" -Destination "$DistPath\"
Copy-Item "Toggle-DNS.ps1" -Destination "$DistPath\"

if (Test-Path "DNSAgent.Service\install-service.ps1") {
    Copy-Item "DNSAgent.Service\install-service.ps1" -Destination "$DistPath\"
}

# 6. Copy Extension
Write-Host "Copying Browser Extension..." -ForegroundColor Yellow
$ExtDistPath = Join-Path $DistPath "extension"
New-Item -Path $ExtDistPath -ItemType Directory -Force | Out-Null
Copy-Item "extension\*" -Destination "$ExtDistPath\" -Recurse -Exclude ".git*"

# 7. Cleanup Temp Folders
Write-Host "Cleaning up temp folders..." -ForegroundColor Yellow
Remove-Item $TempService -Recurse -Force
Remove-Item $TempTray -Recurse -Force
Remove-Item $TempWeb -Recurse -Force

# 8. Create ZIP Archive
Write-Host "Calculating expected payload size..." -ForegroundColor Yellow
$DistSize = (Get-ChildItem $DistPath -Recurse | Measure-Object -Property Length -Sum).Sum
$FileCount = (Get-ChildItem $DistPath -Recurse | Where-Object { !$_.PSIsContainer }).Count
Write-Host "Payload: $($DistSize / 1MB) MB ($FileCount files)" -ForegroundColor Cyan

Write-Host "Creating $ReleaseName.zip..." -ForegroundColor Green
$ZipFile = Join-Path $ReleasePath "$ReleaseName.zip"
if (Test-Path $ZipFile) { Remove-Item $ZipFile -Force }
Compress-Archive -Path "$DistPath\*" -DestinationPath $ZipFile -Force

# 9. Integrity Validation (STRICT)
Write-Host "Validating ZIP integrity..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
$ZipSize = (Get-Item $ZipFile).Length
Write-Host "Final ZIP Size: $($ZipSize / 1MB) MB" -ForegroundColor Cyan

if ($ZipSize -lt 35MB) {
    Write-Host "CRITICAL ERROR: ZIP size discrepancy detected ($($ZipSize / 1MB) MB)." -ForegroundColor Red
    Write-Host "Expected > 35MB for a complete DNS Agent release." -ForegroundColor Red
    Write-Host "Build invalidated to prevent corrupt distribution." -ForegroundColor Red
    exit 1
}

Write-Host "ZIP Integrity Verified! Ready for Git." -ForegroundColor Green
Write-Host "Archive: $ZipFile" -ForegroundColor Cyan
Write-Host "`nBuild complete. Ready for distribution!" -ForegroundColor Green
