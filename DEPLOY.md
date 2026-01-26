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

### 2. Utilities
- **`Toggle-DNS.ps1`**: A convenience script for developers to quickly toggle their local machine's primary adapter between using the local DNS Agent (`127.0.0.1`) and Automatic (DHCP). It automatically flushes the DNS cache after each change.

### 3. Deployment Methodology (Master Setup)
The deployment is handled by `Start-Setup.bat` (wrapper) and `Setup-DNSAgent.ps1`, which invoke the `install-service.ps1` master script.

**What the Master Setup Ensures:**
1.  **Process Management**: Automatically terminates any hung service or tray processes listening on critical ports (53, 5123).
2.  **Firewall Orchestration**: Sets up application-level firewall rules for both the Service and the Tray app to ensure network-wide protection.
3.  **Data Persistence (Rescue Logic)**:
    - Detects existing installations.
    - Backs up the current database (if empty) to `.bak`.
    - Automatically imports existing user data (`dnsagent.db`) from the old installation to the new one.
4.  **Service Lifecycle**: Uninstalls old versions and installs the new service with correct working directory registry keys for static asset resolution.
5.  **Integration**: Configures the Tray Icon to auto-start on user login and launches it immediately alongside the service.

## 📈 Version Tracking
We maintain a detailed [CHANGELOG.md](CHANGELOG.md) in the repository. Before every release, ensure the following are synchronized:
- `Constants.cs` (AppVersion)
- `.csproj` files (Version)
- `Build-Release.ps1` ($Version)

## 🏗️ Manual Installer
For Inno Setup instructions, see [installer/BUILD.md](installer/BUILD.md).
