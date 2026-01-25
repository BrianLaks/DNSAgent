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

## ⚡ Quick Start (Public Release v1.6.2)

1. **Download `DNSAgent.zip`** from the `Release` folder in this repo.
2. Right-click the folder and **Extract All**.
3. Right-click **`Start-Setup.bat`** and select **Run as Administrator**.
4. Visit `http://localhost:5123` to access your Dashboard.

---

## 🔌 Browser Extension Support

### 🦁 Brave / 🌐 Chrome / 🧭 Microsoft Edge
Our extension works on all Chromium-based browsers:
1. Open your browser's extensions page (`brave://extensions` or `edge://extensions` or `chrome://extensions`).
2. Enable **Developer Mode** (usually a toggle in the corner).
3. Click **Load unpacked** and select the `extension` folder from your extracted ZIP.

### 🏛️ Internet Explorer / Legacy Browsers
Internet Explorer does not support modern extensions. However, **you are still protected!** 
Because DNS Agent is a **DNS Server**, it blocks ads at the network level. To protect IE:
1. Change your Windows Network Settings to use `127.0.0.1` (or your server's IP) as your **DNS Server**.
2. DNS Agent will now block ad domains for *every* app on that machine, including IE.

---

## 🛠️ Troubleshooting

### Dashboard is not accessible from other devices
If the server is running but you cannot reach the dashboard from another machine (e.g., `http://192.168.1.168:5123`), the Windows Firewall may be blocking traffic. 

Run this command in an **Administrator PowerShell** to fix it instantly:
```powershell
New-NetFirewallRule -DisplayName "DNS Agent Web" -Direction Inbound -LocalPort 5123 -Protocol TCP -Action Allow; New-NetFirewallRule -DisplayName "DNS Agent DNS" -Direction Inbound -LocalPort 53 -Protocol UDP -Action Allow
```

### Dashboard looks unstyled (No graphics)
Ensure you extracted **all files** from the ZIP before running the setup. The `wwwroot` folder must be in the same directory as `DNSAgent.Service.exe`.

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
