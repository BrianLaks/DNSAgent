# Changelog - DNS Agent 🛡️

All notable changes to this project will be documented in this file.

## [2.3] - 2026-01-25
### Changed
- Refined deployment process with robust service management and database rescue.
- Updated `Build-Release.ps1` to retain historical release artifacts in the `Release/` folder.
- Synchronized all project versions to 2.3 for deployment continuity.

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
