# Research: DisplayFlip multi-monitor layout

**Feature**: 001-displayflip-monitor-layout
**Date**: 2026-11-12
**Spec**: [spec.md](spec.md)

## Decision Summary

| # | Decision | Status |
|---|----------|--------|
| D1 | Stack & proyek: C# .NET 8, single-project, Core portable vs Windows adapter vs Tray/CLI | Resolved |
| D2 | Identitas monitor stabil untuk kunci profil | Resolved (design) — uji di monitor asli |
| D3 | Operasi Mirror primary-preserving yang legal | Resolved (design) — uji di monitor asli |
| D4 | SetPrimary via (0,0) + legalizer | Resolved |
| D5 | Profil JSON + perilaku monitor hilang | Resolved |
| D6 | Pola CCD validate-then-apply + error handling | Resolved |
| D7 | Portability ke PowerToys | Resolved (boundary) |

---

### D1 — Supersede Planet-Lazy: Tray via WinForms NotifyIcon, tanpa paket eksternal

- **Decision**: Gunakan `System.Windows.Forms.NotifyIcon` + `ContextMenuStrip` (sudah ada di .NET 8 Windows desktop pack) untuk tray; jangan tambah paket tray pihak ketiga pada v1. Entry point ganda: `Program.cs` mendeteksi `--mirror` / `--apply` → mode headless CLI, sebaliknya → `Application.Run(new TrayHost())`.
- **Rationale**: Inbox API, tanpa volume unduhan, perilaku konsisten di Windows 10/11. Spec hanya butuh klik sekali dan submenu monitor/profil — tidak butuh flyout kompleks ala PowerToys.
- **Alternatives considered**: WinUI 3 tray — rejected, lebih berat, butuh extra bring-up; `Hardcodet.NotifyIcon` atau `Microsoft.Toolkit` — rejected, dependensi external untuk fungsi yang sudah tersedia.

### D2 — Identitas Monitor Stabil (menutup NEEDS CLARIFICATION implisit)

- **Decision**: Kunci profil ke gabungan identitas fisik dari `DISPLAYCONFIG_TARGET_DEVICE_NAME`: `monitorDevicePath` + friendly name + EDID manufacture/product. Derivasi `DeviceID` dilakukan via SetupAPI dari `monitorDevicePath` substring (contoh `DELF06B`). Primary key persisten = `{ monitorDevicePath, EDID Manufacture+Product+Serial bila tersedia, FriendlyName }`. Jangan gunakan ordinal "Display 1/2" dan jangan gunakan hash `adapterId+id` mentah sebagai kunci persisten.
- **Rationale**: Microsoft Learn menegaskan `monitorDevicePath` bisa dipakai SetupAPI untuk memperoleh nama di installation package; substring shared memungkinkan pemetaan path→DeviceID yang dipakai Chromium/libdisplaydevice untuk stabilitas lintas reboot/driver/port. Klaim `adapterId` berubah tidak terverifikasi di riset — karenanya jangan dijadikan premis, tapi juga jangan mengandalkan adapterId untuk persisten.
- **Alternatives considered**: `DEVMODE dmDeviceName` / ordinal — rejected, tidak stabil saat Windows mengacak penomoran; hanya `adapterId+id` — rejected, hilang makna lintas call; Full EDID blob hash — rejected, brittle untuk profil berawak; ContainerID/InstanceID saja — rejected, tidak cukup untuk Bedakan dua monitor identik di port berbeda (dikombinasikan sebagai field pendukung di Data Model, bukan kunci tunggal).
- **Risk & test**: Jika dua monitor identik memakai port berbeda, EDID saja tidak membedakan. Ditutup di Tasks dengan test manual docolok/cabut di mesin asli (lihat quickstart). Kriteria sukses: profil Applied di 2 monitor sama-sama "DELL P2419H" pada port A/B tetap memilih yang benar setelah reboot.

### D3 — Mirror Primary-Preserving yang Legal (diferensiasi DisplayFlip)

- **Decision**: Kontrak operasional: Given sekumpulan `Position { monitorId, x, y, width, height }` dengan primary pada (0,0), operasi Mirror menghasilkan `x' = totalWidth − (x + width)` per monitor, lalu menetapkan monitor yang sama sebagai primary (yakni `x'=0` untuk monitor primary itu), lalu **legalisasi**: pastikan tidak ada gap/overlap dengan menggeser sepanjang x sesuai urutan mirror (Kruskal sederhana: sort by `x'`, letakkan tanpa celah dari 0). Semua operasi menghasilkan layout legal sebelum validate.
- **Rationale**: Hasil riset: tidak ada dari DisplayProfileManager / Monarch / Freds-Monitor-Tool yang terverifikasi menyediakan ini. Kontrak ini dijaga presisi agar dapat diuji unit tanpa monitor fisik.
- **Alternatives considered**: Hanya swap dua monitor tanpa menghitung ulang — rejected, rusak untuk 3+ monitor & ukuran berbeda; Menjadikan monitor kiri sebagai primary baru — rejected, melanggar kebutuhan user "primary tetap sama"; Memakai `SDC_ALLOW_CHANGES` untuk membiarkan Windows menata ulang — rejected, perilaku tidak tentu untuk Speckit.

### D4 — SetPrimary via (0,0) + Legalizer

- **Decision**: Untuk SetPrimary(monitorId): geser semua posisi agar `position(monitorId) = (0,0)`, lalu legalisasi urutan lain mengikuti di belakangnya dengan aturan tanpa gap/overlap. Monitor lain yang semula di kiri target digeser ke kanan secara tertib. Validate lalu apply.
- **Rationale**: Dokumentasi `DISPLAYCONFIG_SOURCE_MODE`: permukaan di (0,0) selalu primary; GDI akan menata ulang bila aturan gap/overlap dilanggar. Kontrak ini membuat hasil dapat ditebak dan dapat diuji.
- **Alternatives considered**: Langsung ubah flag primary tanpa menggeser koordinat — rejected, tidak sesuai CCD semantics; Memakai `SetDisplayConfig` dengan `SDC_ALLOW_CHANGES` sebagai shortcut — rejected, bukan cara yang dijamin.

### D5 — Profil JSON + Perilaku Monitor Hilang (selaras Freds-Monitor-Tool)

- **Decision**: Profil disimpan sebagai JSON schema-versioned di `%LocalAppData%/DisplayFlip/profiles.json`. Setiap profil menyimpan `schemaVersion`, `name`, list `MonitorState { stableKey, position, resolution, orientation, enabled }`, dan `primaryStableKey`. Saat ApplyProfile menemukan monitor hilang: jangan menggantung; tampilkan pesan "monitor X/Y hilang, lanjutkan dengan yang tersisa?" dengan opsi Batal / Lanjutkan; log `MissingMonitor` ke stderr pada CLI (exit code non-zero bila dibatalkan).
- **Rationale**: Peringatan Freds-Monitor-Tool "YOU MAY NEED TO DELETE AND RECREATE PROFILES AFTER DRIVER OR WINDOWS UPDATES" terbukti benar di ekosistem. Profil yang diasumsikan abadi akan menyesatkan. Best-effort + opt-in lanjutan menyeimbangkan utilitas dan kejelasan.
- **Alternatives considered**: Auto-recreate profil diam-diam — rejected, membingungkan; Gagal total saat ada satu monitor hilang — rejected, terlalu kaku untuk setup kantor yang berubah.

### D6 — Pola CCD Validate-then-Apply (SDC_VALIDATE → SDC_APPLY)

- **Decision**: Baca topologi dengan `QDC_ONLY_ACTIVE_PATHS` (bukan `QDC_ALL_PATHS` — mahal dan tidak direkomendasikan). Setiap mutasi (Mirror/SetPrimary/ApplyProfile): (1) hitung topologi baru di Core, (2) panggil `SetDisplayConfig(..., SDC_VALIDATE)`, (3) bila valid, panggil `SetDisplayConfig(..., SDC_APPLY)` — hanya satu flag yang aktif per panggilan. Bila `ERROR_BAD_CONFIGURATION`, tampilkan pesan dan jangan fallback sembarang ke `SDC_ALLOW_CHANGES` tanpa persetujuan user; tangani secara eksplisit di Tasks.
- **Rationale**: Ini adalah pola resmi Microsoft Learn. `QDC_ALL_PATHS` dokumentasinya "bisa sangat mahal" sehingga hanya untuk validasi koneksi bila perlu.
- **Alternatives considered**: Apply langsung tanpa validate — rejected, gagal tidak dapat didiagnosis; Default `SDC_ALLOW_CHANGES` — rejected, menyembunyikan error desain.

### D7 — Batas Portability ke PowerToys

- **Decision**: Seluruh logika murni (topology math, legalizer, profil (de)serialize, missing-monitor policy) hidup di `DisplayFlip.Core` tanpa P/Invoke. Windows adapter / tray / CLI adalah shell tipis. Pada Fase 2, bawa hanya `DisplayFlip.Core` + adapter tipis ke modul PowerToys Power Display / arah Issue #48286; jangan fork seluruh repo PowerToys sebelum prototipe terbukti.
- **Rationale**: Mengurangi risiko merge upstream & beban build (C++/C# campuran, VS workload) yang bukan bagian klaim terverifikasi; ukur langsung di mesin target saat Speckit. Riset membuktikan Power Display masih DDC-only sehingga Fase 1 tidak tumpang tindih/ duplikasi.
- **Alternatives considered**: Fork PowerToys sejak hari pertama — rejected, biaya build + merge upstream tinggi sebelum nilai terbukti; Menulis ulang dari nol tanpa prior art — rejected, DisplayProfileManager/Monarch sudah membuktikan pola tray/profile yang sama.
