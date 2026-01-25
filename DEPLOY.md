# Deployment and Release Guide 🛡️

This guide documents the established patterns for building, packaging, and publishing a new release of DNS Agent.

## 📦 Packaging Version 2.0+

The build process is automated via PowerShell scripts.

### 1. Build the Release
Run the build script from the repository root:
```powershell
powershell.exe -ExecutionPolicy Bypass -File "Build-Release.ps1"
```

**What happens:**
- Cleans the `Release/` directory.
- Publishes `DNSAgent.Service` and `DNSAgent.Tray` in Release mode.
- Copies the **Browser Extension** and critical setup scripts (`Start-Setup.bat`, `Setup-DNSAgent.ps1`).
- Generates a ZIP archive named **`DNSAgent_V2.zip`** (following the V2 architectural requirement).

### 2. Publishing to GitHub
The publish script handles the GitHub API integration:
```powershell
.\Publish-Release.ps1 -Version "2.0" -GitHubToken "your_token"
```

**Key Patterns:**
- **Naming**: Ensure the zip file includes the `_V2` suffix for the current architectural generation.
- **Assets**: Always include the `/extension` folder in the distribution zip.
- **Notes**: Release notes are automatically generated from a template within the script.

## 🏗️ Installer Generation
For users who prefer a traditional installer, the `installer/DNSAgent.iss` file can be compiled using **Inno Setup**. Instructions are located in [installer/BUILD.md](installer/BUILD.md).
