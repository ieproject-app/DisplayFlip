using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DisplayFlip.Core;
using DisplayFlip.UI.Helpers;
using Wpf.Ui.Controls;
using WpfButton = Wpf.Ui.Controls.Button;
using WpfTextBlock = Wpf.Ui.Controls.TextBlock;
using SystemVerticalAlignment = System.Windows.VerticalAlignment;
using SystemHorizontalAlignment = System.Windows.HorizontalAlignment;
using SystemOrientation = System.Windows.Controls.Orientation;

namespace DisplayFlip.UI.Views.Pages;

public partial class AboutPage : Page
{
    private readonly ProfileStore _store;

    public AboutPage() : this(new ProfileStore())
    {
    }

    public AboutPage(ProfileStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        InitializeComponent();
        PopulateCliCommands();
        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        PageInfoBar.IsOpen = false;
        PopulateCliCommands();
    }

    private void PopulateCliCommands()
    {
        CliCommandsPanel.Children.Clear();

        var commands = GeneralHelper.GetCliCommands();
        foreach (var cmd in commands)
        {
            var card = CreateCliCommandCard(cmd);
            CliCommandsPanel.Children.Add(card);
        }
    }

    private CardControl CreateCliCommandCard(CliCommandItem cmd)
    {
        var card = new CardControl
        {
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Icon
        card.Icon = new SymbolIcon
        {
            Symbol = GetSymbolForCommand(cmd.Option),
            FontSize = 22
        };

        // Header StackPanel
        var headerStack = new StackPanel();

        // 1. Title Row
        var titleRow = new StackPanel
        {
            Orientation = SystemOrientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 4)
        };

        titleRow.Children.Add(new WpfTextBlock
        {
            FontTypography = FontTypography.BodyStrong,
            Text = cmd.Title,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = SystemVerticalAlignment.Center
        });

        titleRow.Children.Add(new Badge
        {
            Appearance = ControlAppearance.Info,
            Content = cmd.Option,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = SystemVerticalAlignment.Center
        });

        titleRow.Children.Add(new Badge
        {
            Appearance = ControlAppearance.Secondary,
            Content = cmd.Category,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = SystemVerticalAlignment.Center
        });

        if (!string.IsNullOrWhiteSpace(cmd.Aliases))
        {
            titleRow.Children.Add(new WpfTextBlock
            {
                Appearance = TextColor.Secondary,
                FontTypography = FontTypography.Caption,
                Text = $"Alias: {cmd.Aliases}",
                VerticalAlignment = SystemVerticalAlignment.Center
            });
        }

        headerStack.Children.Add(titleRow);

        // 2. Syntax Box
        var syntaxBorder = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 4, 10, 4),
            Margin = new Thickness(0, 4, 0, 6),
            HorizontalAlignment = SystemHorizontalAlignment.Left
        };

        if (Application.Current?.TryFindResource("CardStrokeColorDefaultBrush") is Brush borderBrush)
            syntaxBorder.BorderBrush = borderBrush;
        syntaxBorder.BorderThickness = new Thickness(1);

        if (Application.Current?.TryFindResource("SubtleFillColorSecondaryBrush") is Brush bgBrush)
            syntaxBorder.Background = bgBrush;

        var syntaxText = new System.Windows.Controls.TextBlock
        {
            FontFamily = new FontFamily("Consolas, Courier New, monospace"),
            FontSize = 12.5,
            FontWeight = FontWeights.Medium,
            Text = cmd.Syntax
        };

        if (Application.Current?.TryFindResource("TextFillColorPrimaryBrush") is Brush textBrush)
            syntaxText.Foreground = textBrush;

        syntaxBorder.Child = syntaxText;
        headerStack.Children.Add(syntaxBorder);

        // 3. Description
        headerStack.Children.Add(new WpfTextBlock
        {
            Appearance = TextColor.Secondary,
            Text = cmd.Description,
            TextWrapping = TextWrapping.Wrap
        });

        card.Header = headerStack;

        // Copy Button on the right
        var copyBtn = new WpfButton
        {
            Content = "Salin",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Copy24 },
            Appearance = ControlAppearance.Secondary,
            VerticalAlignment = SystemVerticalAlignment.Center,
            MinWidth = 90
        };

        copyBtn.Click += async (_, _) => await OnCopyCommandClickedAsync(copyBtn, cmd);

        card.Content = copyBtn;

        return card;
    }

    private async Task OnCopyCommandClickedAsync(WpfButton btn, CliCommandItem cmd)
    {
        if (!btn.IsEnabled)
            return;

        bool ok = GeneralHelper.TryCopyToClipboard(cmd.Syntax);
        if (ok)
        {
            btn.IsEnabled = false;
            btn.Content = "Disalin!";
            btn.Icon = new SymbolIcon { Symbol = SymbolRegular.Checkmark24 };
            btn.Appearance = ControlAppearance.Primary;

            ShowNotification(InfoBarSeverity.Success, "Perintah Disalin ke Clipboard",
                $"Perintah '{cmd.Syntax}' telah disalin dan siap dijalankan.");

            await Task.Delay(1800);

            btn.Content = "Salin";
            btn.Icon = new SymbolIcon { Symbol = SymbolRegular.Copy24 };
            btn.Appearance = ControlAppearance.Secondary;
            btn.IsEnabled = true;
        }
        else
        {
            ShowNotification(InfoBarSeverity.Error, "Gagal Menyalin",
                "Akses clipboard Windows sedang terkunci oleh aplikasi lain.");
        }
    }

    private static SymbolRegular GetSymbolForCommand(string option) => option switch
    {
        "--swap" => SymbolRegular.ArrowSwap24,
        "--identify" => SymbolRegular.Eye24,
        "--solo <1|2>" => SymbolRegular.Desktop24,
        "--extend" => SymbolRegular.FullScreenMaximize24,
        "--apply <profile>" => SymbolRegular.Play24,
        "--list" => SymbolRegular.DocumentBulletList24,
        "--settings" => SymbolRegular.Settings24,
        "--tray" => SymbolRegular.Navigation24,
        _ => SymbolRegular.Code24
    };

    // =========================================================================
    // TAUTAN CEPAT & SUMBER DAYA
    // =========================================================================

    public void OnOpenDocsClick(object sender, RoutedEventArgs e)
    {
        const string docsUrl = "https://github.com/akses/multi-monitor";
        var (ok, msg) = GeneralHelper.OpenUrl(docsUrl);
        if (ok)
        {
            ShowNotification(InfoBarSeverity.Success, "Membuka Browser",
                $"Membuka repositori dan dokumentasi proyek di browser: {docsUrl}");
        }
        else
        {
            ShowNotification(InfoBarSeverity.Warning, "Tautan Dokumentasi", msg);
        }
    }

    public void OnOpenAppFolderClick(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = GeneralHelper.OpenAppFolder();
        if (ok)
        {
            ShowNotification(InfoBarSeverity.Success, "Folder Aplikasi Dibuka", msg);
        }
        else
        {
            ShowNotification(InfoBarSeverity.Error, "Gagal Membuka Folder", msg);
        }
    }

    public void OnOpenConfigFileClick(object sender, RoutedEventArgs e)
    {
        var (ok, msg) = GeneralHelper.OpenStorageFolder(_store.FilePath);
        if (ok)
        {
            ShowNotification(InfoBarSeverity.Success, "Folder Profil Dibuka", msg);
        }
        else
        {
            ShowNotification(InfoBarSeverity.Error, "Gagal Membuka Folder", msg);
        }
    }

    private void ShowNotification(InfoBarSeverity severity, string title, string message)
    {
        PageInfoBar.Severity = severity;
        PageInfoBar.Title = title;
        PageInfoBar.Message = message;
        PageInfoBar.IsOpen = true;
    }
}
