using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DisplayFlip.UI.Helpers;
using DisplayFlip.UI.Services;
using DisplayFlip.UI.Views;
using DisplayFlip.UI.Views.Pages;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using TextBlock = Wpf.Ui.Controls.TextBlock;

namespace DisplayFlip.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private SingleInstanceService? _singleInstance;
    private TrayIconManager? _trayManager;
    private GlobalHotkeyManager? _hotkeyManager;
    private SettingsWindow? _mainWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Ekspor Screenshot jika diminta
        if (e.Args.Contains("--export-screenshots"))
        {
            await ExportScreenshotsAsync();
            Shutdown(0);
            return;
        }

        // 2. Single-Instance Guard
        _singleInstance = new SingleInstanceService();
        if (!_singleInstance.IsFirstInstance)
        {
            // Jika ada argumen aksi CLI, jalankan perintah langsung dan keluar
            if (CliRunner.IsCliAction(e.Args))
            {
                int exitCode = CliRunner.Run(e.Args);
                _singleInstance.Dispose();
                Shutdown(exitCode);
                return;
            }

            // Jika dibuka tanpa argumen (atau --settings), beri tahu instans pertama untuk aktif ke foreground
            SingleInstanceService.SignalRunningInstance();
            _singleInstance.Dispose();
            Shutdown(0);
            return;
        }

        // 3. Jika instans pertama menerima argumen aksi CLI (misal --swap langsung dari terminal)
        if (CliRunner.IsCliAction(e.Args))
        {
            int exitCode = CliRunner.Run(e.Args);
            _singleInstance.Dispose();
            Shutdown(exitCode);
            return;
        }

        // 4. Aplikasi residen di latar belakang: jangan shutdown saat jendela disembunyikan
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 5. Inisialisasi System Tray & Hotkey Listener
        _trayManager = new TrayIconManager();
        _hotkeyManager = GlobalHotkeyManager.Instance;
        _hotkeyManager.ReloadHotkeys();

        // 6. Buat Jendela Utama Settings
        _mainWindow = new SettingsWindow();

        // Callback sinyal IPC instans kedua -> munculkan jendela ke depan
        _singleInstance.OnWakeupRequested = () =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                _mainWindow?.BringToForeground();
            });
        };

        // Callback Tray: Klik Kiri -> Toggle / Restore jendela
        _trayManager.ToggleOrRestoreWindowAction = () =>
        {
            if (_mainWindow == null)
                return;

            if (_mainWindow.IsVisible && _mainWindow.WindowState != WindowState.Minimized && _mainWindow.IsActive)
            {
                _mainWindow.Hide();
            }
            else
            {
                _mainWindow.BringToForeground();
            }
        };

        // Callback Tray: Buka Pengaturan
        _trayManager.OpenSettingsAction = () =>
        {
            _mainWindow?.BringToForeground();
        };

        // Callback Tray: Keluar
        _trayManager.ExitApplicationAction = () =>
        {
            ExitApplication();
        };

        // 7. Cek apakah aplikasi dimulai dalam mode senyap (--tray atau --minimized)
        bool startInTray = e.Args.Contains("--tray") || e.Args.Contains("--minimized");
        if (!startInTray)
        {
            _mainWindow.BringToForeground();
        }
    }

    /// <summary>
    /// Menerapkan preferensi gaya ikon ke seluruh tampilan aplikasi (Tray, TitleBar, Taskbar).
    /// </summary>
    public void ApplyIconStyle(string style)
    {
        _trayManager?.UpdateIcon(style);
        _mainWindow?.UpdateIconStyle(style);
    }

    /// <summary>
    /// Menutup aplikasi DisplayFlip secara eksplisit dan membersihkan seluruh resource sistem.
    /// </summary>
    public void ExitApplication()
    {
        _mainWindow?.AllowExplicitExit();
        _mainWindow?.Close();

        _trayManager?.Dispose();
        _trayManager = null;

        _hotkeyManager?.Dispose();
        _hotkeyManager = null;

        _singleInstance?.Dispose();
        _singleInstance = null;

        Shutdown(0);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayManager?.Dispose();
        _hotkeyManager?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private async Task ExportScreenshotsAsync()
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "docs", "screenshots");
        outDir = Path.GetFullPath(outDir);
        Directory.CreateDirectory(outDir);

        // LIGHT MODE RUN
        ApplicationThemeManager.Apply(ApplicationTheme.Light);
        var win = new SettingsWindow();
        win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        win.Width = 1000;
        win.Height = 720;
        win.Show();
        await Task.Delay(1000);

        // 1. Layar & Profil (Light)
        win.RootNavigation.Navigate(typeof(DisplaysPage));
        await Task.Delay(800);
        SaveScreenshot(win, Path.Combine(outDir, "01_layar_dan_profil_light.png"));

        // 2. Pintasan Keyboard (Light)
        win.RootNavigation.Navigate(typeof(ShortcutsPage));
        await Task.Delay(800);
        SaveScreenshot(win, Path.Combine(outDir, "02_pintasan_keyboard_light.png"));

        // 3. Umum & Ikon (Light)
        win.RootNavigation.Navigate(typeof(GeneralPage));
        await Task.Delay(800);
        SaveScreenshot(win, Path.Combine(outDir, "03_umum_dan_ikon_light.png"));

        // 4. Tentang & CLI (Light)
        win.RootNavigation.Navigate(typeof(AboutPage));
        await Task.Delay(800);
        SaveScreenshot(win, Path.Combine(outDir, "04_tentang_dan_cli_light.png"));
        win.AllowExplicitExit();
        win.Close();

        // DARK MODE RUN: Set theme BEFORE creating the window so entire visual tree & Mica init in Dark
        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Mica, true);
        var winDark = new SettingsWindow(false);
        winDark.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        winDark.Width = 1000;
        winDark.Height = 720;
        winDark.Show();
        await Task.Delay(1000);

        // 5. Dark Mode: Layar & Profil
        winDark.RootNavigation.Navigate(typeof(DisplaysPage));
        await Task.Delay(800);
        SaveScreenshot(winDark, Path.Combine(outDir, "05_layar_dan_profil_dark.png"));

        // 6. Dark Mode: Pintasan Keyboard
        winDark.RootNavigation.Navigate(typeof(ShortcutsPage));
        await Task.Delay(800);
        SaveScreenshot(winDark, Path.Combine(outDir, "06_pintasan_keyboard_dark.png"));

        // 7. Dark Mode: Umum & Ikon
        winDark.RootNavigation.Navigate(typeof(GeneralPage));
        await Task.Delay(800);
        SaveScreenshot(winDark, Path.Combine(outDir, "07_umum_dan_ikon_dark.png"));

        // 8. Dark Mode: Tentang & CLI
        winDark.RootNavigation.Navigate(typeof(AboutPage));
        await Task.Delay(800);
        SaveScreenshot(winDark, Path.Combine(outDir, "08_tentang_dan_cli_dark.png"));

        winDark.AllowExplicitExit();
        winDark.Close();

        // 9. Menu Cepat System Tray (Light)
        ApplicationThemeManager.Apply(ApplicationTheme.Light);
        var trayLightWin = BuildTrayShowcaseWindow(isDark: false);
        trayLightWin.Show();
        await Task.Delay(500);
        SaveScreenshot(trayLightWin, Path.Combine(outDir, "09_menu_cepat_tray_light.png"));
        trayLightWin.Close();

        // 10. Menu Cepat System Tray (Dark)
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        var trayDarkWin = BuildTrayShowcaseWindow(isDark: true);
        trayDarkWin.Show();
        await Task.Delay(500);
        SaveScreenshot(trayDarkWin, Path.Combine(outDir, "10_menu_cepat_tray_dark.png"));
        trayDarkWin.Close();
    }

    private static Window BuildTrayShowcaseWindow(bool isDark)
    {
        var win = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = false,
            Background = isDark
                ? new SolidColorBrush(Color.FromRgb(0x1C, 0x1C, 0x1E))
                : new SolidColorBrush(Color.FromRgb(0xF0, 0xF2, 0xF5)),
            Width = 520,
            Height = 520,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ShowInTaskbar = false
        };

        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) });

        var header = new TextBlock
        {
            Text = "DisplayFlip • Menu Cepat System Tray",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = isDark ? new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0)) : new SolidColorBrush(Color.FromRgb(0x60, 0x60, 0x60)),
            Margin = new Thickness(24, 16, 24, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        Grid.SetRow(header, 0);
        rootGrid.Children.Add(header);

        var flyoutCard = new Border
        {
            Width = 320,
            Background = isDark ? new SolidColorBrush(Color.FromRgb(0x2C, 0x2C, 0x2E)) : Brushes.White,
            BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x46)) : new SolidColorBrush(Color.FromRgb(0xDC, 0xDE, 0xE2)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6),
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 48, 12),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 4,
                Direction = 270,
                Opacity = isDark ? 0.45 : 0.15,
                Color = Colors.Black
            }
        };

        var menuStack = new StackPanel();

        UIElement CreateMenuItem(SymbolRegular symbol, string text, bool hasSubmenu = false, bool isBold = false)
        {
            var itemBorder = new Border
            {
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 1, 0, 1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (hasSubmenu)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });

            var icon = new SymbolIcon
            {
                Symbol = symbol,
                FontSize = 16,
                Foreground = isDark ? new SolidColorBrush(Color.FromRgb(0x00, 0xA2, 0xED)) : new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            var label = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = isBold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = isDark ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(label, 1);
            grid.Children.Add(label);

            if (hasSubmenu)
            {
                var chevron = new SymbolIcon
                {
                    Symbol = SymbolRegular.ChevronRight20,
                    FontSize = 14,
                    Foreground = isDark ? new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)) : new SolidColorBrush(Color.FromRgb(0x90, 0x90, 0x90)),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(chevron, 2);
                grid.Children.Add(chevron);
            }

            itemBorder.Child = grid;
            return itemBorder;
        }

        UIElement CreateSeparator()
        {
            return new Separator
            {
                Margin = new Thickness(6, 4, 6, 4),
                Background = isDark ? new SolidColorBrush(Color.FromRgb(0x3E, 0x3E, 0x42)) : new SolidColorBrush(Color.FromRgb(0xE4, 0xE6, 0xEB))
            };
        }

        menuStack.Children.Add(CreateMenuItem(SymbolRegular.ArrowSwap24, "Tukar Kiri-Kanan (Quick Swap)"));
        menuStack.Children.Add(CreateMenuItem(SymbolRegular.Eye24, "Identifikasi Layar"));
        menuStack.Children.Add(CreateMenuItem(SymbolRegular.Desktop24, "Multi-Layar", hasSubmenu: true));
        menuStack.Children.Add(CreateMenuItem(SymbolRegular.Folder24, "Profil Tampilan", hasSubmenu: true));
        menuStack.Children.Add(CreateSeparator());
        menuStack.Children.Add(CreateMenuItem(SymbolRegular.Settings24, "Buka Pengaturan DisplayFlip", isBold: true));
        menuStack.Children.Add(CreateMenuItem(SymbolRegular.Dismiss24, "Keluar"));

        flyoutCard.Child = menuStack;
        Grid.SetRow(flyoutCard, 1);
        rootGrid.Children.Add(flyoutCard);

        var taskbar = new Border
        {
            Background = isDark ? new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x22)) : new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFC)),
            BorderBrush = isDark ? new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x36)) : new SolidColorBrush(Color.FromRgb(0xE0, 0xE2, 0xE6)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(24, 0, 24, 0)
        };

        var tbGrid = new Grid();
        tbGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tbGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var tbNote = new TextBlock
        {
            Text = "Taskbar Windows 11",
            FontSize = 11,
            Foreground = isDark ? new SolidColorBrush(Color.FromRgb(0x70, 0x70, 0x70)) : new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0)),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(tbNote, 0);
        tbGrid.Children.Add(tbNote);

        var trayCluster = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        trayCluster.Children.Add(new SymbolIcon
        {
            Symbol = SymbolRegular.ChevronUp20,
            FontSize = 16,
            Margin = new Thickness(6, 0, 8, 0),
            Foreground = isDark ? Brushes.LightGray : Brushes.DarkGray
        });

        var trayIconBorder = new Border
        {
            Background = isDark ? new SolidColorBrush(Color.FromRgb(0x35, 0x35, 0x38)) : new SolidColorBrush(Color.FromRgb(0xE4, 0xE8, 0xF0)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 4, 6, 4),
            Margin = new Thickness(4, 0, 8, 0)
        };
        trayIconBorder.Child = new SymbolIcon
        {
            Symbol = SymbolRegular.DesktopPulse24,
            FontSize = 18,
            Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4))
        };
        trayCluster.Children.Add(trayIconBorder);

        trayCluster.Children.Add(new SymbolIcon
        {
            Symbol = SymbolRegular.Speaker224,
            FontSize = 16,
            Margin = new Thickness(6, 0, 6, 0),
            Foreground = isDark ? Brushes.LightGray : Brushes.DarkGray
        });
        trayCluster.Children.Add(new SymbolIcon
        {
            Symbol = SymbolRegular.Wifi124,
            FontSize = 16,
            Margin = new Thickness(6, 0, 10, 0),
            Foreground = isDark ? Brushes.LightGray : Brushes.DarkGray
        });

        trayCluster.Children.Add(new TextBlock
        {
            Text = "12:00\n15/11/2026",
            FontSize = 10,
            TextAlignment = TextAlignment.Right,
            Foreground = isDark ? Brushes.White : Brushes.Black,
            VerticalAlignment = VerticalAlignment.Center
        });

        Grid.SetColumn(trayCluster, 1);
        tbGrid.Children.Add(trayCluster);

        taskbar.Child = tbGrid;
        Grid.SetRow(taskbar, 2);
        rootGrid.Children.Add(taskbar);

        win.Content = rootGrid;
        return win;
    }

    private static void SaveScreenshot(Window window, string path)
    {
        int w = (int)(window.ActualWidth > 0 ? window.ActualWidth : window.Width);
        int h = (int)(window.ActualHeight > 0 ? window.ActualHeight : window.Height);
        if (w <= 0) w = 1000;
        if (h <= 0) h = 720;

        bool isDark = path.Contains("_dark.png", StringComparison.OrdinalIgnoreCase);
        var bgBrush = isDark
            ? new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20))
            : new SolidColorBrush(Color.FromRgb(0xF8, 0xF8, 0xF8));

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(bgBrush, null, new Rect(0, 0, w, h));
            dc.DrawRectangle(new VisualBrush(window), null, new Rect(0, 0, w, h));
        }

        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(visual);

        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(path);
        enc.Save(fs);
        Console.WriteLine($"[EXPORT] {Path.GetFileName(path)} ({new FileInfo(path).Length} bytes)");
    }
}
