using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DisplayFlip;

/// <summary>
/// Satu-satunya sumber token visual Fluent (dokumen bagian 6 + jawaban owner).
/// Terang/gelap mengikuti tema Windows; aksen mengikuti sistem dengan fallback.
/// Tidak ada warna/ukuran hardcode di luar file ini.
/// </summary>
public sealed record FluentPalette(
    Color Background,
    Color Surface,
    Color Text,
    Color TextSecondary,
    Color Hover,
    Color Press,
    Color Divider,
    Color Accent,
    Color RadioBorder,
    Color CautionBackground,
    Color CautionIcon,
    Color BadgeAccent,
    Color ShellBackground,
    Color WellBackground,
    Color SubtleFill);

public static class FluentTheme
{
    // Aksen fallback (jawaban owner): terang #0067B8, gelap #4CC2FF.
    public static readonly Color AccentFallbackLight = Color.FromArgb(0x00, 0x67, 0xB8);
    public static readonly Color AccentFallbackDark = Color.FromArgb(0x4C, 0xC2, 0xFF);

    // Caution (jawaban owner 10): terang bg #FFF4CE ikon #9D5D00; gelap bg #433519 ikon #FCE100.
    public static readonly Color CautionBackgroundLight = Color.FromArgb(0xFF, 0xF4, 0xCE);
    public static readonly Color CautionIconLight = Color.FromArgb(0x9D, 0x5D, 0x00);
    public static readonly Color CautionBackgroundDark = Color.FromArgb(0x43, 0x35, 0x19);
    public static readonly Color CautionIconDark = Color.FromArgb(0xFC, 0xE1, 0x00);

    private static readonly FluentPalette Light = new(
        Background: Color.FromArgb(0xF3, 0xF3, 0xF3),
        Surface: Color.FromArgb(0xFF, 0xFF, 0xFF),
        Text: Color.FromArgb(0x1B, 0x1B, 0x1B),
        TextSecondary: Color.FromArgb(0x6B, 0x6B, 0x6B),
        Hover: Color.FromArgb(0x0A, 0x00, 0x00, 0x00),
        Press: Color.FromArgb(0x12, 0x00, 0x00, 0x00),
        Divider: Color.FromArgb(0x17, 0x00, 0x00, 0x00),
        Accent: AccentFallbackLight,
        RadioBorder: Color.FromArgb(0x6B, 0x6B, 0x6B),
        CautionBackground: CautionBackgroundLight,
        CautionIcon: CautionIconLight,
        BadgeAccent: Color.FromArgb(0xF2, 0xB1, 0x34),
        ShellBackground: Color.FromArgb(0xEB, 0xEB, 0xEB),
        WellBackground: Color.FromArgb(0xE6, 0xE6, 0xE6),
        SubtleFill: Color.FromArgb(0xFB, 0xFB, 0xFB));

    private static readonly FluentPalette Dark = new(
        Background: Color.FromArgb(0x20, 0x20, 0x20),
        Surface: Color.FromArgb(0x2B, 0x2B, 0x2B),
        Text: Color.FromArgb(0xFF, 0xFF, 0xFF),
        TextSecondary: Color.FromArgb(0xC7, 0xC7, 0xC7),
        Hover: Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF),
        Press: Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF),
        Divider: Color.FromArgb(0x1A, 0xFF, 0xFF, 0xFF),
        Accent: AccentFallbackDark,
        RadioBorder: Color.FromArgb(0xC7, 0xC7, 0xC7),
        CautionBackground: CautionBackgroundDark,
        CautionIcon: CautionIconDark,
        BadgeAccent: Color.FromArgb(0xF2, 0xB1, 0x34),
        ShellBackground: Color.FromArgb(0x1C, 0x1C, 0x1C),
        WellBackground: Color.FromArgb(0x17, 0x17, 0x17),
        SubtleFill: Color.FromArgb(0x32, 0x32, 0x32));

    // Metrik (effective px pada 100%).
    public const int FlyoutWidth = 240;
    public const int SubmenuWidth = 260;
    public const int DialogWidth = 420;
    public const int MenuPadding = 6;
    public const int ItemHeight = 32;
    public const int MonitorRowHeight = 44;
    public const int IconSize = 16;
    public const int SmallIconSize = 12;
    public const int RadiusMenu = 8;
    public const int RadiusItem = 4;
    public const int FontSizeTitle = 20;
    public const int FontSizeSubtitle = 14;
    public const int FontSizeBody = 12;
    public const int FontSizeCaption = 11;

    public static bool IsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var v = key?.GetValue("AppsUseLightTheme");
            if (v is int i)
                return i == 0;
        }
        catch
        {
            // Default terang bila registry tidak terbaca.
        }
        return false;
    }

    public static Color SystemAccent(bool dark)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            if (key?.GetValue("AccentPalette") is byte[] pal && pal.Length >= 24)
            {
                // Index 2 = Light1, Index 3 = Base Accent, Index 4 = Dark1
                int idx = dark ? 3 : 3;
                var baseCol = Color.FromArgb(pal[idx * 4], pal[idx * 4 + 1], pal[idx * 4 + 2]);
                if (dark && baseCol.GetBrightness() < 0.38f)
                {
                    idx = 2;
                    baseCol = Color.FromArgb(pal[idx * 4], pal[idx * 4 + 1], pal[idx * 4 + 2]);
                }
                else if (!dark && baseCol.GetBrightness() > 0.72f)
                {
                    idx = 4;
                    baseCol = Color.FromArgb(pal[idx * 4], pal[idx * 4 + 1], pal[idx * 4 + 2]);
                }
                if (baseCol.GetBrightness() is > 0.15f and < 0.92f)
                    return baseCol;
            }
        }
        catch
        {
            // Fall through to DWM colorization.
        }
        try
        {
            int hr = DwmGetColorizationColor(out uint c, out _);
            if (hr == 0)
            {
                var col = Color.FromArgb(
                    (int)((c >> 16) & 0xFF),
                    (int)((c >> 8) & 0xFF),
                    (int)(c & 0xFF));
                if (col.GetBrightness() is > 0.25f and < 0.92f)
                    return col;
            }
        }
        catch
        {
            // Fall through ke fallback.
        }
        return dark ? AccentFallbackDark : AccentFallbackLight;
    }

    /// <summary>Palet aktif: ikut tema Windows saat ini.</summary>
    public static FluentPalette Current
    {
        get
        {
            bool dark = IsDarkMode();
            var base_ = dark ? Dark : Light;
            return base_ with { Accent = SystemAccent(dark) };
        }
    }

    private static Font? s_title;
    private static Font? s_subtitle;
    private static Font? s_body;
    private static Font? s_bodySemibold;
    private static Font? s_caption;
    private static Font? s_displayNumber;
    private static Font? s_mono;

    // Font di-cache seumur aplikasi agar paint berulang tidak membocorkan GDI.
    public static Font TitleFont =>
        s_title ??= new Font("Segoe UI Variable Display", FontSizeTitle, FontStyle.Bold, GraphicsUnit.Pixel);

    public static Font SubtitleFont =>
        s_subtitle ??= new Font("Segoe UI Variable Text", FontSizeSubtitle, FontStyle.Regular, GraphicsUnit.Pixel);

    public static Font BodyFont =>
        s_body ??= new Font("Segoe UI Variable Text", FontSizeBody, FontStyle.Regular, GraphicsUnit.Pixel);

    public static Font BodySemiboldFont =>
        s_bodySemibold ??= new Font("Segoe UI Variable Text", FontSizeBody, FontStyle.Bold, GraphicsUnit.Pixel);

    public static Font CaptionFont =>
        s_caption ??= new Font("Segoe UI Variable Text", FontSizeCaption, FontStyle.Regular, GraphicsUnit.Pixel);

    public static Font DisplayNumberFont =>
        s_displayNumber ??= new Font("Segoe UI Variable Display", 26, FontStyle.Bold, GraphicsUnit.Pixel);

    public static Font MonoFont =>
        s_mono ??= new Font("Consolas", 11, FontStyle.Regular, GraphicsUnit.Pixel);

    public static double ContrastRatio(Color a, Color b)
    {
        static double Lum(Color c)
        {
            static double Lin(double v) =>
                v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            return 0.2126 * Lin(c.R / 255.0) + 0.7152 * Lin(c.G / 255.0) + 0.0722 * Lin(c.B / 255.0);
        }
        double l1 = Lum(a), l2 = Lum(b);
        if (l1 < l2)
            (l1, l2) = (l2, l1);
        return (l1 + 0.05) / (l2 + 0.05);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetColorizationColor(out uint pcrColorization, out bool pfOpaqueBlend);
}
