using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using DisplayFlip.UI.Services;
using DisplayFlip.UI.Views.Pages;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DisplayFlip.UI.Views;

/// <summary>
/// Settings window for DisplayFlip using WPF and WPF-UI Fluent design.
/// Mendukung background residency (minimize to tray saat ditutup X)
/// dan auto-refresh tampilan saat display settings berubah.
/// </summary>
public partial class SettingsWindow : FluentWindow
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private readonly PageService _pageService = new();
    private bool _isExplicitExit;

    public SettingsWindow()
    {
        // Synchronize light/dark theme and system accent colors with Windows 11 before components load
        SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);

        InitializeComponent();

        RootNavigation.SetPageProviderService(_pageService);
        Loaded += OnLoaded;

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RootNavigation.Navigate(typeof(DisplaysPage));
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (IsVisible)
            {
                var displaysPage = _pageService.GetPage<DisplaysPage>();
                displaysPage?.RefreshData();
            }
        });
    }

    /// <summary>
    /// Memunculkan jendela ke depan (foreground), merestore dari keadaan minimized / hidden.
    /// </summary>
    public void BringToForeground()
    {
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;

        if (!IsVisible)
            Show();

        Activate();
        Focus();

        var handle = new WindowInteropHelper(this).Handle;
        if (handle != IntPtr.Zero)
        {
            SetForegroundWindow(handle);
        }

        var displaysPage = _pageService.GetPage<DisplaysPage>();
        displaysPage?.RefreshData();
    }

    /// <summary>
    /// Mengizinkan penutupan jendela secara permanen (hanya dipanggil saat Shutdown/Keluar).
    /// </summary>
    public void AllowExplicitExit()
    {
        _isExplicitExit = true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (!_isExplicitExit)
        {
            // Intersepsi tombol X: batalkan penutupan jendela dan sembunyikan ke background tray
            e.Cancel = true;
            Hide();
        }
        else
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        }
    }
}
