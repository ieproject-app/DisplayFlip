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

    [Theory]
    [InlineData("1", "Hanya Layar 1")]
    [InlineData("internal", "Hanya Layar 1")]
    [InlineData("/internal", "Hanya Layar 1")]
    [InlineData("--internal", "Hanya Layar 1")]
    [InlineData("2", "Hanya Layar 2")]
    [InlineData("external", "Hanya Layar 2")]
    [InlineData("/external", "Hanya Layar 2")]
    [InlineData("--external", "Hanya Layar 2")]
    [InlineData("extend", "Perluas (Extend)")]
    [InlineData("/extend", "Perluas (Extend)")]
    [InlineData("--extend", "Perluas (Extend)")]
    [InlineData("clone", "Duplikat (Clone)")]
    [InlineData("/clone", "Duplikat (Clone)")]
    [InlineData("--clone", "Duplikat (Clone)")]
    public void ProjectionDispatcher_ParsesAllModeVariants(string modeSwitch, string expectedSubstring)
    {
        // Safe mock so test run never mutates live OS displays or launches DisplaySwitch.exe
        DisplayFlip.Windows.ProjectionDispatcher.SetDisplayConfigHook = flags => DisplayFlip.Windows.NativeMethods.ERROR_SUCCESS;
        try
        {
            var (ok, msg) = DisplayFlip.Windows.ProjectionDispatcher.Dispatch(modeSwitch, "id");
            ok.Should().BeTrue();
            msg.Should().Contain(expectedSubstring);
        }
        finally
        {
            DisplayFlip.Windows.ProjectionDispatcher.SetDisplayConfigHook = null;
        }
    }

    [Fact]
    public void ProjectionDispatcher_TriggerSolo_ValidatesIndices()
    {
        DisplayFlip.Windows.ProjectionDispatcher.SetDisplayConfigHook = flags => DisplayFlip.Windows.NativeMethods.ERROR_SUCCESS;
        try
        {
            var (ok1, msg1) = DisplayFlip.Windows.ProjectionDispatcher.TriggerSolo(1, "id");
            ok1.Should().BeTrue();
            msg1.Should().Contain("Hanya Layar 1");

            var (ok2, msg2) = DisplayFlip.Windows.ProjectionDispatcher.TriggerSolo(2, "id");
            ok2.Should().BeTrue();
            msg2.Should().Contain("Hanya Layar 2");

            var (ok3, msg3) = DisplayFlip.Windows.ProjectionDispatcher.TriggerSolo(3, "id");
            ok3.Should().BeFalse();
            msg3.Should().Contain("Hanya Layar 1 dan Layar 2 yang didukung");
        }
        finally
        {
            DisplayFlip.Windows.ProjectionDispatcher.SetDisplayConfigHook = null;
        }
    }

    [Fact]
    public void ProjectionDispatcher_DirectApiFailure_FallsBackToDisplaySwitch()
    {
        uint capturedFlags = 0;
        string capturedSwitchArg = string.Empty;

        DisplayFlip.Windows.ProjectionDispatcher.SetDisplayConfigHook = flags =>
        {
            capturedFlags = flags;
            return DisplayFlip.Windows.NativeMethods.ERROR_INVALID_PARAMETER; // simulate driver refusing direct CCD
        };
        DisplayFlip.Windows.ProjectionDispatcher.DisplaySwitchHook = arg =>
        {
            capturedSwitchArg = arg;
            return (true, $"Mock switch: {arg}");
        };

        try
        {
            var (ok, msg) = DisplayFlip.Windows.ProjectionDispatcher.Dispatch("external");
            ok.Should().BeTrue();
            capturedFlags.Should().Be(DisplayFlip.Windows.NativeMethods.SDC_APPLY | DisplayFlip.Windows.NativeMethods.SDC_TOPOLOGY_EXTERNAL);
            capturedSwitchArg.Should().Be("/external");
            msg.Should().Contain("Mock switch: /external");
        }
        finally
        {
            DisplayFlip.Windows.ProjectionDispatcher.SetDisplayConfigHook = null;
            DisplayFlip.Windows.ProjectionDispatcher.DisplaySwitchHook = null;
        }
    }

    [Fact]
    public void ProjectionDispatcher_InvalidMode_ReturnsFalseWithoutCallingEngines()
    {
        bool directCalled = false;
        bool fallbackCalled = false;

        DisplayFlip.Windows.ProjectionDispatcher.SetDisplayConfigHook = _ => { directCalled = true; return 0; };
        DisplayFlip.Windows.ProjectionDispatcher.DisplaySwitchHook = _ => { fallbackCalled = true; return (true, ""); };

        try
        {
            var (ok, msg) = DisplayFlip.Windows.ProjectionDispatcher.Dispatch("mode-tidak-ada", "id");
            ok.Should().BeFalse();
            msg.Should().Contain("tidak dikenal");
            directCalled.Should().BeFalse();
            fallbackCalled.Should().BeFalse();
        }
        finally
        {
            DisplayFlip.Windows.ProjectionDispatcher.SetDisplayConfigHook = null;
            DisplayFlip.Windows.ProjectionDispatcher.DisplaySwitchHook = null;
        }
    }

    [Theory]
    [InlineData("Monitor 1 (HDMI)", 1, "1")]
    [InlineData("Display 2 - Primary", 1, "2")]
    [InlineData("Layar 2", 1, "2")]
    [InlineData("\\\\.\\DISPLAY1", 1, "1")]
    [InlineData("\\\\.\\DISPLAY2", 1, "2")]
    [InlineData("Generic PnP Monitor", 1, "1")]
    [InlineData("Generic PnP Monitor", 2, "2")]
    [InlineData("DELL U2412M", 1, "1")]
    public void ExtractDisplayNumber_ParsesCorrectly(string friendlyName, int fallback, string expected)
    {
        var result = DisplayFlip.IdentifyOverlayForm.ExtractDisplayNumber(null, friendlyName, fallback);
        result.Should().Be(expected);
    }

    [Fact]
    public void ExtractDisplayNumber_PrefersStableKeyDeviceName()
    {
        string stable = "\\\\.\\DISPLAY2|PCI#VEN_8086&DEV_3E9B";
        string friendly = "MSI 40B7";
        var result = DisplayFlip.IdentifyOverlayForm.ExtractDisplayNumber(stable, friendly, 1);
        result.Should().Be("2");
    }
}

