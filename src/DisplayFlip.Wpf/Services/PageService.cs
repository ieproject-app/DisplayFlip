using Wpf.Ui.Abstractions;

namespace DisplayFlip.UI.Services;

/// <summary>
/// Simple page provider and cache for NavigationView in DisplayFlip.Wpf.
/// </summary>
public sealed class PageService : INavigationViewPageProvider
{
    private readonly Dictionary<Type, object> _pages = new();

    public object? GetPage(Type pageType)
    {
        if (!_pages.TryGetValue(pageType, out var page))
        {
            page = Activator.CreateInstance(pageType);
            if (page != null)
            {
                _pages[pageType] = page;
            }
        }
        return page;
    }
}
