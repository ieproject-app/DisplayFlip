# DisplayFlip 🖥️🔄

> **Modern Windows 11 Fluent Multi-Monitor Management & Hotkey Profile Utility**  
> Dibangun dengan **.NET 10** (`net10.0-windows`) dan **WPF-UI 4.3.0** dengan aksen Windows 11 Mica/Acrylic, System Tray resident, Global Hotkeys latar belakang, Single-Instance Guard, dan integrasi CLI otomatisasi.

---

## 🚀 Panduan Penggunaan Harian (Siap Pakai Langsung)

Aplikasi telah dipublikasikan ke file executable mandiri (**file utuh**) tanpa perlu menjalankan terminal atau `dotnet run`.

### Lokasi Executable Siap Pakai:
| Varian | Lokasi File | Ukuran | Kebutuhan Runtime | Keterangan |
| :--- | :--- | :--- | :--- | :--- |
| **Utama (Rekomendasi)** | [`artifacts/DisplayFlip/DisplayFlip.exe`](file:///D:/GitHub/multi-monitor/artifacts/DisplayFlip/DisplayFlip.exe) | **~6.8 MB** | Memerlukan .NET 10 Desktop Runtime | Ringan, startup instan, single-file |
| **Standalone (Portabel)** | [`artifacts/DisplayFlip-Standalone/DisplayFlip.exe`](file:///D:/GitHub/multi-monitor/artifacts/DisplayFlip-Standalone/DisplayFlip.exe) | **~74 MB** | *Zero Dependency* (Mandiri) | Seluruh .NET 10 runtime ter-bundle di dalam file, dapat berjalan di PC Windows x64 mana pun |

---

## 📌 Cara Menggunakan Harian

1. **Jalankan Aplikasi Langsung**:
   - Cukup **double-click** `DisplayFlip.exe` di folder `artifacts\DisplayFlip\` (atau buat shortcut di Desktop / Pin ke Taskbar / Start Menu).
   - Jendela antarmuka utama pengaturan (*Fluent Settings*) akan langsung terbuka.

2. **Berjalan Tenang di System Tray (Latar Belakang)**:
   - Jalankan dengan parameter `--tray`:
     ```cmd
     DisplayFlip.exe --tray
     ```
   - Ikon DisplayFlip akan muncul di area notifikasi (system tray) Windows taskbar.
   - **Klik Kiri** pada ikon tray: Tampilkan atau sembunyikan jendela antarmuka.
   - **Klik Kanan** pada ikon tray: Menu pintas cepat (Tukar Monitor, Mode Proyeksi Solo/Extend, Profil, Pengaturan, dan Keluar).
   - **Tombol 'X' (Tutup)** pada jendela: Otomatis meminimalkan jendela ke tray tanpa mematikan aplikasi.
   - **Pintasan Keyboard Global** tetap aktif mendengarkan di latar belakang meskipun jendela sedang disembunyikan.

3. **Mulai Otomatis Saat Login Windows (Auto-Start)**:
   - Buka menu **Umum & Ikon** di dalam DisplayFlip.
   - Aktifkan toggle **Mulai otomatis saat login Windows (Startup Registry)**.
   - DisplayFlip otomatis mendaftarkan path executable aktif ke `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` dengan flag `--tray`.

4. **Single-Instance Guard**:
   - Jika DisplayFlip sudah berjalan di tray, membuka kembali `DisplayFlip.exe` (atau melalui shortcut) akan otomatis memanggil instans yang ada ke latar depan (*foreground*) tanpa menduplikasi proses.

---

## ⌨️ Pintasan Keyboard Global (Default)

| Fungsi | Pintasan Default | Keterangan |
| :--- | :--- | :--- |
| **Tukar Posisi Monitor** | <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>F</kbd> | Membalik posisi horizontal Layar 1 & Layar 2 secara instan. |
| **Ganti Layar Utama (Primary)** | <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>P</kbd> | Memindahkan status monitor primer secara berurutan. |
| **Identifikasi Layar** | <kbd>Ctrl</kbd> + <kbd>Alt</kbd> + <kbd>I</kbd> | Menampilkan overlay nomor fisik layar di pojok setiap monitor. |
| **Hanya Layar 1 (Solo 1)** | *(Dapat diatur)* | Menonaktifkan monitor sekunder, hanya menyalakan monitor 1. |
| **Hanya Layar 2 (Solo 2)** | *(Dapat diatur)* | Menonaktifkan monitor primer, hanya menyalakan monitor 2. |
| **Bentangkan (Extend)** | *(Dapat diatur)* | Mengembalikan desktop ke mode proyeksi multi-layar penuh. |
| **Pintasan Profil Khusus** | *(Per-profil)* | Dapat dikonfigurasi di halaman *Pintasan Keyboard*. |

---

## 💻 Kontrol Otomatisasi Terminal (CLI)

Anda dapat menggunakan `DisplayFlip.exe` dari PowerShell, Command Prompt, atau skrip batch:

```powershell
# Menukar posisi monitor primer dan sekunder
displayflip.exe --swap

# Menampilkan overlay nomor fisik monitor
displayflip.exe --identify

# Mengaktifkan mode Solo (hanya Layar 1 atau Layar 2)
displayflip.exe --solo 1
displayflip.exe --solo 2

# Membentangkan tampilan ke seluruh monitor
displayflip.exe --extend

# Menerapkan profil konfigurasi yang tersimpan
displayflip.exe --apply "Kerja Siang"

# Menampilkan daftar profil tersimpan
displayflip.exe --list
displayflip.exe --list --json

# Membuka jendela antarmuka pengaturan
displayflip.exe --settings

# Menjalankan di background system tray
displayflip.exe --tray

# Menampilkan bantuan CLI
displayflip.exe --help
```

---

## 🛠️ Kompilasi & Build Ulang

Gunakan skrip automasi [`publish.ps1`](file:///D:/GitHub/multi-monitor/publish.ps1):

```powershell
# Membangun varian rekomendasi (Framework-Dependent ~6.8 MB)
.\publish.ps1

# Membangun varian Standalone mandiri (~74 MB)
.\publish.ps1 -Mode Standalone

# Membangun kedua varian sekaligus dengan pembersihan artefak lama
.\publish.ps1 -Mode All -Clean
```

Atau menggunakan perintah .NET SDK standar:
```powershell
dotnet publish src/DisplayFlip.Wpf/DisplayFlip.Wpf.csproj /p:PublishProfile=FolderProfile-FrameworkDependent
```

---

## 🧪 Pengujian Unit & Regresi

Jalankan seluruh test suite (261 unit test):
```powershell
dotnet test
```

---

## 📄 Lisensi
Hak Cipta © 2026 DisplayFlip Team.
