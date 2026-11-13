# Data Model: DisplayFlip multi-monitor layout

**Feature**: 001-displayflip-monitor-layout
**Date**: 2026-11-12

## Entities

### Monitor

- **StableKey** (primary key persisten): gabungan `{ monitorDevicePath, edidManufactureProduct, edidSerialNumber_if_available, friendlyName }` — lihat research D2. Bukan ordinal DISPLAY, bukan hash `adapterId+id` mentah.
- **FriendlyName**: nama tampilan untuk tray/menu (dari `DISPLAYCONFIG_TARGET_DEVICE_NAME.friendlyName`).
- **IsPrimary** (derived): true bila `position == (0,0)` pada topologi aktif.
- **Position**: `POINTL x,y` + `width,height` (dari `DISPLAYCONFIG_SOURCE_MODE.position` + `DISPLAYCONFIG_SOURCE_MODE` extent).
- **Mode**: `resolution { width,height }`, `orientation`, `isEnabled` (dari target/source mode).
- **Validation**: StableKey wajib non-kosong; posisi must be within GDI desktop range dengan lebar/tinggi > 0; lebar total harus > 0.

### LayoutProfile

- **SchemaVersion**: int, saat ini `1`.
- **Name**: string, unik per store (case-insensitive), non-kosong, max 100 karakter.
- **PrimaryStableKey**: StableKey monitor primary dalam profil.
- **Monitors**: list `MonitorState` minimal 1, tidak boleh ada duplikat StableKey.
- **MonitorState**: `{ StableKey, Position { x,y,width,height }, Resolution { width,height }, Orientation, IsEnabled }`.
- **CreatedAt / UpdatedAt**: ISO-8601 UTC.

Relationships: `LayoutProfile` merujuk banyak `Monitor` (via MonitorState list). Setiap profil mereferensi tepat satu `PrimaryStableKey` yang harus ada di daftar MonitorState profil itu.

State transitions: `Saved → Applied → (ModifiedByOS) Stale → Reapplied | Discarded`. Tidak ada transisi status lain di v1.

### LayoutOperation (runtime, tidak disimpan)

- **Kind**: enum `Mirror | SetPrimary | ApplyProfile`.
- **TargetStableKey** (untuk SetPrimary) / **ProfileName** (untuk ApplyProfile).
- **Result**: `Applied | ValidationFailed | AbortedMissingMonitor | NoOp` + pesan untuk tray/CLI/stderr.
- **Idempotency**: Mirror applied dua kali kembali ke posisi asli bila environment tidak berubah (jika tidak ada perubahan tengah jalan).

### Settings (runtime store)

- **AutoStartEnabled**: bool, default false; sinkron dengan registry `HKCU\...\Run` nilai `DisplayFlip`.
- **LastAppliedProfile**: string nama profil terakhir berhasil diterapkan (untuk UI/state), nullable.

## Rules

- **Primary rule**: tepat satu monitor memiliki IsPrimary=true per topologi aktif; aplikasi tidak akan menghasilkan topologi tanpa primary.
- **Legal layout rule**: tidak boleh ada gap/overlap antar source surfaces; primary di (0,0). Jika Mirror/SetPrimary menghitung posisi melanggar ini, legalizer menggeser sebelum validate.
- **Identity rule**: Serialize ke JSON memakai StableKey saja; deserialize melakukan lookup via adapter.friendly/path/EDID saat Apply; jika tidak ditemukan, nyatakan `MissingMonitor`.
- **Versioning rule**: JSON selalu disertai `schemaVersion`; reader harus menolak versi yang lebih baru dengan pesan upgrade, bukan menebak.
- **Missing monitor rule**: ApplyProfil dengan monitor hilang tidak berhasil diam-diam; harus opt-in eksplisit "Lanjutkan dengan yang tersisa?" (D5).
