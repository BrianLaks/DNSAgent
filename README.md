# DNS Agent - Network-Wide Ad Blocker

A professional DNS-based ad blocking solution for Windows with web management interface, designed for deployment on local networks.

## 🚀 Quick Start

### Current Status: Windows Service Ready ✅

```powershell
# Navigate to the service binary
cd c:\Users\BRIAN\source\repos\DNSAgent\DNSAgent.Service\bin\Debug\net9.0

# Install as Windows Service (Run as Administrator)
.\install-service.ps1 install

# The service will start automatically
# Web UI available at: http://localhost:5123
```

---

## 📋 Features

### ✅ Currently Implemented
- **DNS Blocking**: 69,986 domains from StevenBlack blocklist
- **Authentication**: Login system with Admin/User roles
- **Web Dashboard**: Real-time statistics and monitoring
- **Query Logs**: View all DNS requests (Admin only)
- **Domain Whitelisting**: Allow specific domains globally
- **Windows Service**: Auto-start on boot, runs in background
- **SQLite Database**: Persistent logs and configuration
- **Configurable**: Edit settings via `appsettings.json`

---

## 🔐 Default Login Credentials

**Username**: `Admin`  
**Password**: `Admin`

> ⚠️ **Security Note**: The Query Logs page is protected and only accessible to Admin users. Change the default password after first login!

### 🚧 Planned Features
- **Authentication**: Admin/User role-based access
- **Per-Client Whitelisting**: IP/hostname-based rules
- **Real-Time Updates**: SignalR live dashboard
- **YouTube Blocking**: Experimental regex-based blocking (30-40% effective)
- **Browser Extension**: Chrome/Firefox extension (80%+ YouTube blocking)
- **System Tray App**: Windows tray icon for easy management
- **Auto-Updates**: Scheduled blocklist updates

---

## ⚙️ Configuration

Edit `appsettings.json`:

```json
{
  "DnsAgent": {
    "UpstreamDns": "8.8.8.8",
    "BlocklistUrl": "https://raw.githubusercontent.com/StevenBlack/hosts/master/hosts",
    "EnableWebUI": true,
    "WebUIPort": 5123,
    "EnableLogging": true,
    "EnableBlocking": true
  }
}
```

---

## 🌐 Network Setup

### Option 1: Router-Level (Recommended)
1. Set your router's DNS server to this machine's IP
2. All devices on network automatically protected
3. No per-device configuration needed

### Option 2: Per-Device
Set DNS server to this machine's IP on each device

---

## 📊 Web Interface

Access at `http://localhost:5123`

### Pages
- **Dashboard** (`/`): Real-time stats, queries blocked today
- **Query Logs** (`/logs`): View all DNS requests, whitelist domains
- **Whitelist** (`/whitelist`): Manage allowed domains

---

## 🛠️ Service Management

```powershell
# Check status
.\install-service.ps1 status

# Stop/Start/Uninstall
.\install-service.ps1 [stop|start|uninstall]
```

---

## 🎯 What Gets Blocked?

### ✅ Effectively Blocked
- Banner ads, pop-ups, tracking scripts
- Malware/phishing domains
- Most display advertising networks

### ⚠️ Partially Blocked
- **YouTube ads**: 0% (requires browser extension - see plans)
- **Facebook ads**: Limited (first-party ads)

---

## 📚 Documentation

- **Implementation Plan**: `implementation_plan.md`
- **Walkthrough**: `walkthrough.md`
- **YouTube Blocking**: `youtube_blocking_plan.md`
- **Browser Extension**: `browser_extension_plan.md`

---

## ⚠️ Requirements

- Windows 10/11 or Windows Server
- .NET 9.0 Runtime
- Port 53 available
- Administrator rights for installation

---

## 🐛 Troubleshooting

### Port 53 Already in Use
```powershell
netstat -ano | findstr :53
taskkill /F /PID <PID>
```

### Service Won't Start
Check Event Viewer → Windows Logs → Application

---

**Happy ad-free browsing! 🎉**
