# Building the Installer

## Prerequisites

1. **Install Inno Setup**
   - Download from: https://jrsoftware.org/isdl.php
   - Install the latest version (6.0+)

2. **Build the Project**
   ```powershell
   cd c:\Users\BRIAN\source\repos\DNSAgent
   dotnet publish DNSAgent.Service/DNSAgent.Service.csproj -c Release -o DNSAgent.Service/publish
   ```

## Create the Installer

### Option 1: Using Inno Setup GUI
1. Open Inno Setup Compiler
2. File → Open → Select `installer\DNSAgent.iss`
3. Build → Compile
4. Installer will be created in `installer\` folder as `DNSAgent-Setup-1.0.0.exe`

### Option 2: Command Line
```powershell
# Compile the installer
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\DNSAgent.iss
```

## What the Installer Does

✅ **Installation:**
- Copies all files to `C:\Program Files\DNS Agent`
- Installs as Windows Service (auto-start)
- Configures Windows Firewall (ports 53 and 5123)
- Creates Start Menu shortcuts
- Optional: Creates desktop shortcut
- Optional: Starts service immediately
- Opens dashboard after installation

✅ **Uninstallation:**
- Stops the service
- Removes the service
- Removes firewall rules
- Deletes all files
- Removes shortcuts

## Installer Features

- **Modern UI**: Clean, professional installation wizard
- **.NET Check**: Verifies .NET 9.0 is installed (offers download if not)
- **Firewall Rules**: Automatically configures Windows Firewall
- **Service Management**: Handles Windows Service installation/removal
- **Desktop Shortcut**: Optional shortcut to web dashboard
- **Post-Install**: Opens dashboard in browser after installation

## Distribution

The final installer (`DNSAgent-Setup-1.0.0.exe`) is:
- Self-contained (single .exe file)
- ~60-80 MB (includes all dependencies)
- Can be distributed via GitHub Releases
- Digitally signable (for future releases)

## Testing the Installer

1. Build the installer
2. Run `DNSAgent-Setup-1.0.0.exe`
3. Follow the wizard
4. Test the service is running
5. Test uninstallation works cleanly

## Future Enhancements

- Code signing certificate (removes "Unknown Publisher" warning)
- Auto-update functionality
- Custom installation directory option
- Silent installation mode (`/SILENT` flag already supported)
