# DisplayFlip ke Microsoft Store

Panduan rilis dan referensi listing Store untuk aplikasi **DisplayFlip**. Kode + manifest sudah disiapkan di repo; langkah akun dan pembaruan rilis dilakukan melalui Partner Center.

## Status Rilis & Identitas Resmi Microsoft Store

- **Status Submission:** **LIVE** di Microsoft Store
- **Microsoft Store URL:** <https://apps.microsoft.com/detail/9npnh94mkjnr>
- **Product ID (Store ID):** `9NPNH94MKJNR`
- **IARC Global Rating ID:** `cc46f316-c301-8bab-8620-3fc86083db84`
- **Jalur Distribusi Utama:** MSIX Packaged (Microsoft Store) sebagai rekomendasi utama pengguna umum, didampingi unduhan langsung/portable di GitHub Releases:
  - ZIP (Framework-Dependent): `https://github.com/ieproject-app/DisplayFlip/releases/download/v1.1.0/DisplayFlip-v1.1.0-win-x64.zip`
  - Standalone EXE (Self-Contained): `https://github.com/ieproject-app/DisplayFlip/releases/download/v1.1.0/DisplayFlip-v1.1.0-Standalone-win-x64.exe`

## Pilih jalur distribusi

| Jalur | Cocok bila | Konsekuensi |
| :--- | :--- | :--- |
| **MSIX packaged (rekomendasi & aktif)** | Ingin instal/update otomatis, hosting Microsoft, fitur Store penuh | Perlu proyek identitas paket (sudah ada: [Package.appxmanifest](../src/DisplayFlip.Package/Package.appxmanifest)), submit `.msix` |
| **EXE unpackaged (cepat)** | Ingin listing cepat tanpa MSIX | Hosting file di server sendiri, URL versi statis, installer `.exe` wajib silent + signature dari CA publik |

Acuan resmi: [distribusi app Win32 via Store](https://learn.microsoft.com/en-us/windows/apps/distribute-through-store/how-to-distribute-your-win32-app-through-microsoft-store),
[checklist kesiapan MSIX](https://learn.microsoft.com/en-us/windows/apps/publish/store-readiness-checklist),
[syarat paket MSI/EXE](https://learn.microsoft.com/en-us/windows/apps/publish/publish-your-app/msi/app-package-requirements).

## Jalur MSIX (langkah)

1. **Akun + nama.** Buat akun developer Microsoft, verifikasi, lalu reserve
   nama `DisplayFlip` di Partner Center (Product ID aktif: `9NPNH94MKJNR`).
2. **Samakan identitas.** Di halaman Product identity, salin `Name`,
   `Publisher`, `PublisherDisplayName`, lalu jalankan:
   ```powershell
   .\package-store.ps1 -Version 1.1.0.0 -Publisher "CN=..." -Name "DisplayFlip"
   ```
   Butuh sekali: `winget install Microsoft.WindowsSDK --silent` (untuk `makeappx.exe`).
3. **Cek hasil.** Pastikan `.msix` Release, versi 4 bagian, dan lebih tinggi
   dari rilis Store sebelumnya.
4. **Uji lokal (opsional).** `.\package-store.ps1 -DevSign`, install sertifikat
   ke Trusted People, klik 2x `.msix`. Verifikasi: tray, Quick Swap, Solo 1/2
   (label kini memuat nama device), Identify, profil, hotkey, dan startup.
5. **Listing.** Siapkan minimal: 1 screenshot, ikon Store (otomatis dari skrip),
   privacy policy URL, deskripsi ID + EN, kategori Utilities, dan sertifikasi rating IARC (`cc46f316-c301-8bab-8620-3fc86083db84`).
6. **Submit + sertifikasi.** Upload `.msix` (tanpa sign manual — Microsoft
   menandatangani ulang). Perbaiki temuan sertifikasi bila ada, ulangi dari
   langkah 2 dengan versi lebih tinggi. Listing publik dapat diakses di `https://apps.microsoft.com/detail/9npnh94mkjnr`.

## Batasan Store build yang diketahui

- **Startup otomatis.** Toggle di app menulis registry `Run`, yang di dalam
  kontainer MSIX bersifat tervirtualisasi. Manifest sudah mendeklarasikan
  `windows.startupTask` (aktif) agar pengguna mengatur via
  Settings > Apps > Startup. Follow-up: migrasikan toggle in-app ke StartupTask API.
- **File profil.** `ProfileStore` memakai `%LOCALAPPDATA%\DisplayFlip\`;
  di paket MSIX path ini dialihkan ke area privat paket. Profil ikut terhapus
  bila app di-uninstall — ekspor catat nama profil penting sebelum uninstall.
- **Alias CLI.** Manifest mendeklarasikan alias `displayflip.exe` agar perintah
  `--swap/--solo/--extend` tetap bisa dipakai dari terminal.

## Jalur EXE (bila tanpa MSIX)

- Sediakan URL HTTPS langsung ke `.exe` installer (bukan stub downloader).
- Semua `.exe`/`.dll` wajib ter-sign sertifikat code signing dari CA anggota
  [Microsoft Trusted Root Program](https://learn.microsoft.com/en-us/security/trusted-root/participants-list).
- Installer harus silent (UI hanya UAC), dan tiap update memakai URL berversi baru.
- Rilis GitHub zip/exe tetap jalan paralel sebagai jalur non-Store
  (lihat [publish.ps1](../publish.ps1)).
