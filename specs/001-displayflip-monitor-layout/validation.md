# Validation Results: DisplayFlip

**Date**: 2026-11-12 (UTC)
**Machine**: D:\GitHub\multi-monitor (Windows, 1 display aktif di sesi headless/RDP — guard <2 monitor aktif dan berfungsi mewakili S1 sebagian)

## Build

```powershell
dotnet build DisplayFlip.slnx -c Release
# Build succeeded. 0 Error(s), 1 Warning(s) NETSDK1137 (SDK attribution, harmless)
# DisplayFlip.Core, DisplayFlip.Windows, DisplayFlip.Tests, DisplayFlip all built
```

## Unit Tests (12/12 passed)

```powershell
dotnet test src/DisplayFlip.Tests/DisplayFlip.Tests.csproj -c Release
# Passed! - Failed: 0, Passed: 12, Total: 12
# - Mirror_TwoMonitors_PrimaryStaysSameIdentity (primary-preserving mirror = identity untuk 2 monitor equal-size, sesuai kontrak D3)
# - Mirror_ThreeMonitors_ReversesOrderPrimaryStays (B/C bertukar, primary tetap)
# - Mirror_Twice_ReturnsToOriginal, SetPrimary_MovesTargetToOrigin, Legalize_ClosesGaps
# - Profile Save/Find round-trip, Upsert, SplitPresent missing, Validate duplicate
# - CliParity Mirror dan case-insensitive profile name
```

## CLI Smoke (artifacts/DisplayFlip/DisplayFlip.exe, self-contained 154MB single-file)

```powershell
.\artifacts\DisplayFlip\DisplayFlip.exe --list
# (no profiles)

.\artifacts\DisplayFlip\DisplayFlip.exe --list --json
# []

.\artifacts\DisplayFlip\DisplayFlip.exe --mirror
# Mirror requires 2+ active monitors.  (guard FR-001 path, expected di 1-display)

.\artifacts\DisplayFlip\DisplayFlip.exe --apply "NoSuchProfile"
# Profile not found: 'NoSuchProfile' (see --list)  -> exit non-zero, sesuai kontrak cli.md
```

## Tray Mode

- Tray process (`DisplayFlip.exe` tanpa arg) start dan hidup minimal 2 detik (PID terverifikasi) lalu dihentikan; NotifyIcon dengan refresh state monitor/profil setiap 1.5s (pengganti WM_DISPLAYCHANGE polling ringan, lihat TrayHost.cs).
- Di mesin dengan 2+ monitor aktif (kantor user), Mirror/SetPrimary/Apply/Profile memerlukan pengujian manual S1–S8 di quickstart.md. Logika Core yang sama sudah terverifikasi unit; tray wiring sudah termasuk tetapi tidak dapat divalidasi visual di sesi headless 1-display ini.

## Verifikasi Portability Boundary (T035)

- `DisplayFlip.Core` target `net8.0`, tidak ada referensi P/Invoke / `User32` / `SetDisplayConfig` / `QueryDisplayConfig` (`Select-String` di *.cs Core mengembalikan kosong). Semua Win32 CCD hanya di `DisplayFlip.Windows`.

## Lokasi Artefak

- Source: `src/DisplayFlip.Core/`, `src/DisplayFlip.Windows/`, `src/DisplayFlip/`, `src/DisplayFlip.Tests/`
- Publish: `artifacts/DisplayFlip/DisplayFlip.exe` (self-contained, langsung jalan tanpa .NET runtime eksternal)
- Spec/Plan/Tasks: `specs/001-displayflip-monitor-layout/`
