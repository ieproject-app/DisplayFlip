using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DisplayFlip.UI.Helpers;
using DisplayFlip.UI.Services;
using DisplayFlip.UI.Views;
using DisplayFlip.UI.Views.Pages;
using Wpf.Ui.Appearance;

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
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        var winDark = new SettingsWindow();
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
    }

    private static void SaveScreenshot(Window window, string path)
    {
        int w = (int)Math.Max(window.ActualWidth, 1000);
        int h = (int)Math.Max(window.ActualHeight, 720);
        var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(window);

        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        using var fs = File.Create(path);
        enc.Save(fs);
        Console.WriteLine($"[EXPORT] {Path.GetFileName(path)} ({new FileInfo(path).Length} bytes)");
    }
}
