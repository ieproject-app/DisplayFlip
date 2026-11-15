using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;
using System.Windows.Media;
using DisplayFlip.Core;
using DisplayFlip.UI.Services;
using FluentAssertions;
using Wpf.Ui.Controls;
using Xunit;

namespace DisplayFlip.Tests;

public sealed class WpfProfileAndIconAuditTests
{
    private static DisplayMonitor MakeMonitor(string key, int x = 0, int y = 0, int w = 1920, int h = 1080) =>
        new(
            StableKey: key,
            FriendlyName: key,
            Position: new Position(x, y, w, h),
            ResolutionWidth: w,
            ResolutionHeight: h,
            Orientation: Orientation.Landscape,
            IsEnabled: true);

    private static ProfileStore CreateTempStoreWithProfile(string tempDir, string profileName, params DisplayMonitor[] monitors)
    {
        string path = Path.Combine(tempDir, "profiles.json");
        var store = new ProfileStore(path);
        var states = monitors.Select(m => new MonitorState(
            StableKey: m.StableKey,
            FriendlyName: m.FriendlyName,
            Position: m.Position,
            ResolutionWidth: m.ResolutionWidth,
            ResolutionHeight: m.ResolutionHeight,
            Orientation: m.Orientation,
            IsEnabled: m.IsEnabled)).ToList();

        store.Save(new LayoutProfile
        {
            SchemaVersion = LayoutProfile.SupportedSchemaVersion,
            Name = profileName,
            PrimaryStableKey = monitors.FirstOrDefault()?.StableKey ?? string.Empty,
            Monitors = states,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o")
        });

        return store;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DisplayFlip.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate DisplayFlip.slnx from test base directory.");
    }

    private static GlyphTypeface LoadWpfUiRegularFont()
    {
        var asm = typeof(SymbolIcon).Assembly;
        using var resStream = asm.GetManifestResourceStream("Wpf.Ui.g.resources")
            ?? throw new InvalidOperationException("Wpf.Ui.g.resources not found.");
        using var reader = new ResourceReader(resStream);
        foreach (DictionaryEntry entry in reader)
        {
            if (entry.Key is string key &&
                key.Equals("resources/fonts/fluentsystemicons-regular.ttf", StringComparison.OrdinalIgnoreCase) &&
                entry.Value is Stream fontStream)
            {
                string tmpFile = Path.Combine(Path.GetTempPath(), $"wpfui_regular_{Guid.NewGuid():N}.ttf");
                using (var fs = File.Create(tmpFile))
                {
                    fontStream.CopyTo(fs);
                }
                return new GlyphTypeface(new Uri(tmpFile));
            }
        }
        throw new InvalidOperationException("fluentsystemicons-regular.ttf not found in Wpf.Ui.g.resources.");
    }

    // =========================================================================
    // 1. SYMBOL AUDIT & TRAY MENU ICON FIX TESTS
    // =========================================================================

    [Fact]
    public void WpfUiFont_AllUsedSymbolsInDisplayFlipWpf_HaveValidGlyphs()
    {
        var repoRoot = FindRepoRoot();
        var wpfDir = Path.Combine(repoRoot, "src", "DisplayFlip.Wpf");
        var glyphTypeface = LoadWpfUiRegularFont();

        var usedSymbols = new HashSet<string>(StringComparer.Ordinal);
        var csRegex = new Regex(@"SymbolRegular\.([A-Za-z0-9_]+)", RegexOptions.Compiled);
        var xamlRegex = new Regex(@"Symbol\s*=\s*""([A-Za-z0-9_]+)""", RegexOptions.Compiled);

        foreach (var file in Directory.EnumerateFiles(wpfDir, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            foreach (Match m in csRegex.Matches(content))
                usedSymbols.Add(m.Groups[1].Value);
        }

        foreach (var file in Directory.EnumerateFiles(wpfDir, "*.xaml", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            foreach (Match m in xamlRegex.Matches(content))
                usedSymbols.Add(m.Groups[1].Value);
        }

        usedSymbols.Should().NotBeEmpty();

        var missingGlyphs = new List<string>();
        foreach (var name in usedSymbols.OrderBy(n => n))
        {
            if (!Enum.TryParse<SymbolRegular>(name, out var symbol))
            {
                missingGlyphs.Add($"{name} (not a valid SymbolRegular enum member)");
                continue;
            }

            if (!glyphTypeface.CharacterToGlyphMap.ContainsKey((int)symbol))
            {
                missingGlyphs.Add($"{name} (0x{(int)symbol:X4} missing in FluentSystemIcons-Regular.ttf)");
            }
        }

        missingGlyphs.Should().BeEmpty("all icons in DisplayFlip.Wpf must exist in the bundled WPF-UI font without glyph fallback");
    }

    [Fact]
    public void TrayIconManager_ExtendMenuItem_UsesValidFullScreenMaximizeIcon()
    {
        var repoRoot = FindRepoRoot();
        var trayManagerPath = Path.Combine(repoRoot, "src", "DisplayFlip.Wpf", "Services", "TrayIconManager.cs");
        var content = File.ReadAllText(trayManagerPath);

        content.Should().NotContain("ArrowExpandAll24");
        content.Should().Contain("SymbolRegular.FullScreenMaximize24");
    }

    // =========================================================================
    // 2. SMART PROFILE APPLY ORCHESTRATION TESTS
    // =========================================================================

    [Fact]
    public void ApplyProfile_WhenInSoloMode_AutoWakesWithExtendAndAppliesFullProfile()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"df_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var mon1 = MakeMonitor("MON1", 0, 0);
            var mon2 = MakeMonitor("MON2", 1920, 0);
            var store = CreateTempStoreWithProfile(tempDir, "Dual Kerja", mon1, mon2);

            bool extended = false;
            int readCallsAfterExtend = 0;
            int dispatchCalls = 0;
            bool tryApplyCalled = false;

            var service = new DisplayActionService(
                adapter: null,
                store: store,
                readActiveMonitors: () =>
                {
                    if (!extended)
                        return new[] { mon1 }; // Solo 1: only MON1 active

                    readCallsAfterExtend++;
                    // Simulate Windows taking 1 poll cycle to activate MON2
                    return readCallsAfterExtend >= 2 ? new[] { mon1, mon2 } : new[] { mon1 };
                },
                dispatchProjection: mode =>
                {
                    dispatchCalls++;
                    mode.Should().Be("extend");
                    extended = true;
                    return (true, "Mode multi-layar diterapkan: Perluas (Extend).");
                },
                tryApplyProfile: (name, continueIfMissing) =>
                {
                    tryApplyCalled = true;
                    name.Should().Be("Dual Kerja");
                    continueIfMissing.Should().BeTrue();
                    return (true, "Applied 'Dual Kerja'.");
                },
                sleep: _ => { });

            var (success, isPartial, message) = service.ApplyProfileDetailed("Dual Kerja");

            success.Should().BeTrue();
            isPartial.Should().BeFalse();
            dispatchCalls.Should().Be(1);
            tryApplyCalled.Should().BeTrue();
            message.Should().Be("Profil 'Dual Kerja' berhasil diterapkan.");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ApplyProfile_WhenEnoughMonitorsAlreadyActive_DoesNotCallExtend()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"df_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var mon1 = MakeMonitor("MON1", 0, 0);
            var mon2 = MakeMonitor("MON2", 1920, 0);
            var store = CreateTempStoreWithProfile(tempDir, "Dual Kerja", mon1, mon2);

            int dispatchCalls = 0;

            var service = new DisplayActionService(
                adapter: null,
                store: store,
                readActiveMonitors: () => new[] { mon1, mon2 },
                dispatchProjection: _ =>
                {
                    dispatchCalls++;
                    return (true, "ok");
                },
                tryApplyProfile: (_, _) => (true, "Applied"),
                sleep: _ => { });

            var (success, isPartial, message) = service.ApplyProfileDetailed("Dual Kerja");

            success.Should().BeTrue();
            isPartial.Should().BeFalse();
            dispatchCalls.Should().Be(0);
            message.Should().Be("Profil 'Dual Kerja' berhasil diterapkan.");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ApplyProfile_WhenSecondMonitorPhysicallyDisconnectedAfterExtend_ReturnsHonestPartialMessage()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"df_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var mon1 = MakeMonitor("MON1", 0, 0);
            var mon2 = MakeMonitor("MON2", 1920, 0);
            var store = CreateTempStoreWithProfile(tempDir, "Dual Kerja", mon1, mon2);

            int dispatchCalls = 0;
            int sleepCalls = 0;

            var service = new DisplayActionService(
                adapter: null,
                store: store,
                readActiveMonitors: () => new[] { mon1 }, // MON2 remains disconnected even after extend
                dispatchProjection: _ =>
                {
                    dispatchCalls++;
                    return (true, "ok");
                },
                tryApplyProfile: (_, continueIfMissing) =>
                {
                    continueIfMissing.Should().BeTrue();
                    return (true, "Applied 'Dual Kerja'.");
                },
                sleep: _ => { sleepCalls++; });

            var (success, isPartial, message) = service.ApplyProfileDetailed("Dual Kerja");

            success.Should().BeTrue();
            isPartial.Should().BeTrue();
            dispatchCalls.Should().Be(1);
            sleepCalls.Should().Be(7); // Max poll attempts reached
            message.Should().Contain("diterapkan sebagian");
            message.Should().Contain("1 monitor");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ApplyProfile_WhenNoProfileMonitorsConnected_FailsWithoutCallingTryApply()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"df_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var mon1 = MakeMonitor("MON1", 0, 0);
            var mon2 = MakeMonitor("MON2", 1920, 0);
            var other = MakeMonitor("MON_OTHER", 0, 0);
            var store = CreateTempStoreWithProfile(tempDir, "Dual Kerja", mon1, mon2);

            bool tryApplyCalled = false;

            var service = new DisplayActionService(
                adapter: null,
                store: store,
                readActiveMonitors: () => new[] { other },
                dispatchProjection: _ => (true, "ok"),
                tryApplyProfile: (_, _) =>
                {
                    tryApplyCalled = true;
                    return (true, "ok");
                },
                sleep: _ => { });

            var (success, isPartial, message) = service.ApplyProfileDetailed("Dual Kerja");

            success.Should().BeFalse();
            isPartial.Should().BeFalse();
            tryApplyCalled.Should().BeFalse();
            message.Should().Contain("Tidak ada monitor");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void ApplyProfile_WhenProfileMissingOrTryApplyFails_ReturnsFailure()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"df_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var mon1 = MakeMonitor("MON1", 0, 0);
            var store = CreateTempStoreWithProfile(tempDir, "Solo", mon1);

            var service = new DisplayActionService(
                adapter: null,
                store: store,
                readActiveMonitors: () => new[] { mon1 },
                dispatchProjection: _ => (true, "ok"),
                tryApplyProfile: (_, _) => (false, "Validation failed"),
                sleep: _ => { });

            var missingResult = service.ApplyProfile("TidakAda");
            missingResult.Success.Should().BeFalse();
            missingResult.Message.Should().Contain("tidak ditemukan");

            var failResult = service.ApplyProfile("Solo");
            failResult.Success.Should().BeFalse();
            failResult.Message.Should().Be("Validation failed");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
