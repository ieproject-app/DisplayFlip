using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DisplayFlip.Core;
using DisplayFlip.Core.Localization;
using DisplayFlip.UI.Helpers;
using DisplayFlip.UI.Services;
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
        LoadSettings();
        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        Loc.Instance.LanguageChanged -= OnLanguageChanged;
        Loc.Instance.LanguageChanged += OnLanguageChanged;
        PageInfoBar.IsOpen = false;
        LoadSettings();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        Loc.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateEffectiveLanguageLabel();
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

            // 3. Bahasa Antarmuka
            var langPref = _store.GetLanguagePreference();
            SelectLanguageComboItem(langPref);
            UpdateEffectiveLanguageLabel();

            // 4. Tema Aplikasi
            var themeMode = GeneralHelper.GetCurrentThemeMode();
            UpdateThemeSelectionVisuals(themeMode);

            // 5. Gaya Ikon Modern
            var cfg = _store.Load();
            var iconStyle = GeneralHelper.NormalizeIconStyle(cfg.IconStyle);
            UpdateIconStyleSelectionVisuals(iconStyle);
            LoadIconPreviews();

            // 6. Lokasi Direktori Penyimpanan
            TxtStoragePath.Text = _store.FilePath;
        }
        catch (Exception ex)
        {
            ShowNotification(InfoBarSeverity.Error, Loc.Instance.Get("General.LoadErrorTitle"), ex.Message);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void SelectLanguageComboItem(string pref)
    {
        var normalized = LocalizationCatalog.NormalizePreference(pref);
        foreach (var item in LanguageComboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag as string, normalized, StringComparison.OrdinalIgnoreCase))
            {
                LanguageComboBox.SelectedItem = item;
                return;
            }
        }
    }

    private void UpdateEffectiveLanguageLabel()
    {
        string langKey = Loc.Instance.EffectiveLanguage == LocalizationCatalog.LanguageIndonesian
            ? "General.Language.Name.id"
            : "General.Language.Name.en";
        TxtEffectiveLanguage.Text = Loc.Instance.Format("General.Language.CurrentEffective", Loc.Instance.Get(langKey));
    }

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
            return;

        if (LanguageComboBox.SelectedItem is not ComboBoxItem item || item.Tag is not string rawPref)
            return;

        try
        {
            string normalized = LocalizationCatalog.NormalizePreference(rawPref);
            Loc.Instance.SetPreference(normalized, persist: true, store: _store);
            UpdateEffectiveLanguageLabel();

            string langNameKey = Loc.Instance.EffectiveLanguage == LocalizationCatalog.LanguageIndonesian
                ? "General.Language.Name.id"
                : "General.Language.Name.en";
            string langDisplayName = Loc.Instance.Get(langNameKey);
            string msg = normalized == LocalizationCatalog.PreferenceAuto
                ? Loc.Instance.Format("General.Language.ChangedAuto", langDisplayName)
                : Loc.Instance.Format("General.Language.ChangedExplicit", langDisplayName);

            ShowNotification(InfoBarSeverity.Success, Loc.Instance.Get("General.Language.InfoTitle"), msg);
        }
        catch (Exception ex)
        {
            ShowNotification(
                InfoBarSeverity.Error,
                Loc.Instance.Get("General.AutoApply.SaveFailedTitle"),
                Loc.Instance.Format("General.Language.SaveError", ex.Message));
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
            ShowNotification(InfoBarSeverity.Success, Loc.Instance.Get("General.Startup.SuccessTitle"), msg);
        }
        else
        {
            // Kembalikan switch ke kondisi sebenarnya bila gagal
            ToggleStartup.IsChecked = GeneralHelper.IsStartupEnabled();
            ShowNotification(InfoBarSeverity.Error, Loc.Instance.Get("General.Startup.FailedTitle"), msg);
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
            ShowNotification(InfoBarSeverity.Success, Loc.Instance.Get("General.AutoApply.UpdatedTitle"), msg);
        }
        else
        {
            ToggleAutoApply.IsChecked = GeneralHelper.GetAutoApplyDrag(_store);
            ShowNotification(InfoBarSeverity.Error, Loc.Instance.Get("General.AutoApply.SaveFailedTitle"), msg);
        }
    }

    // =========================================================================
    // 3. PERSONALISASI TEMA APLIKASI
    // =========================================================================

    private void OnThemeSystemCardClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            SelectTheme(AppThemeMode.System);
    }

    private void OnThemeSystemCardKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            SelectTheme(AppThemeMode.System);
            e.Handled = true;
        }
    }

    private void OnThemeLightCardClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            SelectTheme(AppThemeMode.Light);
    }

    private void OnThemeLightCardKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            SelectTheme(AppThemeMode.Light);
            e.Handled = true;
        }
    }

    private void OnThemeDarkCardClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            SelectTheme(AppThemeMode.Dark);
    }

    private void OnThemeDarkCardKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            SelectTheme(AppThemeMode.Dark);
            e.Handled = true;
        }
    }

    private void SelectTheme(AppThemeMode mode)
    {
        UpdateThemeSelectionVisuals(mode);
        bool applied = GeneralHelper.ApplyTheme(mode);
        string modeTitle = GeneralHelper.GetThemeName(mode);

        if (applied)
        {
            ShowNotification(InfoBarSeverity.Success, Loc.Instance.Get("General.Theme.UpdatedTitle"),
                Loc.Instance.Format("General.Theme.UpdatedMsg", modeTitle));
        }
        else
        {
            ShowNotification(InfoBarSeverity.Warning, Loc.Instance.Get("General.Theme.FailedTitle"),
                Loc.Instance.Format("General.Theme.FailedMsg", modeTitle));
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

    private void OnIconCardFluentClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            SelectIconStyle("fluent");
    }

    private void OnIconCardFluentKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            SelectIconStyle("fluent");
            e.Handled = true;
        }
    }

    private void OnIconCardAccentClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            SelectIconStyle("accent");
    }

    private void OnIconCardAccentKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            SelectIconStyle("accent");
            e.Handled = true;
        }
    }

    private void OnIconCardMinimalClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            SelectIconStyle("minimal");
    }

    private void OnIconCardMinimalKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            SelectIconStyle("minimal");
            e.Handled = true;
        }
    }

    private void LoadIconPreviews()
    {
        try
        {
            IconPreviewFluent.Source = IconHelper.GetIconStylePreview("fluent", 44);
            IconPreviewAccent.Source = IconHelper.GetIconStylePreview("accent", 44);
            IconPreviewMinimal.Source = IconHelper.GetIconStylePreview("minimal", 44);
        }
        catch
        {
            // Abaikan kegagalan preview
        }
    }

    private void SelectIconStyle(string styleKey)
    {
        var normalized = GeneralHelper.NormalizeIconStyle(styleKey);
        UpdateIconStyleSelectionVisuals(normalized);

        var (ok, msg) = GeneralHelper.SetIconStyle(_store, normalized);
        if (ok)
        {
            (Application.Current as App)?.ApplyIconStyle(normalized);
            ShowNotification(InfoBarSeverity.Success, Loc.Instance.Get("General.Icon.UpdatedTitle"), msg);
        }
        else
        {
            ShowNotification(InfoBarSeverity.Error, Loc.Instance.Get("General.Icon.FailedTitle"), msg);
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
            ShowNotification(InfoBarSeverity.Success, Loc.Instance.Get("General.Storage.OpenedTitle"), msg);
        }
        else
        {
            ShowNotification(InfoBarSeverity.Error, Loc.Instance.Get("General.FolderErrorTitle"), msg);
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
            var accentBrush = Application.Current?.TryFindResource("AccentFillColorDefaultBrush") as Brush
                ?? SystemColors.HighlightBrush;
            card.BorderBrush = accentBrush;
            card.BorderThickness = new Thickness(1.5);
        }
        else
        {
            card.ClearValue(Control.BorderBrushProperty);
            card.ClearValue(Control.BorderThicknessProperty);
        }
    }
}
