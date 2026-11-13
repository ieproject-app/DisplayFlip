# DisplayFlip — Windows 11 Native Mockup

Versi: v1.0 (2026-11-13)
Stack saat ini: C# .NET 8 + WinForms (tray + acrylic-less) — dokumen ini adalah desain target Windows 11 yang disengaja dibuat agar mudah dikonversi ke stack saat ini, lalu ke WinUI 3 tanpa menulis ulang logika inti.

Untuk Claude: mulai dari **wireframe ASCII + tabel token** di bawah ini. Jangan buat HTML interaktif penuh; cukup tampilkan 5 layar sesuai spesifikasi visual. Gunakan `Segoe UI Variable` dan Fluent Icons (bukan emoji).

---

## 1. Prinsip desain (Fluent / Windows 11)

| Prinsip | Keputusan untuk DisplayFlip |
|---|---|
| Mica untuk jendela settings | Akrylic hanya untuk flyout kecil & balloon, karena `Acrylic` penuh di WinForms tidak native penuh. Mica di WinForms dimungkinkan via `DwmSetWindowAttribute` + `DWM_SYSTEMBACKDROP_TYPE`. |
| Kedalaman via shadow, bukan border tebal | Flyout & dialog memakai shadow Win11 lembut; border 1px hanya untuk fokus keyboard |
| Sudut membulat konsisten | Jendela 8px, kartu/kontrol 4px, FAB/icon 8px. Tidak ada `rounded-2xl` seragam |
| Motion singkat | 120–200ms ease-out untuk show/hide flyout; hormati `SystemParameters.MenuAnimation` |
| Tipografi berhierarki | `Segoe UI Variable Display` untuk judul, `Segoe UI Variable Text` untuk isi. Judul 20/28, subtitle 14/20, body 12/16, caption 11/14. Bukan satu ukuran seragam |
| Aksen sistem | Hormati `UserAccentColor` untuk tombol primary & indikator; tidak pakai gradien ungu/biru |
| Bukan slop generik | Tidak ada gradien hero, tidak ada `bg-clip-text`, tidak ada card grid 3 kolom identik, tidak ada eyebrow pill + headline + 2 CTA identik, tidak ada bento, tidak ada glassmorphism di semua permukaan, tidak ada emoji sebagai ikon fitur |

## 2. Logo & ikon

- Logo saat ini: lencana charcoal netral `#24292E` dengan outline terang `#E6E8EB`, dua layar putih, panah tukar amber `#F2B134`. Ada varian 16–256px yang kontras di taskbar terang maupun gelap; pratinjau di `artifacts/DisplayFlip-logo-preview.png`.
- Ikon menu memakai **Fluent Icons / Segoe MDL2 Assets**, bukan emoji:
  - `Tukar kiri-kanan` → `ViewStream` (swap horizontal)
  - `Jadikan primary` → `Star` (atau `Pin` bila lebih jelas di 16px)
  - `Simpan` → `Save`
  - `Terapkan profil` → `Play` (atau `History`)
  - `Jalan otomatis` → `Contact` / `Settings`-toggle context
  - `Keluar` → `ChromeClose`
- Ikon tampil di 16px untuk tray flyout, 20px untuk submenu, 24px untuk dialog. Semua ikon berupa outline monochrome kecuali indikator primary (`Star` solid + badge).

## 3. Layar 1 — Tray flyout (layar utama)

Konteks: flyout muncul di atas ikon tray (kanan bawah), menganga ke atas, lebar 240px, padding 6px, acrylic background, sudut 8px, shadow level 2.

**Wireframe (ASCII):**

```
┌─────────────────────────────────────────┐
│ ▸ Tukar kiri-kanan              [swap]  │  ← hover state
│ ─────────────────────────────────────── │
│ Jadikan primary            ›  [monitor]│  ← submenu kanan
│   ┌───────────────────────────┐         │
│   │ ● Mouse A (primary) 1920… │         │
│   │ ○ HDMI 2          1920…   │         │
│   └───────────────────────────┘         │
│ Simpan layout sekarang        [save]   │
│ Terapkan profil               › [play] │
│ ─────────────────────────────────────── │
│ ☑ Jalan otomatis saat Windows start    │  ← checkbox, bukan toggle di menu
│ Keluar                       [close]   │
└─────────────────────────────────────────┘
```

**Tata letak & state:**

- Item height 32px (28px compact), icon 16px kiri, label 12pt Segoe UI Variable Text, shortcut hint kanan bila ada (mis. `Ctrl+Shift+M`).
- Hover: fill `LayerFillColorDefault` + radius 4px; press: fill `LayerFillColorAlt`; fokus keyboard: outline terlihat via `WM_KEY`.
- Separator 1px `DividerStrokeColorDefault`; tidak ada border kiri aksen.
- Submenu keluar ke kanan, lebar 260px, item monitor menampilkan friendly name + resolusi + badge `primary` (ikon `Star` solid amber kecil).
- Saat `<2 monitor`: item Mirror disabled dengan subteks sekunder `Perlu 2+ monitor`; submenu primary tetap dibuka untuk melihat daftar.
- Bila `Terapkan profil` kosong: tampilkan satu item disabled `(belum ada profil)` + CTA kecil di bawah `Simpan profil pertama`.

## 4. Layar 2 — Jendela Settings / Profil (Mica)

Konteks: jendela tunggal `720×540` dibuka dari tray item `Pengaturan…` (ditambahkan di Fase Desain, belum ada di build saat ini yang hanya popup InputBox) atau double-click ikon. Mica backdrop, judul `DisplayFlip` + logo 20px di titlebar. Navigasi kiri `NavigationView` sempit 200px (daftar: Profil, Umum, Tentang). Konten mengatur item terpilih.

**Wireframe:**

```
┌────────────────────────────────────────────────────────┐
│ [logo] DisplayFlip      —  □  X                        │
├──────────┬─────────────────────────────────────────────┤
│ Profil   │  Profil tersimpan                  [+ Baru] │
│ Umum     │  ┌─────────────────────────────────────────┐ │
│ Tentang  │  │ Primary Kanan          2 monitor  [Apply]│
│          │  │ Meja Kantor            2 monitor  [Apply]│
│          │  └─────────────────────────────────────────┘ │
│          │  Catatan: monitor hilang tidak diterapkan  │
│          │  secara diam-diam.                         │
├──────────┴─────────────────────────────────────────────┤
│ Ready  •  2 monitor terdeteksi                           │
└────────────────────────────────────────────────────────┘
```

**Komponen:**

- `NavigationView` kiri dengan indikator pill aksen 3px pada item aktif.
- Daftar profil sebagai `ListView` baris 56px: nama (semibold 12pt), metadata sekunder `2 monitor • diperbarui 13 Nov 2026`, tombol `Apply` di kanan (button secondary), `Hapus` dalam menu `...`.
- Status bar bawah 28px: dot hijau + teks `2 monitor terdeteksi • Primary: \\.\DISPLAY1`.
- Dark/light follows system: Mica tint mengikuti tema Windows.

## 5. Layar 3 — Dialog Simpan / Hapus

- `ContentDialog` 420px, judul `Simpan layout sekarang`, textbox nama profil (default `Primary Kanan`), tombol `Batal` / `Simpan` (primary). Bila nama duplikat, tampilkan inline warning `Nama sudah ada — akan ditimpa` dengan tombol `Timpa`.
- Dialog `Terapkan profil` dengan monitor hilang: judul `Monitor hilang`, body menjelaskan monitor mana yang hilang, tombol `Batal` / `Lanjutkan dengan yang tersisa`.

## 6. Token desain

| Token | Nilai |
|---|---|
| Font | Segoe UI Variable (Display untuk header, Text untuk isi) |
| Warna terang | Background `F3F3F3`, permukaan `FFFFFF`, teks `1B1B1B`, teks sekunder `6B6B6B` |
| Warna gelap | Background `202020`, permukaan `2B2B2B`, teks `FFFFFF`, teks sekunder `C7C7C7` |
| Aksen | Ikuti `AccentColor` sistem; fallback `#0067B8` (terang) / `#4CC2FF` (gelap) |
| Sudut | Window/flyout 8, kontrol 4, tombol 4 |
| Shadow | Win11 Level 2 untuk flyout/dialog (blur 24px, offset 0/8) |
| Padding base | 4px grid (8, 12, 16, 24) |
| Motion | 120ms flyout show, 83ms hide; kurva ease-out `cubic-bezier(0.1, 0.2, 0.4, 1)` |

## 7. Aksesibilitas & DPI

- Semua aksi flyout dapat dicapai via keyboard; `Esc` menutup flyout; `F10` membuka tray menu secara programatik untuk test keyboard.
- Kontras teks utama ≥ 4.5:1 di kedua tema (logo charcoal `#24292E` di atas permukaan terang tetap terbaca).
- Pada DPI ≥150% flyout dan dialog di-scaling via DPI awareness `PerMonitorV2`; ikon 16px tidak boleh di-upscale bitmap — pakai varian 20/24/32px dari ICO.
- Hormati `prefers-reduced-motion` sistem: lewati animasi show bila diminta.

## 8. Jalur konversi ke stack saat ini (tanpa rewrite)

1. **WinForms Fluent-like (sekarang, cepat):** aktifkan `DwmEnableBlurBehindWindow` + `ACCENTPOLICY` Acrylic untuk `ContextMenuStrip` flyout; gambar ulang dengan `ToolStripProfessionalRenderer` + owner-draw hover/separator memakai token di atas. Ikon ganti dari emoji ke `Segoe MDL2` via `System.Drawing.Icon` dari file `.ico` kecil atau `FontIcon` yang digambar manual untuk 16/20px.
2. **Tanpa ubah logika inti:** `DisplayFlip.Core.DisplayTopology` + `ProfileStore` (pure logic) dan adapter Windows (`EnumDisplayMonitors`/`ChangeDisplaySettingsEx`) tidak disentuh desain. Desain hanya mengganti `TrayHost` presentation + menambahkan Settings window sebagai permukaan baru yang memanggil service yang sama.
3. **WinUI 3 (nantinya, bila perlu):** port `DisplayFlip.Core` apa adanya ke `DisplayFlip.WinUI` dengan `MicaBackdrop`, `NavigationView`, dan `Flyout`. Larangan modernisasi di Fase 1 adalah sengaja — fokus pada preview statis ini untuk ACC dari owner sebelum mengikat ke toolkit.

## 9. Yang TIDAK ada di mockup

Tidak ada onboarding wizard, tidak ada bento feature grid, tidak ada glow berwarna, tidak ada gradien ungu-biru, tidak ada popover tambahan selain yang dijelaskan. Belum ada multi-bahasa — UI Indonesia dulu.

## 10. Cara Claude mereproduksi preview

Buat 5 gambar statis (HTML atau PNG terserah) dengan lebar yang sama seperti wireframe di atas, menggunakan `Segoe UI Variable`, warna token di bagian 6, ikon `Segoe Fluent Icons`, dan tampilkan flyout di atas latar taskbar terang + gelap untuk membuktikan logo terbaca. Pengiriman atau ACC nanti diserahkan ke owner; belum ada kode WinForms yang diubah dari dokumen ini.
