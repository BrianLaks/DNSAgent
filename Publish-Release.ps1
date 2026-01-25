# Publish-Release.ps1
# Automated GitHub Release Publisher for DNS Agent
# Usage: .\Publish-Release.ps1 -Version "1.3" -GitHubToken "your_token_here"

param(
    [Parameter(Mandatory = $false)]
    [string]$Version = "1.3",
    
    [Parameter(Mandatory = $false)]
    [string]$GitHubToken = $env:GITHUB_TOKEN,
    
    [Parameter(Mandatory = $false)]
    [string]$RepoOwner = "BrianLaks",
    
    [Parameter(Mandatory = $false)]
    [string]$RepoName = "DNSAgent"
)

Write-Host "=== DNS Agent Release Publisher ===" -ForegroundColor Cyan
Write-Host ""

# Validate GitHub token
if ([string]::IsNullOrEmpty($GitHubToken)) {
    Write-Host "ERROR: GitHub token not provided!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please provide a token via:" -ForegroundColor Yellow
    Write-Host "  1. Parameter: .\Publish-Release.ps1 -GitHubToken 'your_token'" -ForegroundColor Yellow
    Write-Host "  2. Environment: `$env:GITHUB_TOKEN = 'your_token'" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Create a token at: https://github.com/settings/tokens" -ForegroundColor Cyan
    Write-Host "Required scopes: repo (Full control of private repositories)" -ForegroundColor Cyan
    exit 1
}

# Paths
$RootPath = $PSScriptRoot
$ZipPath = Join-Path $RootPath "Release\DNSAgent_v$Version.zip"
$ReleasePath = Join-Path $RootPath "Release\Dist"

# Verify zip exists
if (-not (Test-Path $ZipPath)) {
    Write-Host "ERROR: Release zip not found at: $ZipPath" -ForegroundColor Red
    Write-Host "Please run Build-Release.ps1 first!" -ForegroundColor Yellow
    exit 1
}

Write-Host "Found release package: $ZipPath" -ForegroundColor Green
$zipSize = (Get-Item $ZipPath).Length / 1MB
Write-Host "Package size: $([math]::Round($zipSize, 2)) MB" -ForegroundColor Gray
Write-Host ""

# Create release notes
$releaseNotes = @"
## 🛡️ DNS Agent v$Version - Security Hardening

### ✨ New Features
- 🔒 **DNS-over-HTTPS (DoH)** - Encrypted upstream queries for privacy
- 🛡️ **DNSSEC Validation** - Cryptographic integrity verification
- ⚡ **Multi-Source Threat Intel** - 119,000+ malicious domains blocked
- 📊 **Security Metadata** - Transport & DNSSEC status in Query Logs
- 🎀 **Admin-Only Controls** - Security toggles require authentication
- 🔄 **Tray App Restart** - Convenient service restart from system tray

### 🐞 Bug Fixes
- Fixed 500 error on Query Logs page
- Database migration for new security columns
- Improved deployment process

### 📦 Installation

**Windows Service (Recommended):**
``````powershell
# Extract the zip file
Expand-Archive -Path DNSAgent_v$Version.zip -DestinationPath C:\DNSAgent

# Install as Windows Service (run as Administrator)
cd C:\DNSAgent
.\install-service.ps1 install
``````

**Quick Setup:**
``````powershell
.\Setup-DNSAgent.ps1
``````

### 🔐 Default Credentials
- **Username:** admin
- **Password:** admin123

### 🌐 Access
- **Web Dashboard:** http://localhost:5123
- **DNS Server:** Port 53 (UDP)

### 📚 Documentation
- [Quick Start Guide](https://github.com/$RepoOwner/$RepoName#readme)
- [Configuration Guide](https://github.com/$RepoOwner/$RepoName/wiki)

### 🔒 Security Rating
**7/10** - Excellent DNS-level protection. Pair with endpoint antivirus for comprehensive security.

---

**What's Next?** v1.4 will include browser extension with YouTube ad blocking! 🎯
"@

Write-Host "Creating GitHub release v$Version..." -ForegroundColor Cyan

# GitHub API endpoint
$apiUrl = "https://api.github.com/repos/$RepoOwner/$RepoName/releases"

# Check if release already exists
Write-Host "Checking for existing release..." -ForegroundColor Gray
$headers = @{
    "Authorization" = "token $GitHubToken"
    "Accept"        = "application/vnd.github.v3+json"
}

try {
    $existingReleases = Invoke-RestMethod -Uri $apiUrl -Headers $headers -Method Get
    $existingRelease = $existingReleases | Where-Object { $_.tag_name -eq "v$Version" }
    
    if ($existingRelease) {
        Write-Host "WARNING: Release v$Version already exists!" -ForegroundColor Yellow
        $confirm = Read-Host "Delete and recreate? (y/n)"
        if ($confirm -eq 'y') {
            Write-Host "Deleting existing release..." -ForegroundColor Yellow
            Invoke-RestMethod -Uri $existingRelease.url -Headers $headers -Method Delete | Out-Null
            Write-Host "Deleted!" -ForegroundColor Green
        }
        else {
            Write-Host "Aborted." -ForegroundColor Red
            exit 1
        }
    }
}
catch {
    Write-Host "Note: Could not check for existing releases (this is OK for first release)" -ForegroundColor Gray
}

# Create the release
Write-Host "Creating release..." -ForegroundColor Cyan
$releaseData = @{
    tag_name         = "v$Version"
    target_commitish = "main"
    name             = "v$Version - Security Hardening"
    body             = $releaseNotes
    draft            = $false
    prerelease       = $false
} | ConvertTo-Json

try {
    $release = Invoke-RestMethod -Uri $apiUrl -Headers $headers -Method Post -Body $releaseData -ContentType "application/json"
    Write-Host "✅ Release created successfully!" -ForegroundColor Green
    Write-Host "Release URL: $($release.html_url)" -ForegroundColor Cyan
}
catch {
    Write-Host "ERROR: Failed to create release!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# Upload the zip file
Write-Host ""
Write-Host "Uploading DNSAgent_v$Version.zip..." -ForegroundColor Cyan

$uploadUrl = $release.upload_url -replace '\{\?name,label\}', "?name=DNSAgent_v$Version.zip"
$uploadHeaders = @{
    "Authorization" = "token $GitHubToken"
    "Content-Type"  = "application/zip"
}

try {
    $zipBytes = [System.IO.File]::ReadAllBytes($ZipPath)
    $asset = Invoke-RestMethod -Uri $uploadUrl -Headers $uploadHeaders -Method Post -Body $zipBytes
    Write-Host "✅ Asset uploaded successfully!" -ForegroundColor Green
    Write-Host "Download URL: $($asset.browser_download_url)" -ForegroundColor Cyan
}
catch {
    Write-Host "ERROR: Failed to upload asset!" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== 🎉 Release Published Successfully! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Release Page: $($release.html_url)" -ForegroundColor Cyan
Write-Host "Download Link: $($asset.browser_download_url)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Users can now install DNS Agent v$Version from GitHub!" -ForegroundColor Green
