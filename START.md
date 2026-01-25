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
   This batch file handles:
   - Setting PowerShell execution policy to bypass.
   - Automatically cleaning up hung processes/ports from previous versions.
   - Running the primary `Setup-DNSAgent.ps1` script.

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
