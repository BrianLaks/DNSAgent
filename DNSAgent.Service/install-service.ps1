# DNS Agent - Service Installation Script
# Run as Administrator

param(
    [string]$Action = "install"
)

$ServiceName = "DNSAgent"
$DisplayName = "DNS Agent - Network Ad Blocker"
$Description = "DNS-based advertisement and tracking blocker with web management interface"
$BinaryPath = "$PSScriptRoot\DNSAgent.Service.exe"

function Install-DNSAgentService {
    Write-Host "Installing DNS Agent Service..." -ForegroundColor Cyan
    
    # Check if service already exists
    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "Service already exists. Uninstalling first..." -ForegroundColor Yellow
        Uninstall-DNSAgentService
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
    
    $service = Get-Service -Name $ServiceName
    Write-Host "Service Status: $($service.Status)" -ForegroundColor Green
    
    Write-Host "`nDNS Agent is now running!" -ForegroundColor Green
    Write-Host "Web Interface: http://localhost:5123" -ForegroundColor Cyan
    Write-Host "`nTo configure your network:"
    Write-Host "1. Set your router's DNS to this server's IP address"
    Write-Host "2. Or set individual device DNS to this server's IP"
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
    } else {
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
    } else {
        Write-Host "DNS Agent Service is not installed." -ForegroundColor Red
    }
}

# Main execution
switch ($Action.ToLower()) {
    "install" { Install-DNSAgentService }
    "uninstall" { Uninstall-DNSAgentService }
    "start" { Start-DNSAgentService }
    "stop" { Stop-DNSAgentService }
    "status" { Get-DNSAgentStatus }
    default {
        Write-Host "Usage: .\install-service.ps1 [install|uninstall|start|stop|status]" -ForegroundColor Yellow
        Write-Host "`nExamples:"
        Write-Host "  .\install-service.ps1 install    - Install and start the service"
        Write-Host "  .\install-service.ps1 uninstall  - Stop and remove the service"
        Write-Host "  .\install-service.ps1 status     - Check service status"
    }
}
