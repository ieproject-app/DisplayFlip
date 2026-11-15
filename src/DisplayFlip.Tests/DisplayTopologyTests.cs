using DisplayFlip.Core;
using FluentAssertions;
using Xunit;
using MonitorDisplay = DisplayFlip.Core.DisplayMonitor;

namespace DisplayFlip.Tests;

public sealed class DisplayTopologyTests
{
    private static List<MonitorDisplay> TwoMonitors() => new()
    {
        new("keyA", "Main", new Position(0, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
        new("keyB", "Ext", new Position(1920, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
    };

    private static List<MonitorDisplay> ThreeMonitors() => new()
    {
        new("keyA", "A", new Position(0, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
        new("keyB", "B", new Position(1920, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
        new("keyC", "C", new Position(3840, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
    };

    [Fact]
    public void Mirror_TwoMonitors_PrimaryStaysSameIdentity()
    {
        var mirrored = DisplayTopology.Mirror(TwoMonitors());

        mirrored.Should().HaveCount(2);
        var newPrimary = mirrored.First(m => m.Position.X == 0);
        newPrimary.StableKey.Should().Be("keyA"); // primary fixed
        // For 2 monitors, primary-preserving mirror moves keyB from right (+1920) to left (-1920).
        mirrored.Should().SatisfyRespectively(
            a => { a.StableKey.Should().Be("keyB"); a.Position.X.Should().Be(-1920); },
            b => { b.StableKey.Should().Be("keyA"); b.Position.X.Should().Be(0); });
        DisplayTopology.IsLegal(mirrored).Should().BeTrue();
    }

    [Fact]
    public void Mirror_ThreeMonitors_ReversesOrderPrimaryStays()
    {
        var mirrored = DisplayTopology.Mirror(ThreeMonitors());
        mirrored[0].StableKey.Should().Be("keyC");
        mirrored[0].Position.X.Should().Be(-3840);
        mirrored[1].StableKey.Should().Be("keyB");
        mirrored[1].Position.X.Should().Be(-1920);
        mirrored[2].StableKey.Should().Be("keyA");
        mirrored[2].Position.X.Should().Be(0);
        DisplayTopology.IsLegal(mirrored).Should().BeTrue();
    }

    [Fact]
    public void Mirror_Twice_ReturnsToOriginal()
    {
        var original = TwoMonitors();
        var once = DisplayTopology.Mirror(original);
        var twice = DisplayTopology.Mirror(once);
        twice[0].StableKey.Should().Be("keyA");
        twice[0].Position.X.Should().Be(0);
        twice[1].StableKey.Should().Be("keyB");
        twice[1].Position.X.Should().Be(1920);
    }

    [Fact]
    public void SetPrimary_MovesTargetToOrigin()
    {
        var result = DisplayTopology.SetPrimary(TwoMonitors(), "keyB");
        result[0].StableKey.Should().Be("keyB");
        result[0].Position.X.Should().Be(0);
        result[0].Position.Y.Should().Be(0);
        DisplayTopology.IsLegal(result).Should().BeTrue();
    }

    [Fact]
    public void SetPrimary_AlreadyPrimary_NoOpLayout()
    {
        var before = TwoMonitors();
        var after = DisplayTopology.SetPrimary(before, "keyA");
        after[0].StableKey.Should().Be("keyA");
        after[0].Position.Should().Be(before[0].Position);
    }

    [Fact]
    public void Legalize_ClosesGaps()
    {
        var gapped = new List<MonitorDisplay>
        {
            new("keyA", "A", new Position(0, 0, 1000, 1000), 1000, 1000, Orientation.Landscape, true),
            new("keyB", "B", new Position(2000, 0, 1000, 1000), 1000, 1000, Orientation.Landscape, true),
        };
        var legal = DisplayTopology.Legalize(gapped);
        legal[1].Position.X.Should().Be(1000);
        DisplayTopology.IsLegal(legal).Should().BeTrue();
    }

    [Fact]
    public void SnapMonitorPosition_SupportsFree2DSides_And_VerticalOffset()
    {
        var monitors = TwoMonitors();

        // Drag keyB to the left of keyA with a custom -240px vertical offset
        var snappedLeft = DisplayTopology.SnapMonitorPosition(monitors, "keyB", -1850, -240, alignSnapThreshold: 64);
        snappedLeft.X.Should().Be(-1920);
        snappedLeft.Y.Should().Be(-240);

        // Drag keyB above keyA
        var snappedTop = DisplayTopology.SnapMonitorPosition(monitors, "keyB", 20, -1020, alignSnapThreshold: 64);
        snappedTop.X.Should().Be(0);
        snappedTop.Y.Should().Be(-1080);

        // Drag keyB below keyA
        var snappedBottom = DisplayTopology.SnapMonitorPosition(monitors, "keyB", -15, 1050, alignSnapThreshold: 64);
        snappedBottom.X.Should().Be(0);
        snappedBottom.Y.Should().Be(1080);
    }

    [Fact]
    public void NormalizeArrangement_PreservesVerticalOffset_And_MovesSelectedPrimaryToOrigin()
    {
        var arranged = new List<MonitorDisplay>
        {
            new("keyA", "DISPLAY2", new Position(500, 300, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
            new("keyB", "DISPLAY1", new Position(2420, 60, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
        };

        var normalized = DisplayTopology.NormalizeArrangement(arranged, "keyA");
        var monA = normalized.First(m => m.StableKey == "keyA");
        var monB = normalized.First(m => m.StableKey == "keyB");

        monA.Position.X.Should().Be(0);
        monA.Position.Y.Should().Be(0);
        monB.Position.X.Should().Be(1920);
        monB.Position.Y.Should().Be(-240);
    }

    [Fact]
    public void AlignMonitorsVertically_AlignsTopCenterBottom_And_HandlesNonZeroPrimaryAndVerticalStack()
    {
        var offsetMonitors = new List<MonitorDisplay>
        {
            new("keyA", "A", new Position(1920, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
            new("keyB", "B", new Position(0, -250, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
        };

        var topAligned = DisplayTopology.AlignMonitorsVertically(offsetMonitors, VerticalAlignment.Top, "keyA");
        topAligned.First(m => m.StableKey == "keyA").Position.Should().Be(new Position(0, 0, 1920, 1080));
        topAligned.First(m => m.StableKey == "keyB").Position.Should().Be(new Position(-1920, 0, 1920, 1080));

        // Vertically stacked monitors at X=0 should pack horizontally at Y=0 when aligned
        var stacked = new List<MonitorDisplay>
        {
            new("keyA", "A", new Position(0, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
            new("keyB", "B", new Position(0, -1080, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
        };
        var unstacked = DisplayTopology.AlignMonitorsVertically(stacked, VerticalAlignment.Top, "keyA");
        unstacked.First(m => m.StableKey == "keyB").Position.Y.Should().Be(0);
        unstacked.First(m => m.StableKey == "keyB").Position.X.Should().Be(1920);
    }

    [Fact]
    public void NormalizeArrangement_PreservesThreeMonitorMixedResolutionLShape()
    {
        var lShape = new List<MonitorDisplay>
        {
            new("mon1", "Primary1080", new Position(0, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
            new("mon2", "Right4K", new Position(1920, 0, 3840, 2160), 3840, 2160, Orientation.Landscape, true),
            new("mon3", "TopOf4K", new Position(1920, -1080, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
        };

        var normalized = DisplayTopology.NormalizeArrangement(lShape, "mon1");
        normalized.First(m => m.StableKey == "mon1").Position.Should().Be(new Position(0, 0, 1920, 1080));
        normalized.First(m => m.StableKey == "mon2").Position.Should().Be(new Position(1920, 0, 3840, 2160));
        normalized.First(m => m.StableKey == "mon3").Position.Should().Be(new Position(1920, -1080, 1920, 1080));
    }

    [Theory]
    [InlineData(0.08, 125)]
    [InlineData(0.20, 50)]
    public void SnapThreshold_FollowsScaleEquivalence(double scale, int expectedThreshold)
    {
        int threshold = (int)Math.Clamp(Math.Round(10.0 / scale), 40, 240);
        threshold.Should().Be(expectedThreshold);
    }

    [Fact]
    public void NormalizeArrangement_WithAlignThreshold_DoesNotJumpSnappedPosition()
    {
        var monitors = TwoMonitors();
        var snapped = DisplayTopology.SnapMonitorPosition(monitors, "keyB", -1880, 20, alignSnapThreshold: 100);
        snapped.X.Should().Be(-1920);
        snapped.Y.Should().Be(0);

        var updated = monitors.Select(m => m.StableKey == "keyB" ? m with { Position = snapped } : m).ToList();
        var normalized = DisplayTopology.NormalizeArrangement(updated, "keyA", alignSnapThreshold: 100);

        var monB = normalized.First(m => m.StableKey == "keyB");
        monB.Position.X.Should().Be(-1920);
        monB.Position.Y.Should().Be(0);
    }
}

