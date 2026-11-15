using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using DisplayFlip.Core;
using DisplayFlip.UI.Services;
using DisplayFlip.Windows;

namespace DisplayFlip.UI.Helpers;

/// <summary>
/// Pelaksana perintah Command-Line Interface (CLI) DisplayFlip untuk otomatisasi dan skrip terminal.
/// </summary>
public static class CliRunner
{
    private const int ATTACH_PARENT_PROCESS = -1;

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    /// <summary>
    /// Mengecek apakah ada argumen perintah baris (CLI) yang meminta aksi tanpa UI penuh.
    /// </summary>
    public static bool IsCliAction(string[] args)
    {
        if (args == null || args.Length == 0)
            return false;

        return args.Any(a => a is "--swap" or "--flip" or "--mirror"
            or "--identify" or "-i"
            or "--solo" or "--internal" or "--external"
            or "--extend" or "--perluas"
            or "--clone" or "--duplicate"
            or "--apply" or "--profile" or "-p"
            or "--list" or "--help" or "-h" or "/?");
    }

    /// <summary>
    /// Menjalankan perintah CLI secara sinkron dan mengembalikan exit code.
    /// </summary>
    public static int Run(string[] args)
    {
        if (!Console.IsOutputRedirected || !Console.IsErrorRedirected)
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
        }

        var actions = DisplayActionService.Instance;
        var store = actions.Store;

        try
        {
            // 1. HELP / INFORMASI PERINTAH
            if (args.Contains("--help") || args.Contains("-h") || args.Contains("/?"))
            {
                PrintHelp();
                return 0;
            }

            // 2. LIST PROFIL
            if (args.Contains("--list"))
            {
                var profiles = store.Load().Profiles;
                if (args.Contains("--json"))
                {
                    var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
                    Console.WriteLine(json);
                }
                else
                {
                    Console.WriteLine($"Daftar Profil Tersimpan ({profiles.Count}):");
                    foreach (var p in profiles)
                    {
                        string shortcut = string.IsNullOrEmpty(p.Shortcut) ? "-" : p.Shortcut;
                        Console.WriteLine($"  • {p.Name,-24} | Pintasan: {shortcut,-12} | Monitor: {p.Monitors.Count}");
                    }
                }
                return 0;
            }

            // 3. TUKAR POSISI MONITOR (MIRROR / SWAP)
            if (args.Contains("--swap") || args.Contains("--flip") || args.Contains("--mirror"))
            {
                var task = actions.QuickSwapAsync();
                task.Wait();
                var (ok, msg) = task.Result;
                if (ok)
                {
                    Console.WriteLine($"[DisplayFlip] {msg}");
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine($"[DisplayFlip ERROR] {msg}");
                    return 4;
                }
            }

            // 4. IDENTIFIKASI MONITOR
            if (args.Contains("--identify") || args.Contains("-i"))
            {
                actions.IdentifyMonitors();
                Console.WriteLine("[DisplayFlip] Menampilkan overlay nomor monitor.");
                return 0;
            }

            // 5. SOLO DISPLAY (Hanya Layar 1 / 2)
            if (args.Contains("--solo") || args.Contains("--internal") || args.Contains("--external"))
            {
                int targetDisplay = 1;
                int soloIdx = Array.IndexOf(args, "--solo");
                if (soloIdx >= 0 && soloIdx + 1 < args.Length && int.TryParse(args[soloIdx + 1], out int parsed))
                {
                    targetDisplay = parsed;
                }
                else if (args.Contains("--external"))
                {
                    targetDisplay = 2;
                }

                var (ok, msg) = actions.TriggerSolo(targetDisplay);
                if (ok)
                {
                    Console.WriteLine($"[DisplayFlip] {msg}");
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine($"[DisplayFlip ERROR] {msg}");
                    return 5;
                }
            }

            // 6. BENTANGKAN MONITOR (EXTEND)
            if (args.Contains("--extend") || args.Contains("--perluas"))
            {
                var (ok, msg) = actions.TriggerProjection("extend");
                if (ok)
                {
                    Console.WriteLine($"[DisplayFlip] {msg}");
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine($"[DisplayFlip ERROR] {msg}");
                    return 5;
                }
            }

            // 7. TERAPKAN PROFIL TERSIMPAN
            int applyIdx = Array.FindIndex(args, a => a is "--apply" or "--profile" or "-p");
            if (applyIdx >= 0 && applyIdx + 1 < args.Length)
            {
                string profileName = args[applyIdx + 1];
                var (ok, msg) = actions.ApplyProfile(profileName);
                if (ok)
                {
                    Console.WriteLine($"[DisplayFlip] {msg}");
                    return 0;
                }
                else
                {
                    Console.Error.WriteLine($"[DisplayFlip ERROR] {msg}");
                    return 5;
                }
            }

            Console.Error.WriteLine("[DisplayFlip] Perintah CLI tidak dikenali. Gunakan --help untuk melihat daftar perintah.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[DisplayFlip FATAL] {ex.Message}");
            return 5;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"DisplayFlip — Multi-Monitor Manager CLI
Penggunaan:
  displayflip.exe [perintah] [opsi]

Perintah Utama:
  --swap, --flip, --mirror    Tukar posisi horizontal monitor primer dan sekunder.
  --identify, -i              Tampilkan overlay nomor layar fisik pada seluruh monitor.
  --solo <1|2>                Aktifkan hanya Layar 1 atau Layar 2 (proyeksi tunggal).
  --extend, --perluas         Bentangkan tampilan desktop ke seluruh layar terhubung.
  --apply <nama>, -p <nama>   Terapkan profil konfigurasi tata letak monitor tersimpan.
  --list [--json]             Daftar nama profil tersimpan (opsional format JSON).
  --settings, --config        Buka jendela antarmuka pengaturan DisplayFlip.
  --tray, --minimized         Jalankan DisplayFlip tenang di area notifikasi (system tray).
  --help, -h                  Tampilkan bantuan ini.");
    }
}
