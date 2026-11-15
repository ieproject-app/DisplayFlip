using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DisplayFlip.Core;
using DisplayFlip.Core.Localization;
using DisplayFlip.UI.Helpers;
using DisplayFlip.UI.Services;
using DisplayFlip.UI.Views;
using DisplayFlip.UI.Views.Pages;
using DisplayFlip.Windows;
using FluentAssertions;
using Xunit;
using CoreOrientation = DisplayFlip.Core.Orientation;
using WpfMenuItem = System.Windows.Controls.MenuItem;

namespace DisplayFlip.Tests;

[CollectionDefinition("Localization", DisableParallelization = true)]
public sealed class LocalizationCollectionDefinition
{
}

[Collection("Localization")]
public class BilingualLocalizationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _storePath;

    public BilingualLocalizationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DisplayFlip_LocTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _storePath = Path.Combine(_tempDir, "profiles.json");
    }

    public void Dispose()
    {
        Loc.Instance.SetPreference(LocalizationCatalog.PreferenceAuto, persist: false);
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // Ignore temp cleanup failure
        }
    }

    private static IReadOnlySet<string> ExtractPlaceholders(string template)
    {
        var matches = Regex.Matches(template, @"\{(\d+)(?:[,:][^}]*)?\}");
        return matches.Select(m => m.Value).ToHashSet(StringComparer.Ordinal);
    }

    [Fact]
    public void LocalizationCatalog_ShouldHaveExactKeyParityBetweenIndonesianAndEnglish()
    {
        var idKeys = LocalizationCatalog.Indonesian.Keys.ToHashSet(StringComparer.Ordinal);
        var enKeys = LocalizationCatalog.English.Keys.ToHashSet(StringComparer.Ordinal);

        var missingInEn = idKeys.Except(enKeys).OrderBy(k => k).ToList();
        var missingInId = enKeys.Except(idKeys).OrderBy(k => k).ToList();

        missingInEn.Should().BeEmpty("every Indonesian catalog key must also exist in English");
        missingInId.Should().BeEmpty("every English catalog key must also exist in Indonesian");
    }

    [Fact]
    public void LocalizationCatalog_ShouldHaveNonEmptyStringsAndMatchingPlaceholders()
    {
        foreach (var (key, idValue) in LocalizationCatalog.Indonesian)
        {
            idValue.Should().NotBeNullOrWhiteSpace($"Indonesian value for key '{key}' must not be blank");
            var enValue = LocalizationCatalog.English[key];
            enValue.Should().NotBeNullOrWhiteSpace($"English value for key '{key}' must not be blank");

            var idPlaceholders = ExtractPlaceholders(idValue);
            var enPlaceholders = ExtractPlaceholders(enValue);
            enPlaceholders.Should().BeEquivalentTo(
                idPlaceholders,
                $"placeholders for key '{key}' must match between Indonesian and English");
        }
    }

    [Theory]
    [InlineData("id-ID", "id")]
    [InlineData("id", "id")]
    [InlineData("en-US", "en")]
    [InlineData("en-GB", "en")]
    [InlineData("ja-JP", "en")]
    [InlineData("fr-FR", "en")]
    public void ResolveEffectiveLanguage_AutoFollowsIndonesianOrFallsBackToEnglish(string cultureName, string expectedLang)
    {
        var resolved = LocalizationCatalog.ResolveEffectiveLanguage(
            LocalizationCatalog.PreferenceAuto,
            CultureInfo.GetCultureInfo(cultureName));

        resolved.Should().Be(expectedLang);
    }

    [Theory]
    [InlineData(null, "auto")]
    [InlineData("", "auto")]
    [InlineData("invalid-value", "auto")]
    [InlineData("id", "id")]
    [InlineData("id-ID", "id")]
    [InlineData("en", "en")]
    [InlineData("en-US", "en")]
    [InlineData("AUTO", "auto")]
    public void NormalizePreference_NormalizesKnownValuesAndFallsBackToAuto(string? raw, string expected)
    {
        LocalizationCatalog.NormalizePreference(raw).Should().Be(expected);
    }

    [Fact]
    public void ProfileStore_LegacyJsonWithoutLanguageField_DefaultsToAutoAndPreservesProfiles()
    {
        var legacyJson = """
        {
          "schemaVersion": 1,
          "profiles": [
            {
              "schemaVersion": 1,
              "name": "Dual Work",
              "primaryStableKey": "DISPLAY1",
              "monitors": [],
              "shortcut": "Ctrl+Alt+1"
            }
          ],
          "mirrorShortcut": "Ctrl+Alt+F",
          "autoApplyDrag": true,
          "iconStyle": "slate"
        }
        """;
        File.WriteAllText(_storePath, legacyJson);

        var store = new ProfileStore(_storePath);
        var loaded = store.Load();

        loaded.SchemaVersion.Should().Be(LayoutProfile.SupportedSchemaVersion);
        loaded.Language.Should().Be(LocalizationCatalog.PreferenceAuto);
        store.GetLanguagePreference().Should().Be(LocalizationCatalog.PreferenceAuto);
        loaded.Profiles.Should().ContainSingle(p => p.Name == "Dual Work");
        loaded.IconStyle.Should().Be("slate");
        loaded.AutoApplyDrag.Should().BeTrue();
    }

    [Fact]
    public void ProfileStore_SaveLanguagePreference_PersistsLanguageWithoutLosingProfilesOrShortcuts()
    {
        var store = new ProfileStore(_storePath);
        store.Save(new LayoutProfile
        {
            SchemaVersion = LayoutProfile.SupportedSchemaVersion,
            Name = "Gaming",
            PrimaryStableKey = "DISPLAY1",
            Monitors =
            [
                new MonitorState(
                    StableKey: "DISPLAY1",
                    FriendlyName: "DISPLAY1",
                    Position: new Position(0, 0, 1920, 1080),
                    ResolutionWidth: 1920,
                    ResolutionHeight: 1080,
                    Orientation: CoreOrientation.Landscape,
                    IsEnabled: true)
            ]
        });
        store.UpdateSettings(cfg => cfg with { MirrorShortcut = "Ctrl+Alt+9", IconStyle = "orbital" });

        store.SaveLanguagePreference("en");

        var reloaded = store.Load();
        reloaded.Language.Should().Be("en");
        reloaded.MirrorShortcut.Should().Be("Ctrl+Alt+9");
        reloaded.IconStyle.Should().Be("orbital");
        reloaded.Profiles.Should().ContainSingle(p => p.Name == "Gaming");

        store.SaveLanguagePreference("unknown-junk");
        store.GetLanguagePreference().Should().Be("auto");
    }

    [Fact]
    public void LocInstance_LiveSwitch_UpdatesIndexerAndFiresNotifications()
    {
        var store = new ProfileStore(_storePath);
        Loc.Instance.Initialize(store, CultureInfo.GetCultureInfo("en-US"));

        var changedProps = new List<string?>();
        int langEvents = 0;

        void OnPropChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e) => changedProps.Add(e.PropertyName);
        void OnLangChanged(object? s, EventArgs e) => langEvents++;

        Loc.Instance.PropertyChanged += OnPropChanged;
        Loc.Instance.LanguageChanged += OnLangChanged;
        try
        {
            Loc.Instance.SetPreference("id", persist: true, store: store);
            Loc.Instance.EffectiveLanguage.Should().Be("id");
            Loc.Instance["Nav.Displays"].Should().Be("Layar & Profil");

            Loc.Instance.SetPreference("en", persist: true, store: store);
            Loc.Instance.EffectiveLanguage.Should().Be("en");
            Loc.Instance["Nav.Displays"].Should().Be("Displays & Profiles");

            store.GetLanguagePreference().Should().Be("en");
            changedProps.Should().Contain("Item[]");
            langEvents.Should().Be(2);
        }
        finally
        {
            Loc.Instance.PropertyChanged -= OnPropChanged;
            Loc.Instance.LanguageChanged -= OnLangChanged;
        }
    }

    [Fact]
    public void Us1_GeneralPage_STA_LanguageSelectorSwitchesLiveAndPersists()
    {
        ExecuteOnSta(() =>
        {
            var store = new ProfileStore(_storePath);
            Loc.Instance.Initialize(store, CultureInfo.GetCultureInfo("en-US"));

            var page = new GeneralPage(store);
            var tags = page.LanguageComboBox.Items
                .OfType<ComboBoxItem>()
                .Select(i => i.Tag as string)
                .ToList();
            tags.Should().BeEquivalentTo(new[] { "auto", "id", "en" });

            var idItem = page.LanguageComboBox.Items.OfType<ComboBoxItem>().First(i => (string?)i.Tag == "id");
            page.LanguageComboBox.SelectedItem = idItem;

            Loc.Instance.EffectiveLanguage.Should().Be("id");
            store.GetLanguagePreference().Should().Be("id");
            page.TxtEffectiveLanguage.Text.Should().Contain("Indonesia");

            var enItem = page.LanguageComboBox.Items.OfType<ComboBoxItem>().First(i => (string?)i.Tag == "en");
            page.LanguageComboBox.SelectedItem = enItem;

            Loc.Instance.EffectiveLanguage.Should().Be("en");
            store.GetLanguagePreference().Should().Be("en");
            page.TxtEffectiveLanguage.Text.Should().Contain("English");
        });
    }

    [Fact]
    public void Us1_DisplaysPage_STA_LanguageSwitchPreservesPendingArrangementAndProfileNames()
    {
        ExecuteOnSta(() =>
        {
            var store = new ProfileStore(_storePath);
            store.Save(new LayoutProfile
            {
                SchemaVersion = LayoutProfile.SupportedSchemaVersion,
                Name = "Kerja Siang Custom",
                PrimaryStableKey = "MON1",
                Monitors =
                [
                    new MonitorState("MON1", "Internal", new Position(0, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true),
                    new MonitorState("MON2", "External", new Position(1920, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true)
                ]
            });

            Loc.Instance.SetPreference(LocalizationCatalog.LanguageIndonesian, persist: false);
            var page = new DisplaysPage(store);
            var monitors = new List<DisplayMonitor>
            {
                new("MON1", "Dell Primary", new Position(0, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true),
                new("MON2", "LG Secondary", new Position(1920, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true)
            };

            page.MonitorCanvas.Monitors = monitors;
            page.MonitorCanvas.PrimaryMonitorKey = "MON1";
            page.MonitorCanvas.SelectedMonitorKey = "MON2";
            page.MonitorCanvas.StartDrag("MON2", new Point(360, 100));
            page.MonitorCanvas.UpdateDrag(new Point(40, 100));
            page.MonitorCanvas.EndDrag();

            page.HasUnappliedChanges.Should().BeTrue();

            Loc.Instance.SetPreference(LocalizationCatalog.LanguageEnglish, persist: false);
            page.RefreshData(resetCanvasToLive: false);

            page.HasUnappliedChanges.Should().BeTrue();
            page.BtnApply.IsEnabled.Should().BeTrue();
            page.CanvasMonitors.Should().Contain(m => m.StableKey == "MON2" && m.Position.X == -1920);
            store.Load().Profiles.Should().ContainSingle(p => p.Name == "Kerja Siang Custom");
        });
    }

    [Fact]
    public void Us1_AboutPage_STA_ExposesWebsiteAndGitHubAndUpdatesCliCommands()
    {
        ExecuteOnSta(() =>
        {
            AboutPage.WebsiteUrl.Should().Be("https://displayflip.snipgeek.com");
            AboutPage.GitHubUrl.Should().Be("https://github.com/ieproject-app/DisplayFlip");

            var store = new ProfileStore(_storePath);
            Loc.Instance.SetPreference(LocalizationCatalog.LanguageIndonesian, persist: false);
            var idCommands = GeneralHelper.GetCliCommands();
            idCommands.First(c => c.Option == "--swap").Title.Should().Be("Tukar Posisi Monitor");

            Loc.Instance.SetPreference(LocalizationCatalog.LanguageEnglish, persist: false);
            var enCommands = GeneralHelper.GetCliCommands();
            enCommands.First(c => c.Option == "--swap").Title.Should().Be("Swap Monitor Positions");

            var page = new AboutPage(store);
            page.CliCommandsPanel.Children.Count.Should().Be(9);
            page.BtnWebsite.Should().NotBeNull();
            page.BtnDocs.Should().NotBeNull();
        });
    }

    [Fact]
    public void Us2_TrayIconManager_STA_LocalizesContextMenuInIndonesianAndEnglish()
    {
        ExecuteOnSta(() =>
        {
            var store = new ProfileStore(_storePath);
            using var tray = new TrayIconManager(store);

            Loc.Instance.SetPreference(LocalizationCatalog.LanguageIndonesian, persist: false);
            var idMenu = tray.CreateContextMenu();
            var idHeaders = idMenu.Items.OfType<WpfMenuItem>().Select(m => m.Header?.ToString()).ToList();
            idHeaders.Should().Contain("Tukar Kiri-Kanan (Quick Swap)");
            idHeaders.Should().Contain("Buka Pengaturan");
            idHeaders.Should().Contain("Keluar");

            Loc.Instance.SetPreference(LocalizationCatalog.LanguageEnglish, persist: false);
            var enMenu = tray.CreateContextMenu();
            var enHeaders = enMenu.Items.OfType<WpfMenuItem>().Select(m => m.Header?.ToString()).ToList();
            enHeaders.Should().Contain("Swap Left-Right (Quick Swap)");
            enHeaders.Should().Contain("Open Settings");
            enHeaders.Should().Contain("Exit");
        });
    }

    [Fact]
    public void Us2_IdentifyOverlayWindow_STA_LocalizesWindowTitleAndSubtitle()
    {
        ExecuteOnSta(() =>
        {
            Loc.Instance.SetPreference(LocalizationCatalog.LanguageIndonesian, persist: false);
            var idOverlay = new IdentifyOverlayWindow(
                new Position(0, 0, 1920, 1080),
                "1",
                Loc.Instance.Format("Overlay.PrimarySuffix", "Display 1 (DISPLAY1)"));
            try
            {
                idOverlay.Title.Should().Be("DisplayFlip — Identifikasi Layar");
                idOverlay.DisplaySubtitleText.Text.Should().Be("Display 1 (DISPLAY1) • Layar Utama");
            }
            finally
            {
                idOverlay.Close();
            }

            Loc.Instance.SetPreference(LocalizationCatalog.LanguageEnglish, persist: false);
            var enOverlay = new IdentifyOverlayWindow(
                new Position(0, 0, 1920, 1080),
                "1",
                Loc.Instance.Format("Overlay.PrimarySuffix", "Display 1 (DISPLAY1)"));
            try
            {
                enOverlay.Title.Should().Be("DisplayFlip — Identify Displays");
                enOverlay.DisplaySubtitleText.Text.Should().Be("Display 1 (DISPLAY1) • Primary Display");
            }
            finally
            {
                enOverlay.Close();
            }
        });
    }

    [Fact]
    public void Us3_CliRunner_SupportsExplicitLangFlagWithoutMutatingPersistedConfig()
    {
        var store = new ProfileStore(_storePath);
        store.SaveLanguagePreference(LocalizationCatalog.LanguageIndonesian);
        store.Save(new LayoutProfile
        {
            SchemaVersion = LayoutProfile.SupportedSchemaVersion,
            Name = "Work Desk",
            PrimaryStableKey = "MON1",
            Monitors =
            [
                new MonitorState("MON1", "Monitor 1", new Position(0, 0, 1920, 1080), 1920, 1080, CoreOrientation.Landscape, true)
            ]
        });

        var actions = new DisplayActionService(
            adapter: null,
            store: store,
            readActiveMonitors: () => Array.Empty<DisplayMonitor>(),
            dispatchProjection: _ => (true, "ok"),
            tryApplyProfile: (_, _) => (true, "ok"),
            sleep: _ => { });

        // 1. --help with --lang en
        var helpEnOut = new StringWriter();
        var helpEnErr = new StringWriter();
        int code1 = CliRunner.Run(["--help", "--lang", "en"], actions, helpEnOut, helpEnErr, CultureInfo.GetCultureInfo("id-ID"));
        code1.Should().Be(0);
        helpEnOut.ToString().Should().Contain("Usage:");
        helpEnOut.ToString().Should().Contain("--lang <id|en>");

        // 2. --help with --lang=id
        var helpIdOut = new StringWriter();
        var helpIdErr = new StringWriter();
        int code2 = CliRunner.Run(["--help", "--lang=id"], actions, helpIdOut, helpIdErr, CultureInfo.GetCultureInfo("en-US"));
        code2.Should().Be(0);
        helpIdOut.ToString().Should().Contain("Penggunaan:");

        // 3. --list with --lang en and --list --json stays clean JSON schema
        var listEnOut = new StringWriter();
        int code3 = CliRunner.Run(["--list", "--lang", "en"], actions, listEnOut, new StringWriter());
        code3.Should().Be(0);
        listEnOut.ToString().Should().Contain("Saved Profiles (1):");
        listEnOut.ToString().Should().Contain("Work Desk");

        var listJsonOut = new StringWriter();
        int code4 = CliRunner.Run(["--list", "--json", "--lang", "id"], actions, listJsonOut, new StringWriter());
        code4.Should().Be(0);
        listJsonOut.ToString().TrimStart().Should().StartWith("[");
        listJsonOut.ToString().Should().Contain("\"Work Desk\"");

        // 4. Invalid --lang fr falls back cleanly to persisted preference ("id")
        var invalidLangOut = new StringWriter();
        int code5 = CliRunner.Run(["--list", "--lang", "fr"], actions, invalidLangOut, new StringWriter(), CultureInfo.GetCultureInfo("en-US"));
        code5.Should().Be(0);
        invalidLangOut.ToString().Should().Contain("Daftar Profil Tersimpan (1):");

        // 5. Missing profile + unknown command in English
        var applyErrOut = new StringWriter();
        var applyErrWriter = new StringWriter();
        int code6 = CliRunner.Run(["--apply", "MissingProfile", "--lang", "en"], actions, applyErrOut, applyErrWriter);
        code6.Should().Be(5);
        applyErrWriter.ToString().Should().Contain("was not found");

        var unknownErrWriter = new StringWriter();
        int code7 = CliRunner.Run(["--unknown-flag", "--lang", "en"], actions, new StringWriter(), unknownErrWriter);
        code7.Should().Be(1);
        unknownErrWriter.ToString().Should().Contain("Unrecognized CLI command");

        // Verify persisted preference remained "id" throughout all CLI calls
        store.GetLanguagePreference().Should().Be(LocalizationCatalog.LanguageIndonesian);
    }

    [Fact]
    public void Us4_LocInitialize_CleanStoreFollowsUiCultureAndSavesNothing()
    {
        var freshStore = new ProfileStore(Path.Combine(_tempDir, "clean_profiles.json"));
        File.Exists(freshStore.FilePath).Should().BeFalse();

        Loc.Instance.Initialize(freshStore, CultureInfo.GetCultureInfo("en-US"));
        Loc.Instance.Preference.Should().Be("auto");
        Loc.Instance.EffectiveLanguage.Should().Be("en");

        Loc.Instance.Initialize(freshStore, CultureInfo.GetCultureInfo("id-ID"));
        Loc.Instance.Preference.Should().Be("auto");
        Loc.Instance.EffectiveLanguage.Should().Be("id");

        Loc.Instance.Initialize(freshStore, CultureInfo.GetCultureInfo("ja-JP"));
        Loc.Instance.Preference.Should().Be("auto");
        Loc.Instance.EffectiveLanguage.Should().Be("en");
    }

    private static void ExecuteOnSta(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (caught != null)
            throw new AggregateException("STA thread execution failed", caught);
    }
}
