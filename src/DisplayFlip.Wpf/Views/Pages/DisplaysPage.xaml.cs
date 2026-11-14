using System.Windows;
using System.Windows.Controls;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;
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
    private readonly ProfileStore _store = new();
    private readonly ProfileService _profileService;

    private List<DisplayMonitor> _canvasMonitors = new();
    private string? _primaryMonitorKey;
    private string? _selectedMonitorKey;
    private bool _hasUnappliedChanges;
    private bool _isUpdatingUi;

    public DisplaysPage()
    {
        _profileService = new ProfileService(_adapter, _store);

        InitializeComponent();

        MonitorCanvas.MonitorSelected += OnCanvasMonitorSelected;
        MonitorCanvas.ArrangementChanged += OnCanvasArrangementChanged;

        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        RefreshMonitorsFromSystem();
        RefreshProfiles();
    }

    private void RefreshMonitorsFromSystem()
    {
        var monitors = _adapter.ReadTopologies(out _, out _);
        _canvasMonitors = monitors;

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

            BtnApply.IsEnabled = _hasUnappliedChanges;
            BtnReset.IsEnabled = _hasUnappliedChanges;
            MonitorCountBadge.Content = $"{_canvasMonitors.Count} Layar Terdeteksi";

            var sel = _canvasMonitors.FirstOrDefault(m => m.StableKey == _selectedMonitorKey);
            if (sel != null)
            {
                int idx = _canvasMonitors.IndexOf(sel) + 1;
                string numStr = DisplayHelper.ExtractDisplayNumber(sel.StableKey, sel.FriendlyName, idx);
                string cleanName = DisplayHelper.CleanDeviceName(sel.FriendlyName);
                bool isPrimary = sel.StableKey == _primaryMonitorKey || (sel.Position.X == 0 && sel.Position.Y == 0);

                SelectedMonitorTitle.Text = $"Layar {numStr} ({cleanName})";
                SelectedMonitorSubtitle.Text = $"Resolusi {sel.Position.Width} × {sel.Position.Height} • Posisi ({sel.Position.X}, {sel.Position.Y}) • {(isPrimary ? "Layar Utama" : "Layar Sekunder")}";

                BtnSetPrimary.IsEnabled = !isPrimary && _canvasMonitors.Count > 1;
                BtnSoloDisplay.IsEnabled = _canvasMonitors.Count >= 2;
                BtnSoloDisplay.Content = $"Hanya Layar {numStr} Saja";

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

                var supported = _adapter.GetSupportedResolutions(sel);
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
                SelectedMonitorTitle.Text = "Pilih sebuah monitor di kanvas";
                SelectedMonitorSubtitle.Text = "Klik kotak nomor monitor di kanvas atas untuk mengatur properti layar.";
                BtnSetPrimary.IsEnabled = false;
                BtnSoloDisplay.IsEnabled = false;
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

    private void OnCanvasArrangementChanged(object? sender, IReadOnlyList<DisplayMonitor> newArrangement)
    {
        _canvasMonitors = newArrangement.ToList();
        _hasUnappliedChanges = true;
        UpdateUiFromState();
        ShowInfoBar("Susunan Monitor Diubah", "Susunan posisi monitor diperbarui di kanvas. Klik 'Terapkan Perubahan' atau simpan ke profil baru.", InfoBarSeverity.Informational);
    }

    private void OnIdentifyClicked(object sender, RoutedEventArgs e)
    {
        IdentifyOverlayWindow.ShowForMonitors(_canvasMonitors);
    }

    private void OnSnapToggled(object sender, RoutedEventArgs e)
    {
        MonitorCanvas.IsSnapEnabled = ToggleSnap.IsChecked == true;
    }

    private void OnResetClicked(object sender, RoutedEventArgs e)
    {
        RefreshMonitorsFromSystem();
        ShowInfoBar("Tata Letak Direset", "Kanvas dikembalikan ke susunan monitor aktif saat ini.", InfoBarSeverity.Informational);
    }

    private async void OnApplyClicked(object sender, RoutedEventArgs e)
    {
        if (_canvasMonitors.Count == 0)
            return;

        BtnApply.IsEnabled = false;
        var (ok, msg) = await Task.Run(() => _adapter.ApplyArrangement(_canvasMonitors, _primaryMonitorKey));
        if (ok)
        {
            _hasUnappliedChanges = false;
            RefreshMonitorsFromSystem();
            ShowInfoBar("Susunan Diterapkan", "Susunan posisi monitor berhasil diterapkan ke sistem Windows.", InfoBarSeverity.Success);
        }
        else
        {
            BtnApply.IsEnabled = true;
            ShowInfoBar("Gagal Menerapkan Susunan", msg, InfoBarSeverity.Error);
        }
    }

    private void OnSetPrimaryClicked(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedMonitorKey))
            return;

        _primaryMonitorKey = _selectedMonitorKey;
        _canvasMonitors = DisplayTopology.SetPrimary(_canvasMonitors, _selectedMonitorKey);
        _hasUnappliedChanges = true;
        UpdateUiFromState();
        ShowInfoBar("Layar Utama Diubah", "Layar utama telah diubah di kanvas. Klik 'Terapkan Perubahan' untuk menerapkan ke sistem Windows.", InfoBarSeverity.Informational);
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
        int targetDisplay = int.TryParse(numStr, out int dn) && (dn == 1 || dn == 2) ? dn : (idx <= 1 ? 1 : 2);

        var (ok, msg) = ProjectionDispatcher.TriggerSolo(targetDisplay);
        if (ok)
        {
            RefreshMonitorsFromSystem();
            ShowInfoBar("Mode Layar Tunggal (Solo)", msg, InfoBarSeverity.Success);
        }
        else
        {
            ShowInfoBar("Gagal Mode Solo", msg, InfoBarSeverity.Error);
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
            Position = new Position(oldPos.X, oldPos.Y, newW, newH)
        };

        _canvasMonitors = _canvasMonitors
            .Select(m => m.StableKey == _selectedMonitorKey ? updated : m)
            .ToList();

        _canvasMonitors = DisplayTopology.NormalizeArrangement(_canvasMonitors, _primaryMonitorKey);
        _hasUnappliedChanges = true;
        UpdateUiFromState();
        ShowInfoBar("Orientasi Diubah", "Orientasi layar diubah di kanvas. Klik 'Terapkan Perubahan' untuk menerapkan ke sistem.", InfoBarSeverity.Informational);
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
        ShowInfoBar("Resolusi Diubah", "Resolusi layar diubah di kanvas. Klik 'Terapkan Perubahan' untuk menerapkan ke sistem.", InfoBarSeverity.Informational);
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
            string subText = $"{profile.Monitors.Count} Layar • Primary: {primaryName}";
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
                Content = "Terapkan",
                Margin = new Thickness(0, 0, 8, 0)
            };
            applyBtn.Icon = new SymbolIcon { Symbol = SymbolRegular.Play24 };
            applyBtn.Click += async (_, _) => await ApplyProfileAsync(profile);
            actionPanel.Children.Add(applyBtn);

            var deleteBtn = new Wpf.Ui.Controls.Button
            {
                Appearance = ControlAppearance.Secondary,
                Content = "Hapus"
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
        var (ok, msg) = await Task.Run(() => _profileService.TryApply(profile.Name, userOptedContinueIfMissing: true));
        if (ok)
        {
            RefreshMonitorsFromSystem();
            ShowInfoBar("Profil Diterapkan", $"Profil '{profile.Name}' berhasil diterapkan.", InfoBarSeverity.Success);
        }
        else
        {
            ShowInfoBar("Gagal Menerapkan Profil", msg, InfoBarSeverity.Error);
        }
    }

    private async Task DeleteProfileAsync(LayoutProfile profile)
    {
        var win = Window.GetWindow(this);
        var host = win != null ? ContentDialogHost.GetForWindow(win) : null;
        if (host == null)
            return;

        var dialog = new ContentDialog(host)
        {
            Title = "Hapus Profil",
            Content = new Wpf.Ui.Controls.TextBlock
            {
                Text = $"Apakah Anda yakin ingin menghapus profil tata letak '{profile.Name}'? Tindakan ini tidak dapat dibatalkan.",
                TextWrapping = TextWrapping.Wrap
            },
            PrimaryButtonText = "Hapus",
            PrimaryButtonAppearance = ControlAppearance.Danger,
            CloseButtonText = "Batal",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            _store.Delete(profile.Name);
            RefreshProfiles();
            ShowInfoBar("Profil Dihapus", $"Profil '{profile.Name}' telah dihapus dari daftar.", InfoBarSeverity.Success);
        }
    }

    private async void OnSaveNewProfileClicked(object sender, RoutedEventArgs e)
    {
        var win = Window.GetWindow(this);
        var host = win != null ? ContentDialogHost.GetForWindow(win) : null;
        if (host == null)
            return;

        var nameBox = new Wpf.Ui.Controls.TextBox
        {
            PlaceholderText = "Contoh: Kerja Dual Monitor",
            Margin = new Thickness(0, 12, 0, 8),
            MinWidth = 320
        };

        var contentPanel = new StackPanel();
        contentPanel.Children.Add(new Wpf.Ui.Controls.TextBlock
        {
            Text = "Masukkan nama untuk profil tata letak monitor saat ini:"
        });
        contentPanel.Children.Add(nameBox);

        var dialog = new ContentDialog(host)
        {
            Title = "Simpan Profil Baru",
            Content = contentPanel,
            PrimaryButtonText = "Simpan",
            CloseButtonText = "Batal",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            string profileName = nameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(profileName))
            {
                ShowInfoBar("Nama Profil Kosong", "Nama profil tidak boleh kosong.", InfoBarSeverity.Warning);
                return;
            }

            if (profileName.Length > 100)
            {
                ShowInfoBar("Nama Terlalu Panjang", "Nama profil maksimal 100 karakter.", InfoBarSeverity.Warning);
                return;
            }

            try
            {
                _profileService.SaveArrangementAs(profileName, _canvasMonitors, _primaryMonitorKey);
                RefreshProfiles();
                ShowInfoBar("Profil Disimpan", $"Profil tata letak '{profileName}' berhasil disimpan.", InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                ShowInfoBar("Gagal Menyimpan Profil", ex.Message, InfoBarSeverity.Error);
            }
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
