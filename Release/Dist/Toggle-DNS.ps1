$ErrorActionPreference = "Stop"
$LocalDns = "127.0.0.1"

Write-Host "--- DNS Toggle Utility ---" -ForegroundColor Cyan

# 1. Auto-detect primary adapter (Up and has an IPv4 address)
$adapter = Get-NetAdapter | Where-Object { $_.Status -eq "Up" } | Select-Object -First 1
if ($null -eq $adapter) {
    Write-Host "ERROR: No active network adapter found." -ForegroundColor Red
    pause
    exit
}

$AdapterName = $adapter.Name
$currentDns = Get-DnsClientServerAddress -InterfaceAlias $AdapterName -AddressFamily IPv4
$serverAddresses = $currentDns.ServerAddresses

Write-Host "Adapter: $AdapterName" -ForegroundColor Gray

# 2. Determine State and Toggle
if ($serverAddresses -contains $LocalDns) {
    # It's currently pointing to local, swap to automatic
    Write-Host "Current State: Local ($LocalDns)" -ForegroundColor Yellow
    Write-Host "Action: Switching to AUTOMATIC (DHCP)..." -ForegroundColor Cyan
    
    try {
        Set-DnsClientServerAddress -InterfaceAlias $AdapterName -ResetServerAddresses
        Write-Host "Status: Successfully reset to Automatic." -ForegroundColor Green
    }
    catch {
        Write-Host "ERROR: Failed to reset DNS. Make sure you are running as Administrator." -ForegroundColor Red
        pause
        exit
    }
}
else {
    # It's currently pointing elsewhere, swap to local
    $fromText = if ($serverAddresses.Count -eq 0) { "Automatic" } else { $serverAddresses -join ", " }
    Write-Host "Current State: $fromText" -ForegroundColor Yellow
    Write-Host "Action: Switching to LOCAL ($LocalDns)..." -ForegroundColor Cyan
    
    try {
        Set-DnsClientServerAddress -InterfaceAlias $AdapterName -ServerAddresses ($LocalDns)
        Write-Host "Status: Successfully switched to Local DNS." -ForegroundColor Green
    }
    catch {
        Write-Host "ERROR: Failed to set DNS. Make sure you are running as Administrator." -ForegroundColor Red
        pause
        exit
    }
}

# 3. Flush DNS
Write-Host "Flushing DNS Cache..." -ForegroundColor Gray
ipconfig /flushdns | Out-Null
Write-Host "DNS Flush Complete." -ForegroundColor Green

Write-Host "`nDone!" -ForegroundColor Cyan
pause
