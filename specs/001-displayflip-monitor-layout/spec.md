# Feature Specification: DisplayFlip multi-monitor layout

**Feature Branch**: `001-displayflip-monitor-layout`

**Created**: 2026-11-12

**Status**: Draft

**Input**: User description: "Aplikasi Windows tray untuk mengubah susunan multi monitor dengan sekali klik tanpa setting manual. Kebutuhan utama: ganti posisi monitor kadang utama di kanan kadang di kiri termasuk primary. Fitur: tukar kiri-kanan sekali klik, jadikan primary, simpan/terapkan profil layout, mode CLI untuk shortcut/hotkey, jalan otomatis saat startup. Didesain agar gampang diporting ke PowerToys dan bisa diajukan kontribusi. Riset: PowerToys Power Display belum mendukung posisi/primary/resolusi/topologi (hanya DDC/CI), profil topologi masih open issue #48286, prior art GitHub DisplayProfileManager/Monarch/Freds-Monitor-Tool membuktikan kebutuhan."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cerminkan posisi kiri-kanan sekali klik (Priority: P1)

Sebagai pengguna dengan 2+ monitor yang posisinya sering ditukar (kadang monitor utama di kanan, kadang di kiri), saya ingin membalik susunan kiri-kanan dengan satu klik di tray, agar saya tidak perlu membuka Settings Windows setiap kali ganti posisi.

**Why this priority**: Ini permintaan inti dan pembeda dari tray tools yang ada yang kebanyakan hanya menerapkan ulang profil penuh. Tanpa ini, nilai DisplayFlip tidak terasa.

**Independent Test**: Can be fully tested by satu klik menu tray "Tukar kiri-kanan" pada setup 2 monitor aktif dan mengamati posisi desktop terbalik sambil monitor primary tetap monitor yang sama secara identitas. Deliver value nyata tanpa perlu profil tersimpan.

**Acceptance Scenarios**:

1. **Given** 2 monitor aktif dengan posisi berdekatan tanpa celah/tumpang tindih dan satu monitor ditandai primary, **When** pengguna menjalankan operasi tukar kiri-kanan sekali dari tray atau CLI, **Then** urutan posisi berbalik cermin, susunan tetap legal (tanpa gap/overlap), dan monitor yang sebelumnya primary tetap primary.
2. **Given** 3+ monitor aktif dengan ukuran/resolusi berbeda, **When** pengguna menjalankan operasi tukar kiri-kanan, **Then** seluruh posisi dicerminkan terhadap sumbu vertikal susunan dan primary tetap pada monitor yang sama.
3. **Given** hanya 1 monitor aktif, **When** pengguna menjalankan operasi tukar kiri-kanan, **Then** sistem menampilkan pesan bahwa operasi memerlukan minimal 2 monitor aktif dan tidak mengubah tata letak.

---

### User Story 2 - Jadikan monitor tertentu sebagai primary (Priority: P1)

Sebagai pengguna yang kadang bekerja dengan monitor fisik di kanan sebagai utama dan kadang di kiri, saya ingin memilih monitor mana yang menjadi primary dari tray, agar konteks workspace saya berpindah mengikuti monitor yang saya hadap.

**Why this priority**: Bergandengan dengan US1 untuk skenario harian "gonta-ganti setup". Tanpa set-primary, operasi cermin saja akan terasa belum selesai.

**Independent Test**: Can be fully tested by memilih monitor target dari submenu tray dan memverifikasi monitor tersebut menjadi primary OS (berada di koordinat 0,0) dan posisi monitor lain menyesuaikan dengan legal, tanpa perlu menyimpan profil terlebih dahulu.

**Acceptance Scenarios**:

1. **Given** 2+ monitor aktif, **When** pengguna memilih salah satu monitor dari submenu "Jadikan primary", **Then** monitor tersebut menjadi primary dan seluruh posisi dihitung ulang menjadi susunan legal.
2. **Given** monitor yang dipilih adalah monitor yang sudah menjadi primary, **When** pengguna memilihnya lagi, **Then** tidak ada perubahan pemindah-pointer berlebihan dan sistem menegaskan monitor tersebut sudah primary.

---

### User Story 3 - Simpan dan terapkan profil layout dengan sekali klik (Priority: P2)

Sebagai pengguna dengan setup berulang seperti "Primary Kanan" dan "Meja Kantor", saya ingin menyimpan susunan saat ini dengan nama lalu menerapkannya sekali klik kapan saja, agar saya tidak perlu menata ulang manual.

**Why this priority**: Mengurangi kerja berulang setelah operasi dasar (US1/US2) terbukti. Prior art GitHub membuktikan fitur ini umum, tapi DisplayFlip mengutamakannya setelah cermin/primary dasar hidup.

**Independent Test**: Can be fully tested by menyimpan susunan saat ini sebagai profil bernama, mengubah posisi/primary secara manual di OS, lalu menerapkan profil dari tray dan memverifikasi kembali ke susunan tersimpan.

**Acceptance Scenarios**:

1. **Given** susunan monitor aktif saat ini, **When** pengguna memilih "Simpan layout sekarang" dan memberi nama, **Then** profil tersimpan dengan identitas monitor yang stabil (bukan nomor "Display 1/2") dan daftar profil di tray menampilkan nama tersebut.
2. **Given** profil tersimpan dan pengguna secara manual mengubah posisi/primary di OS, **When** pengguna menerapkan profil dari tray, **Then** posisi, primary, resolusi, orientasi, dan status enable/disable kembali persis ke profil tersebut.
3. **Given** profil tersimpan dan salah satu monitor yang ada di profil sudah tidak terhubung, **When** pengguna menerapkan profil, **Then** sistem tidak menggantung; sistem memberi pesan jelas monitor hilang apa, dan pengguna dapat membatalkan atau menerapkan dengan monitor yang tersisa sesuai pilihan.

---

### User Story 4 - Panggil lewat CLI untuk shortcut/hotkey (Priority: P3)

Sebagai pengguna power-user, saya ingin memanggil DisplayFlip dari command line (misalnya `DisplayFlip.exe --apply "Primary Kanan"` atau `--mirror`), agar saya bisa memasangnya ke shortcut, AutoHotkey, atau hotkey bawaan Windows.

**Why this priority**: Ekstensi untuk pengguna mahir setelah fungsi tray/ profil dasar terbukti. Memperluas jangkauan tanpa mengubah logika inti.

**Independent Test**: Can be fully tested by menjalankan perintah `--mirror` dan `--apply <nama>` dari PowerShell/command prompt dan memverifikasi efek tata letak sama seperti klik tray.

**Acceptance Scenarios**:

1. **Given** aplikasi terpasang, **When** pengguna menjalankan `--mirror`, **Then** susunan dicerminkan sama seperti klik tray dan proses CLI keluar dengan kode sukses.
2. **Given** profil "Primary Kanan" tersimpan, **When** pengguna menjalankan `--apply "Primary Kanan"`, **Then** profil tersebut diterapkan; jika nama tidak ditemukan, sistem mengeluarkan pesan error dan kode keluar kegagalan.

---

### User Story 5 - Jalan otomatis saat Windows start (Priority: P3)

Sebagai pengguna, saya ingin mengaktifkan toggle "Jalan otomatis saat Windows start", agar DisplayFlip selalu siap di tray tanpa memulai manual.

**Why this priority**: Kenyamanan sekunder; nilai terbesar datang dari US1–US4. Tidak memblokir pengujian inti.

**Independent Test**: Can be fully tested by menyalakan toggle, restart/logoff-login Windows, dan memverifikasi ikon tray DisplayFlip muncul tanpa intervensi.

**Acceptance Scenarios**:

1. **Given** aplikasi terpasang, **When** pengguna mengaktifkan toggle startup, **Then** DisplayFlip muncul di tray otomatis pada login berikutnya.
2. **Given** toggle startup aktif, **When** pengguna menonaktifkannya, **Then** DisplayFlip tidak muncul otomatis pada login berikutnya.

### Edge Cases

- What happens when monitor baru dicolokkan atau cabut di tengah sesi? Sistem harus me-refresh daftar monitor tray dan tidak memalsukan identitas.
- How does system handle layout hasil cermin/penegasan primary yang akan menabrak aturan legal (gap/overlap)? Sistem harus menghitung ulang koordinat hingga legal sebelum menerapkan, dan jika tidak ada susunan legal, gagal dengan pesan jelas tanpa memaksa.
- What happens when profil diterapkan setelah update driver/Windows dan monitor lama hilang atau ID berubah? Sistem memberi pesan monitor hilang dan tidak mengklaim selamanya abadi.
- How does system handle operasi saat 0 monitor aktif selain display bawaan yang sudah internal? Sistem memberi pesan dan tidak mengubah apa pun.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST memungkinkan pengguna menukar susunan kiri-kanan dengan satu klik dari tray, dengan semantics primary-preserving mirror (monitor primary tetap identitas yang sama, hanya posisinya dicerminkan).
- **FR-002**: System MUST memungkinkan pengguna menjadikan monitor spesifik sebagai primary dari tray, menggeser koordinat primary ke posisi 0,0 dan menghitung ulang posisi lain agar tetap legal tanpa gap/overlap.
- **FR-003**: System MUST memungkinkan pengguna menyimpan susunan aktif (posisi, primary, resolusi, orientasi, enable/disable) sebagai profil bernama dan menerapkan kembali dengan satu klik.
- **FR-004**: System MUST mengidentifikasi monitor secara stabil untuk kunci profil (berdasarkan identitas fisik perangkat, bukan nomor DISPLAY ordinal), dan MUST menampilkan daftar monitor dengan nama yang dapat dikenali.
- **FR-005**: System MUST menyediakan mode command line untuk `--mirror` dan `--apply "<nama profil>"` dengan kode keluar 0 saat berhasil dan non-zero saat gagal.
- **FR-006**: System MUST menyediakan toggle startup yang mengatur apakah aplikasi muncul di tray otomatis saat Windows start.
- **FR-007**: System MUST memvalidasi setiap susunan yang akan diterapkan sebelum mengaktifkannya, dan MUST menangani kegagalan dengan pesan yang dapat ditindaklanjuti (jangan menganggap apply selalu berhasil).
- **FR-008**: System MUST memberi pesan jelas ketika profil merujuk monitor yang hilang, dan MUST memberi pengguna pilihan untuk membatalkan atau lanjut dengan monitor yang tersisa.
- **FR-009**: System MUST tetap usable tanpa perlu membuka Windows Settings untuk semua operasi di FR-001–FR-003.
- **FR-010**: System MUST meng-refresh daftar monitor aktif ketika topologi berubah (colok/cabut, perubahan resolusi/orientasi).

### Key Entities

- **Monitor**: Satu layar aktif teridentifikasi secara fisik (device path, friendly name, EDID manufacture/product). Atribut kunci: identitas stabil, nama tampilan, posisi saat ini, status primary, resolusi/orientasi aktif.
- **LayoutProfile**: Kumpulan state tata letak bernama yang menyimpan posisi, pilihan primary, resolusi/orientasi, dan status aktif per monitor, terkunci ke identitas stabil. Relasi: merujuk banyak Monitor.
- **LayoutOperation**: Operasi sekali klik yang mengubah topologi (Mirror, SetPrimary, ApplyProfile). Atribut kunci: jenis operasi, target monitor/profil, hasil (berhasil/gagal dengan alasan).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete a left-right swap or primary change in under 3 seconds from single tray click to settled layout on a 2-monitor setup.
- **SC-002**: 100% of valid mirror and set-primary operations on 2+ active monitors result in a legal layout (no gaps/overlaps) with correct primary position after one attempt.
- **SC-003**: 90% of users can save a named profile and reapply it correctly on first attempt without needing to reopen Windows Settings.
- **SC-004**: Command line `--mirror` and `--apply "<profile>"` produce the same settled layout as the equivalent tray click in 100% of matched visual checks on the same machine.

## Assumptions

- Target platform is Windows 10/11 x64 with 2+ active displays; single-monitor fallback is pesan informatif, bukan operasi.
- Pengguna memvalidasi variasi resolusi/ukuran berbeda pada monitor asli di kantor; asumsi legalisasi cermin (tanpa gap/overlap) diuji dengan setup yang berbeda-beda.
- Profil di desain best-effort lintas reboot/driver/port dengan pemulihan yang jelas saat monitor hilang, bukan jaminan permanen abadi (selaras peringatan Freds-Monitor-Tool).
- Celah diferensiasi "cermin sekali klik dengan primary tetap" diperlakukan sebagai hipotesis yang harus dibuktikan di pengujian monitor asli, bukan klaim pemasaran.
- Logika inti (query topologi → ubah position/primary → validate → apply) disengaja dibuat portable agar Fase 2 dapat diporting ke PowerToys Power Display / Issue #48286; bukan fork yang langsung.
- Evidence riset diambil 2026-11-12 (Power Display DDC-only, profil shipped hanya brightness/contrast/volume/color-temperature, topologi belum dirilis); status Issue #48286 dan dokumentasi Power Display harus dicek ulang sebelum jendela kontribusi.
