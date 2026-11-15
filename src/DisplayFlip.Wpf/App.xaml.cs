using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DisplayFlip.UI.Views;
using DisplayFlip.UI.Views.Pages;
using Wpf.Ui.Appearance;

namespace DisplayFlip.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains("--export-screenshots"))
        {
            await ExportScreenshotsAsync();
            Shutdown(0);
            return;
        }

        var mainWindow = new SettingsWindow();
        mainWindow.Show();
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
