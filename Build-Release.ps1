# DNS Agent - Build and Release Script
# This script automates the publishing and packaging of DNS Agent

$ErrorActionPreference = "Stop"
$Version = "2.4.1"
$ReleaseName = "DNSAgent_V$Version"
$ProjectRoot = Get-Location
$ReleasePath = Join-Path $ProjectRoot "Release"
$StagingRoot = Join-Path $ProjectRoot "_BuildStaging"
$DistPath = Join-Path $StagingRoot "Dist"
$TempService = Join-Path $StagingRoot "TempService"
$TempTray = Join-Path $StagingRoot "TempTray"
$TempWeb = Join-Path $StagingRoot "TempWeb"

Add-Type -AssemblyName System.IO.Compression.FileSystem

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

# 1. Cleanup staging
if (Test-Path $StagingRoot) {
    Write-Host "Cleaning up staging folder..." -ForegroundColor Yellow
    try { Remove-Item $StagingRoot -Recurse -Force -ErrorAction SilentlyContinue } catch {}
}
if (!(Test-Path $ReleasePath)) { New-Item $ReleasePath -ItemType Directory -Force | Out-Null }
New-Item -Path $StagingRoot -ItemType Directory -Force | Out-Null
New-Item -Path $DistPath -ItemType Directory -Force | Out-Null
New-Item -Path $TempService -ItemType Directory -Force | Out-Null
New-Item -Path $TempTray -ItemType Directory -Force | Out-Null
New-Item -Path $TempWeb -ItemType Directory -Force | Out-Null

# 2. Build and Publish DNSAgent.Web (UI Dashboard)
Write-Host "Publishing DNSAgent.Web..." -ForegroundColor Yellow
dotnet publish "DNSAgent.Web\DNSAgent.Web.csproj" -c Release -o "$TempWeb" --self-contained false
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

# 7. Package Extension separately for direct download
Write-Host "Creating separate Extension package..." -ForegroundColor Yellow
$ExtensionZip = Join-Path $ReleasePath "DNSAgent_Extension_v$Version.zip"
if (Test-Path $ExtensionZip) { Remove-Item $ExtensionZip -Force }
[System.IO.Compression.ZipFile]::CreateFromDirectory($ExtDistPath, $ExtensionZip, [System.IO.Compression.CompressionLevel]::Optimal, $false)

# Copy extension zip to Service assets for direct dashboard download
$ServiceAssets = Join-Path $ProjectRoot "DNSAgent.Service\wwwroot\assets"
if (!(Test-Path $ServiceAssets)) { New-Item -Path $ServiceAssets -ItemType Directory -Force | Out-Null }
Copy-Item $ExtensionZip -Destination "$ServiceAssets\" -Force

# 8. Cleanup Staging Folder
Write-Host "Cleaning up staging folder (non-fatal)..." -ForegroundColor Yellow
try {
    # Attempt to kill any potential lockers again
    & taskkill.exe /F /IM "dotnet.exe" /T 2>$null
    & taskkill.exe /F /IM "DNSAgent.*" /T 2>$null
    Start-Sleep -Seconds 1
    Remove-Item $StagingRoot -Recurse -Force -ErrorAction SilentlyContinue 
}
catch {
    Write-Host "Warning: Staging folder could not be fully cleaned. This can be ignored." -ForegroundColor Gray
}

# 8. Create ZIP Archive (Using robust .NET method)
$ZipFile = Join-Path $ReleasePath "$ReleaseName.zip"
if (Test-Path $ZipFile) { Remove-Item $ZipFile -Force }

Write-Host "Calculating payload size..." -ForegroundColor Yellow
$DistFiles = Get-ChildItem $DistPath -Recurse | Where-Object { !$_.PSIsContainer }
$TotalBytes = ($DistFiles | Measure-Object -Property Length -Sum).Sum
Write-Host "Payload: $($TotalBytes / 1MB) MB ($($DistFiles.Count) files)" -ForegroundColor Cyan

Write-Host "Compressing to $ReleaseName.zip..." -ForegroundColor Green
[System.IO.Compression.ZipFile]::CreateFromDirectory($DistPath, $ZipFile, [System.IO.Compression.CompressionLevel]::Optimal, $false)

# 9. Integrity Validation (STRICT)
Write-Host "Validating ZIP integrity..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
if (!(Test-Path $ZipFile)) { 
    Write-Host "CRITICAL ERROR: ZIP file was not created!" -ForegroundColor Red
    exit 1 
}
$ZipSize = (Get-Item $ZipFile).Length
Write-Host "Final ZIP Size: $($ZipSize / 1MB) MB" -ForegroundColor Green

if ($ZipSize -lt 35MB) {
    Write-Host "CRITICAL ERROR: ZIP size discrepancy detected ($($ZipSize / 1MB) MB)." -ForegroundColor Red
    Write-Host "Expected > 35MB for a complete DNS Agent release." -ForegroundColor Red
    Write-Host "This build is INVALID." -ForegroundColor Red
    # List top 10 largest files to help debug
    $DistFiles | Sort-Object Length -Descending | Select-Object FullName, Length -First 10
    exit 1
}

Write-Host "`nRelease package created successfully!" -ForegroundColor Green
Write-Host "Archive: $ZipFile" -ForegroundColor Cyan
Write-Host "`nBuild complete. Ready for distribution!" -ForegroundColor Green
