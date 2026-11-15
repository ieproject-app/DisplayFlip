using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;
using Wpf.Ui.Controls;
using WpfButton = Wpf.Ui.Controls.Button;
using WpfTextBlock = Wpf.Ui.Controls.TextBlock;
using SystemVerticalAlignment = System.Windows.VerticalAlignment;
using SystemHorizontalAlignment = System.Windows.HorizontalAlignment;

namespace DisplayFlip.UI.Views.Pages;

public partial class ShortcutsPage : Page
{
    private readonly ProfileStore _store = new();

    public ShortcutsPage()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        PageInfoBar.IsOpen = false;
        RefreshShortcutsData();
    }

    private void RefreshShortcutsData()
    {
        var cfg = _store.Load();

        SetButtonKeycapContent(BtnShortcutMirror, cfg.MirrorShortcut);
        SetButtonKeycapContent(BtnShortcutPrimary, cfg.CyclePrimaryShortcut);
        SetButtonKeycapContent(BtnShortcutIdentify, cfg.IdentifyShortcut);
        SetButtonKeycapContent(BtnShortcutSolo1, cfg.Solo1Shortcut);
        SetButtonKeycapContent(BtnShortcutSolo2, cfg.Solo2Shortcut);
        SetButtonKeycapContent(BtnShortcutExtend, cfg.ExtendShortcut);

        ProfilesShortcutsPanel.Children.Clear();

        if (cfg.Profiles.Count == 0)
        {
            EmptyProfilesCard.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyProfilesCard.Visibility = Visibility.Collapsed;

            foreach (var prof in cfg.Profiles)
            {
                string capturedName = prof.Name;
                string shortcut = prof.Shortcut;

                var card = new CardControl
                {
                    Margin = new Thickness(0, 0, 0, 12)
                };

                var headerPanel = new StackPanel();
                headerPanel.Children.Add(new WpfTextBlock
                {
                    FontTypography = FontTypography.BodyStrong,
                    Text = $"Profil: {prof.Name}"
                });

                string cleanPrimary = DisplayHelper.CleanDeviceName(prof.PrimaryStableKey);
                headerPanel.Children.Add(new WpfTextBlock
                {
                    Appearance = TextColor.Secondary,
                    Text = $"{prof.Monitors.Count} monitor • Layar Utama: {cleanPrimary}"
                });

                card.Header = headerPanel;
                card.Icon = new SymbolIcon { Symbol = SymbolRegular.Desktop24 };

                var btn = new WpfButton
                {
                    Appearance = ControlAppearance.Secondary,
                    Padding = new Thickness(12, 6, 12, 6),
                    Tag = "profile:" + capturedName
                };
                btn.Click += OnProfileShortcutClicked;
                SetButtonKeycapContent(btn, shortcut);

                card.Content = btn;
                ProfilesShortcutsPanel.Children.Add(card);
            }
        }
    }

    private static void SetButtonKeycapContent(WpfButton btn, string shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut))
        {
            var stack = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                VerticalAlignment = SystemVerticalAlignment.Center
            };
            stack.Children.Add(new SymbolIcon
            {
                Symbol = SymbolRegular.Add24,
                Margin = new Thickness(0, 0, 6, 0)
            });
            stack.Children.Add(new WpfTextBlock
            {
                Text = "+ Pintasan",
                VerticalAlignment = SystemVerticalAlignment.Center
            });
            btn.Content = stack;
            btn.ToolTip = "Klik untuk mengatur kombinasi tombol pintas";
        }
        else
        {
            btn.Content = ShortcutHelper.CreateKeycapsBadge(shortcut);
            btn.ToolTip = $"Pintasan aktif: {shortcut}. Klik untuk mengubah atau menghapus.";
        }
    }

    private async void OnGlobalShortcutClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string keyKind)
            return;

        var cfg = _store.Load();
        var def = ShortcutHelper.GetGlobalShortcutDefinitions(cfg).FirstOrDefault(d => d.KeyKind == keyKind);
        if (def == null)
            return;

        await OpenShortcutRecorderDialogAsync(keyKind, def.Title, def.CurrentShortcut);
    }

    private async void OnProfileShortcutClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string targetId)
            return;

        string profileName = targetId.StartsWith("profile:") ? targetId.Substring("profile:".Length) : targetId;
        var prof = _store.Find(profileName);
        if (prof == null)
            return;

        await OpenShortcutRecorderDialogAsync(targetId, $"Profil: {prof.Name}", prof.Shortcut);
    }

    private async Task OpenShortcutRecorderDialogAsync(string targetId, string targetTitle, string currentShortcut)
    {
        var win = Window.GetWindow(this);
        var host = win != null ? ContentDialogHost.GetForWindow(win) : null;
        if (host == null)
        {
            ShowInfoBar("Error Dialog", "Tidak dapat membuka modal dialog pintasan.", InfoBarSeverity.Error);
            return;
        }

        var cfg = _store.Load();
        string recordedShortcut = ShortcutHelper.Normalize(currentShortcut);
        bool isShowingPending = false;

        var rootPanel = new StackPanel { MinWidth = 400, MaxWidth = 480 };

        var subTitle = new WpfTextBlock
        {
            Text = $"Target: {targetTitle}\nTekan kombinasi tombol pada keyboard Anda (misal Ctrl+Alt+F atau F9):",
            Appearance = TextColor.Secondary,
            Margin = new Thickness(0, 0, 0, 16),
            TextWrapping = TextWrapping.Wrap
        };
        rootPanel.Children.Add(subTitle);

        var boxBorder = new Border
        {
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(8),
            MinHeight = 56,
            Padding = new Thickness(12, 10, 12, 10),
            Margin = new Thickness(0, 0, 0, 12),
            Focusable = true
        };
        boxBorder.SetResourceReference(Border.BackgroundProperty, "ControlFillColorDefaultBrush");
        boxBorder.SetResourceReference(Border.BorderBrushProperty, "AccentFillColorDefaultBrush");
        boxBorder.MouseDown += (_, _) => boxBorder.Focus();

        var boxContentHost = new ContentPresenter
        {
            HorizontalAlignment = SystemHorizontalAlignment.Center,
            VerticalAlignment = SystemVerticalAlignment.Center
        };
        boxBorder.Child = boxContentHost;
        rootPanel.Children.Add(boxBorder);

        var conflictInfoBar = new InfoBar
        {
            Severity = InfoBarSeverity.Warning,
            IsClosable = false,
            IsOpen = false,
            Margin = new Thickness(0, 0, 0, 12)
        };
        rootPanel.Children.Add(conflictInfoBar);

        var errorBlock = new WpfTextBlock
        {
            Foreground = Brushes.OrangeRed,
            FontTypography = FontTypography.Caption,
            Margin = new Thickness(0, 0, 0, 12),
            Visibility = Visibility.Collapsed
        };
        rootPanel.Children.Add(errorBlock);

        var presetsHeader = new WpfTextBlock
        {
            Text = "Preset Cepat:",
            FontTypography = FontTypography.Caption,
            Appearance = TextColor.Secondary,
            Margin = new Thickness(0, 0, 0, 6)
        };
        rootPanel.Children.Add(presetsHeader);

        var presetsPanel = new WrapPanel { Margin = new Thickness(0, 0, 0, 16) };
        string[] commonPresets = ["Ctrl+Alt+F", "Ctrl+Alt+P", "Ctrl+Alt+1", "Ctrl+Alt+2", "Ctrl+Alt+I"];

        var dialog = new ContentDialog(host)
        {
            Title = "Atur Pintasan Keyboard",
            Content = rootPanel,
            PrimaryButtonText = "Simpan Pintasan",
            SecondaryButtonText = "Hapus Pintasan",
            CloseButtonText = "Batal",
            DefaultButton = ContentDialogButton.Primary
        };
        foreach (var preset in commonPresets)
        {
            string pText = preset;
            var pBtn = new WpfButton
            {
                Content = pText,
                Appearance = ControlAppearance.Secondary,
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(8, 4, 8, 4)
            };
            pBtn.Click += (_, _) =>
            {
                recordedShortcut = pText;
                isShowingPending = false;
                UpdateUi();
            };
            presetsPanel.Children.Add(pBtn);
        }

        var clearBtn = new WpfButton
        {
            Content = "Kosongkan",
            Appearance = ControlAppearance.Secondary,
            Margin = new Thickness(0, 0, 8, 8),
            Padding = new Thickness(8, 4, 8, 4)
        };
        clearBtn.Click += (_, _) =>
        {
            recordedShortcut = string.Empty;
            isShowingPending = false;
            UpdateUi();
        };
        presetsPanel.Children.Add(clearBtn);

        rootPanel.Children.Add(presetsPanel);

        void UpdateUi()
        {
            errorBlock.Visibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(recordedShortcut))
            {
                var placeholder = new WpfTextBlock
                {
                    Text = "(Kosong — Tekan kombinasi tombol...)",
                    FontStyle = FontStyles.Italic,
                    VerticalAlignment = SystemVerticalAlignment.Center
                };
                placeholder.SetResourceReference(WpfTextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
                boxContentHost.Content = placeholder;
                conflictInfoBar.IsOpen = false;
                dialog.IsPrimaryButtonEnabled = true;
            }
            else
            {
                boxContentHost.Content = ShortcutHelper.CreateKeycapsBadge(recordedShortcut);
                dialog.IsPrimaryButtonEnabled = true;

                var conflict = ShortcutHelper.CheckConflict(cfg, targetId, recordedShortcut);
                if (conflict != null)
                {
                    conflictInfoBar.Title = "Pintasan Sudah Digunakan";
                    conflictInfoBar.Message = $"Pintasan '{recordedShortcut}' saat ini digunakan oleh {conflict.TargetName}. Jika disimpan, pintasan pemegang sebelumnya akan otomatis dilepaskan.";
                    conflictInfoBar.IsOpen = true;
                }
                else
                {
                    conflictInfoBar.IsOpen = false;
                }
            }
        }

        void ShowPendingModifiers(ModifierKeys mods, bool isWin)
        {
            var parts = new List<string>(5);
            if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (mods.HasFlag(ModifierKeys.Windows) || isWin) parts.Add("Win");
            parts.Add("?");

            boxContentHost.Content = ShortcutHelper.CreateKeycapsBadge(string.Join("+", parts));
            errorBlock.Visibility = Visibility.Collapsed;
            conflictInfoBar.IsOpen = false;
            dialog.IsPrimaryButtonEnabled = false;
        }

        void ShowError(string err)
        {
            errorBlock.Text = err;
            errorBlock.Visibility = Visibility.Visible;
        }

        UpdateUi();

        dialog.PreviewKeyDown += (_, e) =>
        {
            var key = e.Key == Key.System ? e.SystemKey : (e.Key == Key.ImeProcessed ? e.ImeProcessedKey : e.Key);

            if (key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
                return;
            if (key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
                return;
            if (key == Key.Return && Keyboard.Modifiers == ModifierKeys.None)
                return;
            if (key == Key.Space && Keyboard.Modifiers == ModifierKeys.None && Keyboard.FocusedElement is System.Windows.Controls.Primitives.ButtonBase)
                return;

            e.Handled = true;

            if ((key == Key.Back || key == Key.Delete) && Keyboard.Modifiers == ModifierKeys.None)
            {
                recordedShortcut = string.Empty;
                isShowingPending = false;
                UpdateUi();
                return;
            }

            bool isWin = Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin);
            var mods = Keyboard.Modifiers;

            if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
                or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.None)
            {
                isShowingPending = true;
                ShowPendingModifiers(mods, isWin);
                return;
            }

            if (ShortcutHelper.TryParseWpfKey(key, mods, isWin, out var formatted, out var err))
            {
                recordedShortcut = formatted;
                isShowingPending = false;
                UpdateUi();
            }
            else if (!string.IsNullOrEmpty(err))
            {
                ShowError(err);
            }
        };

        dialog.PreviewKeyUp += (_, _) =>
        {
            if (isShowingPending)
            {
                var remainingMods = Keyboard.Modifiers;
                bool remainingWin = Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin);
                if (remainingMods == ModifierKeys.None && !remainingWin)
                {
                    isShowingPending = false;
                    UpdateUi();
                }
                else
                {
                    ShowPendingModifiers(remainingMods, remainingWin);
                }
            }
        };

        dialog.Loaded += (_, _) => boxBorder.Focus();

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            SaveShortcut(targetId, targetTitle, recordedShortcut);
        }
        else if (result == ContentDialogResult.Secondary)
        {
            SaveShortcut(targetId, targetTitle, string.Empty);
        }
    }

    private void SaveShortcut(string targetId, string targetTitle, string shortcut)
    {
        string? freedFrom = null;
        bool isProfile = targetId.StartsWith("profile:");
        bool changed;

        if (isProfile)
        {
            string profileName = targetId.Substring("profile:".Length);
            (changed, freedFrom) = ShortcutHelper.ApplyProfileShortcut(_store, profileName, shortcut);
        }
        else
        {
            (changed, freedFrom) = ShortcutHelper.ApplyGlobalShortcut(_store, targetId, shortcut);
        }

        if (!changed)
        {
            ShowInfoBar("Gagal Menyimpan", $"Tidak dapat memperbarui pintasan untuk '{targetTitle}'.", InfoBarSeverity.Error);
            return;
        }

        RefreshShortcutsData();

        string msg = string.IsNullOrEmpty(shortcut)
            ? $"Pintasan untuk '{targetTitle}' berhasil dikosongkan."
            : $"Pintasan '{shortcut}' berhasil disimpan untuk '{targetTitle}'.";

        if (!string.IsNullOrEmpty(freedFrom))
        {
            msg += $" (Pintasan otomatis dilepaskan dari {freedFrom}).";
        }

        ShowInfoBar("Pintasan Diperbarui", msg, InfoBarSeverity.Success);
    }

    private void ShowInfoBar(string title, string message, InfoBarSeverity severity)
    {
        PageInfoBar.Title = title;
        PageInfoBar.Message = message;
        PageInfoBar.Severity = severity;
        PageInfoBar.IsOpen = true;
    }
}
