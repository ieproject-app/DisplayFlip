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
        var src = TraySource();
        src.Should().NotContain("ð");
        src.Should().NotContain("â");
        src.Should().NotContain("Tukar kiri-kanan (perlu 2+ DisplayMonitor)", "global rename artefact must not leak to UI");
        // Every character in user-facing source must be ASCII (comments included after cleanup)
        src.All(c => c < 128 || c == '\r' || c == '\n').Should().BeTrue("TrayHost.cs must be ASCII-clean to avoid encoding mojibake");
        src.Should().Contain("\"Tukar kiri-kanan\"");
        src.Should().Contain("\"Jadikan primary\"");
        src.Should().Contain("\"Simpan layout sekarang\"");
        src.Should().Contain("\"Terapkan profil\"");
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
