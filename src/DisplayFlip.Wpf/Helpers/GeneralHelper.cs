using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using DisplayFlip.Core;
using DisplayFlip.UI.Services;
using DisplayFlip.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DisplayFlip.UI.Helpers;

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public sealed record IconStyleOption(
    string Key,
    string Title,
    string Description,
    string BadgeText
);

public sealed record CliCommandItem(
    string Option,
    string Aliases,
    string Title,
    string Syntax,
    string Description,
    string Category
);

public static class GeneralHelper
{
    public const string DefaultIconStyle = "fluent";

    private static AppThemeMode _currentThemeMode = AppThemeMode.System;

    private static IReadOnlyList<IconStyleOption> BuildIconStyles() =>
    [
        new(
            "fluent",
            Loc.Instance.Get("General.Icon.Fluent.Title"),
            Loc.Instance.Get("General.Icon.Fluent.Subtitle"),
            Loc.Instance.Get("General.Icon.Fluent.Badge")
        ),
        new(
            "accent",
            Loc.Instance.Get("General.Icon.Accent.Title"),
            Loc.Instance.Get("General.Icon.Accent.Subtitle"),
            Loc.Instance.Get("General.Icon.Accent.Badge")
        ),
        new(
            "minimal",
            Loc.Instance.Get("General.Icon.Minimal.Title"),
            Loc.Instance.Get("General.Icon.Minimal.Subtitle"),
            Loc.Instance.Get("General.Icon.Minimal.Badge")
        )
    ];

    private static IReadOnlyList<CliCommandItem> BuildCliCommands() =>
    [
        new(
            "--swap",
            "--flip, --mirror",
            Loc.Instance.Get("Cli.Doc.Swap.Title"),
            "displayflip.exe --swap",
            Loc.Instance.Get("Cli.Doc.Swap"),
            Loc.Instance.Get("Cli.Cat.Layout")
        ),
        new(
            "--identify",
            "-i",
            Loc.Instance.Get("Cli.Doc.Identify.Title"),
            "displayflip.exe --identify",
            Loc.Instance.Get("Cli.Doc.Identify"),
            Loc.Instance.Get("Cli.Cat.Visual")
        ),
        new(
            "--solo <1|2>",
            "--internal, --external",
            Loc.Instance.Get("Cli.Doc.Solo.Title"),
            "displayflip.exe --solo 1",
            Loc.Instance.Get("Cli.Doc.Solo"),
            Loc.Instance.Get("Cli.Cat.Projection")
        ),
        new(
            "--extend",
            "--perluas",
            Loc.Instance.Get("Cli.Doc.Extend.Title"),
            "displayflip.exe --extend",
            Loc.Instance.Get("Cli.Doc.Extend"),
            Loc.Instance.Get("Cli.Cat.Projection")
        ),
        new(
            "--apply <profile>",
            "--profile, -p",
            Loc.Instance.Get("Cli.Doc.Profile.Title"),
            "displayflip.exe --apply \"Kerja Siang\"",
            Loc.Instance.Get("Cli.Doc.Profile"),
            Loc.Instance.Get("Cli.Cat.Profile")
        ),
        new(
            "--list",
            "--list --json",
            Loc.Instance.Get("Cli.Doc.List.Title"),
            "displayflip.exe --list",
            Loc.Instance.Get("Cli.Doc.List"),
            Loc.Instance.Get("Cli.Cat.Profile")
        ),
        new(
            "--lang <id|en>",
            "--lang=id, --lang=en",
            Loc.Instance.Get("Cli.Doc.Lang.Title"),
            "displayflip.exe --list --lang en",
            Loc.Instance.Get("Cli.Doc.Lang"),
            Loc.Instance.Get("Cli.Cat.Language")
        ),
        new(
            "--settings",
            "--config",
            Loc.Instance.Get("Cli.Doc.Settings.Title"),
            "displayflip.exe --settings",
            Loc.Instance.Get("Cli.Doc.Settings"),
            Loc.Instance.Get("Cli.Cat.App")
        ),
        new(
            "--tray",
            "--minimized",
            Loc.Instance.Get("Cli.Doc.Tray.Title"),
            "displayflip.exe --tray",
            Loc.Instance.Get("Cli.Doc.Tray"),
            Loc.Instance.Get("Cli.Cat.App")
        )
    ];

    public static IReadOnlyList<IconStyleOption> GetIconStyles() => BuildIconStyles();

    public static string NormalizeIconStyle(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return DefaultIconStyle;

        var clean = key.Trim().ToLowerInvariant();
        return clean switch
        {
            "accent" => "accent",
            "minimal" => "minimal",
            _ => "fluent"
        };
    }

    public static IconStyleOption GetIconStyleOption(string? key)
    {
        var normalized = NormalizeIconStyle(key);
        return BuildIconStyles().First(s => s.Key == normalized);
    }

    public static string GetThemeName(AppThemeMode mode) => mode switch
    {
        AppThemeMode.Light => Loc.Instance.Get("General.Theme.Light.Title"),
        AppThemeMode.Dark => Loc.Instance.Get("General.Theme.Dark.Title"),
        _ => Loc.Instance.Get("General.Theme.System.Label")
    };

    public static AppThemeMode GetCurrentThemeMode()
    {
        return _currentThemeMode;
    }

    public static bool ApplyTheme(AppThemeMode mode)
    {
        try
        {
            _currentThemeMode = mode;
            switch (mode)
            {
                case AppThemeMode.Light:
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.Mica, true);
                    break;
                case AppThemeMode.Dark:
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);
                    break;
                case AppThemeMode.System:
                default:
                    ApplicationThemeManager.ApplySystemTheme(true);
                    break;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsStartupEnabled()
    {
        try
        {
            return StartupRegistry.IsEnabled();
        }
        catch
        {
            return false;
        }
    }

    public static (bool Success, string Message) SetStartupEnabled(bool enabled)
    {
        try
        {
            StartupRegistry.SetEnabled(enabled);
            var isNow = StartupRegistry.IsEnabled();
            if (enabled == isNow)
            {
                return (true, Loc.Instance.Get(enabled ? "General.Startup.EnabledMsg" : "General.Startup.DisabledMsg"));
            }

            return (false, Loc.Instance.Get("General.Startup.RegistryError"));
        }
        catch (Exception ex)
        {
            return (false, Loc.Instance.Format("General.Startup.UpdateError", ex.Message));
        }
    }

    public static bool GetAutoApplyDrag(ProfileStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        try
        {
            return store.Load().AutoApplyDrag;
        }
        catch
        {
            return false;
        }
    }

    public static (bool Success, string Message) SetAutoApplyDrag(ProfileStore store, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(store);
        try
        {
            store.UpdateSettings(cfg => cfg with { AutoApplyDrag = enabled });
            return (true, Loc.Instance.Get(enabled ? "General.AutoApply.EnabledMsg" : "General.AutoApply.DisabledMsg"));
        }
        catch (Exception ex)
        {
            return (false, Loc.Instance.Format("General.AutoApply.SaveError", ex.Message));
        }
    }

    public static (bool Success, string Message) SetIconStyle(ProfileStore store, string styleKey)
    {
        ArgumentNullException.ThrowIfNull(store);
        try
        {
            var normalized = NormalizeIconStyle(styleKey);
            store.UpdateSettings(cfg => cfg with { IconStyle = normalized });
            var option = GetIconStyleOption(normalized);
            return (true, Loc.Instance.Format("General.Icon.UpdatedMsg", option.Title));
        }
        catch (Exception ex)
        {
            return (false, Loc.Instance.Format("General.Icon.SaveError", ex.Message));
        }
    }

    public static string GetStorageDirectory(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return string.Empty;

        try
        {
            if (Directory.Exists(filePath))
                return Path.GetFullPath(filePath);

            return Path.GetDirectoryName(filePath) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public static (bool Success, string Message) OpenStorageFolder(string? filePath)
    {
        var dir = GetStorageDirectory(filePath);
        if (string.IsNullOrWhiteSpace(dir))
            return (false, Loc.Instance.Get("General.Storage.InvalidDir"));

        try
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{dir}\"",
                UseShellExecute = true
            };
            Process.Start(startInfo);
            return (true, Loc.Instance.Format("General.Storage.OpenSuccess", dir));
        }
        catch (Exception ex)
        {
            return (false, Loc.Instance.Format("General.Storage.OpenError", ex.Message));
        }
    }

    public static (bool Success, string Message) OpenAppFolder()
    {
        try
        {
            var dir = AppContext.BaseDirectory;
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return (false, Loc.Instance.Get("General.AppFolder.NotFound"));

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{dir}\"",
                UseShellExecute = true
            });
            return (true, Loc.Instance.Format("General.AppFolder.Opened", dir));
        }
        catch (Exception ex)
        {
            return (false, Loc.Instance.Format("General.AppFolder.OpenError", ex.Message));
        }
    }

    public static (bool Success, string Message) OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (false, Loc.Instance.Get("General.Url.Invalid"));

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return (true, Loc.Instance.Format("General.Url.Opening", url));
        }
        catch (Exception ex)
        {
            return (false, Loc.Instance.Format("General.Url.OpenError", ex.Message));
        }
    }

    public static IReadOnlyList<CliCommandItem> GetCliCommands() => BuildCliCommands();

    public static bool TryCopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Clipboard.SetDataObject(text, true);
                return true;
            }
            catch
            {
                System.Threading.Thread.Sleep(50);
            }
        }

        return false;
    }
}
