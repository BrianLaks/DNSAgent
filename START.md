# Getting Started with DNS Agent 🚀

This guide outlines the standard patterns for setting up the development environment and running the system locally.

## 📋 Prerequisites

- **.NET 9 SDK**: Required for building the service and tray application.
- **Git**: For version control and repository management.
- **Administrator Privileges**: Required to install the Windows Service and configure firewall rules.

## 🛠️ Development Setup

1. **Clone the Repository**
   ```powershell
   git clone https://github.com/BrianLaks/DNSAgent.git
   cd DNSAgent
   ```

2. **Quick Setup (Recommended)**
   Right-click **`Start-Setup.bat`** and select **Run as Administrator**.
   This is the **official and most robust** deployment method. It handles:
   - Setting PowerShell execution policy to bypass.
   - Automatically unblocking package files (SmartScreen fix).
   - Cleaning up hung processes/ports from previous versions.
   - Running the primary `Setup-DNSAgent.ps1` script to install the service.

3. **What `Setup-DNSAgent.ps1` Does**
   - Verifies the .NET 9 runtime.
   - Installs the **DNSAgent Service** on your machine.
   - Launches the **DNSAgent Tray** application.

## ⚙️ Host Configuration (Ethernet 2)

If you are using **Ethernet 2** as your primary adapter, use this command to set the DNS to loopback for system-wide protection:

```powershell
Set-DnsClientServerAddress -InterfaceAlias "Ethernet 2" -ServerAddresses ("127.0.0.1")
ipconfig /flushdns
```

To revert to automatic DNS:
```powershell
Set-DnsClientServerAddress -InterfaceAlias "Ethernet 2" -ResetServerAddresses
```

## 🔄 Instant DNS Toggle
For quick switching during development, run the toggle script:
```powershell
.\Toggle-DNS.ps1
```
This utility auto-detects your primary adapter, toggles between Local (127.0.0.1) and Automatic, and flushes your DNS cache.

## 🛠️ Version Maintenance

When preparing a new release, ensure the version is synchronized in the following locations:
1.  **`DNSAgent.Service/Configuration/Constants.cs`**: Update `AppVersion`.
2.  **`DNSAgent.Service/DNSAgent.Service.csproj`**: Update `<Version>` tag.
3.  **`Build-Release.ps1`**: Update `$Version` variable.

## 🌐 Accessing the Dashboards

- **Web UI**: `http://localhost:5123`
- **Default Credentials**: `admin@dnsagent.local` / `Admin123!`

## 🔌 Browser Extension

To load the extension in a Chromium browser (Chrome/Edge/Brave):
1. Navigate to your browser's extensions page.
2. Enable **Developer Mode**.
3. Click **Load unpacked** and select the `/extension` folder from this repository.

## 💾 Database Persistence & Migration

To prevent data loss during updates, the following mechanisms are required:

1.  **Installer-Level Rescue (`install-service.ps1`)**:
    *   The installer must detect if a previous version of the service exists.
    *   It must verify the presence of `dnsagent.db` in the old installation directory.
    *   It must copy the existing database to the new installation folder *before* starting the service.
    *   If a database already exists in the destination (e.g. from testing), it must be backed up (renamed to `.bak`) before the old data is imported.

2.  **Schema Auto-Migration (`Program.cs`)**:
    *   On startup, the service must perform granular checks for all table schemas.
    *   It must attempt to add any missing columns (e.g., `SourceHostname`, `IsDnssec`) individually using `try/catch` blocks to ensure partial failures do not crash the migration.
    *   This ensures that an imported legacy database is automatically upgraded to the current version's schema without data loss.

3.  **Build Exclusion**:
    *   The `dnsagent.db` file must be explicitly excluded from the release build artifacts (`.csproj`) to prevent shipping a blank database that could accidentally overwrite user data.
