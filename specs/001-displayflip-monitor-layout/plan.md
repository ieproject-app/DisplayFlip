# Implementation Plan: DisplayFlip multi-monitor layout

**Branch**: `001-displayflip-monitor-layout` | **Date**: 2026-11-12 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-displayflip-monitor-layout/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

DisplayFlip adalah aplikasi Windows tray (C# .NET 8) untuk tata letak multi-monitor sekali klik: tukar kiri-kanan dengan primary tetap pada identitas yang sama, jadikan monitor tertentu sebagai primary, simpan/terapkan profil layout bernama, CLI `--mirror` / `--apply "<nama>"`, dan toggle startup. Logika inti (QueryDisplayConfig → Mirror/SetPrimary → SDC_VALIDATE → SDC_APPLY via CCD) diisolasi ke library portable agar Fase 2 bisa diporting ke PowerToys Power Display (Issue #48286) — bukan fork repo PowerToys langsung.

## Technical Context

**Language/Version**: C# on .NET 8 (`net8.0-windows`, `net8.0-windows10.0.19041.0` via SDK 10.0.403 yang terpasang) — Decision: tetap .NET 8 LTS seperti riset awal; SDK 10 dapat menarget `net8.0-windows` tanpa menaikkan runtime ke .NET 10.

**Primary Dependencies**: Win32 CCD через P/Invoke / CsWin32 (`QueryDisplayConfig`, `SetDisplayConfig`, `DisplayConfigGetDeviceInfo`), `System.Windows.Forms` `NotifyIcon` + `ContextMenuStrip` untuk tray, `System.Text.Json` untuk profil, `System.Management` / SetupAPI via P/Invoke untuk `monitorDevicePath` → DeviceID. No external NuGet pada v1 selain `Microsoft.Windows.CsWin32` (opsional) — hindari paket tray pihak ketiga.

**Storage**: File JSON di `%LocalAppData%/DisplayFlip/profiles.json` (beserta `settings.json` untuk startup flag), kunci registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` untuk auto-start, tanpa database.

**Testing**: xUnit + FluentAssertions untuk pure logic (mirror primary-preserving, legalisasi posisi, profil serialize); `quickstart.md` manual di mesin asli untuk validasi CCD end-to-end (PowerShell + monitor fisik).

**Target Platform**: Windows 10/11 x64 desktop, minimum build 19041; CCD tersedia sejak Windows 7 sehingga OS baseline aman.

**Project Type**: desktop tray app + CLI (single-project, dual entry point tray & CLI)

**Performance Goals**: Mirror / SetPrimary / ApplyProfile dari klik tray ke susunan settled < 3 detik pada setup 2-monitor (SC-001); CLI parity 100% dengan tray di mesin yang sama (SC-004).

**Constraints**: Semua susunan harus legal tanpa gap/overlap dengan permukaan primary di (0,0) (GDI enforcement); validate (`SDC_VALIDATE`) selalu sebelum apply (`SDC_APPLY`); tangani `ERROR_BAD_CONFIGURATION` secara eksplisit dengan opsi `SDC_ALLOW_CHANGES`; jangan pakai nomor DISPLAY ordinal atau `adapterId` mentah sebagai kunci persisten; hormati profil best-effort saat monitor hilang.

**Scale/Scope**: 2–4 monitor aktif tipikal di laptop + monitor eksternal; mendukung N monitor dengan resolusi/ukuran berbeda; single-machine tanpa sinkronisasi cloud pada v1.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Constitution saat ini adalah template default (`[PROJECT_NAME]` placeholder) — tidak ada prinsip yang melarang pendekatannya. Penilaian:

- Prinsip umum spec-kit (simplicity, explicit purpose) terpenuhi: menambah satu proyek desktop tunggal yang jelas, bukan library tidak beralasan.
- Tidak ada pelanggaran yang perlu dijustifikasi pada Complexity Tracking; semua gate lulus.

**Result (pre-Phase-0): PASS** — lanjut Phase 0.

### Post-Design Re-Check (after Phase 1)

Desain (research D1–D7, data-model, contracts, quickstart) tetap memegang:

- Simplicity: satu proyek Core+Adapter+Tray yang kecil, tanpa framework ekstra; Complexity Tracking tetap kosong.
- Portability (batas PowerToys D7) ditegakkan lewat isolasi `DisplayFlip.Core` — tidak ada pelanggaran menambah kompleksitas yang tidak perlu.

**Result (post-design): PASS** — siap `/speckit-tasks`.

## Project Structure

### Documentation (this feature)

```text
specs/001-displayflip-monitor-layout/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
│   ├── cli.md
│   ├── tray-menu.md
│   └── profile-store.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── DisplayFlip.Core/          # Portable pure logic; no Win32 calls — mirror/set-primary math, legalizer, profile serialize/deserialize, missing-monitor policy
│   └── DisplayTopology.cs
├── DisplayFlip.Windows/       # Win32 adapter: CCD P/Invoke, monitor identity via DisplayConfigGetDeviceInfo + SetupAPI, JSON store, registry startup
│   └── CcdTopologyAdapter.cs
├── DisplayFlip/               # Tray app entry point (WinForms NotifyIcon) + CLI entry (DisplayFlip.exe --mirror / --apply)
│   ├── Program.cs
│   └── TrayHost.cs
└── DisplayFlip.Tests/         # xUnit pure-logic tests for DisplayFlip.Core
tests/
└── (covered by src/DisplayFlip.Tests per single-project default) — lihat quickstart.md untuk manual validation
```

**Structure Decision**: Memilih Single project (Option 1) dengan pemisahan Core (portable, tanpa P/Invoke) vs Windows adapter vs Tray/CLI shell. Ini disengaja agar `DisplayFlip.Core` mudah diporting ke modul PowerToys Power Display nanti tanpa membawa dependensi tray/CLI. Diprioritaskan daripada struktur web/mobile yang tidak relevan.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Tidak ada pelanggaran Constitution — bagian ini disengaja kosong.
