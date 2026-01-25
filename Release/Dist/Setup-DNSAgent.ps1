# DNS Agent - Quick Setup Script 🛡️
# This script installs and starts the DNS Agent service.

$ErrorActionPreference = "Stop"

# Ensure running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (!$currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Please run this script as Administrator!"
    exit
}

Write-Host "--- DNS Agent Setup ---" -ForegroundColor Cyan

# Check for .NET 9 Runtime
Write-Host "Verifying .NET 9 Runtime..." -ForegroundColor Gray
try {
    $runtimes = dotnet --list-runtimes 2>$null
    $hasDotNet9 = $runtimes -match "Microsoft.AspNetCore.App 9\."
    
    if (!$hasDotNet9) {
        Write-Host "❌ Error: .NET 9 ASP.NET Core Runtime is not installed!" -ForegroundColor Red
        Write-Host "This is required to run the DNS Agent service." -ForegroundColor Red
        Write-Host "`nPlease download and install the '.NET 9 ASP.NET Core Runtime (Hosting Bundle)' from:" -ForegroundColor Yellow
        Write-Host "👉 https://dotnet.microsoft.com/en-us/download/dotnet/9.0" -ForegroundColor Cyan
        Write-Host "`nAfter installing, please run this setup script again."
        pause
        exit
    }
    Write-Host "✅ .NET 9 Runtime detected." -ForegroundColor Green
}
catch {
    Write-Host "❌ Error: 'dotnet' command not found." -ForegroundColor Red
    Write-Host "Please install the .NET 9 ASP.NET Core Runtime (Hosting Bundle)." -ForegroundColor Yellow
    Write-Host "👉 https://dotnet.microsoft.com/en-us/download/dotnet/9.0" -ForegroundColor Cyan
    pause
    exit
}

# Determine if we are in a ZIP/Release folder or Source tree
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$publishPath = ""

if (Test-Path "$scriptPath\DNSAgent.Service.dll") {
    # We are inside the publish folder already
    $publishPath = $scriptPath
}
elseif (Test-Path "$scriptPath\DNSAgent.Service\publish\DNSAgent.Service.dll") {
    # We are in the root of the source tree
    $publishPath = "$scriptPath\DNSAgent.Service\publish"
}
else {
    Write-Error "Could not find DNSAgent.Service.dll. Please ensure you are running this from the extracted ZIP or compiled source folders."
    exit
}

Set-Location $publishPath

# Verify install-service.ps1 exists
if (!(Test-Path "install-service.ps1")) {
    Write-Error "Missing install-service.ps1 in $publishPath"
    exit
}

Write-Host "Running installation script from $publishPath..." -ForegroundColor Yellow
& ".\install-service.ps1" install

Write-Host "`n✅ DNS Agent has been installed successfully!" -ForegroundColor Green
Write-Host "📊 Dashboard: http://localhost:5123" -ForegroundColor Cyan
Write-Host "🛡️ Tray App: Running (check your system tray)" -ForegroundColor Cyan
Write-Host "`nTo manage the service in the future, use the tray icon or the scripts in:"
Write-Host "$publishPath"

# Try to start the tray app
$trayApp = Join-Path $publishPath "DNSAgent.Tray.exe"
if (Test-Path $trayApp) {
    Start-Process $trayApp
}

pause
