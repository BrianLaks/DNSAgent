$ErrorActionPreference = "Stop"
Write-Host "--- DNS Agent Setup ---" -ForegroundColor Cyan

# 1. Check for .NET 9
try {
    $runtimes = dotnet --list-runtimes 2>$null
    if (!($runtimes -match "Microsoft.AspNetCore.App 9\.")) {
        Write-Host "ERROR: .NET 9 Runtime not found. Please install it first." -ForegroundColor Red
        exit
    }
}
catch {
    Write-Host "ERROR: dotnet command not found." -ForegroundColor Red
    exit
}

# 2. Find paths and Unblock
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
if ($scriptPath -eq "") { $scriptPath = Get-Location }
Set-Location $scriptPath

Write-Host "Unblocking package files..." -ForegroundColor Gray
Get-ChildItem -Path $scriptPath -Recurse | Unblock-File

# 3. Run the installer
if (Test-Path "install-service.ps1") {
    & ".\install-service.ps1" install
}
else {
    Write-Host "ERROR: install-service.ps1 not found in $scriptPath" -ForegroundColor Red
}

Write-Host "Setup Complete." -ForegroundColor Green
pause
