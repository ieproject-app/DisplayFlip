using FluentAssertions;
using Xunit;

namespace DisplayFlip.Tests;

/// <summary>Token tema Fase A: nilai sesuai dokumen, kontras teks >= 4.5.</summary>
public sealed class FluentThemeTests
{
    [Fact]
    public void Palette_Matches_DocumentTokens()
    {
        // Terang (dokumen bagian 6 + jawaban owner 10).
        DisplayFlip.FluentTheme.AccentFallbackLight.ToArgb().Should().Be(
            System.Drawing.Color.FromArgb(0x00, 0x67, 0xB8).ToArgb());
        DisplayFlip.FluentTheme.AccentFallbackDark.ToArgb().Should().Be(
            System.Drawing.Color.FromArgb(0x4C, 0xC2, 0xFF).ToArgb());
        DisplayFlip.FluentTheme.CautionBackgroundLight.ToArgb().Should().Be(
            System.Drawing.Color.FromArgb(0xFF, 0xF4, 0xCE).ToArgb());
        DisplayFlip.FluentTheme.CautionIconLight.ToArgb().Should().Be(
            System.Drawing.Color.FromArgb(0x9D, 0x5D, 0x00).ToArgb());
        DisplayFlip.FluentTheme.CautionBackgroundDark.ToArgb().Should().Be(
            System.Drawing.Color.FromArgb(0x43, 0x35, 0x19).ToArgb());
        DisplayFlip.FluentTheme.CautionIconDark.ToArgb().Should().Be(
            System.Drawing.Color.FromArgb(0xFC, 0xE1, 0x00).ToArgb());
    }

    [Fact]
    public void TextContrast_Meets_45_in_BothThemes()
    {
        var lightText = System.Drawing.Color.FromArgb(0x1B, 0x1B, 0x1B);
        var lightSurface = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF);
        var lightSecondary = System.Drawing.Color.FromArgb(0x6B, 0x6B, 0x6B);
        var darkText = System.Drawing.Color.FromArgb(0xFF, 0xFF, 0xFF);
        var darkSurface = System.Drawing.Color.FromArgb(0x2B, 0x2B, 0x2B);
        var darkSecondary = System.Drawing.Color.FromArgb(0xC7, 0xC7, 0xC7);
        DisplayFlip.FluentTheme.ContrastRatio(lightText, lightSurface).Should().BeGreaterThanOrEqualTo(4.5);
        DisplayFlip.FluentTheme.ContrastRatio(lightSecondary, lightSurface).Should().BeGreaterThanOrEqualTo(4.5);
        DisplayFlip.FluentTheme.ContrastRatio(darkText, darkSurface).Should().BeGreaterThanOrEqualTo(4.5);
        DisplayFlip.FluentTheme.ContrastRatio(darkSecondary, darkSurface).Should().BeGreaterThanOrEqualTo(4.5);
    }

    [Fact]
    public void Metrics_Match_Document()
    {
        DisplayFlip.FluentTheme.FlyoutWidth.Should().Be(240);
        DisplayFlip.FluentTheme.SubmenuWidth.Should().Be(260);
        DisplayFlip.FluentTheme.ItemHeight.Should().Be(32);
        DisplayFlip.FluentTheme.MonitorRowHeight.Should().Be(44);
        DisplayFlip.FluentTheme.RadiusMenu.Should().Be(8);
        DisplayFlip.FluentTheme.RadiusItem.Should().Be(4);
        DisplayFlip.FluentTheme.FontSizeBody.Should().Be(12);
    }

    [Fact]
    public void HotkeyParser_ParsesValidShortcuts_And_RejectsInvalid()
    {
        DisplayFlip.HotkeyParser.TryParse("Ctrl+Alt+F", out uint mods, out uint vk).Should().BeTrue();
        mods.Should().Be(DisplayFlip.HotkeyParser.MOD_CONTROL | DisplayFlip.HotkeyParser.MOD_ALT);
        vk.Should().Be((uint)System.Windows.Forms.Keys.F);

        DisplayFlip.HotkeyParser.TryParse("Ctrl+Alt+1", out uint mods2, out uint vk2).Should().BeTrue();
        mods2.Should().Be(DisplayFlip.HotkeyParser.MOD_CONTROL | DisplayFlip.HotkeyParser.MOD_ALT);
        vk2.Should().Be((uint)System.Windows.Forms.Keys.D1);

        DisplayFlip.HotkeyParser.TryParse("F9", out uint modsF9, out uint vkF9).Should().BeTrue();
        modsF9.Should().Be(0u);
        vkF9.Should().Be((uint)System.Windows.Forms.Keys.F9);

        string numFormatted = DisplayFlip.HotkeyParser.FormatKeyCode(System.Windows.Forms.Keys.NumPad1);
        numFormatted.Should().Be("Num1");
        DisplayFlip.HotkeyParser.TryParse($"Ctrl+Alt+{numFormatted}", out uint modsNum, out uint vkNum).Should().BeTrue();
        modsNum.Should().Be(DisplayFlip.HotkeyParser.MOD_CONTROL | DisplayFlip.HotkeyParser.MOD_ALT);
        vkNum.Should().Be((uint)System.Windows.Forms.Keys.NumPad1);

        DisplayFlip.HotkeyParser.TryParse("", out _, out _).Should().BeFalse();
        DisplayFlip.HotkeyParser.TryParse("A", out _, out _).Should().BeFalse();
        DisplayFlip.HotkeyParser.TryParse("InvalidMod+F9", out _, out _).Should().BeFalse();
        DisplayFlip.HotkeyParser.TryParse("Ctrl+99999", out _, out _).Should().BeFalse();
        DisplayFlip.HotkeyParser.TryParse("Ctrl+A+B", out _, out _).Should().BeFalse();
    }

    [Fact]
    public void ModernIconGenerator_ProducesValidMultiSizeIcoForAllStyles()
    {
        foreach (var style in new[] { "fluent", "accent", "minimal" })
        {
            var bytes = DisplayFlip.FluentIcons.BuildMultiSizeIcoBytes(style);
            bytes.Length.Should().BeGreaterThan(2048);
            bytes[2].Should().Be(1);
            BitConverter.ToUInt16(bytes, 4).Should().Be(8);
        }
    }

    [Fact]
    public void ModernIconGenerator_ProducesLargestResolutionFirst_ForCrispDownscaling()
    {
        var bytes = DisplayFlip.FluentIcons.BuildMultiSizeIcoBytes("fluent");
        // Entry 0 width and height: 0 represents 256px in ICO header
        bytes[6].Should().Be(0);
        bytes[7].Should().Be(0);

        // Update the file in src/DisplayFlip.Wpf/DisplayFlip.ico and src/DisplayFlip/DisplayFlip.ico
        var wpfIcoPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DisplayFlip.Wpf", "DisplayFlip.ico"));
        if (File.Exists(wpfIcoPath))
        {
            File.WriteAllBytes(wpfIcoPath, bytes);
        }

        var winFormsIcoPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DisplayFlip", "DisplayFlip.ico"));
        if (File.Exists(winFormsIcoPath))
        {
            File.WriteAllBytes(winFormsIcoPath, bytes);
        }
    }
}
