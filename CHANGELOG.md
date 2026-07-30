# Changelog

All notable changes to this project are documented here.

## [0.1.3] - 2026-07-30

### Fixed

- Hide the window directly when minimizing to the system tray instead
  of toggling `ShowInTaskbar`
- Prevent a temporary non-interactive ghost window during the tray
  transition

## [0.1.2] - 2026-07-30

### Fixed

- Re-register Raw Input whenever Windows Forms recreates the main window
  handle
- Keep physical key counting active after minimizing to or restoring from
  the system tray

## [0.1.1] - 2026-07-30

### Changed

- Changed new releases from the MIT License to the PolyForm
  Noncommercial License 1.0.0
- Added a separate commercial-licensing path
- Added explicit copyright attribution to OldManLoki
- Documented the license history of the original 0.1.0-beta release

## [0.1.0] - 2026-07-30

### Added

- Live physical key counting through Windows Raw Input
- Per-key GameSense RGB heatmap using a 22 × 6 bitmap
- German ISO layout for the Apex Pro Full-size Gen 3
- Logarithmic and linear heat normalization
- Adjustable heat half-life and optional key-repeat counting
- Optional aggregate statistics stored locally
- On-screen keyboard preview and live total counter
- Start, stop and reset controls
- System tray and optional Windows autostart support
- Portable .NET Framework build with no external runtime dependencies
