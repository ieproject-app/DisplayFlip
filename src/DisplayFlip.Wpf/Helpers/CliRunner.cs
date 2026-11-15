using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using DisplayFlip.Core;
using DisplayFlip.Core.Localization;
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
    /// Resolves effective CLI language from non-mutating `--lang <id|en>` or `--lang=<id|en>` override,
    /// falling back to persisted ProfileStore.Language or current Windows UI culture.
    /// </summary>
    public static string ResolveCliLanguage(string[] args, ProfileStore? store = null, CultureInfo? uiCulture = null)
    {
        if (args != null)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.IsNullOrWhiteSpace(arg))
                    continue;

                if (arg.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
                {
                    var inlineVal = arg.Substring("--lang=".Length);
                    var explicitLang = LocalizationCatalog.TryResolveExplicitLanguage(inlineVal);
                    if (explicitLang != null)
                        return explicitLang;
                }
                else if (arg.Equals("--lang", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    var nextVal = args[i + 1];
                    if (!nextVal.StartsWith("-", StringComparison.Ordinal))
                    {
                        var explicitLang = LocalizationCatalog.TryResolveExplicitLanguage(nextVal);
                        if (explicitLang != null)
                            return explicitLang;
                    }
                }
            }
        }

        string savedPref = LocalizationCatalog.PreferenceAuto;
        try
        {
            savedPref = (store ?? new ProfileStore()).GetLanguagePreference();
        }
        catch
        {
            savedPref = LocalizationCatalog.PreferenceAuto;
        }

        return LocalizationCatalog.ResolveEffectiveLanguage(savedPref, uiCulture);
    }

    /// <summary>
    /// Menjalankan perintah CLI secara sinkron dan mengembalikan exit code.
    /// </summary>
    public static int Run(string[] args)
    {
        if (AttachConsole(ATTACH_PARENT_PROCESS))
        {
            var stdOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(stdOut);
            var stdErr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetError(stdErr);
        }

        return Run(args, DisplayActionService.Instance, Console.Out, Console.Error);
    }

    internal static int Run(
        string[] args,
        DisplayActionService actions,
        TextWriter stdout,
        TextWriter stderr,
        CultureInfo? uiCulture = null)
    {
        var store = actions.Store;
        string lang = ResolveCliLanguage(args, store, uiCulture);

        try
        {
            // 1. HELP / INFORMASI PERINTAH
            if (args.Contains("--help") || args.Contains("-h") || args.Contains("/?"))
            {
                stdout.WriteLine(LocalizationCatalog.Get("Cli.Help.Body", lang));
                return 0;
            }

            // 2. LIST PROFIL
            if (args.Contains("--list"))
            {
                var profiles = store.Load().Profiles;
                if (args.Contains("--json"))
                {
                    var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
                    stdout.WriteLine(json);
                }
                else
                {
                    stdout.WriteLine(LocalizationCatalog.Format("Cli.List.Header", lang, profiles.Count));
                    foreach (var p in profiles)
                    {
                        string shortcut = string.IsNullOrEmpty(p.Shortcut) ? "-" : p.Shortcut;
                        stdout.WriteLine(LocalizationCatalog.Format("Cli.List.Row", lang, p.Name, shortcut, p.Monitors.Count));
                    }
                }
                return 0;
            }

            // 3. TUKAR POSISI MONITOR (MIRROR / SWAP)
            if (args.Contains("--swap") || args.Contains("--flip") || args.Contains("--mirror"))
            {
                var task = actions.QuickSwapAsync(lang);
                task.Wait();
                var (ok, msg) = task.Result;
                if (ok)
                {
                    stdout.WriteLine($"[DisplayFlip] {msg}");
                    return 0;
                }
                else
                {
                    stderr.WriteLine($"[DisplayFlip ERROR] {msg}");
                    return 4;
                }
            }

            // 4. IDENTIFIKASI MONITOR
            if (args.Contains("--identify") || args.Contains("-i"))
            {
                actions.IdentifyMonitors();
                stdout.WriteLine(LocalizationCatalog.Get("Cli.Identify.Shown", lang));
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

                var (ok, msg) = actions.TriggerSolo(targetDisplay, lang);
                if (ok)
                {
                    stdout.WriteLine($"[DisplayFlip] {msg}");
                    return 0;
                }
                else
                {
                    stderr.WriteLine($"[DisplayFlip ERROR] {msg}");
                    return 5;
                }
            }

            // 6. BENTANGKAN / DUPLIKASI MONITOR (EXTEND / CLONE)
            if (args.Contains("--extend") || args.Contains("--perluas"))
            {
                var (ok, msg) = actions.TriggerProjection("extend", lang);
                if (ok)
                {
                    stdout.WriteLine($"[DisplayFlip] {msg}");
                    return 0;
                }
                else
                {
                    stderr.WriteLine($"[DisplayFlip ERROR] {msg}");
                    return 5;
                }
            }

            if (args.Contains("--clone") || args.Contains("--duplicate"))
            {
                var (ok, msg) = actions.TriggerProjection("clone", lang);
                if (ok)
                {
                    stdout.WriteLine($"[DisplayFlip] {msg}");
                    return 0;
                }
                else
                {
                    stderr.WriteLine($"[DisplayFlip ERROR] {msg}");
                    return 5;
                }
            }

            // 7. TERAPKAN PROFIL TERSIMPAN
            int applyIdx = Array.FindIndex(args, a => a is "--apply" or "--profile" or "-p");
            if (applyIdx >= 0 && applyIdx + 1 < args.Length)
            {
                string profileName = args[applyIdx + 1];
                var (ok, msg) = actions.ApplyProfile(profileName, lang);
                if (ok)
                {
                    stdout.WriteLine($"[DisplayFlip] {msg}");
                    return 0;
                }
                else
                {
                    stderr.WriteLine($"[DisplayFlip ERROR] {msg}");
                    return 5;
                }
            }

            stderr.WriteLine(LocalizationCatalog.Get("Cli.Error.UnknownCommand", lang));
            return 1;
        }
        catch (Exception ex)
        {
            stderr.WriteLine($"[DisplayFlip FATAL] {ex.Message}");
            return 5;
        }
    }
}
