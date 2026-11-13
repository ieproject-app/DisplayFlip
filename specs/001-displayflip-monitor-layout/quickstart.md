# Quickstart: DisplayFlip Validation Guide

**Feature**: 001-displayflip-monitor-layout
**Goal**: Validasi end-to-end di mesin Windows asli dengan monitor fisik (laptop + monitor eksternal). Tidak perlu UI otomatis; semua skenario dapat dijalankan dari PowerShell.

## Prerequisites

- Windows 10/11 x64, .NET 8 SDK terpasang (`dotnet --version` → 8.x atau 10.x yang bisa target `net8.0-windows`).
- Minimal 2 monitor aktif untuk skenario Mirror/SetPrimary; 1 monitor cukup untuk skenario error path.
- Kesabaran untuk reboot saat uji stabilitas profil (D2).

## Build

```powershell
dotnet build src/DisplayFlip/DisplayFlip.csproj -c Release
dotnet publish src/DisplayFlip/DisplayFlip.csproj -c Release -r win-x64 --self-contained false -o .\artifacts\DisplayFlip
```

Run tray (untuk uji tray):

```powershell
.\artifacts\DisplayFlip\DisplayFlip.exe
```

## Scenarios

### S1 — Mirror sekali klik (SC-001/FRI-001)

1. Atur 2 monitor berdamping tanpa gap di Windows Settings. Catat monitor mana yang primary dan posisinya.
2. Jalankan:

   ```powershell
   Start-Process .\artifacts\DisplayFlip\DisplayFlip.exe -ArgumentList --mirror; Start-Sleep 1
   ```

   Atau klik tray → "🔄 Tukar kiri-kanan".
3. **Expected**: susunan terbalik cermin; primary tetap di identitas yang sama. Ukur waktu dari klik hingga desktop settled (< 3 detik). Letakkan cursor: setelah mirror, posisi youtube tetap mengikuti monitor tersebut di sisi baru.
4. Ulangi `--mirror` → kembali ke awal (idempotency).

### S2 — SetPrimary (FR-002)

1. Jalankan tray → "⭐ Jadikan primary" → pilih monitor kedua.
2. **Expected**: monitor kedua pindah ke (0,0) sebagai primary, posisi legal, taskbar pindah ke monitor tersebut. Ulangi memilih yang sama → pesan "Sudah primary" tanpa flicker.

### S3 — Save & Apply Profil (FR-003)

1. Atur layout tertentu, klik tray → "💾 Simpan layout sekarang", nama `Primary Kanan`.
2. Secara manual ubah posisi di Settings (Geser monitor) → jalankan `--list` untuk verifikasi profil muncul.
3. Jalankan `.\DisplayFlip.exe --apply "Primary Kanan"` → susunan kembali persis.

```powershell
.\DisplayFlip.exe --list
.\DisplayFlip.exe --apply "Primary Kanan"
```

### S4 — CLI parity (SC-004)

1. `.\DisplayFlip.exe --mirror` vs klik tray menghasilkan layout sama.
2. `.\DisplayFlip.exe --apply "Primary Kanan"` vs tray > Terapkan profil > Primary Kanan menghasilkan layout sama.
3. `.\DisplayFlip.exe --apply "NoSuchProfile"` → exit code 3, pesan ke stderr.

### S5 — Missing monitor (FR-008)

1. Simpan profil berisi 2 monitor, putuskan monitor eksternal.
2. Jalankan Apply → tray akan bertanya "Lanjutkan dengan yang tersisa?" — pilih Batal → tidak ada perubahan; pilih Lanjutkan → topologi hanya dengan laptop. CLI: exit 4 bila dibatalkan.

### S6 — Startup toggle (FR-006)

1. Aktifkan toggle di tray. Logoff/login → ikon DisplayFlip muncul di tray tanpa klik manual.
2. Nonaktifkan → logoff/login → tidak muncul otomatis.

### S7 — Resolusi berbeda / 3 monitor (legalizer)

1. Atur 3 monitor dengan ukuran/resolusi berbeda berdamping. Jalankan `--mirror` → susunan tetap legal tanpa gap/overlap, primary tetap, desktop tidak tertata ulang oleh GDI.
2. Bila hasilnya tidak legal, catat sebagai bug; jangan fallback ke `SDC_ALLOW_CHANGES` tanpa redesign.

### S8 — Reboot stability (D2)

1. Simpan profil dengan 2 monitor identik pada port berbeda. Reboot.
2. Apply profil → monitor di port yang benar dipilih (cek dengan edid serial/friendly name di `--list --json` dan menu tray).
3. Bila dua monitor identik tidak bisa dibedakan, catat sebagai kebutuhan tambahan identitas (ContainerID/InstanceID) — bukan memaksa StableKey baru.

## Unit Tests

```powershell
dotnet test src/DisplayFlip.Tests/DisplayFlip.Tests.csproj -c Release
```

Pure logic (mirror primary-preserving, legalizer, profil encode/decode, missing-monitor policy) harus lulus tanpa monitor fisik.
