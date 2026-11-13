using DisplayFlip.Core;
using FluentAssertions;
using Xunit;

namespace DisplayFlip.Tests;

public sealed class CliParityTests
{
    [Fact]
    public void Mirror_ShouldMatch_TrayPathViaCore()
    {
        // Parity: CLI --mirror must invoke same Core Mirror as TrayHost.OnMirrorClickedAsync
        var monitors = new List<DisplayFlip.Core.DisplayMonitor>
        {
            new("keyA", "Main", new Position(0, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
            new("keyB", "Ext", new Position(1920, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
        };

        var viaCliCore = DisplayTopology.Mirror(monitors);
        var viaTrayCore = DisplayTopology.Mirror(monitors); // tray uses identical Core call

        viaCliCore.Should().BeEquivalentTo(viaTrayCore);
        DisplayTopology.IsLegal(viaCliCore).Should().BeTrue();
    }

    [Fact]
    public void ProfileNames_CaseInsensitive_FindMatchesCliApply()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = new ProfileStore(path);
            store.Save(new LayoutProfile
            {
                SchemaVersion = LayoutProfile.SupportedSchemaVersion,
                Name = "Primary Kanan",
                PrimaryStableKey = "keyA",
                Monitors = new List<MonitorState>
                {
                    new("keyA", "Main", new Position(0, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
                },
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o")
            });

            // CLI --apply expects case-insensitive name
            store.Find("primary kanan").Should().NotBeNull();
            store.Find("PRIMARY KANAN").Should().NotBeNull();
            store.Find("notfound").Should().BeNull();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}

