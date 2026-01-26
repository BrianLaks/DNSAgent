# Changelog - DNS Agent 🛡️

All notable changes to this project will be documented in this file.

## [2.3.4] - 2026-01-25
### Fixed
- **Database Rescue**: Fixed logic failure in `install-service.ps1` when upgrading within the same directory.
- **Service Management**: Further improved service stop/start reliability.

## [2.3.3] - 2026-01-25
### Fixed
- **Packaging Error**: Restored missing `Toggle-DNS.ps1` to the release ZIP root.
- **Service Ghosting**: Improved `install-service.ps1` to aggressively purge previous version artifacts (fixing "v1.6" ghosting issues).
- **Toggle Bat**: Refined `Toggle-DNS.bat` path logic for better portable execution.

## [2.3.2] - 2026-01-25
### Added
- **Premium Identity**: Brand-new high-tech shield icons for the extension.
- **Version Tracking**: The dashboard now displays the connected extension version for each device.
### Changed
- **Unified Versioning**: Synchronized all components to v2.3.2 and established a strict version alignment policy in `DEPLOY.md`.

## [2.3.1] - 2026-01-25
### Fixed
- **System Analytics**: Resolved race condition in chart rendering logic.
- **Heartbeat Logic**: Aligned heartbeat detection with local server time to fix "0 Active" device issue.
### Added
- **Proxy Detection**: Extension now explicitly reports "DNS Proxy: Active/Inactive" status based on real-time log analysis.
- **Utility Wrapper**: Added `Toggle-DNS.bat` for easy one-click administrator access.

## [2.3] - 2026-01-25
### Changed
- Refined deployment process with robust service management and database rescue.
- Updated `Build-Release.ps1` to retain historical release artifacts in the `Release/` folder.
- Synchronized all project versions to 2.3 for deployment continuity.
- Added `Toggle-DNS.ps1` utility for quick DNS switching with auto-detection and flush logic.

## [2.2] - 2026-01-25
### Added
- Complete database rescue logic in installer (`install-service.ps1`) to prevent data loss on upgrade.
- Added database auto-import to installer.
- Excluded development database from publish artifacts to prevent accidental overwrites.

## [2.1.x] - 2026-01-25
### Fixed
- Razor compilation errors in `SystemReports.razor`.
- Service startup logic (content root and working directory fixes).
- Broken HTML in `QueryLogs`.
- Missing `using` statements in various components.
### Added
- **Premium UI Enhancements**: Visual polish for detailed reports with device volume charts.
- **Top Devices visualization** in reporting.
- **Active device tracking** on dashboard.
- **Premium UI animations** and dashboard graph repairs.
- **Detailed Reports page** implementation.
