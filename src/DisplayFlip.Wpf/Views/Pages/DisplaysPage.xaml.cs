using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;
using DisplayFlip.UI.Services;
using DisplayFlip.Windows;
using Wpf.Ui.Controls;
using CoreOrientation = DisplayFlip.Core.Orientation;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace DisplayFlip.UI.Views.Pages;

public partial class DisplaysPage : Page
{
    public sealed record ResolutionOption(int Width, int Height)
    {
        public override string ToString() => $"{Width} × {Height}";
    }

    private readonly CcdTopologyAdapter _adapter = new();
    private readonly ProfileStore _store;
    private readonly ProfileService _profileService;

    private List<DisplayMonitor> _canvasMonitors = new();
    private readonly Dictionary<string, List<(int Width, int Height)>> _supportedResolutionsCache = new();
    private string? _primaryMonitorKey;
    private string? _selectedMonitorKey;
    private bool _hasUnappliedChanges;
    private bool _isUpdatingUi;
    private bool _isApplyingArrangement;
    private bool _pendingReapply;

    public bool HasUnappliedChanges => _hasUnappliedChanges;
    public bool IsApplyingArrangement => _isApplyingArrangement;
    public IReadOnlyList<DisplayMonitor> CanvasMonitors => _canvasMonitors;

    public DisplaysPage() : this(new ProfileStore())
    {
    }

    internal DisplaysPage(ProfileStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _profileService = new ProfileService(_adapter, _store);

        InitializeComponent();

        MonitorCanvas.MonitorSelected += OnCanvasMonitorSelected;
        MonitorCanvas.ArrangementChanged += OnCanvasArrangementChanged;

        Loaded += OnPageLoaded;
        Unloaded += OnPageUnloaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        Loc.Instance.LanguageChanged -= OnLanguageChanged;
        Loc.Instance.LanguageChanged += OnLanguageChanged;
        RefreshData(resetCanvasToLive: true);
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        Loc.Instance.LanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (!IsLoaded)
            return;
        // Preserve pending canvas drag edits (HasUnappliedChanges) — refresh text & profile cards only.
        UpdateUiFromState();
        RefreshProfiles();
        MonitorCanvas?.InvalidateVisual();
    }

    public void RefreshData(bool resetCanvasToLive = true)
    {
        if (resetCanvasToLive && !_hasUnappliedChanges && !_isApplyingArrangement)
        {
            RefreshMonitorsFromSystem();
        }
        else
        {
            UpdateUiFromState();
        }
        RefreshProfiles();
    }

    public void RefreshMonitorsFromSystem()
    {
        var monitors = _adapter.ReadTopologies(out _, out _);
        _canvasMonitors = monitors;
        _supportedResolutionsCache.Clear();

        var primary = monitors.FirstOrDefault(m => m.Position.X == 0 && m.Position.Y == 0)
                      ?? monitors.FirstOrDefault();
        _primaryMonitorKey = primary?.StableKey;

        if (string.IsNullOrEmpty(_selectedMonitorKey) || !_canvasMonitors.Any(m => m.StableKey == _selectedMonitorKey))
        {
            _selectedMonitorKey = _primaryMonitorKey;
        }

        _hasUnappliedChanges = false;
        UpdateUiFromState();
    }

    private void UpdateUiFromState()
    {
        _isUpdatingUi = true;
        try
        {
            MonitorCanvas.Monitors = _canvasMonitors;
            MonitorCanvas.PrimaryMonitorKey = _primaryMonitorKey;
            MonitorCanvas.SelectedMonitorKey = _selectedMonitorKey;
            MonitorCanvas.IsSnapEnabled = ToggleSnap.IsChecked == true;
            MonitorCanvas.ShowMonitorNames = ToggleMonitorNames?.IsChecked == true;

            BtnApply.IsEnabled = _hasUnappliedChanges && !_isApplyingArrangement;
            BtnReset.IsEnabled = _hasUnappliedChanges && !_isApplyingArrangement;

            if (_canvasMonitors.Count == 0)
            {
                CanvasSubtitleText.Text = Loc.Instance.Get("Displays.Canvas.NoMonitorsSubtitle");
            }
            else
            {
                CanvasSubtitleText.Text = Loc.Instance.Format("Displays.Canvas.ActiveSubtitle", _canvasMonitors.Count);
            }

            var sel = _canvasMonitors.FirstOrDefault(m => m.StableKey == _selectedMonitorKey);
            if (sel != null)
            {
                int idx = _canvasMonitors.IndexOf(sel) + 1;
                string numStr = DisplayHelper.ExtractDisplayNumber(sel.StableKey, sel.FriendlyName, idx);
                bool isPrimary = sel.StableKey == _primaryMonitorKey || (sel.Position.X == 0 && sel.Position.Y == 0);

                int hz = DisplayHelper.GetCurrentRefreshRate(sel.StableKey, sel.FriendlyName);
                string hzText = hz > 0 ? $" @ {hz} Hz" : string.Empty;

                SelectedMonitorTitle.Text = DisplayHelper.GetShortDisplayName(sel.FriendlyName, sel.StableKey, idx);
                string roleText = Loc.Instance.Get(isPrimary ? "Displays.Inspector.PrimaryRole" : "Displays.Inspector.SecondaryRole");
                SelectedMonitorSubtitle.Text = Loc.Instance.Format(
                    "Displays.Inspector.DetailsFormat",
                    sel.Position.Width,
                    sel.Position.Height,
                    hzText,
                    sel.Position.X,
                    sel.Position.Y,
                    roleText);

                BtnSetPrimary.IsEnabled = !isPrimary && _canvasMonitors.Count > 1;

                bool isEligibleSolo = int.TryParse(numStr, out int dn) ? (dn == 1 || dn == 2) : (idx <= 2);
                BtnSoloDisplay.IsEnabled = _canvasMonitors.Count >= 2 && isEligibleSolo;
                BtnSoloDisplay.Content = isEligibleSolo
                    ? Loc.Instance.Format("Displays.Inspector.SoloNumbered", numStr)
                    : Loc.Instance.Get("Displays.Inspector.SoloDefault");

                // Orientasi
                OrientationComboBox.IsEnabled = true;
                OrientationComboBox.SelectedIndex = sel.Orientation switch
                {
                    CoreOrientation.Landscape => 0,
                    CoreOrientation.Portrait => 1,
                    CoreOrientation.Rotated => 2,
                    CoreOrientation.Unhandled => 3,
                    _ => 0
                };

                // Resolusi yang didukung
                ResolutionComboBox.IsEnabled = true;
                ResolutionComboBox.Items.Clear();

                if (!_supportedResolutionsCache.TryGetValue(sel.StableKey, out var cachedRes))
                {
                    cachedRes = _adapter.GetSupportedResolutions(sel);
                    _supportedResolutionsCache[sel.StableKey] = cachedRes;
                }
                var supported = new List<(int Width, int Height)>(cachedRes);
                if (!supported.Any(r => r.Width == sel.Position.Width && r.Height == sel.Position.Height))
                {
                    supported.Insert(0, (sel.Position.Width, sel.Position.Height));
                }

                ResolutionOption? matchedOption = null;
                foreach (var (rw, rh) in supported)
                {
                    var opt = new ResolutionOption(rw, rh);
                    ResolutionComboBox.Items.Add(opt);
                    if (rw == sel.Position.Width && rh == sel.Position.Height && matchedOption == null)
                    {
                        matchedOption = opt;
                    }
                }
                ResolutionComboBox.SelectedItem = matchedOption ?? ResolutionComboBox.Items.Cast<ResolutionOption>().FirstOrDefault();
            }
            else
            {
                SelectedMonitorTitle.Text = Loc.Instance.Get("Displays.Inspector.NoSelection");
                SelectedMonitorSubtitle.Text = Loc.Instance.Get("Displays.Inspector.NoSelectionSubtitle");
                BtnSetPrimary.IsEnabled = false;
                BtnSoloDisplay.IsEnabled = false;
                BtnSoloDisplay.Content = Loc.Instance.Get("Displays.Inspector.SoloDefault");
                OrientationComboBox.IsEnabled = false;
                ResolutionComboBox.IsEnabled = false;
                OrientationComboBox.SelectedIndex = -1;
                ResolutionComboBox.Items.Clear();
            }
        }
        finally
        {
            _isUpdatingUi = false;
        }
    }

    private void OnCanvasMonitorSelected(object? sender, string stableKey)
    {
        _selectedMonitorKey = stableKey;
        UpdateUiFromState();
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key is System.Windows.Input.Key.Left or System.Windows.Input.Key.Right or System.Windows.Input.Key.Up or System.Windows.Input.Key.Down)
        {
            if (!IsTextInputOrComboFocused())
            {
                if (MonitorCanvas != null && MonitorCanvas.Nudge(e.Key))
                {
                    e.Handled = true;
                }
            }
        }
    }

    private static bool IsTextInputOrComboFocused()
    {
        var focused = System.Windows.Input.Keyboard.FocusedElement;
        if (focused == null) return false;
        if (focused is System.Windows.Controls.TextBox || focused is Wpf.Ui.Controls.TextBox ||
            focused is System.Windows.Controls.ComboBox || focused is System.Windows.Controls.ComboBoxItem ||
            focused is System.Windows.Controls.Primitives.TextBoxBase)
        {
            return true;
        }

        if (focused is DependencyObject d)
        {
            var parent = VisualTreeHelper.GetParent(d);
            while (parent != null)
            {
                if (parent is System.Windows.Controls.ComboBox || parent is System.Windows.Controls.TextBox)
                    return true;
                parent = VisualTreeHelper.GetParent(parent);
            }
        }
        return false;
    }

    private async void OnCanvasArrangementChanged(object? sender, IReadOnlyList<DisplayMonitor> newArrangement)
    {
        _canvasMonitors = newArrangement.ToList();
        _hasUnappliedChanges = true;
        UpdateUiFromState();

        if (GeneralHelper.GetAutoApplyDrag(_store))
        {
            await ApplyArrangementSerializedAsync(
                Loc.Instance.Get("Displays.Info.AutoAppliedTitle"),
                Loc.Instance.Get("Displays.Info.AutoAppliedMsg"));
        }
        else
        {
            ShowInfoBar(
                Loc.Instance.Get("Displays.Info.ArrangementChangedTitle"),
                Loc.Instance.Get("Displays.Info.ArrangementChangedMsg"),
                InfoBarSeverity.Informational);
        }
    }

    private async Task ApplyArrangementSerializedAsync(string successTitle, string successMessage)
    {
        if (_canvasMonitors.Count == 0)
            return;

        if (_isApplyingArrangement)
        {
            _pendingReapply = true;
            return;
        }

        _isApplyingArrangement = true;
        UpdateUiFromState();
        try
        {
            bool appliedOk = false;

            do
            {
                _pendingReapply = false;
                var snapshotMonitors = _canvasMonitors.ToList();
                var snapshotPrimaryKey = _primaryMonitorKey;

                _hasUnappliedChanges = false;
                UpdateUiFromState();

                var (ok, msg) = await Task.Run(() => _adapter.ApplyArrangement(snapshotMonitors, snapshotPrimaryKey));
                appliedOk = ok;

                if (!ok)
                {
                    _hasUnappliedChanges = true;
                    UpdateUiFromState();
                    ShowInfoBar(Loc.Instance.Get("Displays.Info.ApplyFailedTitle"), msg, InfoBarSeverity.Error);
                    break;
                }

                await Task.Delay(150);
            }
            while (_pendingReapply);

            if (appliedOk)
            {
                RefreshMonitorsFromSystem();
                ShowInfoBar(successTitle, successMessage, InfoBarSeverity.Success);
            }
        }
        finally
        {
            _isApplyingArrangement = false;
            UpdateUiFromState();
        }
    }

    private void OnIdentifyClicked(object sender, RoutedEventArgs e)
    {
        IdentifyOverlayWindow.ShowForMonitors(_canvasMonitors);
    }

    private void OnSnapToggled(object sender, RoutedEventArgs e)
    {
        if (MonitorCanvas != null)
            MonitorCanvas.IsSnapEnabled = ToggleSnap.IsChecked == true;
    }

    private void OnMonitorNamesToggled(object sender, RoutedEventArgs e)
    {
        if (MonitorCanvas != null)
            MonitorCanvas.ShowMonitorNames = ToggleMonitorNames.IsChecked == true;
    }

    private void OnResetClicked(object sender, RoutedEventArgs e)
    {
        RefreshMonitorsFromSystem();
        ShowInfoBar(
            Loc.Instance.Get("Displays.Info.ResetTitle"),
            Loc.Instance.Get("Displays.Info.ResetMsg"),
            InfoBarSeverity.Informational);
    }

    private async void OnApplyClicked(object sender, RoutedEventArgs e)
    {
        await ApplyArrangementSerializedAsync(
            Loc.Instance.Get("Displays.Info.AppliedTitle"),
            Loc.Instance.Get("Displays.Info.AppliedMsg"));
    }

    private async void OnSetPrimaryClicked(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedMonitorKey))
            return;

        _primaryMonitorKey = _selectedMonitorKey;
        _canvasMonitors = DisplayTopology.NormalizeArrangement(_canvasMonitors, _primaryMonitorKey);
        _hasUnappliedChanges = true;
        UpdateUiFromState();

        if (GeneralHelper.GetAutoApplyDrag(_store))
        {
            await ApplyArrangementSerializedAsync(
                Loc.Instance.Get("Displays.Info.PrimaryAppliedTitle"),
                Loc.Instance.Get("Displays.Info.PrimaryAppliedMsg"));
        }
        else
        {
            ShowInfoBar(
                Loc.Instance.Get("Displays.Info.PrimaryChangedTitle"),
                Loc.Instance.Get("Displays.Info.PrimaryChangedMsg"),
                InfoBarSeverity.Informational);
        }
    }

    private void OnSoloDisplayClicked(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedMonitorKey))
            return;

        var sel = _canvasMonitors.FirstOrDefault(m => m.StableKey == _selectedMonitorKey);
        if (sel == null)
            return;

        int idx = _canvasMonitors.IndexOf(sel) + 1;
        string numStr = DisplayHelper.ExtractDisplayNumber(sel.StableKey, sel.FriendlyName, idx);
        if (!int.TryParse(numStr, out int targetDisplay) || (targetDisplay != 1 && targetDisplay != 2))
        {
            targetDisplay = idx <= 1 ? 1 : 2;
        }

        var (ok, msg) = ProjectionDispatcher.TriggerSolo(targetDisplay, Loc.Instance.EffectiveLanguage);
        if (ok)
        {
            RefreshMonitorsFromSystem();
            ShowInfoBar(Loc.Instance.Get("Displays.Info.SoloUpdatedTitle"), msg, InfoBarSeverity.Success);
        }
        else
        {
            ShowInfoBar(Loc.Instance.Get("Displays.Info.SoloFailedTitle"), msg, InfoBarSeverity.Error);
        }
    }

    private void OnOrientationSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || string.IsNullOrEmpty(_selectedMonitorKey))
            return;

        var sel = _canvasMonitors.FirstOrDefault(m => m.StableKey == _selectedMonitorKey);
        if (sel == null)
            return;

        CoreOrientation newOrient = OrientationComboBox.SelectedIndex switch
        {
            0 => CoreOrientation.Landscape,
            1 => CoreOrientation.Portrait,
            2 => CoreOrientation.Rotated,
            3 => CoreOrientation.Unhandled,
            _ => CoreOrientation.Landscape
        };

        if (sel.Orientation == newOrient)
            return;

        var oldPos = sel.Position;
        bool wasPortrait = oldPos.Height > oldPos.Width;
        bool wantPortrait = newOrient is CoreOrientation.Portrait or CoreOrientation.Unhandled;
        int newW = wantPortrait != wasPortrait ? oldPos.Height : oldPos.Width;
        int newH = wantPortrait != wasPortrait ? oldPos.Width : oldPos.Height;

        var updated = sel with
        {
            Orientation = newOrient,
            ResolutionWidth = newW,
            ResolutionHeight = newH,
            Position = new Position(oldPos.X, oldPos.Y, newW, newH)
        };

        _canvasMonitors = _canvasMonitors
            .Select(m => m.StableKey == _selectedMonitorKey ? updated : m)
            .ToList();

        _canvasMonitors = DisplayTopology.NormalizeArrangement(_canvasMonitors, _primaryMonitorKey);
        _hasUnappliedChanges = true;
        UpdateUiFromState();
        ShowInfoBar(
            Loc.Instance.Get("Displays.Info.OrientationChangedTitle"),
            Loc.Instance.Get("Displays.Info.OrientationChangedMsg"),
            InfoBarSeverity.Informational);
    }

    private void OnResolutionSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingUi || string.IsNullOrEmpty(_selectedMonitorKey))
            return;

        var sel = _canvasMonitors.FirstOrDefault(m => m.StableKey == _selectedMonitorKey);
        if (sel == null || ResolutionComboBox.SelectedItem is not ResolutionOption selectedRes)
            return;

        if (sel.Position.Width == selectedRes.Width && sel.Position.Height == selectedRes.Height)
            return;

        var updated = sel with
        {
            ResolutionWidth = selectedRes.Width,
            ResolutionHeight = selectedRes.Height,
            Position = new Position(sel.Position.X, sel.Position.Y, selectedRes.Width, selectedRes.Height)
        };

        _canvasMonitors = _canvasMonitors
            .Select(m => m.StableKey == _selectedMonitorKey ? updated : m)
            .ToList();

        _canvasMonitors = DisplayTopology.NormalizeArrangement(_canvasMonitors, _primaryMonitorKey);
        _hasUnappliedChanges = true;
        UpdateUiFromState();
        ShowInfoBar(
            Loc.Instance.Get("Displays.Info.ResolutionChangedTitle"),
            Loc.Instance.Get("Displays.Info.ResolutionChangedMsg"),
            InfoBarSeverity.Informational);
    }

    #region Manajemen Profil Tampilan

    public void RefreshProfiles()
    {
        ProfilesContainer.Children.Clear();
        var file = _store.Load();
        var profiles = file.Profiles;

        if (profiles.Count == 0)
        {
            ProfilesContainer.Children.Add(EmptyProfilesCard);
            return;
        }

        foreach (var profile in profiles)
        {
            var card = new CardControl
            {
                Margin = new Thickness(0, 0, 0, 10)
            };

            var icon = new SymbolIcon
            {
                Symbol = SymbolRegular.Save24
            };
            card.Icon = icon;

            var headerPanel = new StackPanel();
            var titleBlock = new Wpf.Ui.Controls.TextBlock
            {
                FontTypography = FontTypography.BodyStrong,
                Text = profile.Name
            };
            headerPanel.Children.Add(titleBlock);

            string dateInfo = !string.IsNullOrWhiteSpace(profile.UpdatedAt)
                ? (DateTime.TryParse(profile.UpdatedAt, out var dt) ? dt.ToLocalTime().ToString("dd MMM yyyy HH:mm") : profile.UpdatedAt)
                : string.Empty;

            string primaryName = DisplayHelper.CleanDeviceName(profile.PrimaryStableKey);
            string subText = Loc.Instance.Format("Displays.Profiles.CardMeta", profile.Monitors.Count, primaryName);
            if (!string.IsNullOrWhiteSpace(dateInfo))
                subText += $" • {dateInfo}";

            var subBlock = new Wpf.Ui.Controls.TextBlock
            {
                Appearance = TextColor.Secondary,
                Text = subText
            };
            headerPanel.Children.Add(subBlock);
            card.Header = headerPanel;

            var actionPanel = new StackPanel
            {
                Orientation = WpfOrientation.Horizontal
            };

            var applyBtn = new Wpf.Ui.Controls.Button
            {
                Appearance = ControlAppearance.Primary,
                Content = Loc.Instance.Get("Displays.Profiles.ApplyBtn"),
                Margin = new Thickness(0, 0, 8, 0)
            };
            applyBtn.Icon = new SymbolIcon { Symbol = SymbolRegular.Play24 };
            applyBtn.Click += async (_, _) => await ApplyProfileAsync(profile);
            actionPanel.Children.Add(applyBtn);

            var deleteBtn = new Wpf.Ui.Controls.Button
            {
                Appearance = ControlAppearance.Secondary,
                Content = Loc.Instance.Get("Displays.Profiles.DeleteBtn")
            };
            deleteBtn.Icon = new SymbolIcon { Symbol = SymbolRegular.Delete24 };
            deleteBtn.Click += async (_, _) => await DeleteProfileAsync(profile);
            actionPanel.Children.Add(deleteBtn);

            card.Content = actionPanel;
            ProfilesContainer.Children.Add(card);
        }
    }

    private async Task ApplyProfileAsync(LayoutProfile profile)
    {
        var (ok, isPartial, msg) = await Task.Run(() => DisplayActionService.Instance.ApplyProfileDetailed(profile.Name));
        if (ok)
        {
            RefreshMonitorsFromSystem();
            ShowInfoBar(
                Loc.Instance.Get(isPartial ? "Displays.Info.ProfileAppliedPartialTitle" : "Displays.Info.ProfileAppliedTitle"),
                msg,
                isPartial ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
        }
        else
        {
            ShowInfoBar(Loc.Instance.Get("Displays.Info.ProfileApplyFailedTitle"), msg, InfoBarSeverity.Error);
        }
    }

    private async Task DeleteProfileAsync(LayoutProfile profile)
    {
        var win = Window.GetWindow(this);
        var host = win != null ? ContentDialogHost.GetForWindow(win) : null;
        if (host == null)
        {
            ShowInfoBar(
                Loc.Instance.Get("Displays.Info.DialogErrorTitle"),
                Loc.Instance.Get("Displays.Info.DeleteDialogErrorMsg"),
                InfoBarSeverity.Error);
            return;
        }

        var dialog = new ContentDialog(host)
        {
            Title = Loc.Instance.Get("Displays.Dialog.Delete.Title"),
            Content = new Wpf.Ui.Controls.TextBlock
            {
                Text = Loc.Instance.Format("Displays.Dialog.Delete.Content", profile.Name),
                TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = Loc.Instance.Get("Displays.Dialog.Delete.PrimaryBtn"),
            PrimaryButtonAppearance = ControlAppearance.Danger,
            CloseButtonText = Loc.Instance.Get("Displays.Dialog.Delete.CloseBtn"),
            DefaultButton = ContentDialogButton.Close
        };

        try
        {
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _store.Delete(profile.Name);
                GlobalHotkeyManager.Instance.ReloadHotkeys();
                RefreshProfiles();
                ShowInfoBar(
                    Loc.Instance.Get("Displays.Info.ProfileDeletedTitle"),
                    Loc.Instance.Format("Displays.Info.ProfileDeletedMsg", profile.Name),
                    InfoBarSeverity.Success);
            }
        }
        catch (Exception ex)
        {
            ShowInfoBar(Loc.Instance.Get("Displays.Info.DeleteDialogExceptionTitle"), ex.Message, InfoBarSeverity.Error);
        }
    }

    private async void OnSaveNewProfileClicked(object sender, RoutedEventArgs e)
    {
        var win = Window.GetWindow(this);
        var host = win != null ? ContentDialogHost.GetForWindow(win) : null;
        if (host == null)
        {
            ShowInfoBar(
                Loc.Instance.Get("Displays.Info.DialogErrorTitle"),
                Loc.Instance.Get("Displays.Info.SaveDialogErrorMsg"),
                InfoBarSeverity.Error);
            return;
        }

        var nameBox = new Wpf.Ui.Controls.TextBox
        {
            PlaceholderText = Loc.Instance.Get("Displays.Dialog.Save.Placeholder"),
            Margin = new Thickness(0, 12, 0, 8),
            MinWidth = 320
        };

        var contentPanel = new StackPanel();
        contentPanel.Children.Add(new Wpf.Ui.Controls.TextBlock
        {
            Text = Loc.Instance.Get("Displays.Dialog.Save.Prompt")
        });
        contentPanel.Children.Add(nameBox);

        var dialog = new ContentDialog(host)
        {
            Title = Loc.Instance.Get("Displays.Dialog.Save.Title"),
            Content = contentPanel,
            PrimaryButtonText = Loc.Instance.Get("Displays.Dialog.Save.PrimaryBtn"),
            CloseButtonText = Loc.Instance.Get("Displays.Dialog.Save.CloseBtn"),
            DefaultButton = ContentDialogButton.Primary
        };

        try
        {
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                string profileName = nameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(profileName))
                {
                    ShowInfoBar(
                        Loc.Instance.Get("Displays.Info.EmptyNameTitle"),
                        Loc.Instance.Get("Displays.Info.SaveEmptyName"),
                        InfoBarSeverity.Warning);
                    return;
                }

                if (profileName.Length > 100)
                {
                    ShowInfoBar(
                        Loc.Instance.Get("Displays.Info.NameTooLongTitle"),
                        Loc.Instance.Get("Displays.Info.NameTooLongMsg"),
                        InfoBarSeverity.Warning);
                    return;
                }

                try
                {
                    _profileService.SaveArrangementAs(profileName, _canvasMonitors, _primaryMonitorKey);
                    RefreshProfiles();
                    ShowInfoBar(
                        Loc.Instance.Get("Displays.Info.ProfileSavedTitle"),
                        Loc.Instance.Format("Displays.Info.ProfileSavedMsg", profileName),
                        InfoBarSeverity.Success);
                }
                catch (Exception ex)
                {
                    ShowInfoBar(Loc.Instance.Get("Displays.Info.ProfileSaveFailedTitle"), ex.Message, InfoBarSeverity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            ShowInfoBar(Loc.Instance.Get("Displays.Info.SaveDialogExceptionTitle"), ex.Message, InfoBarSeverity.Error);
        }
    }

    #endregion

    private void ShowInfoBar(string title, string message, InfoBarSeverity severity)
    {
        PageInfoBar.Title = title;
        PageInfoBar.Message = message;
        PageInfoBar.Severity = severity;
        PageInfoBar.IsOpen = true;
    }
}
