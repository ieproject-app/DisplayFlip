using FluentAssertions;
using Xunit;

namespace DisplayFlip.Tests;

/// <summary>Regression tests for tray menu stability, label encoding, and neutral multi-size logo.</summary>
public sealed class TrayStabilityTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "DisplayFlip.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? string.Empty;
    }

    private static string TraySource()
    {
        var root = FindRepoRoot();
        return File.ReadAllText(Path.Combine(root, "src", "DisplayFlip", "TrayHost.cs"));
    }

    [Fact]
    public void TrayHost_MustNotPollRefreshOnTimer()
    {
        var src = TraySource();
        src.Should().NotContain("System.Windows.Forms.Timer", "polling RefreshMenu every 1.5s while the menu is open caused it to collapse");
        src.Should().NotContain("Interval = 1500");
    }

    [Fact]
    public void TrayHost_MustRefreshOnMenuOpening_AndGuardWhileOpen()
    {
        var src = TraySource();
        src.Should().Contain("_menu.Opening");
        src.Should().Contain("_isMenuOpen");
        src.Should().Contain("DisplaySettingsChanged");
    }

    [Fact]
    public void TrayHost_Labels_MustBePlainAscii_NoEmojiOrMojibake()
    {
        var root = FindRepoRoot();
        // Semua file presentasi Fase A harus ASCII-bersih (glyph ikon memakai escape \u).
        var files = new[]
        {
            Path.Combine(root, "src", "DisplayFlip", "TrayHost.cs"),
            Path.Combine(root, "src", "DisplayFlip", "FluentMenu.cs"),
            Path.Combine(root, "src", "DisplayFlip", "FluentTheme.cs"),
            Path.Combine(root, "src", "DisplayFlip", "FluentIcons.cs"),
            Path.Combine(root, "src", "DisplayFlip", "FluentDialogs.cs"),
            Path.Combine(root, "src", "DisplayFlip", "FluentSettingsForm.cs"),
        };
        foreach (var f in files)
        {
            var src = File.ReadAllText(f);
            src.Should().NotContain("ð", $"{f} bebas mojibake");
            src.Should().NotContain("â", $"{f} bebas mojibake");
            src.All(c => c < 128 || c == '\r' || c == '\n').Should()
                .BeTrue($"{Path.GetFileName(f)} must be ASCII-clean to avoid encoding mojibake");
        }
        var tray = File.ReadAllText(Path.Combine(root, "src", "DisplayFlip", "TrayHost.cs"));
        tray.Should().Contain("\"Tukar kiri-kanan\"");
        tray.Should().Contain("\"Jadikan primary\"");
        tray.Should().Contain("\"Multi-layar\"");
        tray.Should().Contain("\"Hanya Layar 1\"");
        tray.Should().Contain("\"Hanya Layar 2\"");
        tray.Should().Contain("\"Perluas (Extend)\"");
        tray.Should().Contain("\"Duplikat (Clone)\"");
        tray.Should().Contain("\"Simpan layout sekarang\"");
        tray.Should().Contain("\"Terapkan profil\"");
    }

    [Fact]
    public void AppIcon_MustContainMultipleSizes_IncludingTraySizes()
    {
        var root = FindRepoRoot();
        var ico = Path.Combine(root, "src", "DisplayFlip", "DisplayFlip.ico");
        var bytes = File.ReadAllBytes(ico);
        bytes[2].Should().Be(1);
        bytes[3].Should().Be(0);
        var count = BitConverter.ToUInt16(bytes, 4);
        count.Should().BeGreaterThanOrEqualTo(6, "tray needs 16/20/24/32 plus larger sizes, not a single 256px image");
        var sizes = new List<int>();
        for (int i = 0; i < count; i++)
        {
            int off = 6 + 16 * i;
            int w = bytes[off] == 0 ? 256 : bytes[off];
            sizes.Add(w);
        }
        sizes.Should().Contain(new[] { 16, 32, 48, 256 });
    }
}
