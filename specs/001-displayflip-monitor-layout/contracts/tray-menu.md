# Contract: Tray Menu

**Feature**: 001-displayflip-monitor-layout
**Surface**: `NotifyIcon` right-click menu (WinForms)

## Menu structure

```text
DisplayFlip
├── 🔄 Tukar kiri-kanan (Mirror)
├── ⭐ Jadikan primary >
│   ├── <Monitor FriendlyName — StableKey suffix> (satu item per monitor aktif)
│   └── ... 
├── 💾 Simpan layout sekarang
├── 📂 Terapkan profil >
│   ├── <ProfileName> (satu item per profil)
│   └── ... (bila kosong: "(belum ada profil)")
├── ⚙️ Jalan otomatis saat Windows start [✓/ ]
└── ❌ Keluar
```

## Behavior

- **Mirror**: memanggil operasi Mirror primary-preserving (FR-001). Setelah selesai, tooltip/trayballoon "Layout dicerminkan — Primary tetap di <FriendlyName>".
- **Jadikan primary > <monitor>**: memanggil SetPrimary(monitorStableKey) (FR-002). Bila target sudah primary, balon "Sudah menjadi primary".
- **Simpan layout sekarang**: membuka dialog InputBox kecil untuk nama profil (default "Primary Kanan" dari topologi saat ini); kaydet ke ProfileStore via Data Model. Validasi nama unik; duplikat harus mendapat konfirmasi overwrite.
- **Terapkan profil > <name>**: memanggil ApplyProfile(name) (FR-003). Bila monitor hilang: dialog "Monitor ... hilang. Lanjutkan dengan yang tersisa?" dengan tombol `Batal | Lanjutkan`. CLI setara untuk headless (exit 4 bila batal).
- **Startup toggle**: checkbox yang menyinkronkan `Settings.AutoStartEnabled` dengan registry Run key (FR-006). Perubahan langsung berlaku untuk login berikutnya; tidak memerlukan restart.
- **Keluar**: melepas NotifyIcon dan keluar dengan exit 0.

## States

- 0 monitor aktif (selain internal bawaan): menu Mirror/SetPrimary/Apply dinonaktifkan, balon pesan "Perlu minimal 2 monitor aktif untuk Mirror".
- Meng-refresh daftar monitor/profil setiap kali topologi berubah (`WM_DISPLAYCHANGE` + polling periodik ringan setelah change) — FR-010.
