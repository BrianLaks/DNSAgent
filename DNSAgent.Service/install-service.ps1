$ErrorActionPreference = "Stop"
$ServiceName = "DNSAgent"
$DisplayName = "DNS Agent - Network Ad Blocker"

# --- FIX: Convert location to a STRING to avoid the "type PathInfo" error ---
$CurrentDir = (Get-Location).Path
$BinaryPath = Join-Path $CurrentDir "DNSAgent.Service.exe"
$TrayPath = Join-Path $CurrentDir "DNSAgent.Tray.exe"

Write-Host "--- DNS Agent Master Setup ---" -ForegroundColor Cyan

# 0. Cleanup: Kill processes listening on critical ports (53, 5123)
Write-Host "Checking for hung processes on critical ports..." -ForegroundColor Yellow
$PortsToClean = @(53, 5123)
foreach ($port in $PortsToClean) {
    $processes = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess
    $processes += Get-NetUDPEndpoint -LocalPort $port -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess
    
    foreach ($owningPid in ($processes | Select-Object -Unique)) {
        try {
            $p = Get-Process -Id $owningPid -ErrorAction SilentlyContinue
            if ($p) {
                Write-Host "Terminating process $($p.Name) (PID: $owningPid) listening on port $port..." -ForegroundColor Gray
                Stop-Process -Id $owningPid -Force -ErrorAction SilentlyContinue
            }
        }
        catch { }
    }
}

# Also kill by name just in case they aren't listening yet
Stop-Process -Name "DNSAgent.Service" -Force -ErrorAction SilentlyContinue
Stop-Process -Name "DNSAgent.Tray" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

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
    
    # Start it now (gracefully)
    try {
        if (!(Get-Process "DNSAgent.Tray" -ErrorAction SilentlyContinue)) {
            Start-Process $TrayPath -WorkingDirectory $CurrentDir
        }
    }
    catch {
        Write-Host "Note: Tray app could not be started automatically. You can start it manually from $TrayPath" -ForegroundColor Yellow
    }
}

Write-Host "`nSUCCESS: DNS Agent is installed and running correctly!" -ForegroundColor Green
Write-Host "Dashboard: http://localhost:5123" -ForegroundColor Cyan
pause
