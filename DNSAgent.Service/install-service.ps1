# DNS Agent - Service Installation Script
# Run as Administrator

param(
    [string]$Action = "install"
)

$ServiceName = "DNSAgent"
$DisplayName = "DNS Agent - Network Ad Blocker"
$Description = "DNS-based advertisement and tracking blocker with web management interface"
$BinaryPath = "$PSScriptRoot\DNSAgent.Service.exe"
$TrayPath = "$PSScriptRoot\DNSAgent.Tray.exe"

function Stop-OldDNSAgentProcesses {
    Write-Host "Checking for old DNS Agent processes..." -ForegroundColor Cyan
    
    # Kill any old DNSAgent.Web or Tray processes
    $oldProcesses = Get-Process | Where-Object { $_.ProcessName -like "*DNSAgent*" }
    foreach ($proc in $oldProcesses) {
        if ($proc.ProcessName -ne "DNSAgent.Service" -or $proc.Path -notlike "*publish*") {
            Write-Host "Stopping process: $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Yellow
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        }
    }
}

function Install-DNSAgentService {
    Write-Host "Installing DNS Agent Service..." -ForegroundColor Cyan
    
    # Stop any old processes first
    Stop-OldDNSAgentProcesses
    
    # Check if service already exists
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "Service already exists. Upgrading..." -ForegroundColor Yellow
        Upgrade-DNSAgentService
        return
    }
    
    # Create service
    New-Service -Name $ServiceName `
        -BinaryPathName $BinaryPath `
        -DisplayName $DisplayName `
        -Description $Description `
        -StartupType Automatic
    
    Write-Host "Service installed successfully!" -ForegroundColor Green
    Write-Host "Starting service..." -ForegroundColor Cyan
    
    Start-Service -Name $ServiceName
    
    # Start tray application
    if (Test-Path $TrayPath) {
        Write-Host "Starting System Tray application..." -ForegroundColor Cyan
        Start-Process $TrayPath
    }
    
    $service = Get-Service -Name $ServiceName
    Write-Host "Service Status: $($service.Status)" -ForegroundColor Green
    
    Write-Host "`nDNS Agent is now running!" -ForegroundColor Green
    Write-Host "Web Interface: http://localhost:5123" -ForegroundColor Cyan
    Write-Host "Default Login: Admin / Admin" -ForegroundColor Yellow
    Write-Host "`nTo configure your network:"
    Write-Host "1. Set your router's DNS to this server's IP address"
    Write-Host "2. Or set individual device DNS to this server's IP"
}

function Upgrade-DNSAgentService {
    Write-Host "Upgrading DNS Agent Service..." -ForegroundColor Cyan
    
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.Status -eq 'Running') {
            Write-Host "Stopping service..." -ForegroundColor Yellow
            Stop-Service -Name $ServiceName -Force
            Start-Sleep -Seconds 2
        }
        
        # Backup and recreate database (handles schema changes)
        $dbPath = "$PSScriptRoot\dnsagent.db"
        if (Test-Path $dbPath) {
            $backupPath = "$PSScriptRoot\dnsagent.db.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
            Write-Host "Backing up database to: $backupPath" -ForegroundColor Yellow
            Copy-Item $dbPath $backupPath
            
            Write-Host "Removing old database (will be recreated with new schema)..." -ForegroundColor Yellow
            Remove-Item $dbPath -Force
        }
        
        Write-Host "Service files updated. Starting service..." -ForegroundColor Cyan
        Start-Service -Name $ServiceName
        Start-Sleep -Seconds 3
        
        # Start tray application
        if (Test-Path $TrayPath) {
            Write-Host "Starting System Tray application..." -ForegroundColor Cyan
            Start-Process $TrayPath
        }

        $service = Get-Service -Name $ServiceName
        if ($service.Status -eq 'Running') {
            Write-Host "Service upgraded and restarted successfully!" -ForegroundColor Green
            Write-Host "Service Status: $($service.Status)" -ForegroundColor Green
            Write-Host "`nWeb Interface: http://localhost:5123" -ForegroundColor Cyan
            Write-Host "Default Login: Admin / Admin" -ForegroundColor Yellow
            Write-Host "`nNote: Database was recreated. Previous logs were backed up." -ForegroundColor Yellow
        }
        else {
            Write-Host "Service failed to start. Check Event Viewer for details." -ForegroundColor Red
        }
    }
    else {
        Write-Host "Service not found. Use 'install' instead." -ForegroundColor Red
    }
}

function Uninstall-DNSAgentService {
    Write-Host "Uninstalling DNS Agent Service..." -ForegroundColor Cyan
    
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        if ($service.Status -eq 'Running') {
            Write-Host "Stopping service..." -ForegroundColor Yellow
            Stop-Service -Name $ServiceName -Force
        }
        
        # Remove service
        sc.exe delete $ServiceName
        Write-Host "Service uninstalled successfully!" -ForegroundColor Green
    }
    else {
        Write-Host "Service not found." -ForegroundColor Yellow
    }
}

function Start-DNSAgentService {
    Write-Host "Starting DNS Agent Service..." -ForegroundColor Cyan
    Start-Service -Name $ServiceName
    $service = Get-Service -Name $ServiceName
    Write-Host "Service Status: $($service.Status)" -ForegroundColor Green
}

function Stop-DNSAgentService {
    Write-Host "Stopping DNS Agent Service..." -ForegroundColor Cyan
    Stop-Service -Name $ServiceName -Force
    Write-Host "Service stopped." -ForegroundColor Green
}

function Get-DNSAgentStatus {
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "`nDNS Agent Service Status" -ForegroundColor Cyan
        Write-Host "========================" -ForegroundColor Cyan
        Write-Host "Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Red' })
        Write-Host "Startup Type: $($service.StartType)"
        Write-Host "`nWeb Interface: http://localhost:5123"
        Write-Host "Default Login: Admin / Admin" -ForegroundColor Yellow
    }
    else {
        Write-Host "DNS Agent Service is not installed." -ForegroundColor Red
    }
}

# Main execution
switch ($Action.ToLower()) {
    "install" { Install-DNSAgentService }
    "upgrade" { Upgrade-DNSAgentService }
    "update" { Upgrade-DNSAgentService }
    "uninstall" { Uninstall-DNSAgentService }
    "start" { Start-DNSAgentService }
    "stop" { Stop-DNSAgentService }
    "status" { Get-DNSAgentStatus }
    default {
        Write-Host "Usage: .\install-service.ps1 [install|upgrade|uninstall|start|stop|status]" -ForegroundColor Yellow
        Write-Host "`nExamples:"
        Write-Host "  .\install-service.ps1 install    - Install and start the service"
        Write-Host "  .\install-service.ps1 upgrade    - Upgrade existing service (stop, update, restart)"
        Write-Host "  .\install-service.ps1 uninstall  - Stop and remove the service"
        Write-Host "  .\install-service.ps1 status     - Check service status"
    }
}
