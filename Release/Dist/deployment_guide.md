# DNS Agent - Deployment Guide

## 🚨 CRITICAL: Version Update Checklist

Before building ANY release, update the version number in:

### 1. Update Constants.cs (REQUIRED)
```csharp
// File: DNSAgent.Service/Configuration/Constants.cs
public const string AppVersion = "1.6.0"; // ← CHANGE THIS FIRST!
```

### 2. Update Extension Manifest (REQUIRED)
```json
// File: extension/manifest.json
"version": "1.6.1", // ← Bump extension for browser updates
```

---

## 🏗️ Build & Deploy Process

### Step 1: Stop All Running Processes

**CRITICAL**: Always stop everything before building!

```powershell
# Stop Windows Service
sc.exe stop DNSAgent

# Kill tray app & service worker
taskkill /F /IM DNSAgent.Tray.exe 2>$null
taskkill /F /IM dotnet.exe 2>$null

# Wait for processes to fully stop
Start-Sleep -Seconds 5
```

---

### Step 2: Clean Build & Publish

```powershell
# Remove old release folder
Remove-Item -Path "Release" -Recurse -Force -ErrorAction SilentlyContinue

# Publish Service
dotnet publish DNSAgent.Service/DNSAgent.Service.csproj -c Release -o Release/Dist --no-self-contained

# Publish Tray App
dotnet publish DNSAgent.Tray/DNSAgent.Tray.csproj -c Release -o Release/Dist --no-self-contained

# Copy scripts & extension
Copy-Item "DNSAgent.Service\install-service.ps1" "Release\Dist\" -Force
Copy-Item "Setup-DNSAgent.ps1" "Release\Dist\" -Force
Copy-Item -Path "extension" -Destination "Release\Dist\extension" -Recurse -Force
```

---

### Step 3: Distribution & Git Sync

```powershell
# Create zip for distribution
Compress-Archive -Path "Release\Dist\*" -DestinationPath "Release\DNSAgent_v1.6.1.zip" -Force

# Commit and Push
git add .
git commit -m "Release v1.6.1: YouTube Intelligence Hub (DeArrow + SponsorBlock) 🛡️🚀"
git push origin main
```

---

**Last Updated**: 2026-01-25 (v1.6.1)
