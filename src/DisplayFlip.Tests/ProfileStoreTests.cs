using DisplayFlip.Core;
using FluentAssertions;
using Xunit;

namespace DisplayFlip.Tests;

public sealed class ProfileStoreTests
{
    private static (ProfileStore store, string path) CreateTempStore()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        return (new ProfileStore(path), path);
    }

    private static LayoutProfile SampleProfile(string name = "Primary Kanan") => new()
    {
        SchemaVersion = LayoutProfile.SupportedSchemaVersion,
        Name = name,
        PrimaryStableKey = "keyA",
        Monitors = new List<MonitorState>
        {
            new("keyA", "Main", new Position(0, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
            new("keyB", "Ext", new Position(1920, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
        },
        CreatedAt = DateTime.UtcNow.ToString("o"),
        UpdatedAt = DateTime.UtcNow.ToString("o")
    };

    [Fact]
    public void Save_Then_Find_RoundTrips()
    {
        var (store, path) = CreateTempStore();
        try
        {
            store.Save(SampleProfile());

            var found = store.Find("primary kanan");
            found.Should().NotBeNull();
            found!.Name.Should().Be("Primary Kanan");
            found.PrimaryStableKey.Should().Be("keyA");
            found.Monitors.Should().HaveCount(2);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Save_Upsert_Overwrites()
    {
        var (store, path) = CreateTempStore();
        try
        {
            store.Save(SampleProfile("A"));
            var updated = SampleProfile("A") with
            {
                Monitors = SampleProfile().Monitors.Take(1).ToList(),
                PrimaryStableKey = "keyA"
            };
            store.Save(updated);

            store.ListNames().Should().HaveCount(1);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Policy_SplitPresent_DetectsMissing()
    {
        var profile = SampleProfile();
        var connected = new[] { "keyA" };

        var (remaining, missing) = ProfilePolicy.SplitPresent(profile, connected);

        remaining.Should().HaveCount(1);
        missing.Should().ContainSingle().Which.Should().Be("keyB");
        ProfilePolicy.Decide(missing, userOptedContinue: false)
            .Should().Be(ProfilePolicy.MissingAction.Abort);
        ProfilePolicy.Decide(missing, userOptedContinue: true)
            .Should().Be(ProfilePolicy.MissingAction.Continue);
    }

    [Fact]
    public void Validate_RejectsDuplicateKeys()
    {
        var bad = SampleProfile() with
        {
            Monitors = new List<MonitorState>
            {
                new("keyA", "A", new Position(0, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
                new("keyA", "A2", new Position(1920, 0, 1920, 1080), 1920, 1080, Orientation.Landscape, true),
            }
        };
        bad.Validate(out var err).Should().BeFalse();
        err.Should().Contain("Duplicate");
    }

    [Fact]
    public void Save_WithShortcut_And_GlobalSettings_RoundTrips()
    {
        var (store, path) = CreateTempStore();
        try
        {
            var prof = SampleProfile("Mode Kerja") with { Shortcut = "Ctrl+Alt+1" };
            store.Save(prof);
            store.UpdateSettings(cfg => cfg with
            {
                MirrorShortcut = "Ctrl+Shift+M",
                Solo1Shortcut = "Ctrl+Alt+1",
                Solo2Shortcut = "Ctrl+Alt+2",
                ExtendShortcut = "Ctrl+Alt+E",
                AutoApplyDrag = true,
                IconStyle = "accent"
            });

            var found = store.Find("mode kerja");
            found.Should().NotBeNull();
            found!.Shortcut.Should().Be("Ctrl+Alt+1");

            var loaded = store.Load();
            loaded.MirrorShortcut.Should().Be("Ctrl+Shift+M");
            loaded.Solo1Shortcut.Should().Be("Ctrl+Alt+1");
            loaded.Solo2Shortcut.Should().Be("Ctrl+Alt+2");
            loaded.ExtendShortcut.Should().Be("Ctrl+Alt+E");
            loaded.AutoApplyDrag.Should().BeTrue();
            loaded.IconStyle.Should().Be("accent");

            store.Rename("Mode Kerja", "Mode Gaming").Should().BeTrue();
            store.Find("Mode Gaming")!.Shortcut.Should().Be("Ctrl+Alt+1");
            store.Rename("Mode Gaming", "").Should().BeFalse();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
