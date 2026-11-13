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
        // For 2 equal-size monitors, primary-preserving mirror is identity
        // (swap then re-anchor primary at 0). So order stays [keyA, keyB].
        mirrored.Should().SatisfyRespectively(
            a => a.StableKey.Should().Be("keyA"),
            b => b.StableKey.Should().Be("keyB"));
        // Legal check via first DisplayMonitor at 0 and contiguous
        DisplayTopology.IsLegal(mirrored).Should().BeTrue();
    }

    [Fact]
    public void Mirror_ThreeMonitors_ReversesOrderPrimaryStays()
    {
        var mirrored = DisplayTopology.Mirror(ThreeMonitors());
        mirrored[0].StableKey.Should().Be("keyA");
        mirrored[1].StableKey.Should().Be("keyC");
        mirrored[2].StableKey.Should().Be("keyB");
        DisplayTopology.IsLegal(mirrored).Should().BeTrue();
    }

    [Fact]
    public void Mirror_Twice_ReturnsToOriginal()
    {
        var once = DisplayTopology.Mirror(TwoMonitors());
        var twice = DisplayTopology.Mirror(once);
        twice[0].Position.X.Should().Be(0);
        twice[0].StableKey.Should().Be("keyA");
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
}

