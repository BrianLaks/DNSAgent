# DNS Agent 🛡️

DNS Agent is a lightweight, high-performance network-wide DNS server designed for advertisement blocking, tracker protection, and per-device analytics. It features a modern Blazor dashboard and a convenient Windows System Tray monitor.

**✨ [Check out our full Feature Showcase with Screenshots!](FEATURES.md)**

## 🚀 Features
- **AD & Tracker Blocking**: Real-time filtering based on community-sourced blocklists.
- **YouTube Intelligence Hub**: (NEW v1.6) Collaborative ad-blocking, SponsorBlock skipping, and DeArrow clickbait cleaning via our hybrid browser extension.
- **Glassmorphic Dashboard**: A professional, blue-themed UI for statistics and logs.
- **Per-Device Analytics**: Monitor traffic distribution across your network.
- **System Tray Monitor**: Stay informed with real-time status and quick controls.
- **Whitelist Management**: Easily unblock false positives.

## ⚡ Quick Start (No Coding Required)

If you just want to run DNS Agent without cloning the code:
1. **Download the latest `DNSAgent.zip`** from this repo.
2. Right-click the folder and **Extract All**.
3. Right-click `Start-Setup.bat` and select **Run as Administrator**.
   - *This handles all permissions, firewall rules, and .NET checks automatically.*
4. 🎉 Your DNS Shield is now active! Visit `http://localhost:5123` to see your stats.

---

## 🛠️ Developer Installation (From Source)

To install and run DNS Agent on your Windows machine, follow these steps:

### 1. Prerequisites
- **.NET 9 SDK**: [Download here](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- **Git**: [Download here](https://git-scm.com/downloads)

### 2. Clone and Build
Open PowerShell as **Administrator** and run:
```powershell
git clone https://github.com/BrianLaks/DNSAgent.git
cd DNSAgent
dotnet publish DNSAgent.Service/DNSAgent.Service.csproj -c Release -o DNSAgent.Service/publish
```

### 3. Install the Windows Service
The service core runs in the background. Install it with:
```powershell
cd DNSAgent.Service/publish
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
.\install-service.ps1 install
```

### 4. Launch the Dashboard
- Open your browser to `http://localhost:5123`
- Default Admin Login: `admin@dnsagent.local` / `Admin123!` (Please change after login)

---

## 📂 Project Structure
- `DNSAgent.Service`: Core DNS engine, Database (SQLite), and Blazor Web Dashboard.
- `DNSAgent.Tray`: WinForms application for the Windows Notification Area.

## ⚖️ License
Licensed under the **GNU General Public License v3.0**. See [LICENSE.txt](LICENSE.txt) for details.
