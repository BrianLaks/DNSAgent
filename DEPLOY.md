# Deployment and Release Guide 🛡️

This guide documents the "Master Setup" approach for building, packaging, and publishing DNS Agent.

## 📦 Successive Build Process

We follow a diligent "Build -> Test -> Resolve" cycle. Every build must ensure continuity of service and data.

### 1. Build and Package (v2.3+)
Run the build script to generate the latest distribution:
```powershell
powershell.exe -ExecutionPolicy Bypass -File "Build-Release.ps1"
```

**Methodology:**
- **Non-Destructive**: Previous release ZIPs are preserved in the `Release/` folder for rollback or historical reference.
- **Comprehensive**: The ZIP contains all binaries, the browser extension, and the master setup scripts.
- **Git Backed**: Completed release ZIPs are force-committed to Git to allow deployment scripts to pull the latest architectural build.
- **Integrity Validation**: **MANDATORY.** Every release ZIP must be verified for payload completeness before pushing. The build script now enforces a minimum size check (~33MB+ compressed / ~89MB+ uncompressed). Never push a "junk" file that has been truncated by file locks or race conditions.
- **Project Dependencies**: The build script MUST explicitly include **DNSAgent.Web** (static assets), **DNSAgent.Service**, and **DNSAgent.Tray**. If any project is omitted, the release will be severely truncated (e.g., dropping from 39MB to 19MB).
- **Compression Safety**: PowerShell's `Compress-Archive` is known to fail silently or truncate files under handle contention (e.g., from `brave.exe` or `dotnet`). Professional releases should use the **.NET ZipFile engine** as implemented in `Build-Release.ps1`.

### 2. Utilities
- **`Toggle-DNS.ps1`**: A convenience script for developers to quickly toggle their local machine's primary adapter between using the local DNS Agent (`127.0.0.1`) and Automatic (DHCP). It automatically flushes the DNS cache after each change.

### 3. Extension Versioning Policy
To ensure architectural alignment and simplify troubleshooting, the **Browser Extension version MUST always match the Service version** (e.g., v2.3.2). 
- When bumping the version, update `Constants.cs`, `manifest.json`, and `Build-Release.ps1` simultaneously.
- The Service tracks connected extension versions in the Dashboard via heartbeat metadata.

### 4. Deployment Methodology (Master Setup)
The deployment is handled by `Start-Setup.bat` (wrapper) and `Setup-DNSAgent.ps1`, which invoke the `install-service.ps1` master script.

**What the Master Setup Ensures:**
1.  **Process Management**: Automatically terminates any hung service or tray processes listening on critical ports (53, 5123).
2.  **Firewall Orchestration**: Sets up application-level firewall rules for both the Service and the Tray app to ensure network-wide protection.
3.  **Data Persistence (Robust Rescue Logic)**:
    - **Installation Awareness**: Detects existing installations via service lookups.
    - **Same-Directory Protection**: Prevents redundant imports if the new installation is in the same directory as the old one (ensures database continuity without file locks).
    - **Automatic Migration**: Safely moves `dnsagent.db` from legacy paths to the new release folder if they differ.
    - **Safety Snapshots**: Backs up placeholder databases to `.bak` before importing production data.
4.  **Service Lifecycle Orchestration**:
    - **Deep Cleanup**: Terminates hung processes and deletes old service entries for a clean upgrade.
    - **Static Asset Resolution**: Configures Registry keys to ensure the service finds CSS/Images correctly in the new path.
5.  **Integration**: Configures the Tray Icon to auto-start on user login and launches it immediately alongside the service.
6.  **Conflict & Zombie Process Management (Critical Policy)**:
    - **Identification**: External processes (e.g., browsers like `brave.exe`, IDEs, or hung `.NET` build processes) can inadvertently hold locks on release ZIPs or binaries, leading to silent build failures or "Corrupted ZIP" errors.
    - **Resolution Requirement**: Every future deployment or build script **MUST** aggressively identify and terminate these locking processes before proceeding.
    - **Global Kill Strategy**: Use `taskkill /F /T` targeting specific binaries (`DNSAgent.Service.exe`, `DNSAgent.Tray.exe`, `dotnet.exe`) and, if necessary, broad-match patterns to ensure a clean slate.
    - **Manual Verification**: If a "Corrupted ZIP" is reported, developer intervention is required to identify the specific PID holding the lock (e.g., via `netstat` or `tasklist /V`) and terminate it manually.

## 📈 Version Tracking
We maintain a detailed [CHANGELOG.md](CHANGELOG.md) in the repository. Before every release, ensure the following are synchronized:
- `Constants.cs` (AppVersion)
- `.csproj` files (Version)
- `Build-Release.ps1` ($Version)

## 🏗️ Manual Installer
For Inno Setup instructions, see [installer/BUILD.md](installer/BUILD.md).

---

## ☣️ Post-Mortem: Zombie Extermination Reference
Use this logic in any future build or setup scripts to ensure no file handles are locked.

```powershell
# 1. Kill by port (53, 5123)
$PortsToClean = @(53, 5123)
foreach ($port in $PortsToClean) {
    $processes = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess
    $processes += Get-NetUDPEndpoint -LocalPort $port -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess
    foreach ($owningPid in ($processes | Select-Object -Unique)) {
        try { Stop-Process -Id $owningPid -Force -ErrorAction SilentlyContinue } catch { }
    }
}

# 2. Kill by binary name (Force + Recursive)
# IMPORTANT: Brave/Chrome must be killed if they have handle-locks on the release folder
try {
    & taskkill.exe /F /IM "DNSAgent.Service.exe" /T 2>$null
    & taskkill.exe /F /IM "DNSAgent.Tray.exe" /T 2>$null
    & taskkill.exe /F /IM "dotnet.exe" /T 2>$null
    & taskkill.exe /F /IM "brave.exe" /T 2>$null
} catch {}

# 3. Aggressive stop (PowerShell Native)
Get-Process -Name "DNSAgent*" -ErrorAction SilentlyContinue | Stop-Process -Force
```
