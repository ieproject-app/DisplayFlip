using System.Windows;
using DisplayFlip.UI.Services;
using DisplayFlip.UI.Views.Pages;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace DisplayFlip.UI.Views;

/// <summary>
/// Settings window for DisplayFlip using WPF and WPF-UI Fluent design.
/// </summary>
public partial class SettingsWindow : FluentWindow
{
    private readonly PageService _pageService = new();

    public SettingsWindow()
    {
        InitializeComponent();

        // Synchronize light/dark theme and system accent colors with Windows 11
        SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);

        RootNavigation.SetPageProviderService(_pageService);
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RootNavigation.Navigate(typeof(DisplaysPage));
    }
}
