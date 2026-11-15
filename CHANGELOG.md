# Changelog

All notable changes to **DisplayFlip** will be documented in this file.

## [1.0.0] - 2026-11-15

### Added
- **Modern Windows 11 Fluent UI (WPF on .NET 10 LTS + WPF-UI 4.3.0):**
  - Native **Mica** backdrop, rounded window corners, and synchronized Light / Dark / System themes.
  - Per-Monitor V2 DPI awareness manifest (`app.manifest`) and multi-resolution pixel-crisp vector icon engine (`16px`–`256px`).
- **Displays & Profiles Page:**
  - Interactive 2D monitor canvas with proportional scaling, magnetic edge snap, alignment guidelines, and toggleable enriched monitor name labels (`DISPLAYn (ModelID)`).
  - Quick Swap, Identify overlay, Primary selection, Orientation, Resolution, Refresh Rate (Hz), and Profile Manager.
- **Smart Auto-Wake Profile Switching:**
  - Automatically wakes sleeping displays via `Extend` when applying a multi-monitor profile from a single-display or duplicated state, with honest partial-apply reporting if a monitor is physically disconnected.
- **Keyboard Shortcuts Page:**
  - Interactive key recorder modal (`ContentDialog`) with canonical normalization and automatic conflict resolution for 6 global actions and per-profile hotkeys.
- **System Tray, Global Hotkeys & CLI:**
  - Fluent System Tray context menu, hide-on-close background residency, background `RegisterHotKey` listener, single-instance guard, and full CLI automation (`--swap`, `--identify`, `--solo`, `--extend`, `--clone`, `--apply`, `--list`, `--tray`).
- **Release Packaging:**
  - Automated `publish.ps1` script producing Framework-Dependent (~6.77 MB) and Standalone Self-Contained (~74.29 MB) single-file executables.
