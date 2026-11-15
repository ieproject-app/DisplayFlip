using System.Windows.Controls;
using System.Windows.Input;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;
using FluentAssertions;
using Xunit;
using CoreOrientation = DisplayFlip.Core.Orientation;
using WpfTextBlock = Wpf.Ui.Controls.TextBlock;

namespace DisplayFlip.Tests;

public class WpfShortcutsTests
{
    private static (ProfileStore store, string path) CreateTempStore()
    {
        var path = Path.Combine(Path.GetTempPath(), $"df_test_shortcuts_{Guid.NewGuid():N}.json");
        return (new ProfileStore(path), path);
    }

    private static LayoutProfile CreateSampleProfile(string name = "Kerja Dual", string shortcut = "") => new()
    {
        SchemaVersion = LayoutProfile.SupportedSchemaVersion,
        Name = name,
        PrimaryStableKey = "MON1",
        Shortcut = shortcut,
        Monitors = new List<MonitorState>
        {
            new("MON1", "Internal", new Position(0, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true),
            new("MON2", "External", new Position(1920, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true)
        },
        CreatedAt = DateTime.UtcNow.ToString("o"),
        UpdatedAt = DateTime.UtcNow.ToString("o")
    };

    #region Normalization Tests

    [Theory]
    [InlineData("Ctrl+Alt+F", "Ctrl+Alt+F")]
    [InlineData("alt+ctrl+f", "Ctrl+Alt+F")]
    [InlineData("Alt+Ctrl+Shift+Win+1", "Ctrl+Alt+Shift+Win+1")]
    [InlineData("win + shift + alt + ctrl + p", "Ctrl+Alt+Shift+Win+P")]
    [InlineData("Control+Alt+F", "Ctrl+Alt+F")]
    [InlineData("Ctrl+Windows+F", "Ctrl+Win+F")]
    [InlineData("ctrl+alt+num1", "Ctrl+Alt+Num1")]
    [InlineData("ctrl+alt+numpad2", "Ctrl+Alt+Num2")]
    [InlineData("ctrl+alt+left", "Ctrl+Alt+Left")]
    [InlineData("ctrl+alt+enter", "Ctrl+Alt+Enter")]
    [InlineData("ctrl+alt+del", "Ctrl+Alt+Delete")]
    [InlineData("ctrl+alt+delete", "Ctrl+Alt+Delete")]
    [InlineData("ctrl+alt+backspace", "Ctrl+Alt+Back")]
    [InlineData("ctrl+alt+back", "Ctrl+Alt+Back")]
    [InlineData("f9", "F9")]
    [InlineData("F12", "F12")]
    [InlineData("  ctrl + alt + f  ", "Ctrl+Alt+F")]
    [InlineData("Ctrl+A+B", "")]
    [InlineData("Ctrl+99999", "")]
    [InlineData("InvalidMod+F9", "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    [InlineData("Ctrl+Alt+", "")]
    public void Normalize_ShouldFormatToCanonicalOrder(string? input, string expected)
    {
        var result = ShortcutHelper.Normalize(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void SplitToKeycaps_WithMultipleModifiers_ReturnsKeycapList()
    {
        var parts = ShortcutHelper.SplitToKeycaps("Ctrl+Alt+F");
        parts.Should().Equal("Ctrl", "Alt", "F");

        var fourParts = ShortcutHelper.SplitToKeycaps("alt+ctrl+shift+win+2");
        fourParts.Should().Equal("Ctrl", "Alt", "Shift", "Win", "2");
    }

    [Fact]
    public void SplitToKeycaps_WithStandaloneKey_ReturnsSingleKeycap()
    {
        var parts = ShortcutHelper.SplitToKeycaps("F11");
        parts.Should().Equal("F11");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SplitToKeycaps_WithEmpty_ReturnsEmptyList(string? input)
    {
        var parts = ShortcutHelper.SplitToKeycaps(input);
        parts.Should().BeEmpty();
    }

    #endregion

    #region WPF Key Parsing Tests

    [Fact]
    public void TryParseWpfKey_WithStandardAlphaAndModifiers_ReturnsCanonicalShortcut()
    {
        bool ok = ShortcutHelper.TryParseWpfKey(
            Key.F,
            ModifierKeys.Control | ModifierKeys.Alt,
            isWinPressed: false,
            out string formatted,
            out string? error);

        ok.Should().BeTrue();
        formatted.Should().Be("Ctrl+Alt+F");
        error.Should().BeNull();
    }

    [Fact]
    public void TryParseWpfKey_WithDigitAndWinModifier_ReturnsCanonicalShortcut()
    {
        bool ok = ShortcutHelper.TryParseWpfKey(
            Key.D1,
            ModifierKeys.Control | ModifierKeys.Shift,
            isWinPressed: true,
            out string formatted,
            out string? error);

        ok.Should().BeTrue();
        formatted.Should().Be("Ctrl+Shift+Win+1");
        error.Should().BeNull();
    }

    [Fact]
    public void TryParseWpfKey_WithNumPadKey_ReturnsCanonicalShortcut()
    {
        bool ok = ShortcutHelper.TryParseWpfKey(
            Key.NumPad2,
            ModifierKeys.Alt,
            isWinPressed: false,
            out string formatted,
            out string? error);

        ok.Should().BeTrue();
        formatted.Should().Be("Alt+Num2");
        error.Should().BeNull();
    }

    [Fact]
    public void TryParseWpfKey_WithArrowKey_ReturnsCanonicalShortcut()
    {
        bool ok = ShortcutHelper.TryParseWpfKey(
            Key.Left,
            ModifierKeys.Control | ModifierKeys.Alt,
            isWinPressed: false,
            out string formatted,
            out string? error);

        ok.Should().BeTrue();
        formatted.Should().Be("Ctrl+Alt+Left");
        error.Should().BeNull();
    }

    [Fact]
    public void TryParseWpfKey_WithDeleteKeyAndModifiers_ReturnsCanonicalShortcut()
    {
        bool ok = ShortcutHelper.TryParseWpfKey(
            Key.Delete,
            ModifierKeys.Control | ModifierKeys.Shift,
            isWinPressed: false,
            out string formatted,
            out string? error);

        ok.Should().BeTrue();
        formatted.Should().Be("Ctrl+Shift+Delete");
        error.Should().BeNull();
    }

    [Fact]
    public void TryParseWpfKey_WithBackKeyAndModifiers_ReturnsCanonicalShortcut()
    {
        bool ok = ShortcutHelper.TryParseWpfKey(
            Key.Back,
            ModifierKeys.Control | ModifierKeys.Alt,
            isWinPressed: false,
            out string formatted,
            out string? error);

        ok.Should().BeTrue();
        formatted.Should().Be("Ctrl+Alt+Back");
        error.Should().BeNull();
    }

    [Fact]
    public void TryParseWpfKey_WithFunctionKeyWithoutModifiers_IsAllowed()
    {
        bool ok = ShortcutHelper.TryParseWpfKey(
            Key.F5,
            ModifierKeys.None,
            isWinPressed: false,
            out string formatted,
            out string? error);

        ok.Should().BeTrue();
        formatted.Should().Be("F5");
        error.Should().BeNull();
    }

    [Fact]
    public void TryParseWpfKey_WithRegularKeyWithoutModifiers_FailsWithRequirementError()
    {
        bool ok = ShortcutHelper.TryParseWpfKey(
            Key.A,
            ModifierKeys.None,
            isWinPressed: false,
            out string formatted,
            out string? error);

        ok.Should().BeFalse();
        formatted.Should().BeEmpty();
        error.Should().Contain("modifier");
    }

    [Theory]
    [InlineData(Key.LeftCtrl)]
    [InlineData(Key.RightAlt)]
    [InlineData(Key.LeftShift)]
    [InlineData(Key.LWin)]
    [InlineData(Key.RWin)]
    public void TryParseWpfKey_WithOnlyModifierKeys_FailsWithPromptForMainKey(Key modifierKey)
    {
        bool ok = ShortcutHelper.TryParseWpfKey(
            modifierKey,
            ModifierKeys.Control,
            isWinPressed: false,
            out string formatted,
            out string? error);

        ok.Should().BeFalse();
        formatted.Should().BeEmpty();
        error.Should().Contain("tombol utama");
    }

    [Theory]
    [InlineData(Key.Back)]
    [InlineData(Key.Delete)]
    public void TryParseWpfKey_WithBackOrDeleteWithoutModifiers_ClearsShortcut(Key clearKey)
    {
        bool ok = ShortcutHelper.TryParseWpfKey(
            clearKey,
            ModifierKeys.None,
            isWinPressed: false,
            out string formatted,
            out string? error);

        ok.Should().BeTrue();
        formatted.Should().BeEmpty();
        error.Should().BeNull();
    }

    #endregion

    #region Conflict Detection Tests

    [Fact]
    public void CheckConflict_WhenGlobalMatchesAnotherGlobal_DetectsConflict()
    {
        var config = new ProfileFile
        {
            MirrorShortcut = "Ctrl+Alt+F",
            CyclePrimaryShortcut = "Ctrl+Alt+P"
        };

        var conflict = ShortcutHelper.CheckConflict(config, "primary", "Ctrl+Alt+F");
        conflict.Should().NotBeNull();
        conflict!.TargetId.Should().Be("mirror");
        conflict.TargetName.Should().Be("Balik / Tukar Posisi Layar");
        conflict.IsProfile.Should().BeFalse();
    }

    [Fact]
    public void CheckConflict_WhenTargetChecksSameKeyItAlreadyOwns_ReturnsNoConflict()
    {
        var config = new ProfileFile
        {
            MirrorShortcut = "Ctrl+Alt+F",
            CyclePrimaryShortcut = "Ctrl+Alt+P"
        };

        var conflict = ShortcutHelper.CheckConflict(config, "mirror", "Ctrl+Alt+F");
        conflict.Should().BeNull();
    }

    [Fact]
    public void CheckConflict_WhenShortcutMatchesProfile_DetectsConflictWithProfile()
    {
        var config = new ProfileFile
        {
            MirrorShortcut = "Ctrl+Alt+F",
            Profiles = new List<LayoutProfile>
            {
                CreateSampleProfile("Mode Kerja", "Ctrl+Alt+1")
            }
        };

        var conflict = ShortcutHelper.CheckConflict(config, "mirror", "Ctrl+Alt+1");
        conflict.Should().NotBeNull();
        conflict!.TargetId.Should().Be("profile:Mode Kerja");
        conflict.TargetName.Should().Be("Profil 'Mode Kerja'");
        conflict.IsProfile.Should().BeTrue();
    }

    [Fact]
    public void CheckConflict_WhenProfileMatchesGlobal_DetectsConflictWithGlobal()
    {
        var config = new ProfileFile
        {
            CyclePrimaryShortcut = "Ctrl+Alt+P",
            Profiles = new List<LayoutProfile>
            {
                CreateSampleProfile("Mode Kerja", "")
            }
        };

        var conflict = ShortcutHelper.CheckConflict(config, "profile:Mode Kerja", "Ctrl+Alt+P");
        conflict.Should().NotBeNull();
        conflict!.TargetId.Should().Be("primary");
        conflict.TargetName.Should().Be("Siklus Layar Utama");
        conflict.IsProfile.Should().BeFalse();
    }

    [Fact]
    public void CheckConflict_WhenShortcutIsUnusedOrEmpty_ReturnsNoConflict()
    {
        var config = new ProfileFile
        {
            MirrorShortcut = "Ctrl+Alt+F"
        };

        ShortcutHelper.CheckConflict(config, "mirror", "Ctrl+Alt+Z").Should().BeNull();
        ShortcutHelper.CheckConflict(config, "mirror", "").Should().BeNull();
        ShortcutHelper.CheckConflict(config, "mirror", null!).Should().BeNull();
    }

    #endregion

    #region Persistence and Conflict Resolution Tests

    [Fact]
    public void ApplyGlobalShortcut_AssignsNewShortcutAndPersistsToStore()
    {
        var (store, path) = CreateTempStore();
        try
        {
            var (changed, freedFrom) = ShortcutHelper.ApplyGlobalShortcut(store, "solo1", "Ctrl+Alt+1");

            changed.Should().BeTrue();
            freedFrom.Should().BeNull();

            var reloaded = store.Load();
            reloaded.Solo1Shortcut.Should().Be("Ctrl+Alt+1");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ApplyGlobalShortcut_WhenConflictWithOtherGlobal_AutomaticallyFreesConflictingOwner()
    {
        var (store, path) = CreateTempStore();
        try
        {
            // Set initial state: Mirror has Ctrl+Alt+F
            store.UpdateSettings(cfg => cfg with { MirrorShortcut = "Ctrl+Alt+F", CyclePrimaryShortcut = "" });

            // Now assign Ctrl+Alt+F to primary
            var (changed, freedFrom) = ShortcutHelper.ApplyGlobalShortcut(store, "primary", "Ctrl+Alt+F");

            changed.Should().BeTrue();
            freedFrom.Should().Be("Balik / Tukar Posisi Layar");

            var reloaded = store.Load();
            reloaded.CyclePrimaryShortcut.Should().Be("Ctrl+Alt+F");
            reloaded.MirrorShortcut.Should().BeEmpty(); // Automatically freed!
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ApplyGlobalShortcut_WhenConflictWithProfile_AutomaticallyFreesProfileShortcut()
    {
        var (store, path) = CreateTempStore();
        try
        {
            var prof = CreateSampleProfile("Kerja Dual", "Ctrl+Alt+W");
            store.Save(prof);

            var (changed, freedFrom) = ShortcutHelper.ApplyGlobalShortcut(store, "extend", "Ctrl+Alt+W");

            changed.Should().BeTrue();
            freedFrom.Should().Be("Profil 'Kerja Dual'");

            var reloaded = store.Load();
            reloaded.ExtendShortcut.Should().Be("Ctrl+Alt+W");

            var reloadedProf = store.Find("Kerja Dual");
            reloadedProf!.Shortcut.Should().BeEmpty(); // Profile shortcut freed!
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ApplyProfileShortcut_AssignsShortcutAndPersistsToStore()
    {
        var (store, path) = CreateTempStore();
        try
        {
            var prof = CreateSampleProfile("Gaming Setup", "");
            store.Save(prof);

            var (changed, freedFrom) = ShortcutHelper.ApplyProfileShortcut(store, "Gaming Setup", "Ctrl+Alt+G");

            changed.Should().BeTrue();
            freedFrom.Should().BeNull();

            var reloaded = store.Find("Gaming Setup");
            reloaded!.Shortcut.Should().Be("Ctrl+Alt+G");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ApplyProfileShortcut_WhenConflictWithGlobal_AutomaticallyFreesGlobalShortcut()
    {
        var (store, path) = CreateTempStore();
        try
        {
            store.UpdateSettings(cfg => cfg with { IdentifyShortcut = "Ctrl+Alt+I" });
            var prof = CreateSampleProfile("Studio Setup", "");
            store.Save(prof);

            var (changed, freedFrom) = ShortcutHelper.ApplyProfileShortcut(store, "Studio Setup", "Ctrl+Alt+I");

            changed.Should().BeTrue();
            freedFrom.Should().Be("Identifikasi Layar");

            var reloaded = store.Load();
            reloaded.IdentifyShortcut.Should().BeEmpty(); // Global shortcut freed!

            var reloadedProf = store.Find("Studio Setup");
            reloadedProf!.Shortcut.Should().Be("Ctrl+Alt+I");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ApplyProfileShortcut_WhenConflictWithAnotherProfile_AutomaticallyFreesOtherProfile()
    {
        var (store, path) = CreateTempStore();
        try
        {
            var profA = CreateSampleProfile("Profil A", "Ctrl+Alt+1");
            var profB = CreateSampleProfile("Profil B", "");
            store.Save(profA);
            store.Save(profB);

            var (changed, freedFrom) = ShortcutHelper.ApplyProfileShortcut(store, "Profil B", "Ctrl+Alt+1");

            changed.Should().BeTrue();
            freedFrom.Should().Be("Profil 'Profil A'");

            var reloadedA = store.Find("Profil A");
            reloadedA!.Shortcut.Should().BeEmpty(); // Profil A freed!

            var reloadedB = store.Find("Profil B");
            reloadedB!.Shortcut.Should().Be("Ctrl+Alt+1");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ApplyProfileShortcut_ClearingShortcut_PersistsEmpty()
    {
        var (store, path) = CreateTempStore();
        try
        {
            var prof = CreateSampleProfile("Profil Reset", "Ctrl+Alt+R");
            store.Save(prof);

            var (changed, freedFrom) = ShortcutHelper.ApplyProfileShortcut(store, "Profil Reset", "");

            changed.Should().BeTrue();
            freedFrom.Should().BeNull();

            var reloaded = store.Find("Profil Reset");
            reloaded!.Shortcut.Should().BeEmpty();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ApplyGlobalShortcut_WithInvalidKeyKind_ReturnsFalseAndLeavesStoreUntouched()
    {
        var (store, path) = CreateTempStore();
        try
        {
            store.UpdateSettings(cfg => cfg with { MirrorShortcut = "Ctrl+Alt+F" });

            var (changed, freedFrom) = ShortcutHelper.ApplyGlobalShortcut(store, "unknown_action", "Ctrl+Alt+F");

            changed.Should().BeFalse();
            freedFrom.Should().BeNull();

            var reloaded = store.Load();
            reloaded.MirrorShortcut.Should().Be("Ctrl+Alt+F"); // Untouched!
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ApplyGlobalShortcut_ClearingOneAction_DoesNotAffectOtherActions()
    {
        var (store, path) = CreateTempStore();
        try
        {
            store.UpdateSettings(cfg => cfg with
            {
                MirrorShortcut = "Ctrl+Alt+F",
                Solo1Shortcut = "Ctrl+Alt+1"
            });

            var (changed, freedFrom) = ShortcutHelper.ApplyGlobalShortcut(store, "mirror", "");

            changed.Should().BeTrue();
            freedFrom.Should().BeNull();

            var reloaded = store.Load();
            reloaded.MirrorShortcut.Should().BeEmpty();
            reloaded.Solo1Shortcut.Should().Be("Ctrl+Alt+1"); // Intact!
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Theory]
    [InlineData("NonExistentProfile")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ApplyProfileShortcut_WithInvalidOrMissingProfile_ReturnsFalse(string? profileName)
    {
        var (store, path) = CreateTempStore();
        try
        {
            var (changed, freedFrom) = ShortcutHelper.ApplyProfileShortcut(store, profileName!, "Ctrl+Alt+1");

            changed.Should().BeFalse();
            freedFrom.Should().BeNull();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    #endregion

    private static void RunInSta(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();
        var thread = new Thread(() =>
        {
            try
            {
                action();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        tcs.Task.GetAwaiter().GetResult();
    }

    #region Visual Keycap Badge Generation Tests

    [Fact]
    public void CreateKeycapsBadge_WithValidShortcut_ReturnsStackPanelWithKeycaps()
    {
        RunInSta(() =>
        {
            var visual = ShortcutHelper.CreateKeycapsBadge("Ctrl+Alt+F");
            visual.Should().BeOfType<StackPanel>();

            var panel = (StackPanel)visual;
            panel.Children.Count.Should().Be(5);
            panel.Children[0].Should().BeOfType<Border>();
            panel.Children[1].Should().BeOfType<WpfTextBlock>();
            panel.Children[2].Should().BeOfType<Border>();
            panel.Children[3].Should().BeOfType<WpfTextBlock>();
            panel.Children[4].Should().BeOfType<Border>();
        });
    }

    [Fact]
    public void CreateKeycapsBadge_WithEmptyShortcut_ReturnsPlaceholderBlock()
    {
        RunInSta(() =>
        {
            var visual = ShortcutHelper.CreateKeycapsBadge("");
            visual.Should().BeOfType<WpfTextBlock>();

            var tb = (WpfTextBlock)visual;
            tb.Text.Should().Contain("Belum diatur");
        });
    }

    #endregion
}
