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
   - Running the primary `Setup-DNSAgent.ps1` script.

3. **What `Setup-DNSAgent.ps1` Does**
   - Verifies the .NET 9 runtime.
   - Installs the **DNSAgent Service** on your machine.
   - Launches the **DNSAgent Tray** application.

## 🌐 Accessing the Dashboards

- **Web UI**: `http://localhost:5123`
- **Default Credentials**: `admin@dnsagent.local` / `Admin123!`

## 🔌 Browser Extension

To load the extension in a Chromium browser (Chrome/Edge/Brave):
1. Navigate to your browser's extensions page.
2. Enable **Developer Mode**.
3. Click **Load unpacked** and select the `/extension` folder from this repository.
