using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using DisplayFlip.Core;
using DisplayFlip.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DisplayFlip.UI.Helpers;

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public sealed record IconStyleOption(
    string Key,
    string Title,
    string Description,
    string BadgeText
);

public sealed record CliCommandItem(
    string Option,
    string Aliases,
    string Title,
    string Syntax,
    string Description,
    string Category
);

public static class GeneralHelper
{
    public const string DefaultIconStyle = "fluent";

    private static AppThemeMode _currentThemeMode = AppThemeMode.System;

    private static readonly IconStyleOption[] IconStyles =
    [
        new(
            "fluent",
            "Fluent Glass Dual",
            "Dua layar berlapis biru-sian modern dengan lencana flip emas elegan.",
            "Rekomendasi"
        ),
        new(
            "accent",
            "Windows 11 Accent",
            "Menyesuaikan warna aksen secara dinamis dengan tema Windows 11 Anda.",
            "Dinamis"
        ),
        new(
            "minimal",
            "Minimalist Precision",
            "Kontras tinggi monokrom tajam untuk area notifikasi taskbar yang bersih.",
            "Monokrom"
        )
    ];

    private static readonly CliCommandItem[] CliCommands =
    [
        new(
            "--swap",
            "--flip, --mirror",
            "Tukar Posisi Monitor",
            "displayflip.exe --swap",
            "Tukar posisi horizontal monitor primer dan sekunder secara instan tanpa membuka jendela antarmuka.",
            "Aksi Tata Letak"
        ),
        new(
            "--identify",
            "-i",
            "Identifikasi Monitor",
            "displayflip.exe --identify",
            "Tampilkan overlay nomor layar fisik berukuran besar di setiap monitor aktif selama beberapa detik.",
            "Visual & Diagnostik"
        ),
        new(
            "--solo <1|2>",
            "--internal, --external",
            "Mode Layar Tunggal (Solo)",
            "displayflip.exe --solo 1",
            "Aktifkan hanya Layar 1 atau Layar 2 secara instan dan nonaktifkan layar lainnya.",
            "Mode Proyeksi"
        ),
        new(
            "--extend",
            "--perluas",
            "Bentangkan Multi-Monitor (Extend)",
            "displayflip.exe --extend",
            "Bentangkan tampilan desktop ke seluruh monitor yang terhubung (mode extend Windows).",
            "Mode Proyeksi"
        ),
        new(
            "--apply <profile>",
            "--profile, -p",
            "Terapkan Profil Tersimpan",
            "displayflip.exe --apply \"Kerja Siang\"",
            "Terapkan koordinat tata letak dan konfigurasi monitor dari nama profil yang telah disimpan.",
            "Manajemen Profil"
        ),
        new(
            "--list",
            "--list --json",
            "Daftar Profil Tersimpan",
            "displayflip.exe --list",
            "Tampilkan seluruh nama profil tata letak yang tersimpan (tambahkan --json untuk keluaran format JSON).",
            "Manajemen Profil"
        ),
        new(
            "--settings",
            "--config",
            "Buka Jendela Pengaturan",
            "displayflip.exe --settings",
            "Buka jendela utama antarmuka DisplayFlip Fluent Settings.",
            "Aplikasi & UI"
        ),
        new(
            "--tray",
            "--minimized",
            "Jalankan di Background Tray",
            "displayflip.exe --tray",
            "Mulai DisplayFlip langsung di system tray latar belakang tanpa memunculkan jendela pengaturan.",
            "Aplikasi & UI"
        )
    ];

    public static IReadOnlyList<IconStyleOption> GetIconStyles() => IconStyles;

    public static string NormalizeIconStyle(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return DefaultIconStyle;

        var clean = key.Trim().ToLowerInvariant();
        return clean switch
        {
            "accent" => "accent",
            "minimal" => "minimal",
            _ => "fluent"
        };
    }

    public static IconStyleOption GetIconStyleOption(string? key)
    {
        var normalized = NormalizeIconStyle(key);
        return IconStyles.First(s => s.Key == normalized);
    }

    public static string GetThemeName(AppThemeMode mode) => mode switch
    {
        AppThemeMode.Light => "Mode Terang (Light)",
        AppThemeMode.Dark => "Mode Gelap (Dark)",
        _ => "Ikuti Sistem (Otomatis)"
    };

    public static AppThemeMode GetCurrentThemeMode()
    {
        return _currentThemeMode;
    }

    public static bool ApplyTheme(AppThemeMode mode)
    {
        try
        {
            _currentThemeMode = mode;
            switch (mode)
            {
                case AppThemeMode.Light:
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.Mica, true);
                    break;
                case AppThemeMode.Dark:
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);
                    break;
                case AppThemeMode.System:
                default:
                    ApplicationThemeManager.ApplySystemTheme(true);
                    break;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsStartupEnabled()
    {
        try
        {
            return StartupRegistry.IsEnabled();
        }
        catch
        {
            return false;
        }
    }

    public static (bool Success, string Message) SetStartupEnabled(bool enabled)
    {
        try
        {
            StartupRegistry.SetEnabled(enabled);
            var isNow = StartupRegistry.IsEnabled();
            if (enabled == isNow)
            {
                return (true, enabled
                    ? "DisplayFlip akan berjalan otomatis di latar belakang (--tray) saat login Windows."
                    : "Startup otomatis dinonaktifkan.");
            }

            return (false, "Gagal mengubah konfigurasi startup Windows di registri.");
        }
        catch (Exception ex)
        {
            return (false, $"Gagal memperbarui startup Windows: {ex.Message}");
        }
    }

    public static bool GetAutoApplyDrag(ProfileStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        try
        {
            return store.Load().AutoApplyDrag;
        }
        catch
        {
            return false;
        }
    }

    public static (bool Success, string Message) SetAutoApplyDrag(ProfileStore store, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(store);
        try
        {
            store.UpdateSettings(cfg => cfg with { AutoApplyDrag = enabled });
            return (true, enabled
                ? "Tata letak monitor akan langsung diterapkan otomatis setiap selesai menggeser di kanvas."
                : "Auto-apply dinonaktifkan. Gunakan tombol 'Terapkan Perubahan' untuk menerapkan tata letak.");
        }
        catch (Exception ex)
        {
            return (false, $"Gagal menyimpan pengaturan auto-apply: {ex.Message}");
        }
    }

    public static (bool Success, string Message) SetIconStyle(ProfileStore store, string styleKey)
    {
        ArgumentNullException.ThrowIfNull(store);
        try
        {
            var normalized = NormalizeIconStyle(styleKey);
            store.UpdateSettings(cfg => cfg with { IconStyle = normalized });
            var option = GetIconStyleOption(normalized);
            return (true, $"Gaya ikon berhasil diperbarui ke '{option.Title}'.");
        }
        catch (Exception ex)
        {
            return (false, $"Gagal menyimpan pilihan gaya ikon: {ex.Message}");
        }
    }

    public static string GetStorageDirectory(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return string.Empty;

        try
        {
            if (Directory.Exists(filePath))
                return Path.GetFullPath(filePath);

            return Path.GetDirectoryName(filePath) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static (bool Success, string Message) OpenStorageFolder(string? filePath)
    {
        var dir = GetStorageDirectory(filePath);
        if (string.IsNullOrWhiteSpace(dir))
            return (false, "Direktori penyimpanan konfigurasi tidak valid.");

        try
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{dir}\"",
                UseShellExecute = true
            };
            Process.Start(startInfo);
            return (true, $"Folder penyimpanan berhasil dibuka: {dir}");
        }
        catch (Exception ex)
        {
            return (false, $"Gagal membuka folder penyimpanan: {ex.Message}");
        }
    }

    public static (bool Success, string Message) OpenAppFolder()
    {
        try
        {
            var dir = AppContext.BaseDirectory;
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return (false, "Folder instalasi aplikasi tidak ditemukan.");

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{dir}\"",
                UseShellExecute = true
            });
            return (true, $"Folder aplikasi berhasil dibuka: {dir}");
        }
        catch (Exception ex)
        {
            return (false, $"Gagal membuka folder aplikasi: {ex.Message}");
        }
    }

    public static (bool Success, string Message) OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (false, "Tautan URL tidak valid.");

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return (true, $"Membuka tautan: {url}");
        }
        catch (Exception ex)
        {
            return (false, $"Gagal membuka tautan: {ex.Message}");
        }
    }

    public static IReadOnlyList<CliCommandItem> GetCliCommands() => CliCommands;

    public static bool TryCopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Clipboard.SetDataObject(text, true);
                return true;
            }
            catch
            {
                System.Threading.Thread.Sleep(50);
            }
        }

        return false;
    }
}
