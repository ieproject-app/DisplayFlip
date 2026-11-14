# Laporan Audit UI Windows 11, Kesiapan Upgrade .NET 10, dan Evaluasi Library Tema WPF — DisplayFlip

**Tanggal Audit**: 14 November 2026  
**Workspace**: `D:\GitHub\multi-monitor` (Branch aktif: `design/win11-fluent`)  
**Mode**: Read-Only Audit & Perencanaan (Tanpa Modifikasi Kode Sumber)

---

## 1. Struktur Proyek dan Teknologi UI yang Dipakai Sekarang

### 1.1 Arsitektur Solusi & Proyek
Solusi didefinisikan menggunakan format XML Solution baru di [DisplayFlip.slnx](file:///D:/GitHub/multi-monitor/DisplayFlip.slnx) dengan konfigurasi properti global di [Directory.Build.props](file:///D:/GitHub/multi-monitor/Directory.Build.props) (`ManagePackageVersionsCentrally = false`). Terdapat **4 proyek** di dalam folder `src/`:

| Proyek | File `.csproj` | Target Framework | SDK & Output | Peran Arsitektural |
| :--- | :--- | :--- | :--- | :--- |
| **DisplayFlip.Core** | [DisplayFlip.Core.csproj](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Core/DisplayFlip.Core.csproj) | `net8.0` | `Microsoft.NET.Sdk` (`Library`) | Logika domain murni bebas Win32/UI: perhitungan topologi 2D, mirror primary-preserving, magnetic snap, legalizer ([DisplayTopology.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Core/DisplayTopology.cs)), model data ([Models.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Core/Models.cs)), dan penyimpanan profil JSON atomik ([ProfileStore.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Core/ProfileStore.cs)). |
| **DisplayFlip.Windows** | [DisplayFlip.Windows.csproj](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/DisplayFlip.Windows.csproj) | `net8.0-windows` | `Microsoft.NET.Sdk` (`Library`, `UseWindowsForms=true`) | Adapter OS Windows: P/Invoke Win32 CCD API ([NativeMethods.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/NativeMethods.cs), [CcdTopologyAdapter.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/CcdTopologyAdapter.cs)), fallback GDI/User32 `EnumDisplayMonitors` + `ChangeDisplaySettingsEx` ([ScreenFallback.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/ScreenFallback.cs)), pembacaan EDID ([MonitorIdentity.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/MonitorIdentity.cs)), orkestrasi layanan profil ([ProfileService.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/ProfileService.cs)), proyeksi multi-layar ([ProjectionDispatcher.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/ProjectionDispatcher.cs)), dan auto-start registry ([StartupRegistry.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/StartupRegistry.cs)). |
| **DisplayFlip** | [DisplayFlip.csproj](file:///D:/GitHub/multi-monitor/src/DisplayFlip/DisplayFlip.csproj) | `net8.0-windows` | `Microsoft.NET.Sdk.WindowsDesktop` (`WinExe`, `UseWindowsForms=true`) | Aplikasi utama: entry point CLI + GUI ([Program.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/Program.cs)), host system tray ([TrayHost.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/TrayHost.cs)), jendela Settings, menu flyout, dialog, tema, dan generator ikon. |
| **DisplayFlip.Tests** | [DisplayFlip.Tests.csproj](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Tests/DisplayFlip.Tests.csproj) | `net8.0-windows` | `Microsoft.NET.Sdk` (`IsTestProject=true`) | Suite pengujian unit dan regresi (xUnit) untuk matematika topologi, penyimpanan profil, paritas CLI, stabilitas tray, token tema, serta uji interaksi dan overlap layout Settings. |

### 1.2 Teknologi UI & Library Tema Saat Ini
- **Framework UI**: **Windows Forms (WinForms)** (`<UseWindowsForms>true</UseWindowsForms>`), **bukan WPF dan bukan WinUI 3**.
- **Library Tema**: **Tidak menggunakan library tema pihak ketiga sama sekali**. Seluruh tampilan ala Windows 11 dibangun secara manual dari nol menggunakan:
  - Custom owner-draw GDI+ (`System.Drawing`, `OnPaint`, `GraphicsPath`) untuk tombol, kartu, checkbox, dropdown, dan kanvas monitor.
  - `ToolStripProfessionalRenderer` kustom ([FluentStripRenderer](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentMenu.cs#L251-L418)) untuk menggambar ulang `ContextMenuStrip`.
  - P/Invoke `dwmapi.dll` (`DwmSetWindowAttribute`, `DwmGetColorizationColor`), `uxtheme.dll` (`SetWindowTheme`), dan `user32.dll` (`SetWindowCompositionAttribute`, `RegisterHotKey`, `WM_NCHITTEST`, `WM_NCCALCSIZE`).
- **Konfigurasi Runtime RollForward**: Ketiga proyek non-test mendeklarasikan `<RollForward>Major</RollForward>` agar binari target `net8.0-windows` dapat dijalankan di atas runtime .NET yang lebih baru (.NET 10). Pada mesin ini, terdeteksi **.NET SDK `10.0.403`** dengan runtime `Microsoft.WindowsDesktop.App 8.0.33` dan `10.0.14` di atas Windows 11 (`10.0.26300`).

### 1.3 Daftar Paket NuGet yang Terpasang
Proyek utama (`DisplayFlip`, `DisplayFlip.Core`, `DisplayFlip.Windows`) memiliki **0 dependensi NuGet eksternal**. Paket NuGet hanya dipasang pada proyek pengujian [DisplayFlip.Tests.csproj](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Tests/DisplayFlip.Tests.csproj#L10-L15):

| Paket NuGet | Proyek | Versi Terpasang | Fungsi |
| :--- | :--- | :--- | :--- |
| `Microsoft.NET.Test.Sdk` | `DisplayFlip.Tests` | `17.11.0` | Infrastruktur MSBuild dan host runner VSTest |
| `xunit` | `DisplayFlip.Tests` | `2.9.0` | Framework pengujian unit utama |
| `xunit.runner.visualstudio` | `DisplayFlip.Tests` | `2.8.0` | Adapter test runner untuk `dotnet test` / Visual Studio |
| `FluentAssertions` | `DisplayFlip.Tests` | `6.12.0` | Library assertion deklaratif (`.Should().Be(...)`) |

---

## 2. Daftar Layar dan Komponen UI Beserta File-nya

Berikut adalah pemetaan lengkap seluruh layar (jendela, flyout, dialog, overlay) dan komponen UI kustom yang ada di kode sumber saat ini:

### 2.1 Layar & Jendela (Windows / Forms / Flyouts)

| No | Nama Layar / Permukaan | Class & Jenis Kontrol | File & Rentang Baris | Ringkasan Isi & Perilaku |
| :-: | :--- | :--- | :--- | :--- |
| **1** | **System Tray Flyout Utama & 3 Submenu** | `TrayHost` (`ApplicationContext`) + `FluentMenu` (`ContextMenuStrip`) | [TrayHost.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/TrayHost.cs#L31-L109)<br>[TrayHost.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/TrayHost.cs#L416-L575)<br>[FluentMenu.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentMenu.cs#L14-L128) | Flyout utama lebar `240px` di atas ikon tray (`NotifyIcon`). Memuat item: *Tukar kiri-kanan*, *Identifikasi layar*, submenu *Multi-layar* (`260px`: *Perluas*, *Hanya Layar 1*, *Hanya Layar 2*, *Duplikat*), separator, submenu *Jadikan primary* (`260px`: daftar monitor 2 baris `44px` + radio + badge bintang amber `primary`), *Simpan layout sekarang*, submenu *Terapkan profil* (`260px`: daftar profil + shortcut hint), separator, *Pengaturan...*, checkbox *Jalan otomatis saat Windows start*, dan *Keluar*. |
| **2** | **Jendela Pengaturan Utama (Settings Window)** | `FluentSettingsForm` (`Form`) | [FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L19-L3585) | Jendela Sizable (`980×720` default, minimum `680×560`, 3 breakpoint responsif: `Compact < 850px`, `Normal 850–1159px`, `Wide >= 1160px`). Terdiri dari custom titlebar (`34px`), panel navigasi kiri (`216px` / `52px` compact / `232px` wide), status bar bawah (`28px`), dan 4 halaman tab:<br>• **Tab 1 — Layar & Profil** (`_profilePage`, [L363–L764](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L363-L764)): banner layar tunggal (`_singleDisplayBanner`), kanvas drag-and-drop monitor 2D (`_monitorCanvas`) dengan toolbar bawah internal (`42px`), 3 kartu pengaturan monitor terpilih (`_cardIdentity`, `_cardQuickMove`, `_cardResOrient`), serta seksi *Profil tersimpan* (`_profileListPanel` + tombol `+ Simpan Profil` + menu konteks `...` per profil).<br>• **Tab 2 — Pintasan Keyboard** (`_shortcutPage`, [L768–L805](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L768-L805), [L2630–L2796](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L2630-L2796)): daftar kartu pintasan untuk 6 *Aksi Cepat Global* dan pintasan per profil tersimpan.<br>• **Tab 3 — Umum & Ikon** (`_generalPage`, [L809–L976](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L809-L976)): 3 kartu pemilih gaya ikon live (*Fluent Glass Dual*, *Windows 11 Accent*, *Minimalist Precision*), kartu startup & auto-apply drag (`_startupCard`), dan kartu lokasi penyimpanan JSON (`_storageCard`).<br>• **Tab 4 — Tentang & CLI** (`_aboutPage`, [L980–L1048](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L980-L1048)): kartu deskripsi aplikasi (`_aboutCard`) dan kartu referensi argumen CLI (`_cliCard`). |
| **3** | **Menu Konteks Baris Profil (`...`)** | `FluentMenu` (`250px`) | [FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L2568-L2628) | Muncul saat tombol `...` (`MoreButton`) pada baris profil diklik. Berisi 5 aksi: *Terapkan profil*, *Edit & setup profil...*, *Atur pintasan keyboard...*, *Perbarui dari susunan kanvas*, separator, dan *Hapus profil*. |
| **4** | **Menu Dropdown Multi-layar di Kanvas** | `FluentMenu` (`188px`) | [FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L1927-L2006) | Muncul saat tombol `Multi-layar ▾` di toolbar bawah kanvas diklik. Menampilkan opsi radio *Perluas (Extend)*, *Hanya Layar 1*, *Hanya Layar 2*, dan *Duplikat (Clone)*. |
| **5** | **Dialog Simpan & Setup Profil** | `SaveProfileDialog` (`FluentDialogBase` / `Form`) | [FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L426-L780) | Dialog modal borderless lebar `420px` untuk membuat atau mengedit profil. Memuat input `Nama profil` (dengan garis bawah aksen `2px`), dropdown `Layar utama (Primary) untuk profil ini`, kotak rekam `Pintasan keyboard` + tombol `Hapus`, pita peringatan kuning (`_cautionBar`) beserta tombol `Timpa` saat nama duplikat, dan footer `Batal` / `Simpan`. |
| **6** | **Dialog Monitor Hilang** | `MissingMonitorDialog` (`FluentDialogBase` / `Form`) | [FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L785-L922) | Dialog modal borderless lebar `420px` yang muncul saat profil yang akan diterapkan merujuk ke monitor yang sedang tidak terhubung. Menampilkan nama monitor yang hilang dan yang tersisa, serta tombol `Batal` dan `Lanjutkan dengan yang tersisa`. |
| **7** | **Dialog Perekam Pintasan Keyboard** | `ShortcutRecorderDialog` (`FluentDialogBase` / `Form`) | [FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L1041-L1198) | Dialog modal borderless lebar `420px` untuk merekam kombinasi tombol global (`Ctrl`/`Alt`/`Shift`/`Win` + Key atau `F1–F12`), dilengkapi 4 tombol preset cepat (`Ctrl+Alt+F`, `Ctrl+Alt+P`, `Ctrl+Alt+1`, `Ctrl+Alt+2`), tombol `Kosongkan`, serta footer `Batal` / `Simpan Pintasan`. |
| **8** | **Overlay Identifikasi Nomor Layar** | `IdentifyOverlayForm` (`Form`) | [FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L1275-L1447) | Jendela popup non-activating (`WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`, ukuran `220×128`) yang tampil selama `2200ms` di sudut kiri-bawah setiap monitor fisik, menampilkan nomor urut layar (`48px Bold`) dan nama monitor. |

### 2.2 Komponen Kontrol & Infrastruktur UI Kustom

| Komponen / Class | File & Baris | Peran Teknis |
| :--- | :--- | :--- |
| `FluentTheme` & `FluentPalette` | [FluentTheme.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentTheme.cs#L12-L214) | Single Source of Truth token warna (Light/Dark), metrik ukuran, pembaca tema/aksen registry Windows & DWM, kalkulator rasio kontras WCAG, serta cache statis objek `Font` (`Segoe UI Variable Display/Text`, `Consolas`). |
| `FluentIcons` | [FluentIcons.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentIcons.cs#L13-L368) | Konstanta kode glyph Unicode `Segoe Fluent Icons` (fallback `Segoe MDL2 Assets`), cache render glyph ke `Bitmap`, serta generator vektor `.ico` multi-size (`16, 20, 24, 32, 48, 64, 128, 256px`) untuk 3 gaya ikon aplikasi (*fluent*, *accent*, *minimal*). |
| `FluentMenuItem` & `FluentStripRenderer` | [FluentMenu.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentMenu.cs#L198-L418) | Item menu dan renderer kustom untuk menggambar state hover/press (radius `4px`), ikon `16px`, checkbox, radio button, subteks baris kedua, shortcut hint, dan lencana `primary`. |
| `FluentDialogBase` | [FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L357-L421) | Form dasar untuk seluruh dialog modal (`FormBorderStyle.None`, `CS_DROPSHADOW`, pemotongan sudut via `DwmRoundCorners` atau fallback `Region`, dan handler tombol `Esc`). |
| `FluentDialogButton` | [FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L8-L132) | Tombol kustom owner-draw (mendukung varian Primary beraksen dan Secondary berbingkai tipis, state hover/press/disabled, cue fokus keyboard, dan glyph ikon di kiri teks). |
| `FluentCheckBox` | [FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L137-L225) | Checkbox kustom owner-draw dengan kotak `18×18px` bersudut `4px` dan centang glyph `\uE73E` (menggantikan checkbox klasik Win32). |
| `FluentSelectButton` | [FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L231-L355) | Kontrol pemilih dropdown kustom yang membuka `FluentMenu` saat diklik (menggantikan `ComboBox` Win32 klasik yang bermasalah di Dark Mode). |
| `GlobalHotkeyHost` & `HotkeyParser` | [FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L927-L1036), [L1203–L1270](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L1203-L1270) | Parser string kombinasi tombol dan `NativeWindow` penangkap pesan `WM_HOTKEY` (`0x0312`) dari Win32 `RegisterHotKey`. |
| `BufferedPanel`, `CaptionButton`, `NavButton`, `MoreButton` | [FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L3412-L3584) | Kontrol internal jendela Settings: panel double-buffered dengan `WS_EX_COMPOSITED`, tombol titlebar (`—`, `□`, `✕`), tombol navigasi kiri dengan indikator pill aksen `3×18px`, dan tombol menu `...`. |

---

## 3. Bagian yang Sudah Mendekati Gaya Windows 11 dan yang Menyimpang

Evaluasi berikut membandingkan kode sumber dan tangkapan layar build di `docs/review/fase-h/` serta `docs/review/fase-c/` terhadap spesifikasi target di [docs/displayflip-windows11-mockup.md](file:///D:/GitHub/multi-monitor/docs/displayflip-windows11-mockup.md) dan pedoman desain Windows 11 (Fluent 2 / WinUI 3).

### 3.1 Bagian yang Sudah Mendekati Gaya Windows 11

1. **Palet Warna Dasar & Kontras Teks ([FluentTheme.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentTheme.cs#L31-L74))**:
   - Token warna terang (`Background #F3F3F3`, `Surface #FFFFFF`, `Text #1B1B1B`, `TextSecondary #6B6B6B`) dan gelap (`Background #202020`, `Surface #2B2B2B`, `Text #FFFFFF`, `TextSecondary #C7C7C7`) serta warna pita peringatan (`#FFF4CE`/`#9D5D00` terang, `#433519`/`#FCE100` gelap) sudah persis mengikuti tabel token bagian 6 dokumen mockup dan lulus uji kontras $\ge 4.5:1$ ([FluentThemeTests.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/ FluentThemeTests.cs)).
2. **Keluarga Font & Ikonografi Vektor ([FluentTheme.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentTheme.cs#L177-L196), [FluentIcons.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentIcons.cs#L13-L98))**:
   - Menggunakan `Segoe UI Variable Display` (judul `20px`, nomor layar `26px`/`48px`) dan `Segoe UI Variable Text` (subtitle `14px`, body `12px`, caption `11px`) dengan satuan `GraphicsUnit.Pixel`.
   - Seluruh ikon fitur menggunakan glyph `Segoe Fluent Icons` (fallback `Segoe MDL2 Assets`) yang di-render langsung sesuai ukuran target (`10px`, `12px`, `14px`, `16px`), tanpa emoji dan tanpa upscale bitmap kasar.
3. **Sudut Jendela DWM & Integrasi Snap Layouts Windows 11**:
   - Pemotongan sudut `8px` dan bayangan jendela/menu mendelegasikan ke atribut resmi Windows 11 `DWMWA_WINDOW_CORNER_PREFERENCE = 33` (`DWMWCP_ROUND = 2`) di [FluentMenu.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentMenu.cs#L100-L112).
   - `FluentSettingsForm` menangani `WM_NCHITTEST` untuk mengembalikan `HTMAXBUTTON (9)` saat kursor berada di atas `_maxBtn` ([FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L1149-L1165), [L1208–L1209](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L1208-L1209)), sehingga fitur **Windows 11 Snap Layouts flyout** berfungsi native.
4. **Pengalaman Kanvas Monitor Interaktif (`_monitorCanvas`)**:
   - Konsep sumur kanvas (`WellBackground`) dengan ubin layar bernomor, lencana bintang primary, drag-and-drop 2D bebas, magnetic edge snap, garis bantu perataan putus-putus (`_activeGuideLines`), serta tiga `SettingsCard` di bawahnya sangat mendekati tata letak **Windows 11 Settings > System > Display**.

### 3.2 Bagian yang Menyimpang / Bermasalah

#### A. Material Mica Tidak Terlihat & Bingkai Non-Client Bertumpuk (Kritis)
- **Mica tertutup cat GDI+ solid**: Di [FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L1870-L1885), `TryEnableMica()` memanggil `DwmSetWindowAttribute(Handle, 38, ref mica, sizeof(int))` (`DWMWA_SYSTEMBACKDROP_TYPE = 2`). Akan tetapi, di WinForms, efek Mica dari DWM hanya dapat terlihat apabila area non-client di-extend ke client area (`DwmExtendFrameIntoClientArea`) dan permukaan di atasnya tidak dicat dengan warna solid opaque. Saat ini, `_root`, `_titleBar`, `_navPanel`, dan `_statusBar` dicat penuh dengan `pal.ShellBackground` (`#EBEBEB` / `#1C1C1C` alpha 255), sedangkan `_contentHost` dicat penuh dengan `pal.Background` (`#F3F3F3` / `#202020` alpha 255). Akibatnya, **material Mica tertutup 100% oleh warna solid** dan tidak pernah tampak.
- **Bingkai jendela klasik & tombol caption ganda (Double-Drawn Caption Buttons)**: Pada [FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L259), jendela disetel `FormBorderStyle = FormBorderStyle.Sizable`, lalu pada `WndProc` ([L1086–L1107](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L1086-L1107)) pesan `WM_NCCALCSIZE` langsung ditelan (`m.Result = IntPtr.Zero`) tanpa memanggil `DwmDefWindowProc` atau `DwmExtendFrameIntoClientArea`. Akibatnya (terlihat jelas di seluruh tangkapan layar `docs/review/fase-h/settings-v2-*.png`), muncul **bingkai tepi biru-abu tebal ala Windows Basic/Aero** di sekeliling jendela, dan **tombol Minimize/Maximize/Close bawaan Win32 tergambar menumpuk** di atas `_minBtn`, `_maxBtn`, dan `_closeBtn` kustom.

#### B. Bug Pemotongan Layout & Scrollbar di Tab Pintasan Keyboard (Kritis)
- **Panel `_shortcutListPanel` terpotong separuh**: Di [FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L2348-L2359), saat pengguna berpindah tab melalui `SwitchTab("Pintasan")`, metode `LayoutShortcutPage()` **tidak dipanggil** (hanya `RefreshData` $\rightarrow$ `RebuildShortcutRows()` yang dijalankan). Selain itu, saat jumlah baris pintasan melebihi tinggi panel (`AutoScroll = true`), kemunculan scrollbar Win32 memicu penyempitan area klien seperti yang terekam pada [docs/review/fase-h/settings-v2-pintasan.png](file:///D:/GitHub/multi-monitor/docs/review/fase-h/settings-v2-pintasan.png): lebar daftar pintasan menyempit drastis menjadi ~280px di tengah halaman, memunculkan **scrollbar horizontal dan vertikal klasik sekaligus**, memotong teks deskripsi, dan **menyembunyikan seluruh tombol perekam pintasan** di sisi kanan kartu.

#### C. Teks, Ikon, dan Tombol yang Terpotong / Cacat Visual (Spasi & Tipografi)
Berdasarkan pemeriksaan kode dan bukti visual di `docs/review/fase-h/`:
1. **Duplikasi tanda `+` pada tombol Simpan Profil**: Di [FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L706-L713), `_newProfileBtn` menggunakan `Glyph = FluentIcons.Add` (`\uE710`, ikon `+`) **sekaligus** string `Text = "+ Simpan Profil"`. Hasilnya di layar tertulis **`+ + Simpan Profil`** (dua tanda tambah berjejer).
2. **Karakter `&` hilang pada judul kartu ke-3**: Di [FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L660-L667), `Label` `_resOrientTitle` diberi teks `"Resolusi & orientasi tampilan"` tanpa menyetel `UseMnemonic = false`. WinForms memproses `& ` sebagai mnemonic garis bawah pada karakter spasi, sehingga teks tampil cacat sebagai **`"Resolusi  orientasi tampilan"`** (dan di mode compact menjadi **`"Resolusi  orientasi"`** serta subteksnya **`"Ukuran piksel  putaran"`** di [L1644–L1645](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L1644-L1645)).
3. **Tombol `"Hanya Layar 2 Saja"` terpotong**: Di `LayoutCardIdentity` ([FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L1593-L1600)), lebar `_soloDisplayBtn` dipatok `126px` (normal) dan `116px` (compact). Lebar ini tidak cukup untuk menampung ikon `14px` + spasi + teks `"Hanya Layar 2 Saja"`, sehingga di mode normal huruf akhir mepet ke border kanan (`"Hanya Layar 2 Saja"`) dan di mode compact/min terpotong menjadi **`"Hanya Layar 2 Sa"`**.
4. **Tombol Toolbar Kanvas terpotong pada mode Compact/Min**: Di `LayoutProfilePage` ([FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L1372-L1382)), pada cabang `availRightSpace >= 340`, `_identifyBtn` diberi lebar `84px` dan `_alignBtn` diberi lebar `80px`. Pada `settings-v2-compact.png` dan `settings-v2-min.png`, teks tombol terpotong menjadi **`"Identifikas"`** dan **`"Sejajarkar"`**.
5. **Teks Tab Umum & Ikon terpotong di 3 tempat** ([settings-v2-umum.png](file:///D:/GitHub/multi-monitor/docs/review/fase-h/settings-v2-umum.png)):
   - Subjudul `_iconSectionTitle` ([L827–L836](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L827-L836)) terpotong menjadi `"Pilih gaya ikon modern DisplayFlip (berlaku untuk Taskbar, Titlebar"`.
   - Checkbox kedua di `_startupCard` (`_autoApplyGeneralCheck`, [L885–L894](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L885-L894)) terpotong menjadi `"Langsung terapkan perubahan saat selesai menggeser la..."` karena `FluentCheckBox` menggambar teks dengan `TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis` ([FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L223)).
   - Subteks `_autoApplySub` ([L907–L915](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L907-L915)) terpotong di tengah kalimat menjadi `"Bebas geser layar tanpa dialog konfirmasi berulang seperti"`.
6. **Teks CLI di Tab Tentang terpotong & wrapping berantakan** ([settings-v2-tentang.png](file:///D:/GitHub/multi-monitor/docs/review/fase-h/settings-v2-tentang.png)):
   - Pada `_cliText` ([L1030–L1041](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L1030-L1041)), spasi perataan manual pada font `Consolas` terlalu panjang untuk lebar kartu sehingga setiap baris perintah patah ke baris baru, dan baris ke-4 (`DisplayFlip.exe --tray`) hilang terpotong di bawah tinggi `Label` (`82px`). Selain itu, argumen CLI baru (`--solo`, `--internal`, `--external`, `--extend`, `--clone`) belum dicantumkan di kartu tersebut.

#### D. Warna Aksen, Sudut Kartu, dan Kontrol yang Masih Bergaya Lama
1. **Pemilihan Shade Warna Aksen & Kontras Radio Dark Mode**:
   - Di [FluentTheme.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentTheme.cs#L117), ekspresi `int idx = dark ? 3 : 3;` selalu mengambil indeks ke-3 dari `AccentPalette` baik untuk mode terang maupun gelap. Pada tangkapan layar `docs/review/fase-c/02-tray-submenu-primary-gelap.png` dan `04-dialog-simpan.png`, ketika aksen sistem berwarna ungu tua, titik radio aktif dan tombol primary `Simpan` menjadi sangat gelap di atas permukaan `#2B2B2B` (hampir tidak terbaca). Sebaliknya, pada `fase-h`, warna magenta menyala dipakai mentah tanpa penyesuaian shade `Light1`/`Dark1` standar WinUI 3.
2. **Artefak Sudut Kotak pada Kartu (`CreateSurfaceCard` & `_contentHost.Region`)**:
   - `CreateSurfaceCard` ([FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L3381-L3399)) menggunakan `Panel` biasa dengan `BackColor = FluentTheme.Current.Surface` dan hanya menggambar garis border lengkung `RoundedRect` di dalam `Paint`. Piksel di luar lengkungan sudut `4px` tetap berwarna `Surface` (`#FFFFFF`), sehingga sudut kartu secara visual tetap tampak lancip/kotak.
   - Sudut kiri-atas `_contentHost` dipotong menggunakan `Region = new Region(path)` ([L1258–L1266](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L1258-L1266)). Karena Win32 window `Region` bersifat 1-bit (tanpa anti-aliasing), lengkungan `8px` tersebut tampak bergerigi (jagged).
3. **Kontrol yang Masih Bergaya Klasik Win32**:
   - **Scrollbar**: Masih memakai scrollbar standar Win32 (yang di-tema via `SetWindowTheme(..., "Explorer" / "DarkMode_Explorer")`), bukan scrollbar tipis modern Windows 11 yang otomatis menyembunyikan track-nya.
   - **Tidak Ada Motion / Animasi**: Dokumen mockup bagian 1 & 6 menetapkan animasi `120ms ease-out` (dan `83ms hide`) dengan menghormati `SystemParameters.MenuAnimation` / `prefers-reduced-motion`. Saat ini seluruh perpindahan tab, hover tombol, indikator pill navigasi, dan pembukaan menu bersifat statis/instan (`0ms`).
   - **Tray Flyout Tanpa Acrylic**: Sesuai komentar di [FluentMenu.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentMenu.cs#L70-L74), `Acrylic.TryEnable` tidak diaktifkan karena `ToolStripDropDown` non-layered tidak menampilkan blur dan `WS_EX_LAYERED` mengganggu kestabilan menu WinForms.

#### E. Perbedaan Cakupan Fitur antara Dokumen Mockup v1.0 vs Kode Saat Ini
- Dokumen [docs/displayflip-windows11-mockup.md](file:///D:/GitHub/multi-monitor/docs/displayflip-windows11-mockup.md) mendeskripsikan jendela Settings `720×540` sederhana dengan 3 tab (`Profil`, `Umum`, `Tentang`) yang hanya berisi daftar profil.
- Implementasi saat ini di branch `design/win11-fluent` telah berkembang jauh melampaui mockup v1.0 menjadi aplikasi `980×720` responsif dengan **4 tab** (`Layar & Profil`, `Pintasan Keyboard`, `Umum & Ikon`, `Tentang & CLI`), kanvas susunan monitor interaktif 2D, pengaturan resolusi/orientasi/proyeksi, generator 3 gaya ikon, serta perekam global hotkey.

---

## 4. Kesiapan Upgrade dari .NET 8 ke .NET 10

### 4.1 Konteks Dukungan & Kondisi Lingkungan
- Dukungan resmi **.NET 8 LTS** telah berakhir pada **10 November 2026** (4 hari sebelum audit ini dibuat).
- **.NET 10 LTS** didukung penuh hingga **14 November 2028**.
- Mesin pengembangan saat ini **sudah menggunakan .NET SDK `10.0.403`** dengan runtime `Microsoft.NETCore.App 10.0.14` dan `Microsoft.WindowsDesktop.App 10.0.14`. Selama ini proyek menggunakan `<RollForward>Major</RollForward>` agar target `net8.0-windows` dapat berjalan di atas runtime .NET 10.

### 4.2 Penyesuaian Target Framework di File Proyek (`.csproj`)
Untuk menaikkan target secara resmi ke .NET 10, perubahan pada file proyek sangat minimal:
1. [src/DisplayFlip.Core/DisplayFlip.Core.csproj](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Core/DisplayFlip.Core.csproj#L3): `<TargetFramework>net8.0</TargetFramework>` $\rightarrow$ `<TargetFramework>net10.0</TargetFramework>`.
2. [src/DisplayFlip.Windows/DisplayFlip.Windows.csproj](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/DisplayFlip.Windows.csproj#L3): `<TargetFramework>net8.0-windows</TargetFramework>` $\rightarrow$ `<TargetFramework>net10.0-windows</TargetFramework>`.
3. [src/DisplayFlip/DisplayFlip.csproj](file:///D:/GitHub/multi-monitor/src/DisplayFlip/DisplayFlip.csproj#L3): `<TargetFramework>net8.0-windows</TargetFramework>` $\rightarrow$ `<TargetFramework>net10.0-windows</TargetFramework>`.
4. [src/DisplayFlip.Tests/DisplayFlip.Tests.csproj](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Tests/DisplayFlip.Tests.csproj#L3): `<TargetFramework>net8.0-windows</TargetFramework>` $\rightarrow$ `<TargetFramework>net10.0-windows</TargetFramework>`.
5. [src/DisplayFlip.Tests/RuntimeTests.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Tests/RuntimeTests.cs#L65): sesuaikan path pengujian statis yang saat ini meng-hardcode folder `"net8.0-windows"` menjadi `"net10.0-windows"`.

### 4.3 Evaluasi Paket NuGet untuk .NET 10
Berdasarkan pengecekan langsung ke indeks resmi NuGet (`api.nuget.org`) pada 14 November 2026:

| Paket NuGet | Versi Saat Ini | Versi Stabil Terbaru di NuGet | Status Kompatibilitas .NET 10 & Catatan Risiko |
| :--- | :--- | :--- | :--- |
| `Microsoft.NET.Test.Sdk` | `17.11.0` | **`18.12.0`** | **Aman (Risiko Rendah)**. Versi `17.11.0` sudah jalan di SDK 10, dan upgrade ke `18.12.0` memberikan dukungan penuh runner .NET 10 tanpa perubahan kode. |
| `xunit` | `2.9.0` | **`2.9.3`** (v2) / **`4.0.3`** (`xunit.v3`) | **Aman (Risiko Rendah)**. Disarankan naik ke `xunit 2.9.3` (drop-in update tanpa ubah kode). Migrasi ke `xunit.v3` (`4.0.3`) bersifat opsional karena memerlukan perubahan struktur proyek test. |
| `xunit.runner.visualstudio` | `2.8.0` | **`4.0.1`** | **Aman (Risiko Rendah)**. Kompatibel penuh dengan `xunit 2.9.3` dan VSTest di .NET 10. |
| `FluentAssertions` | `6.12.0` | **`8.11.0`** | **Perhatian Lisensi (Risiko Non-Teknis)**. Secara teknis `6.12.0` maupun `8.11.0` berjalan 100% di .NET 10. Namun, mulai versi `8.0.0`, `FluentAssertions` mengubah lisensi dari Apache-2.0 menjadi lisensi komersial Xceed (berbayar untuk penggunaan komersial). Disarankan **tetap di `6.12.0` (Apache-2.0)** atau migrasi ke `Xunit.Assert` / `AwesomeAssertions` (fork komunitas MIT/Apache-2.0), terutama karena kode ini ditargetkan untuk kontribusi ke repositori open-source PowerToys. |

### 4.4 Evaluasi Pemanggilan API Windows & P/Invoke di .NET 10
Seluruh pemanggilan API Windows di solusi ini telah diperiksa terhadap perubahan runtime .NET 9 dan .NET 10:
1. **Win32 Display CCD API ([NativeMethods.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/NativeMethods.cs))**:
   - `QueryDisplayConfig`, `SetDisplayConfig`, `SetDisplayConfigNative`, `DisplayConfigGetTargetName`, `DisplayConfigGetDeviceInfo`.
   - Menggunakan `[DllImport("user32.dll")]` dengan struct blittable/marshaled (`ByValTStr`). Tidak ada perubahan marshaling di .NET 10.
2. **Win32 GDI Display Fallback ([ScreenFallback.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/ScreenFallback.cs))**:
   - `EnumDisplayMonitors`, `GetMonitorInfo`, `EnumDisplayDevices`, `EnumDisplaySettingsEx`, `ChangeDisplaySettingsEx`, `CommitDisplaySettings`.
   - Struct `DEVMODE` terverifikasi berukuran `220 byte` ([FallbackTests.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Tests/FallbackTests.cs#L44-L48)). Berjalan identik di .NET 10.
3. **Win32 Windowing, DWM, Hotkey, dan Registry ([FluentTheme.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentTheme.cs), [FluentMenu.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentMenu.cs), [FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs), [FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs), [StartupRegistry.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip.Windows/StartupRegistry.cs))**:
   - `DwmSetWindowAttribute`, `DwmGetColorizationColor`, `SetWindowCompositionAttribute`, `SetWindowTheme`, `RegisterHotKey`, `UnregisterHotKey`, `TrackMouseEvent`, `MonitorFromWindow`, `IsZoomed`, `ReleaseCapture`, `SendMessage`, `ShowScrollBar`, `AttachConsole`, dan `Microsoft.Win32.Registry`.
   - Semuanya merupakan API OS Windows standar yang tidak mengalami deprecate atau perubahan perilaku di .NET 10.
4. **WinForms / WPF Runtime di .NET 10**:
   - Jika tetap di WinForms: .NET 10 menstabilkan `Application.SetColorMode(SystemColorMode.System)` yang diperkenalkan di .NET 9, namun tidak merusak kontrol kustom yang menggunakan `ControlStyles.UserPaint`.
   - Jika beralih ke WPF: .NET 10 menyertakan peningkatan performa rendering, perbaikan DPI per-monitor, dan pembaruan tema Fluent.

### 4.5 Kesimpulan Risiko Upgrade .NET 8 ke .NET 10
- **Perkiraan Risiko: SANGAT RENDAH (Low Risk)**.
- Tidak ada ketergantungan API usang (`BinaryFormatter`, API CAS, atau paket NuGet yang tidak kompatibel). Proses upgrade murni berupa pembaruan `<TargetFramework>` di 4 file `.csproj` dan penyesuaian 1 baris path di `RuntimeTests.cs`.

---

## 5. Evaluasi Library Tema untuk Tampilan Windows 11 (WPF Native Fluent vs Lepoco WPF UI)

Saat ini, mempertahankan UI kompleks ala Windows 11 *System > Display* di atas **WinForms** membutuhkan **>5.900 baris kode UI manual** ([FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs) sendiri mencapai 3.585 baris) dengan berbagai keterbatasan GDI+ (tidak ada layout elastis deklaratif, tidak ada transparansi komposisi sejati untuk Mica, tidak ada animasi, serta rawan teks terpotong saat ukuran jendela berubah).

Karena logika inti sudah dipisahkan dengan bersih di `DisplayFlip.Core` dan `DisplayFlip.Windows`, mengganti proyek presentasi `DisplayFlip` ke **WPF di atas .NET 10 (`net10.0-windows`)** sangat layak dilakukan tanpa menyentuh logika bisnis. Berikut adalah evaluasi mendalam terhadap dua jalur tema Windows 11 di WPF berdasarkan data rilis terbaru per 14 November 2026:

### 5.1 Jalur 1: Tema Fluent Bawaan WPF (`PresentationFramework.Fluent` — .NET 9 & .NET 10)

- **Versi & Status Perawatan**:
  - Bawaan langsung runtime `Microsoft.WindowsDesktop.App` (`10.0.14` di mesin ini), dirawat resmi oleh tim Microsoft di repositori `dotnet/wpf`.
  - Diaktifkan melalui `ThemeMode="System"` di `App.xaml` atau `Application.Current.ThemeMode = ThemeMode.System`.
  - **Status Eksperimental**: Di .NET 9 dan berlanjut di .NET 10, properti `ThemeMode` di C# masih ditandai atribut `[Experimental("WPF0001")]` (diagnostik `WPF0001` / `WPFWIN0001`), yang mengharuskan supresi `#pragma warning disable WPF0001` atau `<NoWarn>WPF0001</NoWarn>` karena Microsoft menyatakan API dan resource dictionary-nya masih terus disempurnakan.
  - **Perkembangan di .NET 10**: Microsoft telah menambal sejumlah celah dari .NET 9 dengan menambahkan gaya Fluent untuk `DatePicker`, `GridSplitter`, `GridView`, `GroupBox`, `Hyperlink`, `Label`, `NavigationWindow`, `RichTextBox`, dan `TextBox`, serta memperbaiki animasi `Expander` dan tata letak RTL.
- **Kelebihan**:
  - **Tanpa paket NuGet tambahan**: Langsung tersedia di SDK .NET 10.
  - Kontrol dasar (`Button`, `CheckBox`, `RadioButton`, `ComboBox`, `TextBox`, `ListView`, `Expander`, `ScrollViewer`) otomatis mendapatkan sudut membulat, warna aksen sistem, dan scrollbar tipis ala Windows 11.
  - Mendukung backdrop Mica dasar pada `Window` apabila `ThemeMode` diaktifkan di level `Application`.
- **Kekurangan & Celah untuk Kebutuhan DisplayFlip**:
  1. **Tidak memiliki kontrol struktural Windows 11 Settings**: Native WPF **tidak menyediakan** `NavigationView` (navigasi kiri dengan indikator pill aksen dan mode compact), **tidak menyediakan** `SettingsCard` / `CardControl` (kartu baris pengaturan berikon dengan header + subteks + kontrol aksi di kanan), **tidak menyediakan** `TitleBar` kustom terintegrasi, **tidak menyediakan** `ContentDialog` in-window maupun `InfoBar`, dan **tidak menyediakan** kontrol `SymbolIcon` (`Segoe Fluent Icons`).
  2. **Masalah stabilitas `Window.ThemeMode` + Custom Chrome**: Mengatur `ThemeMode` per-jendela atau menggabungkannya dengan `WindowChrome` kustom masih memiliki isu dikenal terkait area non-client, posisi tombol caption, dan warna `SystemColors` lama yang tidak ter-update otomatis saat berganti tema secara live.
  3. **Upaya implementasi tetap besar**: Untuk membuat halaman Settings DisplayFlip (navigasi kiri, kartu pengaturan monitor, daftar profil, daftar pintasan, dialog simpan/monitor hilang), kita tetap harus merancang custom XAML `ControlTemplate` untuk semua komponen struktural tersebut dari nol.

### 5.2 Jalur 2: Library WPF UI dari Lepoco (`WPF-UI` / `lepoco/wpfui`)

- **Versi Terbaru & Status Perawatan (Diverifikasi dari NuGet & GitHub API per 14 Nov 2026)**:
  - **Nama Paket NuGet**: [`WPF-UI`](https://www.nuget.org/packages/WPF-UI/) dan paket pendamping [`WPF-UI.Tray`](https://www.nuget.org/packages/WPF-UI.Tray/).
  - **Versi Stabil Terbaru**: **`4.3.0`** (rilis berurutan seri v4: `4.0.0` $\rightarrow$ `4.0.3` $\rightarrow$ `4.1.0` $\rightarrow$ `4.2.0` $\rightarrow$ `4.2.1` $\rightarrow$ **`4.3.0`**).
  - **Dukungan Target Framework Resmi (dari `wpf-ui.nuspec` v4.3.0)**: Mendukung penuh **`net10.0-windows7.0`**, `net9.0-windows7.0`, `net8.0-windows7.0`, serta `.NET Framework 4.6.2/4.7.2/4.8.1`.
  - **Status Repositori GitHub (`lepoco/wpfui`)**: Aktif (`archived: false`), lisensi **MIT**, **9.752 stars**, 468 open issues, dan terus diperbarui sepanjang tahun 2026.
- **Kelebihan untuk Kebutuhan DisplayFlip**:
  1. **Sangat Presisi untuk Halaman Settings ala Windows 11**:
     - `ui:FluentWindow` + `ui:TitleBar`: Menyelesaikan masalah Mica (`WindowBackdropType="Mica"`) dan custom titlebar secara tuntas tanpa bug double-drawn caption button maupun bingkai klasik, serta mendukung penuh **Windows 11 Snap Layouts**.
     - `ui:NavigationView`: Menyediakan navigasi samping ala Windows 11 Settings lengkap dengan indikator pill aksen 3px, ikon Fluent, mode compact otomatis saat jendela dipersempit, dan transisi halaman halus.
     - `ui:CardControl` & `ui:CardExpander`: Merupakan padanan langsung dari komponen `SettingsCard` di aplikasi Settings Windows 11 asli (ikon di kiri, `Header` + `Subtitle` di tengah, kontrol aksi di kanan). Sangat cocok untuk 3 kartu monitor (*Identitas & Primary*, *Pindah Cepat Posisi*, *Resolusi & Orientasi*), baris *Profil Tersimpan*, baris *Pintasan Keyboard*, dan kartu di tab *Umum*.
     - `ui:ContentDialog` & `ui:InfoBar`: Menggantikan `SaveProfileDialog` (lengkap dengan `ui:InfoBar` severity `Warning` untuk nama profil duplikat), `MissingMonitorDialog`, dan `ShortcutRecorderDialog` sebagai dialog modern dengan scrim halus.
     - `ui:SymbolIcon`: Mendukung ribuan ikon Fluent secara deklaratif tanpa perlu merender font ke `Bitmap` secara manual.
     - `SystemThemeWatcher` & `ApplicationThemeManager`: Otomatis menyinkronkan tema Light/Dark dan warna aksen sistem Windows 11 secara real-time.
- **Kekurangan & Hal yang Perlu Diperhatikan**:
  1. Menambahkan dependensi pihak ketiga (`WPF-UI` `4.3.0`, lisensi MIT) pada proyek executable `DisplayFlip` (namun `DisplayFlip.Core` dan `DisplayFlip.Windows` tetap bebas dependensi).
  2. Dua komponen spesifik domain DisplayFlip tetap perlu dibuat sebagai kontrol kustom WPF (`UserControl`):
     - **Kanvas Tata Letak Monitor 2D** (menggunakan WPF `Canvas` + `Border` вектор untuk tiap ubin monitor, drag-and-drop mouse, garis bantu `Line` dengan `StrokeDashArray`, dan toolbar di bagian bawah). Di WPF, pembuatan kanvas ini jauh lebih bersih dan bebas flicker dibanding GDI+.
     - **Kotak Input Perekam Shortcut Keyboard** (`PreviewKeyDown` di WPF `TextBox` / `Button` yang memanggil `HotkeyParser` yang sudah ada).

### 5.3 Matriks Perbandingan & Rekomendasi

| Kriteria Kebutuhan DisplayFlip | WinForms Kustom Saat Ini | WPF Native Fluent (.NET 10) | Lepoco `WPF-UI` v4.3.0 (.NET 10) |
| :--- | :--- | :--- | :--- |
| **Dukungan .NET 10 LTS** | Ya (`net10.0-windows`) | Ya (bawaan `net10.0-windows`) | Ya (resmi menarget `net10.0-windows7.0`) |
| **Mica Backdrop & Custom TitleBar** | Gagal (Mica tertutup cat solid; bug double caption button) | Parsial (butuh `WindowChrome` manual, rawan glitch `WPF0001`) | **Sangat Baik** (`FluentWindow` + `TitleBar` siap pakai) |
| **Navigasi Kiri (`NavigationView`)** | Manual (`NavButton` GDI+, tanpa animasi) | Tidak ada (harus buat dari nol) | **Sangat Baik** (`ui:NavigationView` bawaan) |
| **Kartu Pengaturan (`SettingsCard`)** | Manual (`CreateSettingsCardPanel`, rawan teks terpotong) | Tidak ada (harus buat template manual) | **Sangat Baik** (`ui:CardControl` & `ui:CardExpander` bawaan) |
| **Kanvas Drag-and-Drop Monitor 2D** | Kustom GDI+ (`PaintMonitorCanvas`, sudah jalan) | Kustom WPF `Canvas` (vektor tajam, mudah) | Kustom WPF `Canvas` (vektor tajam, menyatu dengan tema) |
| **Dialog Simpan / Monitor Hilang / Shortcut** | Form borderless terpisah (tanpa overlay scrim) | Harus buat modal/popup sendiri | **Sangat Baik** (`ui:ContentDialog` + `ui:InfoBar` bawaan) |
| **Scrollbar Modern Windows 11** | Klasik Win32 (`SetWindowTheme`, memotong layout) | Ya (bawaan Fluent dictionary) | **Sangat Baik** (auto-expand tipis ala WinUI 3) |
| **Ikon Fluent (`Segoe Fluent Icons`)** | Render manual char ke `Bitmap` | Harus deklarasi `TextBlock` font manual | **Sangat Baik** (`ui:SymbolIcon` bawaan) |
| **Status API & Perawatan** | Beban perawatan internal tinggi (>3.500 baris di 1 file) | Eksperimental (`WPF0001`), celah kontrol lanjutan | **Stabil v4.3.0**, MIT License, ~9.750 stars, aktif |

> **Penilaian & Rekomendasi**:  
> Untuk kebutuhan DisplayFlip (halaman Settings bergaya Windows 11 *System > Display* dengan kanvas posisi monitor, manajemen profil, dan perekam shortcut keyboard custom), **library `WPF-UI` (lepoco) versi `4.3.0` di atas `.NET 10`** adalah pilihan yang **jauh lebih cocok dan matang** dibandingkan tema Fluent bawaan WPF. Tema Fluent bawaan WPF di .NET 10 masih berstatus eksperimental (`WPF0001`) dan tidak memiliki komponen krusial Windows 11 seperti `NavigationView`, `CardControl` (`SettingsCard`), `TitleBar`, maupun `ContentDialog`.

---

## 6. Hal yang Tidak Bisa Dipastikan dari Kode Saja dan Butuh Screenshot dari Owner

Walaupun di dalam folder `docs/review/fase-h/` sudah tersedia gambar hasil ekspor otomatis (`--export-fase-h`), gambar-gambar tersebut dihasilkan menggunakan metode WinForms `Control.DrawToBitmap()` ([FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L2383)). Secara teknis di Windows, `DrawToBitmap()` hanya mengirimkan pesan `WM_PRINT` ke GDI di memori dan **tidak menangkap hasil komposisi Desktop Window Manager (DWM) yang sesungguhnya di layar fisik**.

Oleh karena itu, terdapat **5 hal yang tidak dapat dipastikan dari kode maupun gambar `DrawToBitmap`** dan membutuhkan **screenshot tangkapan layar asli (PrintScreen / Snipping Tool)** dari Anda:

1. **Tampilan Bingkai Jendela, Tombol Caption, dan Mica pada `FluentSettingsForm` di Layar Fisik**:
   - Pada gambar `DrawToBitmap` (`docs/review/fase-h/settings-v2-normal.png`), terlihat bingkai jendela biru-abu klasik dan tombol Minimize/Maximize/Close bawaan Windows yang bertumpuk di atas tombol kustom.
   - **Screenshot yang dibutuhkan**: Tangkapan layar asli jendela Settings saat terbuka di atas desktop Anda (1 di mode Terang, 1 di mode Gelap) untuk memastikan apakah DWM benar-benar menampilkan tombol caption ganda/bingkai klasik tersebut di layar nyata, serta apakah ada sedikit pun efek tembus pandang Mica yang terlihat.
2. **Tampilan Tray Flyout & Submenu saat Terbuka di Atas Taskbar Windows 11 Asli**:
   - Gambar di `docs/review/fase-c/01-tray-flyout-gelap.png` hanya memotret kontrol menu secara terisolasi tanpa taskbar.
   - **Screenshot yang dibutuhkan**: Tangkapan layar area pojok kanan bawah taskbar saat menu klik-kanan DisplayFlip dibuka dan saat submenu *Jadikan primary* atau *Multi-layar* mekar, untuk memverifikasi jarak flyout dari taskbar, arah mekarnya submenu, kehalusan sudut 8px di atas wallpaper/taskbar, dan keterbacaan radio button/badge primary.
3. **Proporsi Ukuran Font & Kontrol pada Skala DPI Monitor Fisik Anda (mis. 100% vs 125% / 150%)**:
   - Karena `FluentSettingsForm` dan `FluentDialogBase` menggunakan `AutoScaleMode = AutoScaleMode.None` dengan ukuran piksel tetap (`const int`), perilaku penskalaan DWM/GDI pada monitor ber-DPI tinggi (atau saat jendela digeser antar monitor dengan DPI berbeda) tidak bisa disimulasikan dari kode saja.
   - **Screenshot yang dibutuhkan**: Tangkapan layar jendela Settings pada monitor utama dan monitor kedua Anda (beserta info berapa % scale DPI yang Anda pakai di tiap monitor).
4. **Tampilan Dialog Simpan & Setup Profil (`SaveProfileDialog`) Versi Terbaru dan `ShortcutRecorderDialog`**:
   - Di folder `docs/review/`, gambar dialog simpan (`04-dialog-simpan.png`) masih versi lama (hanya 1 input nama profil), sedangkan kode `SaveProfileDialog` saat ini sudah memiliki 3 field (*Nama profil*, dropdown *Layar utama*, dan kotak *Pintasan keyboard* + tombol *Hapus*), serta belum ada gambar untuk `ShortcutRecorderDialog`.
   - **Screenshot yang dibutuhkan**: Tangkapan layar saat dialog `+ Simpan Profil` dibuka dan saat dialog `Atur pintasan keyboard` dibuka.
5. **Tampilan Overlay Identifikasi Layar (`IdentifyOverlayForm`) di Pojok Monitor Fisik**:
   - **Screenshot yang dibutuhkan**: Tangkapan layar saat tombol `Identifikasi` diklik untuk memastikan posisi kotak angka `220×128` di pojok kiri-bawah tidak tertutup taskbar dan ukurannya proporsional di kedua monitor Anda.

---

## 7. Usulan Pendekatan Perbaikan per Layar Beserta Risikonya (Belum Dieksekusi)

Bagian ini menyajikan usulan rencana perbaikan bertahap per layar — **dimulai dari jendela Settings** — yang dapat dijalankan setelah Anda memberikan arahan. Untuk setiap layar, disajikan pendekatan untuk **Opsi Strategis (Migrasi UI ke .NET 10 + WPF + `WPF-UI 4.3.0`)** maupun **Opsi Taktis (Perbaikan Cepat di WinForms Saat Ini)** beserta analisis risikonya.

---

### Layar 1 (Prioritas Utama): Jendela Settings (`FluentSettingsForm` / `SettingsWindow`)

#### A. Perbaikan Shell Jendela, TitleBar, Mica Backdrop, dan Navigasi Kiri
- **Masalah Saat Ini**: Bingkai non-client klasik & tombol caption bertumpuk akibat `WM_NCCALCSIZE` di WinForms; material Mica tertutup cat solid `ShellBackground`; sudut kiri-atas area konten (`_contentHost.Region`) bergerigi (aliased).
- **Pendekatan Perbaikan**:
  - **Jika memilih WPF + `WPF-UI 4.3.0` (Direkomendasikan)**:
    1. Gunakan `<ui:FluentWindow ExtendsContentIntoTitleBar="True" WindowBackdropType="Mica" WindowCornerPreference="Round" Width="980" Height="720" MinWidth="680" MinHeight="560">`.
    2. Gunakan `<ui:TitleBar Title="DisplayFlip — Pengaturan Layar &amp; Profil">` dengan ikon aplikasi dinamis (`FluentIcons`).
    3. Gunakan `<ui:NavigationView PaneDisplayMode="Left">` dengan 4 `NavigationViewItem` (*Layar & Profil*, *Pintasan Keyboard*, *Umum & Ikon*, *Tentang & CLI*) dan status bar `28px` di `Footer` / `Grid` baris bawah. Area konten kanan dibungkus `Border` dengan `CornerRadius="8,0,0,0"` ber-anti-aliasing penuh.
  - **Jika memilih bertahan di WinForms**:
    1. Panggil `DwmExtendFrameIntoClientArea` dengan margin `-1` (sheet of glass) atau margin top `TitleBarHeight`, dan hapus tombol caption Win32 standar melalui `SetWindowLong` / `WM_NCCALCSIZE` yang diteruskan ke `DwmDefWindowProc` agar tombol Minimize/Maximize/Close bawaan tidak digambar ganda.
    2. Ganti pemotongan `_contentHost.Region` dengan menggambar latar belakang bulat menggunakan `Graphics.FillPath` ber-`SmoothingMode.AntiAlias` di atas latar shell.

#### B. Perbaikan Tab 1 — Halaman "Layar & Profil"
- **Masalah Saat Ini**:
  - Tombol `_newProfileBtn` menampilkan tanda tambah ganda (`+ + Simpan Profil`).
  - Judul kartu ke-3 kehilangan karakter `&` (`"Resolusi  orientasi tampilan"`).
  - Tombol `"Hanya Layar 2 Saja"`, `"Identifikasi"`, dan `"Sejajarkan"` terpotong pada lebar normal/compact.
- **Pendekatan Perbaikan**:
  1. **Kanvas Susunan Monitor 2D**:
     - Di WPF: Buat kontrol `MonitorCanvasControl` berbasis `Canvas` di dalam `Border` (`CornerRadius="8"`, background subtle well). Ubin monitor dirender sebagai elemen vektor `Border` (`CornerRadius="6"`) yang mendukung drag-and-drop mouse, garis bantu `Line` putus-putus (`StrokeDashArray="4,2"`), dan nudge tombol panah. Toolbar bawah kanvas menggunakan `DockPanel` / `WrapPanel` dengan lebar tombol `Auto` (`Padding="10,5"`) sehingga teks `"Identifikasi"` dan `"Sejajarkan"` **tidak pernah terpotong**.
     - Di WinForms: Hitung lebar tombol toolbar bawah menggunakan `TextRenderer.MeasureText(text, Font).Width + iconWidth + padding` alih-alih angka konstanta sempit (`80px`/`84px`), atau beralih ke mode icon-only dengan `ToolTip` lebih awal saat ruang tidak mencukupi.
  2. **3 Kartu Pengaturan Monitor (`SettingsCard`)**:
     - Di WPF: Gunakan 3 `<ui:CardControl Icon="{ui:SymbolIcon ...}">` standar Windows 11. Seluruh teks header/subtitle otomatis mendukung wrapping tanpa masalah mnemonic `&`, dan tombol `"Hanya Layar X Saja"` menggunakan `Width="Auto"` sehingga tidak pernah terpotong.
     - Di WinForms: Tambahkan `UseMnemonic = false` pada `_resOrientTitle`, `_selectedMonTitle`, `_selectedMonSub`, `_quickMoveTitle`, dan `_quickMoveSub`; perbesar lebar `_soloDisplayBtn` dari `126px` menjadi `Math.Max(148, measuredWidth)`.
  3. **Seksi Profil Tersimpan**:
     - Ubah teks `_newProfileBtn` dari `"+ Simpan Profil"` menjadi `"Simpan Profil"` (karena properti `Glyph = FluentIcons.Add` sudah menggambar ikon `+` di sebelah kiri teks).
     - Untuk setiap baris profil, gunakan `CardControl` (WPF) atau sesuaikan batas kanan label nama profil agar tidak bertabrakan dengan deretan tombol `+ Shortcut`, `Kanvas`, `Apply`, dan `...`.

#### C. Perbaikan Tab 2 — Halaman "Pintasan Keyboard"
- **Masalah Saat Ini**: Daftar `_shortcutListPanel` menyempit dan terpotong separuh disertai scrollbar horizontal/vertikal klasik (terlihat di `settings-v2-pintasan.png`), karena `LayoutShortcutPage()` tidak dipanggil saat `SwitchTab("Pintasan")` dan lebar kartu tidak responsif terhadap munculnya scrollbar.
- **Pendekatan Perbaikan**:
  - **Di WPF**: Gunakan `ScrollViewer` (`HorizontalScrollBarVisibility="Disabled"`, `VerticalScrollBarVisibility="Auto"`) yang membungkus `StackPanel` berisi deretan `ui:CardControl`. Setiap kartu otomatis meregang (`HorizontalAlignment="Stretch"`) mengikuti lebar halaman dikurangi scrollbar tipis modern.
  - **Di WinForms**: Panggil `LayoutShortcutPage()` di dalam `SwitchTab("Pintasan")` dan di akhir `RebuildShortcutRows()`, matikan `HorizontalScroll` pada `_shortcutListPanel`, serta gunakan `Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right` pada setiap kartu pintasan.

#### D. Perbaikan Tab 3 ("Umum & Ikon") dan Tab 4 ("Tentang & CLI")
- **Masalah Saat Ini**: Judul seksi ikon, label checkbox auto-apply, subteks auto-apply, dan daftar argumen CLI terpotong.
- **Pendekatan Perbaikan**:
  1. Pada Tab **Umum & Ikon**:
     - Buat 3 kartu gaya ikon menggunakan `UniformGrid Columns="3"` (pada lebar normal/wide) atau `StackPanel` vertikal (pada lebar compact) dengan gambar pratinjau dari `FluentIcons.RenderModernAppBitmap`.
     - Bungkus opsi *Jalan otomatis saat Windows start* dan *Langsung terapkan perubahan saat selesai menggeser layar di kanvas* ke dalam `ui:CardControl` dengan `ToggleSwitch` atau `CheckBox` yang mendukung `TextWrapping="Wrap"` sehingga kalimat panjang tidak terpotong oleh ellipsis.
  2. Pada Tab **Tentang & CLI**:
     - Ubah daftar perintah CLI dari satu string panjang yang di-spasi manual menjadi tabel `Grid` 2 kolom (Kolom 0: perintah dengan font `Consolas`; Kolom 1: deskripsi Bahasa Indonesia) dan lengkapi seluruh argumen yang didukung di `Program.cs` (`--mirror`, `--apply "<Nama>"`, `--list [--json]`, `--solo 1|2`, `--extend`, `--clone`, `--internal`, `--external`, `--tray`).

---

### Layar 2: Dialog Simpan & Setup Profil (`SaveProfileDialog`) dan Perekam Pintasan (`ShortcutRecorderDialog`)
- **Masalah Saat Ini**: Dialog masih berupa `Form` terpisah tanpa efek peredup latar (scrim/smoke layer) di atas jendela Settings, serta warna tombol primary pada tema gelap bergantung mentah pada kecerahan aksen registry.
- **Pendekatan Perbaikan**:
  - Jika jendela Settings sedang terbuka, tampilkan sebagai `ui:ContentDialog` in-window di atas `ContentDialogHost` (dengan backdrop scrim gelap transparan dan animasi scale-in khas Windows 11). Jika dipanggil langsung dari Tray Flyout saat Settings tertutup, tampilkan sebagai `FluentWindow` modal kecil (`420px`) di tengah layar aktif.
  - Gunakan `ui:InfoBar` (`Severity="Warning"`, `IsOpen="{Binding IsDuplicateName}"`) untuk menampilkan peringatan `"Nama sudah ada — akan ditimpa"` beserta tombol aksi `Timpa`.
  - Tambahkan indikator peringatan ringan bila kombinasi tombol yang direkam di `ShortcutRecorderDialog` sudah dipakai oleh profil/aksi lain (memberi tahu pengguna bahwa pintasan lama akan dipindahkan ke aksi ini).

---

### Layar 3: Dialog Monitor Hilang (`MissingMonitorDialog`)
- **Masalah Saat Ini**: Dapat dipanggil dari dua jalur (klik `Apply` di Settings atau klik profil di Tray Flyout), dan teks tombol `"Lanjutkan dengan yang tersisa"` cukup panjang.
- **Pendekatan Perbaikan**:
  - Pertahankan pemisahan logika `ProfilePolicy.SplitPresent` di `DisplayFlip.Core`.
  - Gunakan layout tombol footer proporsional (`Grid` kolom `1*` untuk `Batal` dan `1.8*` untuk `Lanjutkan dengan yang tersisa`) dengan palet tombol primary yang menggunakan `SystemAccentColorLight2` di Dark Mode dan `SystemAccentColorDark1` di Light Mode agar teks tombol selalu tajam dan memenuhi kontras WCAG AA.

---

### Layar 4: System Tray Flyout & Submenu (`TrayHost` + `FluentMenu`)
- **Masalah Saat Ini**:
  - Kontras titik radio button terpilih di submenu *Jadikan primary* pada tema gelap terlalu redup bila warna aksen sistem gelap (`docs/review/fase-c/02-tray-submenu-primary-gelap.png`).
  - Terdapat perbedaan daftar item antara wireframe awal di `docs/displayflip-windows11-mockup.md` (6 item) dengan kode `TrayHost.cs` saat ini (9 item: ada tambahan *Identifikasi layar*, *Multi-layar*, dan *Pengaturan...*).
- **Pendekatan Perbaikan**:
  1. Konfirmasi terlebih dahulu kepada owner apakah daftar 9 item di `TrayHost.cs` saat ini sudah final (karena fitur *Identifikasi layar* dan *Multi-layar* memang sangat berguna di tray).
  2. Perbaiki pemilihan warna aksen pada `FluentTheme.SystemAccent(bool dark)`: gunakan `AccentPalette[1]` atau `AccentPalette[2]` (`Light2`/`Light1`) saat Dark Mode dan `AccentPalette[3]`/`AccentPalette[4]` (`Base`/`Dark1`) saat Light Mode, sehingga titik radio aktif dan kotak checkbox selalu kontras tinggi di atas permukaan `#2B2B2B`.
  3. Untuk arsitektur tray: `NotifyIcon` + `FluentMenu` yang sudah stabil (bebas bug timer polling sesuai `TrayStabilityTests.cs`) dapat dipertahankan atau diintegrasikan dengan `WPF-UI.Tray`, karena logika stabilitas buka-tutup menu di `TrayHost.cs` sudah sangat teruji.

---

### Layar 5: Overlay Identifikasi Nomor Layar (`IdentifyOverlayForm`)
- **Masalah Saat Ini**: Sudah berjalan baik sebagai jendela `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`, namun perlu memastikan konsistensi nomor layar dan posisi DPI.
- **Pendekatan Perbaikan**:
  - Pastikan pemanggilan `ExtractDisplayNumber` selalu menerima pasangan `(m.StableKey, m.FriendlyName, index)` yang konsisten antara `PaintMonitorCanvas` ([FluentSettingsForm.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentSettingsForm.cs#L2942) saat ini hanya mengirim `m.FriendlyName` tanpa `m.StableKey`!) dan `IdentifyOverlayForm.ShowForMonitors` ([FluentDialogs.cs](file:///D:/GitHub/multi-monitor/src/DisplayFlip/FluentDialogs.cs#L1379) yang mengirim keduanya). Menyamakan pemanggilan ini mencegah ketidaksinkronan nomor layar antara kanvas dan overlay fisik!

---

### Ringkasan Matriks Risiko Perbaikan & Mitigasi

| Area Perubahan | Tingkat Risiko | Potensi Masalah | Strategi Mitigasi |
| :--- | :---: | :--- | :--- |
| **Upgrade `.NET 8` $\rightarrow$ `.NET 10`** | **Rendah** | Path hardcode `"net8.0-windows"` di `RuntimeTests.cs` gagal saat test dijalankan. | Perbarui target di 4 `.csproj` dan sesuaikan `RuntimeTests.cs` secara bersamaan; jalankan seluruh unit test. |
| **Perbaikan Bug Nomor Layar Kanvas vs Overlay (`ExtractDisplayNumber`)** | **Rendah** | Di `PaintMonitorCanvas` (`L2942` & `L2976`), parameter `m.StableKey` belum dilewatkan sehingga pada beberapa driver nomor di kanvas bisa berbeda dari overlay. | Gunakan overload 3 parameter `ExtractDisplayNumber(m.StableKey, m.FriendlyName, idx)` di semua tempat. |
| **Perbaikan Tata Letak & Pemotongan Teks di Jendela Settings (Jalur WinForms)** | **Sedang** | Mengubah `WM_NCCALCSIZE` untuk memperbaiki bingkai dan Mica di WinForms berisiko mengganggu hit-test resize grip atau `HTMAXBUTTON` (`FluentSettingsInteractionTests`). | Pertahankan logika `HitTestPoint` yang sudah diuji oleh `FluentSettingsInteractionTests` dan `LayoutOverlapTests`. |
| **Migrasi Lapisan Presentasi ke WPF + `WPF-UI 4.3.0` (Jalur Strategis)** | **Sedang** | Perubahan teknologi UI di proyek `DisplayFlip` memerlukan penulisan ulang view XAML dan penyesuaian test refleksi WinForms (`FluentSettingsInteractionTests`, `LayoutOverlapTests`). | **100% kode `DisplayFlip.Core` dan `DisplayFlip.Windows` tidak disentuh**. Buat jendela WPF berdampingan atau di branch khusus, pertahankan fungsi ekspor screenshot otomatis untuk verifikasi visual, dan sesuaikan test UI ke pohon visual WPF. |
| **Penanganan High-DPI / Multi-Monitor Berbeda Skala** | **Sedang** | Koordinat desktop dari CCD/GDI bersifat piksel fisik, sedangkan layout WPF bekerja dalam satuan *Device-Independent Pixels* (DIPs, 96 DPI). | Di kanvas monitor, gunakan rasio proporsional dari `DisplayMonitor.Position` (yang murni matematis terhadap `availW`/`availH` kanvas), dan gunakan koordinat piksel fisik hanya saat memposisikan `IdentifyOverlayForm` di layar. |
