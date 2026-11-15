using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;
using Wpf.Ui.Controls;

namespace DisplayFlip.UI.Views.Pages;

public partial class GeneralPage : Page
{
    private readonly ProfileStore _store;
    private bool _isInitializing;

    public GeneralPage() : this(new ProfileStore())
    {
    }

    public GeneralPage(ProfileStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        PageInfoBar.IsOpen = false;
        LoadSettings();
    }

    private void LoadSettings()
    {
        _isInitializing = true;
        try
        {
            // 1. Startup Otomatis Windows
            ToggleStartup.IsChecked = GeneralHelper.IsStartupEnabled();

            // 2. Auto-Apply Drag
            ToggleAutoApply.IsChecked = GeneralHelper.GetAutoApplyDrag(_store);

            // 3. Tema Aplikasi
            var themeMode = GeneralHelper.GetCurrentThemeMode();
            UpdateThemeSelectionVisuals(themeMode);

            // 4. Gaya Ikon Modern
            var cfg = _store.Load();
            var iconStyle = GeneralHelper.NormalizeIconStyle(cfg.IconStyle);
            UpdateIconStyleSelectionVisuals(iconStyle);

            // 5. Lokasi Direktori Penyimpanan
            TxtStoragePath.Text = _store.FilePath;
        }
        catch (Exception ex)
        {
            ShowNotification(InfoBarSeverity.Error, "Kesalahan Memuat Pengaturan", ex.Message);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    // =========================================================================
    // 1. STARTUP OTOMATIS WINDOWS
    // =========================================================================

    private void OnToggleStartupClick(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
            return;

        bool targetState = ToggleStartup.IsChecked == true;
        var (ok, msg) = GeneralHelper.SetStartupEnabled(targetState);

        if (ok)
        {
            ShowNotification(InfoBarSeverity.Success, "Startup Windows Diperbarui", msg);
        }
        else
        {
            // Kembalikan switch ke kondisi sebenarnya bila gagal
            ToggleStartup.IsChecked = GeneralHelper.IsStartupEnabled();
            ShowNotification(InfoBarSeverity.Error, "Gagal Mengubah Startup", msg);
        }
    }

    // =========================================================================
    // 2. AUTO-APPLY DRAG
    // =========================================================================

    private void OnToggleAutoApplyClick(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
            return;

        bool targetState = ToggleAutoApply.IsChecked == true;
        var (ok, msg) = GeneralHelper.SetAutoApplyDrag(_store, targetState);

        if (ok)
        {
            ShowNotification(InfoBarSeverity.Success, "Tata Letak Otomatis", msg);
        }
        else
        {
            ToggleAutoApply.IsChecked = GeneralHelper.GetAutoApplyDrag(_store);
            ShowNotification(InfoBarSeverity.Error, "Gagal Menyimpan Pengaturan", msg);
        }
    }

    // =========================================================================
    // 3. PERSONALISASI TEMA APLIKASI
    // =========================================================================

    private void OnThemeSystemCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        SelectTheme(AppThemeMode.System);
    }

    private void OnRadioThemeSystemClick(object sender, RoutedEventArgs e)
    {
        SelectTheme(AppThemeMode.System);
    }

    private void OnThemeLightCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        SelectTheme(AppThemeMode.Light);
    }

    private void OnRadioThemeLightClick(object sender, RoutedEventArgs e)
    {
        SelectTheme(AppThemeMode.Light);
    }

    private void OnThemeDarkCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        SelectTheme(AppThemeMode.Dark);
    }

    private void OnRadioThemeDarkClick(object sender, RoutedEventArgs e)
    {
        SelectTheme(AppThemeMode.Dark);
    }

    private void SelectTheme(AppThemeMode mode)
    {
        UpdateThemeSelectionVisuals(mode);
        bool applied = GeneralHelper.ApplyTheme(mode);
        string modeTitle = GeneralHelper.GetThemeName(mode);

        if (applied)
        {
            ShowNotification(InfoBarSeverity.Success, "Tema Aplikasi Diperbarui",
                $"Tampilan antarmuka berhasil disesuaikan ke {modeTitle}.");
        }
        else
        {
            ShowNotification(InfoBarSeverity.Warning, "Tema Aplikasi",
                $"Gagal menerapkan tema {modeTitle} secara penuh.");
        }
    }

    private void UpdateThemeSelectionVisuals(AppThemeMode selectedMode)
    {
        RadioThemeSystem.IsChecked = selectedMode == AppThemeMode.System;
        RadioThemeLight.IsChecked = selectedMode == AppThemeMode.Light;
        RadioThemeDark.IsChecked = selectedMode == AppThemeMode.Dark;

        HighlightCard(ThemeSystemCard, selectedMode == AppThemeMode.System);
        HighlightCard(ThemeLightCard, selectedMode == AppThemeMode.Light);
        HighlightCard(ThemeDarkCard, selectedMode == AppThemeMode.Dark);
    }

    // =========================================================================
    // 4. GAYA IKON MODERN
    // =========================================================================

    private void OnIconCardFluentMouseDown(object sender, MouseButtonEventArgs e)
    {
        SelectIconStyle("fluent");
    }

    private void OnRadioIconFluentClick(object sender, RoutedEventArgs e)
    {
        SelectIconStyle("fluent");
    }

    private void OnIconCardAccentMouseDown(object sender, MouseButtonEventArgs e)
    {
        SelectIconStyle("accent");
    }

    private void OnRadioIconAccentClick(object sender, RoutedEventArgs e)
    {
        SelectIconStyle("accent");
    }

    private void OnIconCardMinimalMouseDown(object sender, MouseButtonEventArgs e)
    {
        SelectIconStyle("minimal");
    }

    private void OnRadioIconMinimalClick(object sender, RoutedEventArgs e)
    {
        SelectIconStyle("minimal");
    }

    private void SelectIconStyle(string styleKey)
    {
        var normalized = GeneralHelper.NormalizeIconStyle(styleKey);
        UpdateIconStyleSelectionVisuals(normalized);

        var (ok, msg) = GeneralHelper.SetIconStyle(_store, normalized);
        if (ok)
        {
            ShowNotification(InfoBarSeverity.Success, "Gaya Ikon Diperbarui", msg);
        }
        else
        {
            ShowNotification(InfoBarSeverity.Error, "Gagal Memperbarui Ikon", msg);
        }
    }

    private void UpdateIconStyleSelectionVisuals(string activeStyle)
    {
        bool isFluent = string.Equals(activeStyle, "fluent", StringComparison.OrdinalIgnoreCase);
        bool isAccent = string.Equals(activeStyle, "accent", StringComparison.OrdinalIgnoreCase);
        bool isMinimal = string.Equals(activeStyle, "minimal", StringComparison.OrdinalIgnoreCase);

        RadioIconFluent.IsChecked = isFluent;
        RadioIconAccent.IsChecked = isAccent;
        RadioIconMinimal.IsChecked = isMinimal;

        BadgeIconFluent.Visibility = isFluent ? Visibility.Visible : Visibility.Collapsed;
        BadgeIconAccent.Visibility = isAccent ? Visibility.Visible : Visibility.Collapsed;
        BadgeIconMinimal.Visibility = isMinimal ? Visibility.Visible : Visibility.Collapsed;

        HighlightCard(IconCardFluent, isFluent);
        HighlightCard(IconCardAccent, isAccent);
        HighlightCard(IconCardMinimal, isMinimal);
    }

    // =========================================================================
    // 5. PENYIMPANAN KONFIGURASI
    // =========================================================================

    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = GeneralHelper.OpenStorageFolder(_store.FilePath);
        if (ok)
        {
            ShowNotification(InfoBarSeverity.Success, "Folder Dibuka", msg);
        }
        else
        {
            ShowNotification(InfoBarSeverity.Error, "Gagal Membuka Folder", msg);
        }
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private void ShowNotification(InfoBarSeverity severity, string title, string message)
    {
        PageInfoBar.Severity = severity;
        PageInfoBar.Title = title;
        PageInfoBar.Message = message;
        PageInfoBar.IsOpen = true;
    }

    private static void HighlightCard(Card card, bool isSelected)
    {
        if (isSelected)
        {
            if (Application.Current?.TryFindResource("AccentFillColorDefaultBrush") is Brush accentBrush)
            {
                card.BorderBrush = accentBrush;
                card.BorderThickness = new Thickness(1.5);
            }
        }
        else
        {
            card.ClearValue(Border.BorderBrushProperty);
            card.ClearValue(Border.BorderThicknessProperty);
        }
    }
}
