$ErrorActionPreference = "Stop"
$ServiceName = "DNSAgent"
Write-Host "--- DNS Agent Uninstaller ---" -ForegroundColor Cyan

# 1. Stop and Remove Service
if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Stopping service..." -ForegroundColor Yellow
    Stop-Service $ServiceName -Force -ErrorAction SilentlyContinue
    & sc.exe stop $ServiceName 2>$null
    Start-Sleep -Seconds 1
    
    Write-Host "Removing service..." -ForegroundColor Yellow
    & sc.exe delete $ServiceName 2>$null
    Start-Sleep -Seconds 1
}
else {
    Write-Host "Service '$ServiceName' not found. Skipping removal." -ForegroundColor Gray
}

# 2. Kill associated processes
Write-Host "Cleaning up processes..." -ForegroundColor Yellow
try {
    & taskkill.exe /F /IM "DNSAgent.Service.exe" /T 2>$null
    & taskkill.exe /F /IM "DNSAgent.Tray.exe" /T 2>$null
}
catch {}

# 3. Remove Firewall Rules
Write-Host "Removing Firewall rules..." -ForegroundColor Yellow
Remove-NetFirewallRule -DisplayName "DNS Agent*" -ErrorAction SilentlyContinue

# 4. Remove Startup Shortcut
Write-Host "Removing startup shortcut..." -ForegroundColor Yellow
$startupFolder = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupFolder "DNSAgentTray.lnk"
if (Test-Path $shortcutPath) {
    Remove-Item $shortcutPath -Force
}

Write-Host "`nDNS Agent has been uninstalled successfully." -ForegroundColor Green
Write-Host "Note: Your database (dnsagent.db) and configuration files have been kept in this folder." -ForegroundColor Gray
pause
