# PROMPT: Fase Desain DisplayFlip (Windows 11)

PERAN
Kamu implementer UI untuk DisplayFlip (repo D:\GitHub\multi-monitor; C# .NET 8
+ WinForms; tray app untuk tukar susunan multi monitor dan ganti primary).
Tugas: menerapkan desain Windows 11 yang sudah disetujui ke lapisan presentasi,
tanpa menyentuh logika inti. Kamu bukan desainer: jangan menambah, menghapus,
atau "memperbaiki" desain. Kalau spesifikasi ambigu, tanya, jangan menebak.

SUMBER (baca semuanya sebelum menulis kode)
Prioritas kalau ada konflik, dari tertinggi:
1. Blok KEPUTUSAN OWNER di bawah.
2. docs/displayflip-windows11-mockup.md -> token, ukuran, tipografi, perilaku,
   larangan. Menang atas semua file mockup.
3. docs/mockups/html/*.html -> referensi ukuran, spasi, dan struktur untuk hal
   yang tidak disebut dokumen. Baca sebagai TEKS (angkanya ada di blok <style>
   dan atribut style); boleh dibuka di browser Windows sebagai pembanding.
   Jangan diport baris-per-baris ke WinForms (itu CSS flex, bukan layout
   WinForms).
4. docs/mockups/png/*.png -> target visual untuk membandingkan hasil build.

Peta file (ukuran frame pada skala 100%; kalau PNG berukuran 2x, bagi 2):
01-tray-flyout-terang            640x440 (flyout 240px lebar)
02-tray-flyout-gelap-submenu     640x440 (taskbar gelap + submenu primary terbuka)
03-settings-profil               720x540 (frame = jendelanya sendiri)
04-dialog-simpan                 560x340 (dialog 420px lebar di atas scrim)
05-dialog-monitor-hilang         560x340 (dialog 420px lebar di atas scrim)

BUKAN SPESIFIKASI (hanya properti peraga/contoh di mockup; jangan diimplementasi
sebagai fitur atau konstanta)
- Latar desktop, taskbar, jam, chevron tray, warna scrim di belakang dialog.
- Data contoh: "DELL P2422H", "HDMI 2", 1920x1080, profil "Meja Kantor",
  tanggal "10 Nov 2026".
- Shortcut "Ctrl+Shift+M" di item Tukar kiri-kanan: di dokumen hanya "mis.".
  Tampilkan hanya jika hotkey itu memang ada di app.
- Nilai rgba untuk hover/acrylic/shadow: pendekatan visual. Token bagian 6
  dokumen dan LayerFillColor* yang menang.
- Warna infobar peringatan (#FFF4CE / ikon #9D5D00) dan warna latar area
  tombol dialog: tidak ada di token, asumsi mockup. Kalau dipakai, tandai di
  laporan.
- Ikon navigasi Settings (Profil/Umum/Tentang) dan tombol caption jendela.

PERBEDAAN MOCKUP vs DOKUMEN (sudah diketahui, jangan dianggap bug)
- Submenu primary digambar di KIRI flyout karena flyout ada di tepi kanan
  layar; dokumen bilang "ke kanan". Ikuti perilaku default ToolStrip/OS.
- Ikon item "Jadikan primary": mockup = glyph monitor (mengikuti wireframe);
  dokumen bagian 2 = Star. Lihat KEPUTUSAN OWNER no. 1.
- Ikon digambar di KIRI label (sesuai teks bagian 3); wireframe ASCII
  menaruhnya di kanan.
- Status bar Settings mengikuti teks bagian 4 ("dot hijau + 2 monitor
  terdeteksi * Primary: \\.\DISPLAY1"), bukan wireframe ("Ready").
- Dialog Simpan digambar dalam state nama duplikat (peringatan + Timpa).

KEPUTUSAN OWNER (owner boleh mengubah sebelum prompt dikirim)
1. Ikon "Jadikan primary": Star outline (E734) sesuai dokumen bagian 2.
   Badge "primary" di submenu: Star solid amber (E735).
2. Item "Pengaturan..." di flyout: JANGAN ditambahkan (tidak ada di wireframe).
   Settings dibuka lewat double-click ikon tray saja. Tanyakan posisi item ini
   di laporan Fase B.
3. State "<2 monitor" (dokumen bagian 3 menyebut item "Mirror" yang tidak ada
   di wireframe): JANGAN diimplementasi. Tanyakan di laporan Fase A.
4. Konfirmasi "Hapus profil": aksi Hapus ada di menu "..." per profil, tetapi
   dialognya tidak dispesifikasikan. JANGAN dibuat dialog baru; tanyakan di
   laporan Fase C.

ATURAN KERAS
1. Jangan ubah DisplayFlip.Core (DisplayTopology, ProfileStore) maupun adapter
   Windows (EnumDisplayMonitors/ChangeDisplaySettingsEx). Perubahan hanya di
   lapisan presentasi: TrayHost + window/dialog baru yang memanggil service
   yang sama.
2. Tetap WinForms. Tanpa NuGet baru, tanpa WinUI/WPF. Kalau suatu efek tidak
   bisa dicapai native, pakai fallback sederhana, catat alasannya, jangan ganti
   toolkit.
3. UI Bahasa Indonesia. Tanpa emoji.
4. Satuan: ukuran di dokumen dan HTML = effective pixel (px pada 100% skala).
   Di WinForms pakai GraphicsUnit.Pixel untuk font. Tulisan "12pt" di bagian 3
   dokumen dibaca 12px (konsisten dengan tabel tipografi 12/16); lapor kalau
   kamu ragu.
5. Token bagian 6 dijadikan SATU file tema (terang + gelap, ikut tema Windows,
   accent sistem dengan fallback #0067B8 / #4CC2FF). Tidak ada warna atau
   ukuran hardcode tersebar di form.
6. Ikon = glyph Segoe Fluent Icons (fallback Segoe MDL2 Assets), digambar
   per ukuran 16/20/24px, tidak di-upscale dari bitmap. Kode glyph di mockup
   dipilih manual dan BELUM diverifikasi:
   Tukar kiri-kanan E8AB (dokumen menyebut ViewStream) | Simpan E74E |
   Terapkan profil E768 | Keluar E8BB | chevron submenu E76C | centang E73E |
   Star outline E734 | Star solid E735 | Profil E77B | Umum E713 | Tentang E946 |
   caption E921/E922/E8BB | Tambah E710 | Opsi lain E712 | peringatan E7BA.
   Verifikasi tiap kode ke font yang terpasang (Character Map atau daftar resmi
   Microsoft). Kalau salah atau kosong, cari yang benar dan laporkan.
7. Jangan menambah fitur di luar dokumen (lihat bagian 9 "Yang TIDAK ada").
8. Kerja di branch baru (design/win11-fluent). Commit per fase boleh; jangan
   push, jangan merge.

FASE (berurutan; setiap fase berakhir dengan STOP + laporan)
0. Baca semua sumber. Tulis: (a) ringkasan pemahamanmu maks 10 baris, (b)
   struktur kode yang ada (nama class/file TrayHost, dst.), (c) daftar
   pertanyaan atau ketidakjelasan. Belum ada kode. Tunggu OK owner.
A. Fondasi + tray flyout (layar 01-02): file tema, renderer ContextMenuStrip
   custom (hover/press/separator/radius 4/8), ikon, checkbox "Jalan otomatis",
   submenu primary (radio + badge primary). Acrylic sesuai bagian 8; kalau
   tidak stabil, fallback ke permukaan solid + shadow dan catat alasannya.
B. Settings window (layar 03): 720x540, Mica via DwmSetWindowAttribute (cek
   dokumentasi Microsoft terbaru untuk API dan versi Windows yang didukung),
   titlebar + logo 20px, nav kiri 200px dengan indikator pill 3px, daftar
   profil (Apply, menu "..."), status bar 28px. Dibuka via double-click ikon
   tray.
C. Dialog (layar 04-05): Simpan (nama duplikat -> peringatan + Timpa) dan
   Monitor hilang (Batal / Lanjutkan dengan yang tersisa).
D. Penyempurnaan: keyboard (Esc, F10), fokus terlihat, DPI PerMonitorV2,
   kontras >= 4.5:1, hormati reduced motion, uji tema terang + gelap.

KRITERIA SELESAI PER FASE (A-D)
- Build tanpa warning baru; test DisplayFlip.Core tetap lulus;
  `git diff --stat` tidak menyentuh DisplayFlip.Core maupun adapter Windows.
- Screenshot hasil build: terang + gelap, 100% dan 150% DPI, dibandingkan per
  layar dengan PNG padanannya.

FORMAT LAPORAN (tiap fase)
1. File yang berubah.
2. Yang sama dengan mockup / yang menyimpang, dengan alasan.
3. Nilai dari HTML atau PNG yang kamu pakai padahal tidak ada di dokumen
   (supaya owner bisa memutuskan apakah dijadikan spesifikasi).
4. Hal yang butuh keputusan owner.
Jangan lanjut ke fase berikutnya sebelum owner bilang OK.
