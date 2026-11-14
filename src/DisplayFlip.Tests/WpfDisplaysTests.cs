using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;
using FluentAssertions;
using Xunit;

namespace DisplayFlip.Tests;

public class WpfDisplaysTests
{
    [Theory]
    [InlineData(@"\\.\DISPLAY1|MONITOR\DEL4123", "Dell UltraSharp", 1, "1")]
    [InlineData(@"\\.\DISPLAY2|MONITOR\SAM5555", "Samsung Display", 2, "2")]
    [InlineData("UNKNOWN_KEY", "Layar 3", 1, "3")]
    [InlineData("UNKNOWN_KEY", "Monitor 4", 1, "4")]
    [InlineData("UNKNOWN_KEY", "Generic PnP Monitor", 5, "5")]
    public void DisplayHelper_ExtractDisplayNumber_ShouldExtractCorrectNumber(
        string? stableKey, string? friendlyName, int fallbackIndex, string expectedNumber)
    {
        var result = DisplayHelper.ExtractDisplayNumber(stableKey, friendlyName, fallbackIndex);
        result.Should().Be(expectedNumber);
    }

    [Fact]
    public void DisplayHelper_CleanDeviceName_ShouldStripWin32Prefix()
    {
        DisplayHelper.CleanDeviceName(@"\\.\DISPLAY1").Should().Be("DISPLAY1");
        DisplayHelper.CleanDeviceName(@"Dell 27-inch").Should().Be("Dell 27-inch");
        DisplayHelper.CleanDeviceName("").Should().Be("Monitor");
        DisplayHelper.CleanDeviceName(null).Should().Be("Monitor");
    }

    [Fact]
    public void DisplayHelper_GetShortDisplayName_ShouldFormatProperly()
    {
        var name = DisplayHelper.GetShortDisplayName(@"\\.\DISPLAY2", @"\\.\DISPLAY2|MON", 2);
        name.Should().Be("Layar 2 (DISPLAY2)");
    }

    [Fact]
    public void DisplayHelper_CalculateOverlayBounds_ShouldPositionInBottomLeft()
    {
        // Primary 1080p monitor
        var mon1 = new Position(0, 0, 1920, 1080);
        var (x1, y1, w1, h1) = DisplayHelper.CalculateOverlayBounds(mon1, 220, 130);

        x1.Should().Be(48);
        y1.Should().Be(1080 - 130 - 72); // 878
        w1.Should().Be(220);
        h1.Should().Be(130);

        // Secondary 1440p monitor to the right
        var mon2 = new Position(1920, 0, 2560, 1440);
        var (x2, y2, w2, h2) = DisplayHelper.CalculateOverlayBounds(mon2, 220, 130);

        x2.Should().Be(1920 + 48);
        y2.Should().Be(1440 - 130 - 72); // 1238

        // Negative coordinate monitor to the left
        var monLeft = new Position(-1920, 0, 1920, 1080);
        var (x3, y3, _, _) = DisplayHelper.CalculateOverlayBounds(monLeft, 220, 130);

        x3.Should().Be(-1920 + 48);
        y3.Should().Be(1080 - 130 - 72);
    }

    [Fact]
    public void DisplayHelper_GetCurrentRefreshRate_GracefulFallbackForInvalidDevice()
    {
        int hz = DisplayHelper.GetCurrentRefreshRate("NON_EXISTENT_DEVICE_XYZ");
        hz.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void SnapMonitorPosition_WhenSnapEnabled_SnapsToAdjacentMonitor()
    {
        var mon1 = new DisplayMonitor(
            StableKey: "MON1",
            FriendlyName: "Left",
            Position: new Position(0, 0, 1920, 1080),
            ResolutionWidth: 1920,
            ResolutionHeight: 1080,
            Orientation: Orientation.Landscape,
            IsEnabled: true);

        var mon2 = new DisplayMonitor(
            StableKey: "MON2",
            FriendlyName: "Right",
            Position: new Position(1940, 20, 1920, 1080),
            ResolutionWidth: 1920,
            ResolutionHeight: 1080,
            Orientation: Orientation.Landscape,
            IsEnabled: true);

        var monitors = new List<DisplayMonitor> { mon1, mon2 };

        // Drag MON2 close to MON1 right edge (1920) and top (0) within threshold 60
        var snapped = DisplayTopology.SnapMonitorPosition(monitors, "MON2", 1930, 15, alignSnapThreshold: 60);

        // Should snap X to 1920 and Y to 0
        snapped.X.Should().Be(1920);
        snapped.Y.Should().Be(0);
    }

    [Fact]
    public void NormalizeArrangement_WhenSnapDisabled_DoesNotSnapAlignment()
    {
        var mon1 = new DisplayMonitor(
            StableKey: "MON1",
            FriendlyName: "Left",
            Position: new Position(0, 0, 1920, 1080),
            ResolutionWidth: 1920,
            ResolutionHeight: 1080,
            Orientation: Orientation.Landscape,
            IsEnabled: true);

        var mon2 = new DisplayMonitor(
            StableKey: "MON2",
            FriendlyName: "Right",
            Position: new Position(1940, 20, 1920, 1080),
            ResolutionWidth: 1920,
            ResolutionHeight: 1080,
            Orientation: Orientation.Landscape,
            IsEnabled: true);

        var monitors = new List<DisplayMonitor> { mon1, mon2 };

        // With alignSnapThreshold: 0 (Snap Disabled) - edge attaches, but vertical alignment does NOT snap
        var normalizedNoSnap = DisplayTopology.NormalizeArrangement(monitors, "MON1", alignSnapThreshold: 0);
        normalizedNoSnap.First(m => m.StableKey == "MON2").Position.X.Should().Be(1920);
        normalizedNoSnap.First(m => m.StableKey == "MON2").Position.Y.Should().Be(20);

        // With alignSnapThreshold: 50 (Snap Enabled) - edge attaches AND vertical alignment snaps to 0
        var normalizedSnapped = DisplayTopology.NormalizeArrangement(monitors, "MON1", alignSnapThreshold: 50);
        normalizedSnapped.First(m => m.StableKey == "MON2").Position.X.Should().Be(1920);
        normalizedSnapped.First(m => m.StableKey == "MON2").Position.Y.Should().Be(0);
    }
}
