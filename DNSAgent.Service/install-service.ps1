param([string]$Action = "install")
$ServiceName = "DNSAgent"
$DisplayName = "DNS Agent - Network Ad Blocker"
$Description = "DNS-based advertisement and tracking blocker with web management interface"

# Use Absolute Paths to ensure the Service finds its files
$CurrentDir = Get-Location
$BinaryPath = Join-Path $CurrentDir "DNSAgent.Service.exe"
$TrayPath = Join-Path $CurrentDir "DNSAgent.Tray.exe"
$WwwRoot = Join-Path $CurrentDir "wwwroot"

function Install-DNSAgentService {
    Write-Host "--- Installing DNS Agent Service ---" -ForegroundColor Cyan
    
    if (!(Test-Path $WwwRoot)) {
        Write-Host "Warning: 'wwwroot' folder not found. Website styling may be broken!" -ForegroundColor Yellow
        Write-Host "Ensure you extracted ALL files from the ZIP." -ForegroundColor Yellow
    }

    # --- FIREWALL RULES ---
    Write-Host "Configuring Firewall Rules for Port 5123 (Web) and Port 53 (DNS)..." -ForegroundColor Cyan
    try {
        if (!(Get-NetFirewallRule -DisplayName "DNS Agent Web UI" -ErrorAction SilentlyContinue)) {
            New-NetFirewallRule -DisplayName "DNS Agent Web UI" -Direction Inbound -LocalPort 5123 -Protocol TCP -Action Allow -Description "Allows access to the DNS Agent Web Dashboard" | Out-Null
        }
        if (!(Get-NetFirewallRule -DisplayName "DNS Agent Port 53 Content" -ErrorAction SilentlyContinue)) {
            New-NetFirewallRule -DisplayName "DNS Agent Port 53 Content" -Direction Inbound -LocalPort 53 -Protocol UDP, TCP -Action Allow -Description "Allows network DNS queries" | Out-Null
        }
        Write-Host "Firewall rules configured." -ForegroundColor Green
    }
    catch {
        Write-Host "Warning: Could not set firewall rules automatically." -ForegroundColor Yellow
    }

    $existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host "Service already exists. Removing old version..." -ForegroundColor Yellow
        Stop-Service $ServiceName -Force -ErrorAction SilentlyContinue
        & sc.exe delete $ServiceName
        Start-Sleep -Seconds 2
    }
    
    # Install service with quotes around path to handle spaces
    $BinWithQuotes = "`"$BinaryPath`""
    New-Service -Name $ServiceName -BinaryPathName $BinWithQuotes -DisplayName $DisplayName -Description $Description -StartupType Automatic
    
    Write-Host "Starting Service..." -ForegroundColor Cyan
    Start-Service $ServiceName
    
    if (Test-Path $TrayPath) { Start-Process $TrayPath }
    
    Write-Host "SUCCESS: DNS Agent installed and running." -ForegroundColor Green
    Write-Host "Dashboard: http://localhost:5123" -ForegroundColor Cyan
}

function Uninstall-DNSAgentService {
    Write-Host "Uninstalling..." -ForegroundColor Cyan
    Stop-Service $ServiceName -Force -ErrorAction SilentlyContinue
    & sc.exe delete $ServiceName
    Write-Host "Removed." -ForegroundColor Green
}

switch ($Action.ToLower()) {
    "install" { Install-DNSAgentService }
    "uninstall" { Uninstall-DNSAgentService }
    default { Write-Host "Usage: ./install-service.ps1 install" }
}
