# DNS Agent 🛡️

DNS Agent is a lightweight, high-performance network-wide DNS server designed for advertisement blocking, tracker protection, and per-device analytics. It features a modern Blazor dashboard and a convenient Windows System Tray monitor.

**✨ [Check out our full Feature Showcase with Screenshots!](FEATURES.md)**

## 🚀 Highlights
- **YouTube Intelligence Hub**: Collaborative ad-blocking, SponsorBlock skipping, and clickbait cleaning via our hybrid extension + proxy.
- **Professional Installer**: A robust Windows `.exe` that handles service creation, firewall rules, and data preservation automatically.
- **Algorithm Circumvention**: Surgically modify the YouTube interface to hide watched videos and prioritize trusted channels using your *local* data.
- **Zero-Device Left Behind**: Protection for Smart TVs, IoT, and mobile apps by sanitizing DNS queries at the source.

## 🚀 Quick Start (Windows)
1. **Download the Release**: Grab `DNSAgent_V2.4.3.zip` from the latest [Release](https://github.com/BrianLaks/DNSAgent/releases).
2. **Extract All Files**: Right-click the ZIP and select **Extract All**.
3. **Run as Admin**: Right-click `Start-Setup.bat` and select **Run as Administrator**.
    - This script automatically registers the service and firewall rules for you.
4. **Launch Dashboard**: Open your browser to `http://localhost:5123`.
    - *Default Admin*: `admin@dnsagent.local` / `Admin123!` (Change immediately after login).

> [!TIP]
> **Developer Feature**: We have included an Inno Setup script in `installer/DNSAgent.iss`. If you have [Inno Setup 6](https://jrsoftware.org/isdl.php) installed, you can generate a professional `.exe` installer by running `Build-Release.ps1`.

### 🖥️ Manual / Developer Install
If you prefer running from source:
1. Ensure **.NET 9 SDK** is installed.
2. Run `dotnet publish DNSAgent.Service/DNSAgent.Service.csproj -c Release`.
3. Use the `Setup-DNSAgent.ps1` script in the root folder to register the service.

## 🔌 Browser Extension
To enable Advanced YouTube Filtering:
1. Open your browser's extensions page (`brave://extensions` or `chrome://extensions`).
2. Enable **Developer Mode**.
3. Click **Load unpacked** and select the `/extension` folder from this repository.
4. Set your "DNS Agent URL" in the extension popup to point to your server (e.g., `http://localhost:5123`).

## 📺 SmartTube & Android Guide
Using a Smart TV or Android device? Check out our dedicated [SMARTTUBE_GUIDE.md](SMARTTUBE_GUIDE.md) for instructions on installation, bypassing Google blockers, and integration with DNS Agent.

---

## 📂 Project Structure
- `DNSAgent.Service`: Core DNS engine, Database (SQLite), and Blazor Web Dashboard.
- `DNSAgent.Tray`: WinForms application for the Windows Notification Area.

## ⚖️ License
Licensed under the **GNU General Public License v3.0**. See [LICENSE.txt](LICENSE.txt) for details.
