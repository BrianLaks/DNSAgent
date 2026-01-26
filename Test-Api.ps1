# Test API Stats Ingestion
$Url = "http://localhost:5123/api/youtube-stats"
$Payload = @{
    adsBlocked         = 99
    adsFailed          = 1
    sponsorsSkipped    = 5
    titlesCleaned      = 12
    thumbnailsReplaced = 3
    timeSavedSeconds   = 120.5
    filterVersion      = "v2.3.12-TEST"
    machineName        = "TEST-VERIFIER"
}

Write-Host "Sending test payload to $Url..." -ForegroundColor Cyan
try {
    $Response = Invoke-RestMethod -Uri $Url -Method Post -Body ($Payload | ConvertTo-Json) -ContentType "application/json"
    Write-Host "Response received: $($Response | ConvertTo-Json)" -ForegroundColor Green
    
    Start-Sleep -Seconds 2
    
    # 2. Verify via DB count
    Write-Host "Verifying database counts in C:\DNSAgent_V2.3.10\dnsagent.db..." -ForegroundColor Yellow
    $dbPath = "C:\DNSAgent_V2.3.10\dnsagent.db"
    if (Test-Path $dbPath) {
        # We can't query SQLite directly without a tool, but we can check the file size/timestamp
        # and use our diagnostics dump from the Service project if we build a small console tool
        Write-Host "Database found. Last write: $((Get-Item $dbPath).LastWriteTime)" -ForegroundColor Green
    }
    else {
        Write-Host "Production database not found at $dbPath" -ForegroundColor Red
    }
}
catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
}
