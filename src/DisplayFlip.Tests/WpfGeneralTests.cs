using System;
using System.IO;
using System.Linq;
using System.Threading;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;
using DisplayFlip.UI.Services;
using DisplayFlip.UI.Views.Pages;
using FluentAssertions;
using Xunit;

namespace DisplayFlip.Tests;

public class WpfGeneralTests
{
    [Theory]
    [InlineData("fluent", "fluent")]
    [InlineData("FLUENT", "fluent")]
    [InlineData("Fluent", "fluent")]
    [InlineData("  fluent  ", "fluent")]
    [InlineData("accent", "accent")]
    [InlineData("ACCENT", "accent")]
    [InlineData("Accent", "accent")]
    [InlineData("  accent  ", "accent")]
    [InlineData("minimal", "minimal")]
    [InlineData("MINIMAL", "minimal")]
    [InlineData("Minimal", "minimal")]
    [InlineData("  minimal  ", "minimal")]
    [InlineData("", "fluent")]
    [InlineData("   ", "fluent")]
    [InlineData(null, "fluent")]
    [InlineData("custom_unknown", "fluent")]
    public void NormalizeIconStyle_ShouldNormalizeProperly(string? input, string expected)
    {
        var result = GeneralHelper.NormalizeIconStyle(input);
        result.Should().Be(expected);
    }

    [Fact]
    public void GetIconStyles_ShouldReturnThreeDistinctStyles()
    {
        var styles = GeneralHelper.GetIconStyles();
        styles.Should().HaveCount(3);

        var keys = styles.Select(s => s.Key).ToList();
        keys.Should().Contain(new[] { "fluent", "accent", "minimal" });

        foreach (var style in styles)
        {
            style.Key.Should().NotBeNullOrWhiteSpace();
            style.Title.Should().NotBeNullOrWhiteSpace();
            style.Description.Should().NotBeNullOrWhiteSpace();
            style.BadgeText.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Theory]
    [InlineData("fluent", "Fluent Glass Dual")]
    [InlineData("accent", "Windows 11 Accent")]
    [InlineData("minimal", "Minimalist Precision")]
    [InlineData("invalid", "Fluent Glass Dual")]
    [InlineData(null, "Fluent Glass Dual")]
    public void GetIconStyleOption_ShouldReturnExpectedOption(string? key, string expectedTitle)
    {
        var option = GeneralHelper.GetIconStyleOption(key);
        option.Title.Should().Be(expectedTitle);
    }

    [Theory]
    [InlineData(AppThemeMode.System, "Ikuti Sistem (Otomatis)")]
    [InlineData(AppThemeMode.Light, "Mode Terang (Light)")]
    [InlineData(AppThemeMode.Dark, "Mode Gelap (Dark)")]
    public void GetThemeName_ShouldReturnIndonesianDescription(AppThemeMode mode, string expected)
    {
        var name = GeneralHelper.GetThemeName(mode);
        name.Should().Be(expected);
    }

    [Fact]
    public void ApplyTheme_AllModes_ShouldNotThrow()
    {
        var modes = new[] { AppThemeMode.System, AppThemeMode.Light, AppThemeMode.Dark };
        foreach (var mode in modes)
        {
            var res = GeneralHelper.ApplyTheme(mode);
            res.Should().BeTrue();
        }
    }

    [Fact]
    public void AutoApplyDrag_Persistence_ShouldUpdateAndPersist()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"displayflip_test_{Guid.NewGuid():N}.json");
        try
        {
            var store = new ProfileStore(tempFile);

            // Default
            GeneralHelper.GetAutoApplyDrag(store).Should().BeFalse();

            // Enable
            var (ok1, msg1) = GeneralHelper.SetAutoApplyDrag(store, true);
            ok1.Should().BeTrue();
            msg1.Should().Contain("otomatis");

            var store2 = new ProfileStore(tempFile);
            GeneralHelper.GetAutoApplyDrag(store2).Should().BeTrue();

            // Disable
            var (ok2, msg2) = GeneralHelper.SetAutoApplyDrag(store, false);
            ok2.Should().BeTrue();
            msg2.Should().Contain("dinonaktifkan");

            var store3 = new ProfileStore(tempFile);
            GeneralHelper.GetAutoApplyDrag(store3).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void IconStyle_Persistence_ShouldUpdateAndPersist()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"displayflip_test_{Guid.NewGuid():N}.json");
        try
        {
            var store = new ProfileStore(tempFile);

            // Set to accent
            var (ok1, msg1) = GeneralHelper.SetIconStyle(store, "accent");
            ok1.Should().BeTrue();
            msg1.Should().Contain("Windows 11 Accent");

            var store2 = new ProfileStore(tempFile);
            store2.Load().IconStyle.Should().Be("accent");

            // Set to minimal
            var (ok2, msg2) = GeneralHelper.SetIconStyle(store, "minimal");
            ok2.Should().BeTrue();
            msg2.Should().Contain("Minimalist Precision");

            var store3 = new ProfileStore(tempFile);
            store3.Load().IconStyle.Should().Be("minimal");

            // Set invalid key defaults to fluent
            var (ok3, msg3) = GeneralHelper.SetIconStyle(store, "unknown_something");
            ok3.Should().BeTrue();
            msg3.Should().Contain("Fluent Glass Dual");

            var store4 = new ProfileStore(tempFile);
            store4.Load().IconStyle.Should().Be("fluent");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(@"C:\Users\test\AppData\Local\DisplayFlip\profiles.json", @"C:\Users\test\AppData\Local\DisplayFlip")]
    [InlineData(@"D:\Config\profiles.json", @"D:\Config")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void GetStorageDirectory_ShouldExtractCorrectFolder(string? filePath, string expected)
    {
        var result = GeneralHelper.GetStorageDirectory(filePath);
        result.Should().Be(expected);
    }

    [Fact]
    public void OpenStorageFolder_WithInvalidPath_ShouldReturnFailure()
    {
        var (ok1, msg1) = GeneralHelper.OpenStorageFolder(null);
        ok1.Should().BeFalse();
        msg1.Should().Contain("tidak valid");

        var (ok2, msg2) = GeneralHelper.OpenStorageFolder("");
        ok2.Should().BeFalse();
        msg2.Should().Contain("tidak valid");
    }

    [Fact]
    public void OpenUrl_WithInvalidUrl_ShouldReturnFailure()
    {
        var (ok1, msg1) = GeneralHelper.OpenUrl("");
        ok1.Should().BeFalse();

        var (ok2, msg2) = GeneralHelper.OpenUrl(null!);
        ok2.Should().BeFalse();
    }

    [Fact]
    public void GetCliCommands_ShouldContainAllEightRequiredCommands()
    {
        var commands = GeneralHelper.GetCliCommands();
        commands.Should().HaveCount(8);

        var options = commands.Select(c => c.Option).ToList();
        options.Should().Contain(new[]
        {
            "--swap",
            "--identify",
            "--solo <1|2>",
            "--extend",
            "--apply <profile>",
            "--list",
            "--settings",
            "--tray"
        });

        foreach (var cmd in commands)
        {
            cmd.Option.Should().StartWith("--");
            cmd.Title.Should().NotBeNullOrWhiteSpace();
            cmd.Syntax.Should().StartWith("displayflip.exe");
            cmd.Description.Should().NotBeNullOrWhiteSpace();
            cmd.Category.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void TryCopyToClipboard_NullOrEmpty_ShouldReturnFalse()
    {
        GeneralHelper.TryCopyToClipboard("").Should().BeFalse();
        GeneralHelper.TryCopyToClipboard(null!).Should().BeFalse();
    }

    [Fact]
    public void PageService_ShouldResolveAllFourPages()
    {
        ExecuteOnSta(() =>
        {
            var service = new PageService();

            var displaysPage = service.GetPage(typeof(DisplaysPage));
            displaysPage.Should().NotBeNull();
            displaysPage.Should().BeOfType<DisplaysPage>();

            var shortcutsPage = service.GetPage(typeof(ShortcutsPage));
            shortcutsPage.Should().NotBeNull();
            shortcutsPage.Should().BeOfType<ShortcutsPage>();

            var generalPage = service.GetPage(typeof(GeneralPage));
            generalPage.Should().NotBeNull();
            generalPage.Should().BeOfType<GeneralPage>();

            var aboutPage = service.GetPage(typeof(AboutPage));
            aboutPage.Should().NotBeNull();
            aboutPage.Should().BeOfType<AboutPage>();

            // Verify caching
            var generalPage2 = service.GetPage(typeof(GeneralPage));
            generalPage2.Should().BeSameAs(generalPage);
        });
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
