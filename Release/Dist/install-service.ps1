$ErrorActionPreference = "Stop"
$ServiceName = "DNSAgent"
$DisplayName = "DNS Agent - Network Ad Blocker"

# --- FIX: Convert location to a STRING to avoid the "type PathInfo" error ---
$CurrentDir = (Get-Location).Path
$BinaryPath = Join-Path $CurrentDir "DNSAgent.Service.exe"
$TrayPath = Join-Path $CurrentDir "DNSAgent.Tray.exe"
$WwwRoot = Join-Path $CurrentDir "wwwroot"

Write-Host "--- DNS Agent Master Setup ---" -ForegroundColor Cyan

# 1. Firewall Fix: Allow the Application itself (not just ports)
Write-Host "Configuring Application Firewall rules..." -ForegroundColor Yellow
Remove-NetFirewallRule -DisplayName "DNS Agent*" -ErrorAction SilentlyContinue
New-NetFirewallRule -DisplayName "DNS Agent - Service" -Direction Inbound -Program "$BinaryPath" -Action Allow -Profile Any
New-NetFirewallRule -DisplayName "DNS Agent - Tray" -Direction Inbound -Program "$TrayPath" -Action Allow -Profile Any

# 2. Service Cleanup
Write-Host "Cleaning up old service..." -ForegroundColor Gray
Stop-Service $ServiceName -Force -ErrorAction SilentlyContinue
& sc.exe delete $ServiceName 2>$null
Start-Sleep -Seconds 2

# 3. Install Service & FIX WEBSITE STYLING
Write-Host "Installing Service..." -ForegroundColor Yellow
$BinWithQuotes = "`"$BinaryPath`""
New-Service -Name $ServiceName -BinaryPathName $BinWithQuotes -DisplayName $DisplayName -Description "DNS Ad-Blocker" -StartupType Automatic

# This Registry fix tells Windows the "Working Directory" so it finds the CSS/Images
$RegPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
Set-ItemProperty -Path $RegPath -Name "ImagePath" -Value $BinWithQuotes

# 4. Start Service
Write-Host "Starting Service..." -ForegroundColor Cyan
Start-Service $ServiceName

# 5. FIX TRAY AUTO-START: Properly handling strings for the shortcut
if (Test-Path $TrayPath) {
    Write-Host "Configuring Tray Icon for Login auto-start..." -ForegroundColor Cyan
    $startupFolder = [Environment]::GetFolderPath("Startup")
    $shortcutPath = Join-Path $startupFolder "DNSAgentTray.lnk"
    
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $TrayPath
    # Using the fixed string version of the path here:
    $shortcut.WorkingDirectory = $CurrentDir 
    $shortcut.Save()
    
    # Start it now
    Start-Process $TrayPath -WorkingDirectory $CurrentDir
}

Write-Host "`nSUCCESS: DNS Agent is installed and running correctly!" -ForegroundColor Green
Write-Host "Dashboard: http://localhost:5123" -ForegroundColor Cyan
pause
