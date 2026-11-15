using System.Collections.Frozen;
using System.Globalization;

namespace DisplayFlip.Core.Localization;

/// <summary>
/// Central bilingual localization catalog (Indonesian / English) shared across
/// DisplayFlip.Core, DisplayFlip.Windows, DisplayFlip.Wpf, tray, overlays, and CLI.
/// </summary>
public static class LocalizationCatalog
{
    public const string PreferenceAuto = "auto";
    public const string LanguageIndonesian = "id";
    public const string LanguageEnglish = "en";

    private static readonly IReadOnlyDictionary<string, string> IndonesianMap =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Window & Navigation
            ["App.WindowTitle"] = "DisplayFlip — Pengaturan Multi-Monitor",
            ["App.BrandTitle"] = "DisplayFlip",
            ["Nav.Displays"] = "Layar & Profil",
            ["Nav.Shortcuts"] = "Pintasan Keyboard",
            ["Nav.General"] = "Umum & Ikon",
            ["Nav.About"] = "Tentang & CLI",

            // General & Icons Page
            ["General.PageTitle"] = "Umum & Ikon",
            ["General.PageSubtitle"] = "Preferensi startup sistem, otomatisasi tata letak monitor, bahasa aplikasi, personalisasi tema aplikasi, gaya ikon taskbar, dan direktori penyimpanan.",
            ["General.Section.SystemBehavior"] = "Perilaku Aplikasi",
            ["General.Startup.Title"] = "Jalan otomatis saat Windows start",
            ["General.Startup.Desc"] = "Mulai DisplayFlip di latar belakang (--tray) secara otomatis saat Anda login ke Windows.",
            ["General.AutoApply.Title"] = "Langsung terapkan tata letak setelah digeser (Auto-Apply)",
            ["General.AutoApply.Desc"] = "Langsung terapkan posisi monitor begitu selesai menggeser di kanvas tanpa perlu klik Terapkan Perubahan.",

            ["General.Section.Language"] = "Bahasa Antarmuka",
            ["General.Language.CardTitle"] = "Pilih Bahasa Aplikasi",
            ["General.Language.CardDesc"] = "Berlaku instan di seluruh jendela pengaturan, kanvas 2D, system tray, dan overlay tanpa restart.",
            ["General.Language.FollowWindows"] = "Ikuti Bahasa Windows",
            ["General.Language.Indonesian"] = "Bahasa Indonesia",
            ["General.Language.English"] = "English",
            ["General.Language.CurrentEffective"] = "Bahasa aktif saat ini: {0}",
            ["General.Language.Name.id"] = "Bahasa Indonesia",
            ["General.Language.Name.en"] = "Inggris (English)",
            ["General.Language.InfoTitle"] = "Bahasa Diperbarui",
            ["General.Language.ChangedAuto"] = "Preferensi bahasa diatur mengikuti Windows (aktif: {0}).",
            ["General.Language.ChangedExplicit"] = "Bahasa antarmuka diubah ke {0}.",
            ["General.Language.SaveError"] = "Gagal menyimpan preferensi bahasa: {0}",

            ["General.Section.Theme"] = "Personalisasi Tema Aplikasi",
            ["General.Theme.Desc"] = "Pilih tampilan terang, gelap, atau mengikuti konfigurasi warna sistem Windows 11 Anda secara dinamis.",
            ["General.Theme.System.Title"] = "Ikuti Sistem",
            ["General.Theme.System.Subtitle"] = "Menyesuaikan dengan mode terang atau gelap Windows 11 secara otomatis.",
            ["General.Theme.System.Label"] = "Ikuti Sistem (Otomatis)",
            ["General.Theme.Light.Title"] = "Mode Terang (Light)",
            ["General.Theme.Light.Subtitle"] = "Tampilan antarmuka bersih, terang, dan cerah di segala kondisi pencahayaan.",
            ["General.Theme.Dark.Title"] = "Mode Gelap (Dark)",
            ["General.Theme.Dark.Subtitle"] = "Tampilan antarmuka bernuansa gelap yang elegan dan nyaman untuk mata.",
            ["General.Theme.UpdatedTitle"] = "Tema Aplikasi Diperbarui",
            ["General.Theme.UpdatedMsg"] = "Tampilan antarmuka berhasil disesuaikan ke {0}.",
            ["General.Theme.FailedTitle"] = "Tema Aplikasi",
            ["General.Theme.FailedMsg"] = "Gagal menerapkan tema {0} secara penuh.",

            ["General.Section.Icon"] = "Gaya Ikon Modern (Tray & App)",
            ["General.Icon.Desc"] = "Pilih desain ikon DisplayFlip favorit Anda. Perubahan langsung memperbarui ikon di Title Bar, Taskbar, dan System Tray.",
            ["General.Badge.Active"] = "Aktif",
            ["General.Icon.Fluent.Title"] = "Fluent Glass Dual",
            ["General.Icon.Fluent.Subtitle"] = "Dua layar berlapis biru-sian modern dengan lencana flip emas elegan.",
            ["General.Icon.Fluent.Badge"] = "Rekomendasi",
            ["General.Icon.Accent.Title"] = "Windows 11 Accent",
            ["General.Icon.Accent.Subtitle"] = "Menyesuaikan warna aksen secara dinamis dengan tema Windows 11 Anda.",
            ["General.Icon.Accent.Badge"] = "Dinamis",
            ["General.Icon.Minimal.Title"] = "Minimalist Precision",
            ["General.Icon.Minimal.Subtitle"] = "Kontras tinggi monokrom tajam untuk area notifikasi taskbar yang bersih.",
            ["General.Icon.Minimal.Badge"] = "Monokrom",
            ["General.Icon.UpdatedTitle"] = "Gaya Ikon Diperbarui",
            ["General.Icon.UpdatedMsg"] = "Gaya ikon berhasil diperbarui ke '{0}'.",
            ["General.Icon.FailedTitle"] = "Gagal Memperbarui Ikon",
            ["General.Icon.SaveError"] = "Gagal menyimpan pilihan gaya ikon: {0}",

            ["General.Section.Storage"] = "Direktori Penyimpanan",
            ["General.Storage.Title"] = "Lokasi File Profil & Konfigurasi",
            ["General.Storage.OpenFolder"] = "Buka Folder",
            ["General.Storage.OpenedTitle"] = "Folder Dibuka",
            ["General.Storage.InvalidDir"] = "Direktori penyimpanan konfigurasi tidak valid.",
            ["General.Storage.OpenSuccess"] = "Folder penyimpanan berhasil dibuka: {0}",
            ["General.Storage.OpenError"] = "Gagal membuka folder penyimpanan: {0}",
            ["General.LoadErrorTitle"] = "Kesalahan Memuat Pengaturan",

            ["General.Startup.EnabledMsg"] = "DisplayFlip akan berjalan otomatis di latar belakang (--tray) saat login Windows.",
            ["General.Startup.DisabledMsg"] = "Startup otomatis dinonaktifkan.",
            ["General.Startup.RegistryError"] = "Gagal mengubah konfigurasi startup Windows di registri.",
            ["General.Startup.UpdateError"] = "Gagal memperbarui startup Windows: {0}",
            ["General.Startup.FailedTitle"] = "Gagal Mengubah Startup",
            ["General.Startup.SuccessTitle"] = "Startup Windows Diperbarui",

            ["General.AutoApply.EnabledMsg"] = "Tata letak monitor akan langsung diterapkan otomatis setiap selesai menggeser di kanvas.",
            ["General.AutoApply.DisabledMsg"] = "Auto-apply dinonaktifkan. Gunakan tombol 'Terapkan Perubahan' untuk menerapkan tata letak.",
            ["General.AutoApply.SaveError"] = "Gagal menyimpan pengaturan auto-apply: {0}",
            ["General.AutoApply.UpdatedTitle"] = "Tata Letak Otomatis",
            ["General.AutoApply.SaveFailedTitle"] = "Gagal Menyimpan Pengaturan",
            ["General.FolderErrorTitle"] = "Gagal Membuka Folder",

            ["General.AppFolder.NotFound"] = "Folder instalasi aplikasi tidak ditemukan.",
            ["General.AppFolder.Opened"] = "Folder aplikasi berhasil dibuka: {0}",
            ["General.AppFolder.OpenError"] = "Gagal membuka folder aplikasi: {0}",
            ["General.Url.Invalid"] = "Tautan URL tidak valid.",
            ["General.Url.Opening"] = "Membuka tautan: {0}",
            ["General.Url.OpenError"] = "Gagal membuka tautan: {0}",

            // Displays & Profiles Page
            ["Displays.PageTitle"] = "Layar & Profil",
            ["Displays.PageSubtitle"] = "Atur susunan posisi monitor, orientasi, resolusi, dan simpan profil tata letak multi-layar.",
            ["Displays.Section.Layout"] = "Tata Letak Monitor",
            ["Displays.Canvas.CardTitle"] = "Kanvas Multi-Monitor 2D",
            ["Displays.Canvas.DefaultSubtitle"] = "Klik monitor untuk konfigurasi atau geser kotak monitor untuk mengatur susunan koordinat.",
            ["Displays.Canvas.ActiveSubtitle"] = "{0} layar aktif • Klik monitor untuk konfigurasi atau geser kotak monitor untuk mengatur susunan koordinat.",
            ["Displays.Canvas.NoMonitorsSubtitle"] = "0 layar aktif • Tidak ada monitor aktif terdeteksi.",
            ["Displays.Toolbar.Identify"] = "Identifikasi Layar",
            ["Displays.Toolbar.Snap"] = "Rapatkan ke Tepi (Snap)",
            ["Displays.Toolbar.MonitorNames"] = "Nama Monitor",
            ["Displays.Toolbar.Reset"] = "Reset Tata Letak",
            ["Displays.Toolbar.Apply"] = "Terapkan Perubahan",

            ["Displays.Section.Selected"] = "Konfigurasi Monitor Terpilih",
            ["Displays.Inspector.NoSelection"] = "Pilih sebuah monitor di kanvas",
            ["Displays.Inspector.NoSelectionSubtitle"] = "Klik kotak nomor monitor di kanvas atas untuk mengatur properti layar.",
            ["Displays.Inspector.PrimaryRole"] = "Layar Utama",
            ["Displays.Inspector.SecondaryRole"] = "Layar Sekunder",
            ["Displays.Inspector.DetailsFormat"] = "Resolusi {0} × {1}{2} • Posisi ({3}, {4}) • {5}",
            ["Displays.Inspector.Orientation.Title"] = "Orientasi Tampilan",
            ["Displays.Inspector.Orientation.Desc"] = "Pilih orientasi layar lanskap atau potret untuk monitor yang dipilih.",
            ["Displays.Inspector.Orientation.Landscape"] = "Lanskap",
            ["Displays.Inspector.Orientation.Portrait"] = "Potret",
            ["Displays.Inspector.Orientation.LandscapeFlipped"] = "Lanskap Dibalik",
            ["Displays.Inspector.Orientation.PortraitFlipped"] = "Potret Dibalik",
            ["Displays.Inspector.Resolution.Title"] = "Resolusi Layar",
            ["Displays.Inspector.Resolution.Desc"] = "Pilih resolusi piksel tampilan yang didukung oleh monitor yang dipilih.",
            ["Displays.Inspector.SetPrimary"] = "Jadikan Layar Utama",
            ["Displays.Inspector.SoloDefault"] = "Hanya Layar Ini (Solo)",
            ["Displays.Inspector.SoloNumbered"] = "Hanya Layar {0} Saja",

            ["Displays.Profiles.Title"] = "Profil Tata Letak Tersimpan",
            ["Displays.Profiles.SaveNew"] = "+ Simpan Profil Baru",
            ["Displays.Profiles.EmptyTitle"] = "Belum ada profil tersimpan",
            ["Displays.Profiles.EmptySubtitle"] = "Simpan susunan multi-layar saat ini dengan mengklik '+ Simpan Profil Baru'.",
            ["Displays.Profiles.CardMeta"] = "{0} Layar • Primary: {1}",
            ["Displays.Profiles.ApplyBtn"] = "Terapkan",
            ["Displays.Profiles.DeleteBtn"] = "Hapus",

            ["Displays.Canvas.EmptyState"] = "Tidak ada monitor aktif terdeteksi.",
            ["Displays.Canvas.PrimaryBadge"] = "★ Utama",
            ["Displays.Canvas.SnapBadge"] = "Snap ({0},{1})",

            ["Displays.Info.AutoAppliedTitle"] = "Susunan Diterapkan Otomatis",
            ["Displays.Info.AutoAppliedMsg"] = "Susunan posisi monitor berhasil langsung diterapkan ke sistem Windows.",
            ["Displays.Info.ArrangementChangedTitle"] = "Susunan Monitor Diubah",
            ["Displays.Info.ArrangementChangedMsg"] = "Susunan posisi monitor diperbarui di kanvas. Klik 'Terapkan Perubahan' atau simpan ke profil baru.",
            ["Displays.Info.ApplyFailedTitle"] = "Gagal Menerapkan Susunan",
            ["Displays.Info.ResetTitle"] = "Tata Letak Direset",
            ["Displays.Info.ResetMsg"] = "Kanvas dikembalikan ke susunan monitor aktif saat ini.",
            ["Displays.Info.AppliedTitle"] = "Susunan Diterapkan",
            ["Displays.Info.AppliedMsg"] = "Susunan posisi monitor berhasil diterapkan ke sistem Windows.",
            ["Displays.Info.PrimaryAppliedTitle"] = "Layar Utama Diterapkan",
            ["Displays.Info.PrimaryAppliedMsg"] = "Layar utama dan koordinat susunan berhasil diterapkan ke sistem Windows.",
            ["Displays.Info.PrimaryChangedTitle"] = "Layar Utama Diubah",
            ["Displays.Info.PrimaryChangedMsg"] = "Layar utama telah diubah di kanvas. Klik 'Terapkan Perubahan' untuk menerapkan ke sistem Windows.",
            ["Displays.Info.SoloUpdatedTitle"] = "Mode Layar Tunggal (Solo)",
            ["Displays.Info.SoloFailedTitle"] = "Gagal Mode Solo",
            ["Displays.Info.OrientationChangedTitle"] = "Orientasi Diubah",
            ["Displays.Info.OrientationChangedMsg"] = "Orientasi layar diubah di kanvas. Klik 'Terapkan Perubahan' untuk menerapkan ke sistem.",
            ["Displays.Info.ResolutionChangedTitle"] = "Resolusi Diubah",
            ["Displays.Info.ResolutionChangedMsg"] = "Resolusi layar diubah di kanvas. Klik 'Terapkan Perubahan' untuk menerapkan ke sistem.",
            ["Displays.Info.ProfileAppliedPartialTitle"] = "Profil Diterapkan Sebagian",
            ["Displays.Info.ProfileAppliedTitle"] = "Profil Diterapkan",
            ["Displays.Info.ProfileApplyFailedTitle"] = "Gagal Menerapkan Profil",
            ["Displays.Info.DialogErrorTitle"] = "Dialog Error",
            ["Displays.Info.DeleteDialogErrorMsg"] = "Tidak dapat membuka dialog konfirmasi hapus profil.",
            ["Displays.Dialog.Delete.Title"] = "Hapus Profil",
            ["Displays.Dialog.Delete.Content"] = "Apakah Anda yakin ingin menghapus profil tata letak '{0}'? Tindakan ini tidak dapat dibatalkan.",
            ["Displays.Dialog.Delete.PrimaryBtn"] = "Hapus",
            ["Displays.Dialog.Delete.CloseBtn"] = "Batal",
            ["Displays.Info.ProfileDeletedTitle"] = "Profil Dihapus",
            ["Displays.Info.ProfileDeletedMsg"] = "Profil '{0}' telah dihapus dari daftar.",
            ["Displays.Info.DeleteDialogExceptionTitle"] = "Error Dialog Hapus",
            ["Displays.Info.SaveDialogErrorMsg"] = "Tidak dapat membuka dialog simpan profil baru.",
            ["Displays.Dialog.Save.Title"] = "Simpan Profil Baru",
            ["Displays.Dialog.Save.Prompt"] = "Masukkan nama untuk profil tata letak monitor saat ini:",
            ["Displays.Dialog.Save.Placeholder"] = "Contoh: Kerja Dual Monitor",
            ["Displays.Dialog.Save.PrimaryBtn"] = "Simpan",
            ["Displays.Dialog.Save.CloseBtn"] = "Batal",
            ["Displays.Info.EmptyNameTitle"] = "Nama Profil Kosong",
            ["Displays.Info.SaveEmptyName"] = "Nama profil tidak boleh kosong.",
            ["Displays.Info.NameTooLongTitle"] = "Nama Terlalu Panjang",
            ["Displays.Info.NameTooLongMsg"] = "Nama profil maksimal 100 karakter.",
            ["Displays.Info.ProfileSavedTitle"] = "Profil Disimpan",
            ["Displays.Info.ProfileSavedMsg"] = "Profil tata letak '{0}' berhasil disimpan.",
            ["Displays.Info.ProfileSaveFailedTitle"] = "Gagal Menyimpan Profil",
            ["Displays.Info.SaveDialogExceptionTitle"] = "Error Dialog Simpan",

            ["DisplayHelper.ShortName"] = "Layar {0} ({1})",
            ["DisplayHelper.FallbackMonitor"] = "Monitor",

            // Shortcuts Page & Helper
            ["Shortcuts.PageTitle"] = "Pintasan Keyboard",
            ["Shortcuts.PageSubtitle"] = "Atur tombol pintas global untuk aksi cepat multi-layar dan pergantian profil tata letak.",
            ["Shortcuts.Section.Global"] = "Aksi Cepat Global",
            ["Shortcuts.Global.Mirror.Title"] = "Balik / Tukar Posisi Layar",
            ["Shortcuts.Global.Mirror.Desc"] = "Tukar posisi horizontal antar dua monitor secara instan (default: Ctrl+Alt+F).",
            ["Shortcuts.Global.Primary.Title"] = "Siklus Layar Utama",
            ["Shortcuts.Global.Primary.Desc"] = "Pindahkan monitor utama secara bergiliran ke layar berikutnya (default: Ctrl+Alt+P).",
            ["Shortcuts.Global.Identify.Title"] = "Identifikasi Layar",
            ["Shortcuts.Global.Identify.Desc"] = "Tampilkan nomor monitor fisik besar di setiap sudut layar (default: Ctrl+Alt+I).",
            ["Shortcuts.Global.Solo1.Title"] = "Hanya Layar 1 (Solo Display 1)",
            ["Shortcuts.Global.Solo1.Desc"] = "Hanya menampilkan layar monitor 1 (internal/utama) dan mematikan layar lainnya.",
            ["Shortcuts.Global.Solo2.Title"] = "Hanya Layar 2 (Solo Display 2)",
            ["Shortcuts.Global.Solo2.Desc"] = "Hanya menampilkan layar monitor 2 (eksternal) dan mematikan layar lainnya.",
            ["Shortcuts.Global.Extend.Title"] = "Perluas Multi-Layar (Extend Displays)",
            ["Shortcuts.Global.Extend.Desc"] = "Mengaktifkan kembali seluruh monitor dalam mode bentangkan / perluas (Extend).",
            ["Shortcuts.Conflict.Solo1"] = "Hanya Layar 1",
            ["Shortcuts.Conflict.Solo2"] = "Hanya Layar 2",
            ["Shortcuts.Conflict.Extend"] = "Perluas Multi-Layar",
            ["Shortcuts.Conflict.Profile"] = "Profil '{0}'",
            ["Shortcuts.Section.Profiles"] = "Pintasan Profil Tersimpan",
            ["Shortcuts.Section.ProfilesSubtitle"] = "Atur kombinasi tombol cepat untuk beralih langsung ke susunan profil monitor yang telah disimpan.",
            ["Shortcuts.Empty.Title"] = "Belum Ada Profil Tersimpan",
            ["Shortcuts.Empty.Desc"] = "Simpan profil tata letak di tab Layar & Profil terlebih dahulu untuk mengatur pintasan profil.",
            ["Shortcuts.Profile.CardTitle"] = "Profil: {0}",
            ["Shortcuts.Profile.CardMeta"] = "{0} monitor • Layar Utama: {1}",
            ["Shortcuts.Button.Add"] = "+ Pintasan",
            ["Shortcuts.Button.AddToolTip"] = "Klik untuk mengatur kombinasi tombol pintas",
            ["Shortcuts.Button.ActiveToolTip"] = "Pintasan aktif: {0}. Klik untuk mengubah atau menghapus.",
            ["Shortcuts.Keycap.Unset"] = "(Belum diatur)",
            ["Shortcuts.Error.PrimaryKeyRequired"] = "Tekan tombol utama (mis. A-Z, 0-9, F1-F12, dll.)",
            ["Shortcuts.Error.UnsupportedKey"] = "Tombol ini tidak didukung sebagai pintasan global.",
            ["Shortcuts.Error.ModifierRequired"] = "Pintasan global memerlukan modifier (Ctrl, Alt, Shift, atau Win).",
            ["Shortcuts.Dialog.ErrorTitle"] = "Error Dialog",
            ["Shortcuts.Dialog.OpenError"] = "Tidak dapat membuka modal dialog pintasan.",
            ["Shortcuts.Dialog.Title"] = "Atur Pintasan Keyboard",
            ["Shortcuts.Dialog.Instructions"] = "Target: {0}\nTekan kombinasi tombol pada keyboard Anda (misal Ctrl+Alt+F atau F9):",
            ["Shortcuts.Dialog.PresetsHeader"] = "Preset Cepat:",
            ["Shortcuts.Dialog.ClearPreset"] = "Kosongkan",
            ["Shortcuts.Dialog.SaveBtn"] = "Simpan Pintasan",
            ["Shortcuts.Dialog.RemoveBtn"] = "Hapus Pintasan",
            ["Shortcuts.Dialog.CancelBtn"] = "Batal",
            ["Shortcuts.Dialog.EmptyRecorder"] = "(Kosong — Tekan kombinasi tombol...)",
            ["Shortcuts.Dialog.ConflictTitle"] = "Pintasan Sudah Digunakan",
            ["Shortcuts.Dialog.ConflictMsg"] = "Pintasan '{0}' saat ini digunakan oleh {1}. Jika disimpan, pintasan pemegang sebelumnya akan otomatis dilepaskan.",
            ["Shortcuts.Info.SaveFailedTitle"] = "Gagal Menyimpan",
            ["Shortcuts.Info.SaveFailedMsg"] = "Tidak dapat memperbarui pintasan untuk '{0}'.",
            ["Shortcuts.Info.ClearedMsg"] = "Pintasan untuk '{0}' berhasil dikosongkan.",
            ["Shortcuts.Info.SavedMsg"] = "Pintasan '{0}' berhasil disimpan untuk '{1}'.",
            ["Shortcuts.Info.FreedSuffix"] = " (Pintasan otomatis dilepaskan dari {0}).",
            ["Shortcuts.Info.UpdatedTitle"] = "Pintasan Diperbarui",

            // About & CLI Page
            ["About.PageTitle"] = "Tentang & CLI",
            ["About.PageSubtitle"] = "Informasi aplikasi DisplayFlip, status runtime komponen sistem, dan dokumentasi perintah baris perintah (CLI).",
            ["About.Hero.Badge"] = "v2.0 (Windows 11 Fluent Edition)",
            ["About.Hero.Description"] = "Utilitas manajemen multi-layar modern untuk Windows 11 dengan performa native CCD (Connecting and Configuring Displays) API, mendukung drag-and-drop koordinat 2D presisi, manajemen profil instan, pintasan keyboard global, dan integrasi latar belakang yang senyap.",
            ["About.Section.Cli"] = "Dokumentasi & Referensi Perintah CLI",
            ["About.Section.CliDesc"] = "Seluruh opsi perintah baris perintah (CLI) yang dapat dijalankan melalui Windows Terminal, PowerShell, Command Prompt, atau skrip automasi. Klik tombol Salin untuk menyalin perintah ke clipboard.",
            ["About.Button.Copy"] = "Salin",
            ["About.Button.Copied"] = "Disalin!",
            ["About.Section.Links"] = "Tautan Cepat & Sumber Daya",
            ["About.Links.Docs.Title"] = "Situs Proyek DisplayFlip",
            ["About.Links.Docs.Subtitle"] = "displayflip.snipgeek.com — Halaman resmi, panduan pengguna, & rilis terbaru.",
            ["About.Links.Docs.Btn"] = "Buka Situs",
            ["About.Links.GitHub.Title"] = "Repositori GitHub",
            ["About.Links.GitHub.Subtitle"] = "github.com/ieproject-app/DisplayFlip — Kode sumber, issue, & kontribusi terbuka.",
            ["About.Links.GitHub.Btn"] = "Buka di GitHub",
            ["About.Links.AppFolder.Title"] = "Folder Aplikasi",
            ["About.Links.AppFolder.Subtitle"] = "Akses langsung ke folder executable DisplayFlip dan file biner.",
            ["About.Links.AppFolder.Btn"] = "Buka Folder",
            ["About.Links.ConfigFolder.Title"] = "File Konfigurasi",
            ["About.Links.ConfigFolder.Subtitle"] = "Buka lokasi penyimpanan profil JSON di AppData pengguna.",
            ["About.Links.ConfigFolder.Btn"] = "Buka Folder Profil",
            ["About.Info.CopiedTitle"] = "Perintah Disalin ke Clipboard",
            ["About.Info.CopiedMsg"] = "Perintah '{0}' telah disalin dan siap dijalankan.",
            ["About.Info.CopyFailedTitle"] = "Gagal Menyalin",
            ["About.Info.CopyFailedMsg"] = "Akses clipboard Windows sedang terkunci oleh aplikasi lain.",
            ["About.Info.OpeningBrowserTitle"] = "Membuka Browser",
            ["About.Info.OpeningWebsiteMsg"] = "Membuka situs proyek DisplayFlip di browser: {0}",
            ["About.Info.WebsiteLinkWarnTitle"] = "Tautan Situs Proyek",
            ["About.Info.OpeningGitHubMsg"] = "Membuka repositori GitHub DisplayFlip di browser: {0}",
            ["About.Info.GitHubLinkWarnTitle"] = "Tautan Dokumentasi",
            ["About.Info.AppFolderOpenedTitle"] = "Folder Aplikasi Dibuka",
            ["About.Info.ConfigFolderOpenedTitle"] = "Folder Profil Dibuka",

            // Identify Overlay
            ["Overlay.WindowTitle"] = "DisplayFlip — Identifikasi Layar",
            ["Overlay.PrimarySuffix"] = "{0} • Layar Utama",
            ["Overlay.DefaultSubtitle"] = "Layar 1 • Layar Utama",

            // System Tray
            ["Tray.Tooltip"] = "DisplayFlip — Pengelola Multi-Monitor",
            ["Tray.Menu.Swap"] = "Tukar Kiri-Kanan (Quick Swap)",
            ["Tray.Menu.Identify"] = "Identifikasi Layar",
            ["Tray.Menu.MultiDisplay"] = "Multi-Layar",
            ["Tray.Menu.Solo1"] = "Hanya Layar 1",
            ["Tray.Menu.Solo2"] = "Hanya Layar 2",
            ["Tray.Menu.Extend"] = "Bentangkan / Extend",
            ["Tray.Menu.Profiles"] = "Profil Tampilan",
            ["Tray.Menu.NoProfiles"] = "(Belum ada profil tersimpan)",
            ["Tray.Menu.OpenSettings"] = "Buka Pengaturan",
            ["Tray.Menu.Exit"] = "Keluar",

            // DisplayActionService
            ["Actions.Swap.NeedTwoMonitors"] = "Perlu minimal 2 monitor aktif untuk menukar posisi.",
            ["Actions.Swap.Success"] = "Susunan monitor berhasil ditukar (kiri-kanan).",
            ["Actions.Swap.Failed"] = "Gagal menukar susunan: {0}",
            ["Actions.Swap.BuildFailed"] = "Gagal membangun susunan cermin monitor.",
            ["Actions.Swap.ValidationFailed"] = "Validasi topologi gagal: {0}",
            ["Actions.Swap.ApplyFailed"] = "Gagal menerapkan susunan: {0}",
            ["Actions.CyclePrimary.NeedTwoMonitors"] = "Perlu minimal 2 monitor aktif untuk mengganti layar utama.",
            ["Actions.CyclePrimary.Success"] = "{0} kini menjadi layar utama.",
            ["Actions.CyclePrimary.Failed"] = "Gagal menetapkan layar utama: {0}",
            ["Actions.CyclePrimary.BuildFailed"] = "Gagal membangun susunan topologi.",
            ["Actions.CyclePrimary.ValidationFailed"] = "Validasi gagal: {0}",
            ["Actions.CyclePrimary.ApplyFailed"] = "Gagal menerapkan: {0}",
            ["Actions.Profile.EmptyName"] = "Nama profil tidak boleh kosong.",
            ["Actions.Profile.NotFound"] = "Profil '{0}' tidak ditemukan.",
            ["Actions.Profile.NoneConnected"] = "Tidak ada monitor dari profil '{0}' yang terhubung saat ini.",
            ["Actions.Profile.AppliedPartial"] = "Profil '{0}' diterapkan sebagian ({1} monitor dari profil tidak terhubung secara fisik).",
            ["Actions.Profile.Applied"] = "Profil '{0}' berhasil diterapkan.",

            // ProjectionDispatcher
            ["Projection.Mode.Internal"] = "Hanya Layar 1",
            ["Projection.Mode.External"] = "Hanya Layar 2",
            ["Projection.Mode.Clone"] = "Duplikat (Clone)",
            ["Projection.Mode.Extend"] = "Perluas (Extend)",
            ["Projection.UnknownMode"] = "Mode proyeksi tidak dikenal: '{0}'. Opsi yang valid: extend, clone, internal (solo 1), external (solo 2).",
            ["Projection.Applied"] = "Mode multi-layar diterapkan: {0}.",
            ["Projection.DispatchedViaSwitch"] = "Mode {0} dikirim melalui DisplaySwitch ({1}).",
            ["Projection.DisplaySwitchMissing"] = "Gagal mengubah mode ({0}) dan DisplaySwitch.exe tidak ditemukan.",
            ["Projection.ChangeFailed"] = "Gagal mengubah mode proyeksi: {0}",
            ["Projection.SoloUnsupportedIndex"] = "Hanya Layar 1 dan Layar 2 yang didukung untuk mode layar tunggal.",

            // CLI
            ["Cli.Help.Body"] = """
DisplayFlip — Multi-Monitor Manager CLI
Penggunaan:
  displayflip.exe [perintah] [opsi]

Perintah Utama:
  --swap, --flip, --mirror    Tukar posisi horizontal monitor primer dan sekunder.
  --identify, -i              Tampilkan overlay nomor layar fisik pada seluruh monitor.
  --solo <1|2>                Aktifkan hanya Layar 1 atau Layar 2 (proyeksi tunggal).
  --extend, --perluas         Bentangkan tampilan desktop ke seluruh layar terhubung.
  --clone, --duplicate        Duplikasi tampilan ke seluruh layar terhubung.
  --apply <nama>, -p <nama>   Terapkan profil konfigurasi tata letak monitor tersimpan.
  --list [--json]             Daftar nama profil tersimpan (opsional format JSON).
  --lang <id|en>              Ganti bahasa output CLI sementara tanpa mengubah config.
  --settings, --config        Buka jendela antarmuka pengaturan DisplayFlip.
  --tray, --minimized         Jalankan DisplayFlip tenang di area notifikasi (system tray).
  --help, -h                  Tampilkan bantuan ini.
""",
            ["Cli.List.Header"] = "Daftar Profil Tersimpan ({0}):",
            ["Cli.List.Row"] = "  • {0,-24} | Pintasan: {1,-12} | Monitor: {2}",
            ["Cli.Identify.Shown"] = "[DisplayFlip] Menampilkan overlay nomor monitor.",
            ["Cli.Error.UnknownCommand"] = "[DisplayFlip] Perintah CLI tidak dikenali. Gunakan --help untuk melihat daftar perintah.",

            ["Cli.Doc.Swap"] = "Tukar posisi horizontal monitor primer dan sekunder secara instan tanpa membuka jendela antarmuka.",
            ["Cli.Doc.Swap.Title"] = "Tukar Posisi Monitor",
            ["Cli.Doc.Identify"] = "Tampilkan overlay nomor layar fisik berukuran besar di setiap monitor aktif selama beberapa detik.",
            ["Cli.Doc.Identify.Title"] = "Identifikasi Monitor",
            ["Cli.Doc.Solo"] = "Aktifkan hanya Layar 1 atau Layar 2 secara instan dan nonaktifkan layar lainnya.",
            ["Cli.Doc.Solo.Title"] = "Mode Layar Tunggal (Solo)",
            ["Cli.Doc.Extend"] = "Bentangkan tampilan desktop ke seluruh monitor yang terhubung (mode extend Windows).",
            ["Cli.Doc.Extend.Title"] = "Bentangkan Multi-Monitor (Extend)",
            ["Cli.Doc.Profile"] = "Terapkan koordinat tata letak dan konfigurasi monitor dari nama profil yang telah disimpan.",
            ["Cli.Doc.Profile.Title"] = "Terapkan Profil Tersimpan",
            ["Cli.Doc.List"] = "Tampilkan seluruh nama profil tata letak yang tersimpan (tambahkan --json untuk keluaran format JSON).",
            ["Cli.Doc.List.Title"] = "Daftar Profil Tersimpan",
            ["Cli.Doc.Lang"] = "Ganti bahasa output CLI sementara (`id` atau `en`) tanpa mengubah konfigurasi tersimpan.",
            ["Cli.Doc.Lang.Title"] = "Ganti Bahasa Output CLI",
            ["Cli.Doc.Settings"] = "Buka jendela utama antarmuka DisplayFlip Fluent Settings.",
            ["Cli.Doc.Settings.Title"] = "Buka Jendela Pengaturan",
            ["Cli.Doc.Tray"] = "Mulai DisplayFlip langsung di system tray latar belakang tanpa memunculkan jendela pengaturan.",
            ["Cli.Doc.Tray.Title"] = "Jalankan di Background Tray",
            ["Cli.Cat.Layout"] = "Aksi Tata Letak",
            ["Cli.Cat.Visual"] = "Visual & Diagnostik",
            ["Cli.Cat.Projection"] = "Mode Proyeksi",
            ["Cli.Cat.Profile"] = "Manajemen Profil",
            ["Cli.Cat.Language"] = "Bahasa & Output",
            ["Cli.Cat.App"] = "Aplikasi & UI"
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private static readonly IReadOnlyDictionary<string, string> EnglishMap =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Window & Navigation
            ["App.WindowTitle"] = "DisplayFlip — Multi-Monitor Settings",
            ["App.BrandTitle"] = "DisplayFlip",
            ["Nav.Displays"] = "Displays & Profiles",
            ["Nav.Shortcuts"] = "Keyboard Shortcuts",
            ["Nav.General"] = "General & Icons",
            ["Nav.About"] = "About & CLI",

            // General & Icons Page
            ["General.PageTitle"] = "General & Icons",
            ["General.PageSubtitle"] = "System startup preferences, automatic monitor layout, app language, theme customization, tray icon style, and storage directory.",
            ["General.Section.SystemBehavior"] = "Application Behavior",
            ["General.Startup.Title"] = "Launch automatically on Windows startup",
            ["General.Startup.Desc"] = "Start DisplayFlip silently in the background (--tray) automatically when signing in to Windows.",
            ["General.AutoApply.Title"] = "Auto-apply layout after dragging (Auto-Apply)",
            ["General.AutoApply.Desc"] = "Immediately apply monitor positions right after dragging on the canvas without clicking Apply Changes.",

            ["General.Section.Language"] = "Interface Language",
            ["General.Language.CardTitle"] = "Select Application Language",
            ["General.Language.CardDesc"] = "Applies immediately across settings windows, the 2D canvas, system tray, and overlays without restarting.",
            ["General.Language.FollowWindows"] = "Follow Windows Language",
            ["General.Language.Indonesian"] = "Bahasa Indonesia",
            ["General.Language.English"] = "English",
            ["General.Language.CurrentEffective"] = "Current active language: {0}",
            ["General.Language.Name.id"] = "Indonesian (Bahasa Indonesia)",
            ["General.Language.Name.en"] = "English",
            ["General.Language.InfoTitle"] = "Language Updated",
            ["General.Language.ChangedAuto"] = "Language preference set to follow Windows (active: {0}).",
            ["General.Language.ChangedExplicit"] = "Interface language changed to {0}.",
            ["General.Language.SaveError"] = "Failed to save language preference: {0}",

            ["General.Section.Theme"] = "Application Theme Customization",
            ["General.Theme.Desc"] = "Choose light, dark, or dynamically follow your Windows 11 system color configuration.",
            ["General.Theme.System.Title"] = "Follow System",
            ["General.Theme.System.Subtitle"] = "Automatically adapts to Windows 11 light or dark mode.",
            ["General.Theme.System.Label"] = "Follow System (Auto)",
            ["General.Theme.Light.Title"] = "Light Mode",
            ["General.Theme.Light.Subtitle"] = "Clean, bright interface appearance in any lighting condition.",
            ["General.Theme.Dark.Title"] = "Dark Mode",
            ["General.Theme.Dark.Subtitle"] = "Sleek dark-toned interface appearance that is easy on the eyes.",
            ["General.Theme.UpdatedTitle"] = "Theme Updated",
            ["General.Theme.UpdatedMsg"] = "Interface appearance updated to {0}.",
            ["General.Theme.FailedTitle"] = "Application Theme",
            ["General.Theme.FailedMsg"] = "Failed to fully apply the {0} theme.",

            ["General.Section.Icon"] = "Modern Icon Style (Tray & App)",
            ["General.Icon.Desc"] = "Choose your favorite DisplayFlip icon design. Changes immediately update the Title Bar, Taskbar, and System Tray icons.",
            ["General.Badge.Active"] = "Active",
            ["General.Icon.Fluent.Title"] = "Fluent Glass Dual",
            ["General.Icon.Fluent.Subtitle"] = "Modern layered blue-cyan dual monitors with an elegant gold flip badge.",
            ["General.Icon.Fluent.Badge"] = "Recommended",
            ["General.Icon.Accent.Title"] = "Windows 11 Accent",
            ["General.Icon.Accent.Subtitle"] = "Dynamically matches your Windows 11 system accent color theme.",
            ["General.Icon.Accent.Badge"] = "Dynamic",
            ["General.Icon.Minimal.Title"] = "Minimalist Precision",
            ["General.Icon.Minimal.Subtitle"] = "Crisp high-contrast monochrome style for a clean taskbar notification area.",
            ["General.Icon.Minimal.Badge"] = "Monochrome",
            ["General.Icon.UpdatedTitle"] = "Icon Style Updated",
            ["General.Icon.UpdatedMsg"] = "Icon style updated to '{0}'.",
            ["General.Icon.FailedTitle"] = "Failed to Update Icon",
            ["General.Icon.SaveError"] = "Failed to save icon style: {0}",

            ["General.Section.Storage"] = "Storage Directory",
            ["General.Storage.Title"] = "Profiles & Configuration File Location",
            ["General.Storage.OpenFolder"] = "Open Folder",
            ["General.Storage.OpenedTitle"] = "Folder Opened",
            ["General.Storage.InvalidDir"] = "Configuration storage directory is invalid.",
            ["General.Storage.OpenSuccess"] = "Storage folder opened: {0}",
            ["General.Storage.OpenError"] = "Failed to open storage folder: {0}",
            ["General.LoadErrorTitle"] = "Error Loading Settings",

            ["General.Startup.EnabledMsg"] = "DisplayFlip will run automatically in the background (--tray) at Windows sign-in.",
            ["General.Startup.DisabledMsg"] = "Automatic startup disabled.",
            ["General.Startup.RegistryError"] = "Failed to update Windows startup configuration in the registry.",
            ["General.Startup.UpdateError"] = "Failed to update Windows startup: {0}",
            ["General.Startup.FailedTitle"] = "Failed to Update Startup",
            ["General.Startup.SuccessTitle"] = "Windows Startup Updated",

            ["General.AutoApply.EnabledMsg"] = "Monitor layout will be applied automatically right after finishing a drag on the canvas.",
            ["General.AutoApply.DisabledMsg"] = "Auto-apply disabled. Use the 'Apply Changes' button to apply the layout.",
            ["General.AutoApply.SaveError"] = "Failed to save auto-apply setting: {0}",
            ["General.AutoApply.UpdatedTitle"] = "Automatic Layout",
            ["General.AutoApply.SaveFailedTitle"] = "Failed to Save Settings",
            ["General.FolderErrorTitle"] = "Failed to Open Folder",

            ["General.AppFolder.NotFound"] = "Application installation folder not found.",
            ["General.AppFolder.Opened"] = "Application folder opened: {0}",
            ["General.AppFolder.OpenError"] = "Failed to open application folder: {0}",
            ["General.Url.Invalid"] = "Invalid URL link.",
            ["General.Url.Opening"] = "Opening link: {0}",
            ["General.Url.OpenError"] = "Failed to open link: {0}",

            // Displays & Profiles Page
            ["Displays.PageTitle"] = "Displays & Profiles",
            ["Displays.PageSubtitle"] = "Arrange monitor positions, orientation, resolution, and save multi-display layout profiles.",
            ["Displays.Section.Layout"] = "Display Layout",
            ["Displays.Canvas.CardTitle"] = "2D Multi-Monitor Canvas",
            ["Displays.Canvas.DefaultSubtitle"] = "Click a monitor to configure or drag monitor tiles to arrange coordinates.",
            ["Displays.Canvas.ActiveSubtitle"] = "{0} active displays • Click a monitor to configure or drag monitor tiles to arrange coordinates.",
            ["Displays.Canvas.NoMonitorsSubtitle"] = "0 active displays • No active monitors detected.",
            ["Displays.Toolbar.Identify"] = "Identify Displays",
            ["Displays.Toolbar.Snap"] = "Snap to Edges",
            ["Displays.Toolbar.MonitorNames"] = "Monitor Names",
            ["Displays.Toolbar.Reset"] = "Reset Layout",
            ["Displays.Toolbar.Apply"] = "Apply Changes",

            ["Displays.Section.Selected"] = "Selected Display Configuration",
            ["Displays.Inspector.NoSelection"] = "Select a monitor on the canvas",
            ["Displays.Inspector.NoSelectionSubtitle"] = "Click a numbered monitor tile on the canvas above to configure display properties.",
            ["Displays.Inspector.PrimaryRole"] = "Primary Display",
            ["Displays.Inspector.SecondaryRole"] = "Secondary Display",
            ["Displays.Inspector.DetailsFormat"] = "Resolution {0} × {1}{2} • Position ({3}, {4}) • {5}",
            ["Displays.Inspector.Orientation.Title"] = "Display Orientation",
            ["Displays.Inspector.Orientation.Desc"] = "Choose landscape or portrait screen orientation for the selected monitor.",
            ["Displays.Inspector.Orientation.Landscape"] = "Landscape",
            ["Displays.Inspector.Orientation.Portrait"] = "Portrait",
            ["Displays.Inspector.Orientation.LandscapeFlipped"] = "Landscape (Flipped)",
            ["Displays.Inspector.Orientation.PortraitFlipped"] = "Portrait (Flipped)",
            ["Displays.Inspector.Resolution.Title"] = "Display Resolution",
            ["Displays.Inspector.Resolution.Desc"] = "Choose a pixel resolution supported by the selected monitor.",
            ["Displays.Inspector.SetPrimary"] = "Set as Primary",
            ["Displays.Inspector.SoloDefault"] = "Only This Display (Solo)",
            ["Displays.Inspector.SoloNumbered"] = "Only Display {0}",

            ["Displays.Profiles.Title"] = "Saved Layout Profiles",
            ["Displays.Profiles.SaveNew"] = "+ Save New Profile",
            ["Displays.Profiles.EmptyTitle"] = "No saved profiles yet",
            ["Displays.Profiles.EmptySubtitle"] = "Save the current multi-display arrangement by clicking '+ Save New Profile'.",
            ["Displays.Profiles.CardMeta"] = "{0} Displays • Primary: {1}",
            ["Displays.Profiles.ApplyBtn"] = "Apply",
            ["Displays.Profiles.DeleteBtn"] = "Delete",

            ["Displays.Canvas.EmptyState"] = "No active monitors detected.",
            ["Displays.Canvas.PrimaryBadge"] = "★ Primary",
            ["Displays.Canvas.SnapBadge"] = "Snap ({0},{1})",

            ["Displays.Info.AutoAppliedTitle"] = "Arrangement Auto-Applied",
            ["Displays.Info.AutoAppliedMsg"] = "Monitor positions were applied directly to Windows.",
            ["Displays.Info.ArrangementChangedTitle"] = "Monitor Arrangement Changed",
            ["Displays.Info.ArrangementChangedMsg"] = "Monitor positions updated on the canvas. Click 'Apply Changes' or save as a new profile.",
            ["Displays.Info.ApplyFailedTitle"] = "Failed to Apply Arrangement",
            ["Displays.Info.ResetTitle"] = "Layout Reset",
            ["Displays.Info.ResetMsg"] = "Canvas reverted to the current active monitor arrangement.",
            ["Displays.Info.AppliedTitle"] = "Arrangement Applied",
            ["Displays.Info.AppliedMsg"] = "Monitor arrangement successfully applied to Windows.",
            ["Displays.Info.PrimaryAppliedTitle"] = "Primary Display Applied",
            ["Displays.Info.PrimaryAppliedMsg"] = "Primary display and arrangement coordinates applied to Windows.",
            ["Displays.Info.PrimaryChangedTitle"] = "Primary Display Changed",
            ["Displays.Info.PrimaryChangedMsg"] = "Primary display changed on the canvas. Click 'Apply Changes' to apply to Windows.",
            ["Displays.Info.SoloUpdatedTitle"] = "Solo Display Mode",
            ["Displays.Info.SoloFailedTitle"] = "Solo Mode Failed",
            ["Displays.Info.OrientationChangedTitle"] = "Orientation Changed",
            ["Displays.Info.OrientationChangedMsg"] = "Display orientation changed on the canvas. Click 'Apply Changes' to apply to the system.",
            ["Displays.Info.ResolutionChangedTitle"] = "Resolution Changed",
            ["Displays.Info.ResolutionChangedMsg"] = "Display resolution changed on the canvas. Click 'Apply Changes' to apply to the system.",
            ["Displays.Info.ProfileAppliedPartialTitle"] = "Profile Partially Applied",
            ["Displays.Info.ProfileAppliedTitle"] = "Profile Applied",
            ["Displays.Info.ProfileApplyFailedTitle"] = "Failed to Apply Profile",
            ["Displays.Info.DialogErrorTitle"] = "Dialog Error",
            ["Displays.Info.DeleteDialogErrorMsg"] = "Unable to open the delete profile confirmation dialog.",
            ["Displays.Dialog.Delete.Title"] = "Delete Profile",
            ["Displays.Dialog.Delete.Content"] = "Are you sure you want to delete layout profile '{0}'? This action cannot be undone.",
            ["Displays.Dialog.Delete.PrimaryBtn"] = "Delete",
            ["Displays.Dialog.Delete.CloseBtn"] = "Cancel",
            ["Displays.Info.ProfileDeletedTitle"] = "Profile Deleted",
            ["Displays.Info.ProfileDeletedMsg"] = "Profile '{0}' has been removed from the list.",
            ["Displays.Info.DeleteDialogExceptionTitle"] = "Delete Dialog Error",
            ["Displays.Info.SaveDialogErrorMsg"] = "Unable to open the save new profile dialog.",
            ["Displays.Dialog.Save.Title"] = "Save New Profile",
            ["Displays.Dialog.Save.Prompt"] = "Enter a name for the current monitor layout profile:",
            ["Displays.Dialog.Save.Placeholder"] = "e.g. Dual Monitor Work",
            ["Displays.Dialog.Save.PrimaryBtn"] = "Save",
            ["Displays.Dialog.Save.CloseBtn"] = "Cancel",
            ["Displays.Info.EmptyNameTitle"] = "Empty Profile Name",
            ["Displays.Info.SaveEmptyName"] = "Profile name cannot be empty.",
            ["Displays.Info.NameTooLongTitle"] = "Name Too Long",
            ["Displays.Info.NameTooLongMsg"] = "Profile name must be at most 100 characters.",
            ["Displays.Info.ProfileSavedTitle"] = "Profile Saved",
            ["Displays.Info.ProfileSavedMsg"] = "Layout profile '{0}' saved successfully.",
            ["Displays.Info.ProfileSaveFailedTitle"] = "Failed to Save Profile",
            ["Displays.Info.SaveDialogExceptionTitle"] = "Save Dialog Error",

            ["DisplayHelper.ShortName"] = "Display {0} ({1})",
            ["DisplayHelper.FallbackMonitor"] = "Monitor",

            // Shortcuts Page & Helper
            ["Shortcuts.PageTitle"] = "Keyboard Shortcuts",
            ["Shortcuts.PageSubtitle"] = "Configure global hotkeys for quick multi-display actions and layout profile switching.",
            ["Shortcuts.Section.Global"] = "Global Quick Actions",
            ["Shortcuts.Global.Mirror.Title"] = "Flip / Swap Display Positions",
            ["Shortcuts.Global.Mirror.Desc"] = "Instantly swap horizontal positions between two monitors (default: Ctrl+Alt+F).",
            ["Shortcuts.Global.Primary.Title"] = "Cycle Primary Display",
            ["Shortcuts.Global.Primary.Desc"] = "Rotate the primary monitor sequentially to the next display (default: Ctrl+Alt+P).",
            ["Shortcuts.Global.Identify.Title"] = "Identify Displays",
            ["Shortcuts.Global.Identify.Desc"] = "Show large physical monitor numbers at each screen corner (default: Ctrl+Alt+I).",
            ["Shortcuts.Global.Solo1.Title"] = "Only Display 1 (Solo Display 1)",
            ["Shortcuts.Global.Solo1.Desc"] = "Show only display 1 (internal/primary) and turn off all other displays.",
            ["Shortcuts.Global.Solo2.Title"] = "Only Display 2 (Solo Display 2)",
            ["Shortcuts.Global.Solo2.Desc"] = "Show only display 2 (external) and turn off all other displays.",
            ["Shortcuts.Global.Extend.Title"] = "Extend Multi-Display (Extend Displays)",
            ["Shortcuts.Global.Extend.Desc"] = "Re-enable all monitors in Extend display mode.",
            ["Shortcuts.Conflict.Solo1"] = "Only Display 1",
            ["Shortcuts.Conflict.Solo2"] = "Only Display 2",
            ["Shortcuts.Conflict.Extend"] = "Extend Multi-Display",
            ["Shortcuts.Conflict.Profile"] = "Profile '{0}'",
            ["Shortcuts.Section.Profiles"] = "Saved Profile Shortcuts",
            ["Shortcuts.Section.ProfilesSubtitle"] = "Assign quick key combinations to switch directly to saved monitor layout profiles.",
            ["Shortcuts.Empty.Title"] = "No Saved Profiles Yet",
            ["Shortcuts.Empty.Desc"] = "Save a layout profile in the Displays & Profiles tab first to configure profile shortcuts.",
            ["Shortcuts.Profile.CardTitle"] = "Profile: {0}",
            ["Shortcuts.Profile.CardMeta"] = "{0} displays • Primary: {1}",
            ["Shortcuts.Button.Add"] = "+ Shortcut",
            ["Shortcuts.Button.AddToolTip"] = "Click to configure shortcut key combination",
            ["Shortcuts.Button.ActiveToolTip"] = "Active shortcut: {0}. Click to change or remove.",
            ["Shortcuts.Keycap.Unset"] = "(Not set)",
            ["Shortcuts.Error.PrimaryKeyRequired"] = "Press a primary key (e.g. A-Z, 0-9, F1-F12, etc.)",
            ["Shortcuts.Error.UnsupportedKey"] = "This key is not supported as a global shortcut.",
            ["Shortcuts.Error.ModifierRequired"] = "Global shortcuts require a modifier (Ctrl, Alt, Shift, or Win).",
            ["Shortcuts.Dialog.ErrorTitle"] = "Dialog Error",
            ["Shortcuts.Dialog.OpenError"] = "Unable to open shortcut dialog.",
            ["Shortcuts.Dialog.Title"] = "Configure Keyboard Shortcut",
            ["Shortcuts.Dialog.Instructions"] = "Target: {0}\nPress a key combination on your keyboard (e.g. Ctrl+Alt+F or F9):",
            ["Shortcuts.Dialog.PresetsHeader"] = "Quick Presets:",
            ["Shortcuts.Dialog.ClearPreset"] = "Clear",
            ["Shortcuts.Dialog.SaveBtn"] = "Save Shortcut",
            ["Shortcuts.Dialog.RemoveBtn"] = "Remove Shortcut",
            ["Shortcuts.Dialog.CancelBtn"] = "Cancel",
            ["Shortcuts.Dialog.EmptyRecorder"] = "(Empty — Press key combination...)",
            ["Shortcuts.Dialog.ConflictTitle"] = "Shortcut Already in Use",
            ["Shortcuts.Dialog.ConflictMsg"] = "Shortcut '{0}' is currently used by {1}. If saved, the previous owner's shortcut will be cleared automatically.",
            ["Shortcuts.Info.SaveFailedTitle"] = "Save Failed",
            ["Shortcuts.Info.SaveFailedMsg"] = "Unable to update shortcut for '{0}'.",
            ["Shortcuts.Info.ClearedMsg"] = "Shortcut for '{0}' cleared.",
            ["Shortcuts.Info.SavedMsg"] = "Shortcut '{0}' saved for '{1}'.",
            ["Shortcuts.Info.FreedSuffix"] = " (Automatically unassigned from {0}).",
            ["Shortcuts.Info.UpdatedTitle"] = "Shortcut Updated",

            // About & CLI Page
            ["About.PageTitle"] = "About & CLI",
            ["About.PageSubtitle"] = "DisplayFlip application details, runtime component status, and command-line interface (CLI) documentation.",
            ["About.Hero.Badge"] = "v2.0 (Windows 11 Fluent Edition)",
            ["About.Hero.Description"] = "Modern multi-monitor management utility for Windows 11 built on the native CCD (Connecting and Configuring Displays) API, supporting precise 2D drag-and-drop coordinates, instant layout profiles, global hotkeys, and quiet background tray operation.",
            ["About.Section.Cli"] = "CLI Documentation & Command Reference",
            ["About.Section.CliDesc"] = "All command-line options that can be run from Windows Terminal, PowerShell, Command Prompt, or automation scripts. Click Copy to copy a command to your clipboard.",
            ["About.Button.Copy"] = "Copy",
            ["About.Button.Copied"] = "Copied!",
            ["About.Section.Links"] = "Quick Links & Resources",
            ["About.Links.Docs.Title"] = "DisplayFlip Project Website",
            ["About.Links.Docs.Subtitle"] = "displayflip.snipgeek.com — Official page, user guides, & latest releases.",
            ["About.Links.Docs.Btn"] = "Open Website",
            ["About.Links.GitHub.Title"] = "GitHub Repository",
            ["About.Links.GitHub.Subtitle"] = "github.com/ieproject-app/DisplayFlip — Source code, issues, & open contributions.",
            ["About.Links.GitHub.Btn"] = "Open on GitHub",
            ["About.Links.AppFolder.Title"] = "Application Folder",
            ["About.Links.AppFolder.Subtitle"] = "Direct access to the DisplayFlip executable and binary directory.",
            ["About.Links.AppFolder.Btn"] = "Open Folder",
            ["About.Links.ConfigFolder.Title"] = "Configuration File",
            ["About.Links.ConfigFolder.Subtitle"] = "Open the JSON profile storage directory in user AppData.",
            ["About.Links.ConfigFolder.Btn"] = "Open Profile Folder",
            ["About.Info.CopiedTitle"] = "Command Copied to Clipboard",
            ["About.Info.CopiedMsg"] = "Command '{0}' has been copied and is ready to run.",
            ["About.Info.CopyFailedTitle"] = "Copy Failed",
            ["About.Info.CopyFailedMsg"] = "Windows clipboard access is currently locked by another application.",
            ["About.Info.OpeningBrowserTitle"] = "Opening Browser",
            ["About.Info.OpeningWebsiteMsg"] = "Opening DisplayFlip project website in browser: {0}",
            ["About.Info.WebsiteLinkWarnTitle"] = "Project Website Link",
            ["About.Info.OpeningGitHubMsg"] = "Opening DisplayFlip GitHub repository in browser: {0}",
            ["About.Info.GitHubLinkWarnTitle"] = "Documentation Link",
            ["About.Info.AppFolderOpenedTitle"] = "Application Folder Opened",
            ["About.Info.ConfigFolderOpenedTitle"] = "Profile Folder Opened",

            // Identify Overlay
            ["Overlay.WindowTitle"] = "DisplayFlip — Identify Displays",
            ["Overlay.PrimarySuffix"] = "{0} • Primary Display",
            ["Overlay.DefaultSubtitle"] = "Display 1 • Primary Display",

            // System Tray
            ["Tray.Tooltip"] = "DisplayFlip — Multi-Monitor Manager",
            ["Tray.Menu.Swap"] = "Swap Left-Right (Quick Swap)",
            ["Tray.Menu.Identify"] = "Identify Displays",
            ["Tray.Menu.MultiDisplay"] = "Multi-Display",
            ["Tray.Menu.Solo1"] = "Only Display 1",
            ["Tray.Menu.Solo2"] = "Only Display 2",
            ["Tray.Menu.Extend"] = "Extend Displays",
            ["Tray.Menu.Profiles"] = "Display Profiles",
            ["Tray.Menu.NoProfiles"] = "(No saved profiles yet)",
            ["Tray.Menu.OpenSettings"] = "Open Settings",
            ["Tray.Menu.Exit"] = "Exit",

            // DisplayActionService
            ["Actions.Swap.NeedTwoMonitors"] = "At least 2 active monitors are required to swap positions.",
            ["Actions.Swap.Success"] = "Monitor arrangement swapped (left-right).",
            ["Actions.Swap.Failed"] = "Failed to swap arrangement: {0}",
            ["Actions.Swap.BuildFailed"] = "Failed to build mirrored monitor topology.",
            ["Actions.Swap.ValidationFailed"] = "Topology validation failed: {0}",
            ["Actions.Swap.ApplyFailed"] = "Failed to apply arrangement: {0}",
            ["Actions.CyclePrimary.NeedTwoMonitors"] = "At least 2 active monitors are required to cycle the primary display.",
            ["Actions.CyclePrimary.Success"] = "{0} is now the primary display.",
            ["Actions.CyclePrimary.Failed"] = "Failed to set primary display: {0}",
            ["Actions.CyclePrimary.BuildFailed"] = "Failed to build display topology.",
            ["Actions.CyclePrimary.ValidationFailed"] = "Validation failed: {0}",
            ["Actions.CyclePrimary.ApplyFailed"] = "Failed to apply: {0}",
            ["Actions.Profile.EmptyName"] = "Profile name cannot be empty.",
            ["Actions.Profile.NotFound"] = "Profile '{0}' was not found.",
            ["Actions.Profile.NoneConnected"] = "None of the monitors from profile '{0}' are currently connected.",
            ["Actions.Profile.AppliedPartial"] = "Profile '{0}' partially applied ({1} monitors from the profile are not physically connected).",
            ["Actions.Profile.Applied"] = "Profile '{0}' applied successfully.",

            // ProjectionDispatcher
            ["Projection.Mode.Internal"] = "Only Display 1",
            ["Projection.Mode.External"] = "Only Display 2",
            ["Projection.Mode.Clone"] = "Duplicate (Clone)",
            ["Projection.Mode.Extend"] = "Extend",
            ["Projection.UnknownMode"] = "Unknown projection mode: '{0}'. Valid options: extend, clone, internal (solo 1), external (solo 2).",
            ["Projection.Applied"] = "Multi-display mode applied: {0}.",
            ["Projection.DispatchedViaSwitch"] = "Mode {0} dispatched via DisplaySwitch ({1}).",
            ["Projection.DisplaySwitchMissing"] = "Failed to switch mode ({0}) and DisplaySwitch.exe was not found.",
            ["Projection.ChangeFailed"] = "Failed to switch projection mode: {0}",
            ["Projection.SoloUnsupportedIndex"] = "Only Display 1 and Display 2 are supported for single-display solo mode.",

            // CLI
            ["Cli.Help.Body"] = """
DisplayFlip — Multi-Monitor Manager CLI
Usage:
  displayflip.exe [command] [options]

Commands:
  --swap, --flip, --mirror    Swap horizontal positions of primary and secondary monitors.
  --identify, -i              Display physical screen number overlays across all monitors.
  --solo <1|2>                Activate Display 1 or Display 2 only (single-display projection).
  --extend, --perluas         Extend the desktop across all connected displays.
  --clone, --duplicate        Duplicate the screen across connected displays.
  --apply <name>, -p <name>   Apply a saved monitor layout configuration profile.
  --list [--json]             List saved layout profiles (optional JSON output).
  --lang <id|en>              Temporarily override CLI output language without modifying config.
  --settings, --config        Open the DisplayFlip settings GUI window.
  --tray, --minimized         Start DisplayFlip silently in the notification area (system tray).
  --help, -h                  Show this help message.
""",
            ["Cli.List.Header"] = "Saved Profiles ({0}):",
            ["Cli.List.Row"] = "  • {0,-24} | Shortcut: {1,-12} | Displays: {2}",
            ["Cli.Identify.Shown"] = "[DisplayFlip] Displaying monitor number overlays.",
            ["Cli.Error.UnknownCommand"] = "[DisplayFlip] Unrecognized CLI command. Use --help to view available commands.",

            ["Cli.Doc.Swap"] = "Instantly swap horizontal positions of the primary and secondary monitors without opening the GUI window.",
            ["Cli.Doc.Swap.Title"] = "Swap Monitor Positions",
            ["Cli.Doc.Identify"] = "Show large physical screen number overlays on every active monitor for a few seconds.",
            ["Cli.Doc.Identify.Title"] = "Identify Monitors",
            ["Cli.Doc.Solo"] = "Activate only Display 1 or Display 2 immediately and turn off all other displays.",
            ["Cli.Doc.Solo.Title"] = "Single-Display Mode (Solo)",
            ["Cli.Doc.Extend"] = "Extend the desktop across all connected monitors (Windows Extend mode).",
            ["Cli.Doc.Extend.Title"] = "Extend Multi-Monitor",
            ["Cli.Doc.Profile"] = "Apply layout coordinates and monitor settings from a saved profile name.",
            ["Cli.Doc.Profile.Title"] = "Apply Saved Profile",
            ["Cli.Doc.List"] = "List all saved layout profile names and active displays (add --json for JSON output).",
            ["Cli.Doc.List.Title"] = "List Saved Profiles",
            ["Cli.Doc.Lang"] = "Temporarily override CLI output language (`id` or `en`) without modifying saved config.",
            ["Cli.Doc.Lang.Title"] = "Override CLI Output Language",
            ["Cli.Doc.Settings"] = "Open the main DisplayFlip Fluent Settings window.",
            ["Cli.Doc.Settings.Title"] = "Open Settings Window",
            ["Cli.Doc.Tray"] = "Start DisplayFlip directly in the background system tray without showing the settings window.",
            ["Cli.Doc.Tray.Title"] = "Start in Background Tray",
            ["Cli.Cat.Layout"] = "Layout Actions",
            ["Cli.Cat.Visual"] = "Visual & Diagnostics",
            ["Cli.Cat.Projection"] = "Projection Modes",
            ["Cli.Cat.Profile"] = "Profile Management",
            ["Cli.Cat.Language"] = "Language & Output",
            ["Cli.Cat.App"] = "Application & UI"
        }.ToFrozenDictionary(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, string> Indonesian => IndonesianMap;

    public static IReadOnlyDictionary<string, string> English => EnglishMap;

    /// <summary>
    /// Normalizes a persisted or user-supplied preference string into "auto", "id", or "en".
    /// </summary>
    public static string NormalizePreference(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return PreferenceAuto;

        var trimmed = raw.Trim().ToLowerInvariant();
        if (trimmed == PreferenceAuto || trimmed == "system")
            return PreferenceAuto;

        if (trimmed == LanguageIndonesian || trimmed.StartsWith("id-", StringComparison.Ordinal) || trimmed == "indonesian")
            return LanguageIndonesian;

        if (trimmed == LanguageEnglish || trimmed.StartsWith("en-", StringComparison.Ordinal) || trimmed == "english")
            return LanguageEnglish;

        return PreferenceAuto;
    }

    /// <summary>
    /// Resolves a preference ("auto", "id", "en") and optional UI culture to an effective language ("id" or "en").
    /// When preference is "auto", Indonesian UI cultures ("id*") resolve to "id" and all other cultures resolve to "en".
    /// </summary>
    public static string ResolveEffectiveLanguage(string? preference, CultureInfo? uiCulture = null)
    {
        var normalized = NormalizePreference(preference);
        if (normalized == LanguageIndonesian)
            return LanguageIndonesian;
        if (normalized == LanguageEnglish)
            return LanguageEnglish;

        var culture = uiCulture ?? CultureInfo.CurrentUICulture;
        if (culture.TwoLetterISOLanguageName.Equals("id", StringComparison.OrdinalIgnoreCase)
            || culture.Name.Equals("id", StringComparison.OrdinalIgnoreCase)
            || culture.Name.StartsWith("id-", StringComparison.OrdinalIgnoreCase))
        {
            return LanguageIndonesian;
        }

        return LanguageEnglish;
    }

    /// <summary>
    /// Resolves an explicit CLI `--lang` override value if valid ("id" / "en"), or null when unknown/empty.
    /// </summary>
    public static string? TryResolveExplicitLanguage(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw.Trim().ToLowerInvariant();
        if (trimmed == LanguageIndonesian || trimmed.StartsWith("id-", StringComparison.Ordinal) || trimmed == "indonesian")
            return LanguageIndonesian;

        if (trimmed == LanguageEnglish || trimmed.StartsWith("en-", StringComparison.Ordinal) || trimmed == "english")
            return LanguageEnglish;

        return null;
    }

    /// <summary>
    /// Retrieves a localized string by key for the specified effective language (defaulting to English fallback if unknown).
    /// </summary>
    public static string Get(string key, string? effectiveLanguage = null)
    {
        var lang = effectiveLanguage == LanguageIndonesian ? LanguageIndonesian : LanguageEnglish;
        var primary = lang == LanguageIndonesian ? IndonesianMap : EnglishMap;
        if (primary.TryGetValue(key, out var value))
            return value;

        var secondary = lang == LanguageIndonesian ? EnglishMap : IndonesianMap;
        return secondary.TryGetValue(key, out var fallback) ? fallback : key;
    }

    /// <summary>
    /// Formats a localized string by key with positional arguments using InvariantCulture.
    /// </summary>
    public static string Format(string key, string? effectiveLanguage, params object?[] args)
    {
        var template = Get(key, effectiveLanguage);
        return args.Length == 0
            ? template
            : string.Format(CultureInfo.InvariantCulture, template, args);
    }
}
