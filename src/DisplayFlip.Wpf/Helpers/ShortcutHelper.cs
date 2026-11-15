using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DisplayFlip.Core;
using Wpf.Ui.Controls;
using WpfTextBlock = Wpf.Ui.Controls.TextBlock;
using SystemVerticalAlignment = System.Windows.VerticalAlignment;

namespace DisplayFlip.UI.Helpers;

public sealed record ShortcutConflict(
    string TargetId,
    string TargetName,
    bool IsProfile
);

public sealed record GlobalShortcutDefinition(
    string KeyKind,
    string Title,
    string Description,
    SymbolRegular Icon,
    string CurrentShortcut
);

/// <summary>
/// Logika parsing, normalisasi, resolusi konflik, dan persistensi pintasan keyboard global & profil.
/// </summary>
public static class ShortcutHelper
{
    /// <summary>
    /// Menormalkan representasi string shortcut ke urutan kanonikal: Ctrl + Alt + Shift + Win + Key.
    /// </summary>
    public static string Normalize(string? shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut))
            return string.Empty;

        var tokens = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return string.Empty;

        bool ctrl = false;
        bool alt = false;
        bool shift = false;
        bool win = false;
        string? keyPart = null;

        foreach (var raw in tokens)
        {
            var token = raw.Trim();
            if (token.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || token.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                ctrl = true;
            }
            else if (token.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                alt = true;
            }
            else if (token.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                shift = true;
            }
            else if (token.Equals("Win", StringComparison.OrdinalIgnoreCase) || token.Equals("Windows", StringComparison.OrdinalIgnoreCase))
            {
                win = true;
            }
            else
            {
                keyPart = NormalizeKeyToken(token);
            }
        }

        if (string.IsNullOrEmpty(keyPart))
            return string.Empty;

        var parts = new List<string>(5);
        if (ctrl) parts.Add("Ctrl");
        if (alt) parts.Add("Alt");
        if (shift) parts.Add("Shift");
        if (win) parts.Add("Win");
        parts.Add(keyPart);

        return string.Join("+", parts);
    }

    private static string NormalizeKeyToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        // Angka tunggal 0-9
        if (token.Length == 1 && char.IsDigit(token[0]))
            return token;

        // Huruf tunggal A-Z
        if (token.Length == 1 && char.IsLetter(token[0]))
            return char.ToUpperInvariant(token[0]).ToString();

        // Numpad digit
        if (token.StartsWith("numpad", StringComparison.OrdinalIgnoreCase) && token.Length > 6)
            return "Num" + token.Substring(6);

        if (token.StartsWith("num", StringComparison.OrdinalIgnoreCase) && token.Length > 3)
            return "Num" + token.Substring(3);

        // Tombol fungsi F1 - F24
        if (token.Length >= 2 && (token[0] == 'f' || token[0] == 'F') && int.TryParse(token.Substring(1), out int fNum) && fNum is >= 1 and <= 24)
            return "F" + fNum;

        // Tombol navigasi / arah
        if (token.Equals("left", StringComparison.OrdinalIgnoreCase)) return "Left";
        if (token.Equals("right", StringComparison.OrdinalIgnoreCase)) return "Right";
        if (token.Equals("up", StringComparison.OrdinalIgnoreCase)) return "Up";
        if (token.Equals("down", StringComparison.OrdinalIgnoreCase)) return "Down";
        if (token.Equals("home", StringComparison.OrdinalIgnoreCase)) return "Home";
        if (token.Equals("end", StringComparison.OrdinalIgnoreCase)) return "End";
        if (token.Equals("pageup", StringComparison.OrdinalIgnoreCase) || token.Equals("prior", StringComparison.OrdinalIgnoreCase)) return "PageUp";
        if (token.Equals("pagedown", StringComparison.OrdinalIgnoreCase) || token.Equals("next", StringComparison.OrdinalIgnoreCase)) return "PageDown";
        if (token.Equals("insert", StringComparison.OrdinalIgnoreCase)) return "Insert";
        if (token.Equals("delete", StringComparison.OrdinalIgnoreCase) || token.Equals("del", StringComparison.OrdinalIgnoreCase)) return "Delete";
        if (token.Equals("space", StringComparison.OrdinalIgnoreCase)) return "Space";
        if (token.Equals("tab", StringComparison.OrdinalIgnoreCase)) return "Tab";
        if (token.Equals("enter", StringComparison.OrdinalIgnoreCase) || token.Equals("return", StringComparison.OrdinalIgnoreCase)) return "Enter";
        if (token.Equals("esc", StringComparison.OrdinalIgnoreCase) || token.Equals("escape", StringComparison.OrdinalIgnoreCase)) return "Escape";

        return char.ToUpperInvariant(token[0]) + (token.Length > 1 ? token.Substring(1).ToLowerInvariant() : string.Empty);
    }

    /// <summary>
    /// Memecah shortcut menjadi bagian-bagian badge keycap (contoh: ["Ctrl", "Alt", "F"]).
    /// </summary>
    public static IReadOnlyList<string> SplitToKeycaps(string? shortcut)
    {
        var normalized = Normalize(shortcut);
        if (string.IsNullOrEmpty(normalized))
            return Array.Empty<string>();

        return normalized.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// Mem-parsing input tombol WPF (Key + ModifierKeys) menjadi string shortcut kanonikal.
    /// </summary>
    public static bool TryParseWpfKey(
        Key key,
        ModifierKeys modifiers,
        bool isWinPressed,
        out string formattedShortcut,
        out string? errorMessage)
    {
        formattedShortcut = string.Empty;
        errorMessage = null;

        // Tombol Backspace atau Delete tanpa modifier berarti mengosongkan shortcut
        if ((key == Key.Back || key == Key.Delete) && modifiers == ModifierKeys.None && !isWinPressed)
        {
            formattedShortcut = string.Empty;
            return true;
        }

        // Jika hanya tombol modifier yang ditekan
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.None)
        {
            errorMessage = "Tekan tombol utama (mis. A-Z, 0-9, F1-F12, dll.)";
            return false;
        }

        string? keyName = FormatWpfKey(key);
        if (string.IsNullOrEmpty(keyName))
        {
            errorMessage = "Tombol ini tidak didukung sebagai pintasan global.";
            return false;
        }

        bool ctrl = modifiers.HasFlag(ModifierKeys.Control);
        bool alt = modifiers.HasFlag(ModifierKeys.Alt);
        bool shift = modifiers.HasFlag(ModifierKeys.Shift);
        bool win = modifiers.HasFlag(ModifierKeys.Windows) || isWinPressed;

        bool isFunctionKey = key is >= Key.F1 and <= Key.F24;

        // Pintasan non-fungsi harus memiliki minimal 1 modifier
        if (!ctrl && !alt && !shift && !win && !isFunctionKey)
        {
            errorMessage = "Pintasan global memerlukan modifier (Ctrl, Alt, Shift, atau Win).";
            return false;
        }

        var parts = new List<string>(5);
        if (ctrl) parts.Add("Ctrl");
        if (alt) parts.Add("Alt");
        if (shift) parts.Add("Shift");
        if (win) parts.Add("Win");
        parts.Add(keyName);

        formattedShortcut = string.Join("+", parts);
        return true;
    }

    public static string? FormatWpfKey(Key key)
    {
        if (key is >= Key.A and <= Key.Z)
            return key.ToString();

        if (key is >= Key.D0 and <= Key.D9)
            return ((int)(key - Key.D0)).ToString();

        if (key is >= Key.NumPad0 and <= Key.NumPad9)
            return "Num" + (int)(key - Key.NumPad0);

        if (key is >= Key.F1 and <= Key.F24)
            return key.ToString();

        return key switch
        {
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Insert => "Insert",
            Key.Tab => "Tab",
            Key.Space => "Space",
            Key.Return => "Enter",
            Key.Escape => "Escape",
            _ => null
        };
    }

    /// <summary>
    /// Mendeteksi apakah shortcut sudah digunakan oleh aksi global lain atau profil lain.
    /// </summary>
    public static ShortcutConflict? CheckConflict(ProfileFile config, string targetId, string shortcut)
    {
        string clean = Normalize(shortcut);
        if (string.IsNullOrEmpty(clean))
            return null;

        if (!string.Equals(targetId, "mirror", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Normalize(config.MirrorShortcut), clean, StringComparison.OrdinalIgnoreCase))
        {
            return new ShortcutConflict("mirror", "Balik / Tukar Posisi Layar", false);
        }

        if (!string.Equals(targetId, "primary", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Normalize(config.CyclePrimaryShortcut), clean, StringComparison.OrdinalIgnoreCase))
        {
            return new ShortcutConflict("primary", "Siklus Layar Utama", false);
        }

        if (!string.Equals(targetId, "identify", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Normalize(config.IdentifyShortcut), clean, StringComparison.OrdinalIgnoreCase))
        {
            return new ShortcutConflict("identify", "Identifikasi Layar", false);
        }

        if (!string.Equals(targetId, "solo1", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Normalize(config.Solo1Shortcut), clean, StringComparison.OrdinalIgnoreCase))
        {
            return new ShortcutConflict("solo1", "Hanya Layar 1", false);
        }

        if (!string.Equals(targetId, "solo2", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Normalize(config.Solo2Shortcut), clean, StringComparison.OrdinalIgnoreCase))
        {
            return new ShortcutConflict("solo2", "Hanya Layar 2", false);
        }

        if (!string.Equals(targetId, "extend", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Normalize(config.ExtendShortcut), clean, StringComparison.OrdinalIgnoreCase))
        {
            return new ShortcutConflict("extend", "Perluas Multi-Layar", false);
        }

        foreach (var prof in config.Profiles)
        {
            string profTargetId = "profile:" + prof.Name;
            if (!string.Equals(targetId, profTargetId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Normalize(prof.Shortcut), clean, StringComparison.OrdinalIgnoreCase))
            {
                return new ShortcutConflict(profTargetId, $"Profil '{prof.Name}'", true);
            }
        }

        return null;
    }

    /// <summary>
    /// Menerapkan pintasan untuk aksi global dengan auto-resolusi konflik dan penyimpanan atomic.
    /// </summary>
    public static (bool changed, string? freedFrom) ApplyGlobalShortcut(
        ProfileStore store,
        string keyKind,
        string shortcut)
    {
        string clean = Normalize(shortcut);
        string? freedFrom = null;

        store.UpdateSettings(cfg =>
        {
            if (!string.IsNullOrEmpty(clean))
            {
                if (keyKind != "mirror" && string.Equals(Normalize(cfg.MirrorShortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Balik / Tukar Posisi Layar";
                else if (keyKind != "primary" && string.Equals(Normalize(cfg.CyclePrimaryShortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Siklus Layar Utama";
                else if (keyKind != "identify" && string.Equals(Normalize(cfg.IdentifyShortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Identifikasi Layar";
                else if (keyKind != "solo1" && string.Equals(Normalize(cfg.Solo1Shortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Hanya Layar 1";
                else if (keyKind != "solo2" && string.Equals(Normalize(cfg.Solo2Shortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Hanya Layar 2";
                else if (keyKind != "extend" && string.Equals(Normalize(cfg.ExtendShortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Perluas Multi-Layar";
                else
                {
                    var conflictingProf = cfg.Profiles.FirstOrDefault(p =>
                        string.Equals(Normalize(p.Shortcut), clean, StringComparison.OrdinalIgnoreCase));
                    if (conflictingProf != null)
                        freedFrom = $"Profil '{conflictingProf.Name}'";
                }
            }

            string mirror = keyKind == "mirror" ? clean
                : (string.Equals(Normalize(cfg.MirrorShortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.MirrorShortcut);
            string primary = keyKind == "primary" ? clean
                : (string.Equals(Normalize(cfg.CyclePrimaryShortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.CyclePrimaryShortcut);
            string identify = keyKind == "identify" ? clean
                : (string.Equals(Normalize(cfg.IdentifyShortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.IdentifyShortcut);
            string solo1 = keyKind == "solo1" ? clean
                : (string.Equals(Normalize(cfg.Solo1Shortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.Solo1Shortcut);
            string solo2 = keyKind == "solo2" ? clean
                : (string.Equals(Normalize(cfg.Solo2Shortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.Solo2Shortcut);
            string extend = keyKind == "extend" ? clean
                : (string.Equals(Normalize(cfg.ExtendShortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.ExtendShortcut);

            var profiles = string.IsNullOrEmpty(clean)
                ? cfg.Profiles
                : cfg.Profiles.Select(p =>
                    string.Equals(Normalize(p.Shortcut), clean, StringComparison.OrdinalIgnoreCase)
                        ? p with { Shortcut = string.Empty }
                        : p).ToList();

            return cfg with
            {
                MirrorShortcut = mirror,
                CyclePrimaryShortcut = primary,
                IdentifyShortcut = identify,
                Solo1Shortcut = solo1,
                Solo2Shortcut = solo2,
                ExtendShortcut = extend,
                Profiles = profiles
            };
        });

        return (true, freedFrom);
    }

    /// <summary>
    /// Menerapkan pintasan untuk profil tersimpan dengan auto-resolusi konflik dan penyimpanan atomic.
    /// </summary>
    public static (bool changed, string? freedFrom) ApplyProfileShortcut(
        ProfileStore store,
        string profileName,
        string shortcut)
    {
        var prof = store.Find(profileName);
        if (prof == null)
            return (false, null);

        string clean = Normalize(shortcut);
        string? freedFrom = null;

        if (!string.IsNullOrEmpty(clean))
        {
            store.UpdateSettings(cfg =>
            {
                if (string.Equals(Normalize(cfg.MirrorShortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Balik / Tukar Posisi Layar";
                else if (string.Equals(Normalize(cfg.CyclePrimaryShortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Siklus Layar Utama";
                else if (string.Equals(Normalize(cfg.IdentifyShortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Identifikasi Layar";
                else if (string.Equals(Normalize(cfg.Solo1Shortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Hanya Layar 1";
                else if (string.Equals(Normalize(cfg.Solo2Shortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Hanya Layar 2";
                else if (string.Equals(Normalize(cfg.ExtendShortcut), clean, StringComparison.OrdinalIgnoreCase))
                    freedFrom = "Perluas Multi-Layar";
                else
                {
                    var other = cfg.Profiles.FirstOrDefault(p =>
                        !string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(Normalize(p.Shortcut), clean, StringComparison.OrdinalIgnoreCase));
                    if (other != null)
                        freedFrom = $"Profil '{other.Name}'";
                }

                return cfg with
                {
                    MirrorShortcut = string.Equals(Normalize(cfg.MirrorShortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.MirrorShortcut,
                    CyclePrimaryShortcut = string.Equals(Normalize(cfg.CyclePrimaryShortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.CyclePrimaryShortcut,
                    IdentifyShortcut = string.Equals(Normalize(cfg.IdentifyShortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.IdentifyShortcut,
                    Solo1Shortcut = string.Equals(Normalize(cfg.Solo1Shortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.Solo1Shortcut,
                    Solo2Shortcut = string.Equals(Normalize(cfg.Solo2Shortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.Solo2Shortcut,
                    ExtendShortcut = string.Equals(Normalize(cfg.ExtendShortcut), clean, StringComparison.OrdinalIgnoreCase) ? string.Empty : cfg.ExtendShortcut,
                    Profiles = cfg.Profiles.Select(p =>
                        !string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(Normalize(p.Shortcut), clean, StringComparison.OrdinalIgnoreCase)
                            ? p with { Shortcut = string.Empty }
                            : p).ToList()
                };
            });
        }

        var currentProf = store.Find(profileName) ?? prof;
        store.Save(currentProf with { Shortcut = clean });

        return (true, freedFrom);
    }

    /// <summary>
    /// Menghasilkan elemen visual kumpulan badge keycap khas Windows 11 Fluent Design.
    /// </summary>
    public static UIElement CreateKeycapsBadge(string? shortcut)
    {
        var keycaps = SplitToKeycaps(shortcut);
        if (keycaps.Count == 0)
        {
            var emptyBlock = new WpfTextBlock
            {
                Text = "(Belum diatur)",
                FontStyle = FontStyles.Italic,
                VerticalAlignment = SystemVerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 0)
            };
            emptyBlock.SetResourceReference(WpfTextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
            return emptyBlock;
        }

        var panel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            VerticalAlignment = SystemVerticalAlignment.Center
        };

        for (int i = 0; i < keycaps.Count; i++)
        {
            if (i > 0)
            {
                var sep = new WpfTextBlock
                {
                    Text = "+",
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(4, 0, 4, 0),
                    VerticalAlignment = SystemVerticalAlignment.Center
                };
                sep.SetResourceReference(WpfTextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
                panel.Children.Add(sep);
            }

            var border = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 4),
                VerticalAlignment = SystemVerticalAlignment.Center,
                MinWidth = 28
            };
            border.SetResourceReference(Border.BackgroundProperty, "ControlFillColorSecondaryBrush");
            border.SetResourceReference(Border.BorderBrushProperty, "CardStrokeColorDefaultBrush");

            var tb = new WpfTextBlock
            {
                Text = keycaps[i],
                FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI, Consolas"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center
            };
            tb.SetResourceReference(WpfTextBlock.ForegroundProperty, "TextFillColorPrimaryBrush");

            border.Child = tb;
            panel.Children.Add(border);
        }

        return panel;
    }

    /// <summary>
    /// Definisi daftar pintasan global lengkap untuk UI.
    /// </summary>
    public static IReadOnlyList<GlobalShortcutDefinition> GetGlobalShortcutDefinitions(ProfileFile config) =>
    [
        new(
            "mirror",
            "Balik / Tukar Posisi Layar",
            "Tukar posisi horizontal antar dua monitor secara instan (default: Ctrl+Alt+F).",
            SymbolRegular.ArrowSwap24,
            config.MirrorShortcut
        ),
        new(
            "primary",
            "Siklus Layar Utama",
            "Pindahkan monitor utama secara bergiliran ke layar berikutnya (default: Ctrl+Alt+P).",
            SymbolRegular.ArrowRepeatAll24,
            config.CyclePrimaryShortcut
        ),
        new(
            "identify",
            "Identifikasi Layar",
            "Tampilkan nomor monitor fisik besar di setiap sudut layar (default: Ctrl+Alt+I).",
            SymbolRegular.Eye24,
            config.IdentifyShortcut
        ),
        new(
            "solo1",
            "Hanya Layar 1 (Solo Display 1)",
            "Hanya menampilkan layar monitor 1 (internal/utama) dan mematikan layar lainnya.",
            SymbolRegular.Desktop24,
            config.Solo1Shortcut
        ),
        new(
            "solo2",
            "Hanya Layar 2 (Solo Display 2)",
            "Hanya menampilkan layar monitor 2 (eksternal) dan mematikan layar lainnya.",
            SymbolRegular.Desktop24,
            config.Solo2Shortcut
        ),
        new(
            "extend",
            "Perluas Multi-Layar (Extend Displays)",
            "Mengaktifkan kembali seluruh monitor dalam mode bentangkan / perluas (Extend).",
            SymbolRegular.FullScreenMaximize24,
            config.ExtendShortcut
        )
    ];
}
