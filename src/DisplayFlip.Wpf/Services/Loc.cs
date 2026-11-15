using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using DisplayFlip.Core;
using DisplayFlip.Core.Localization;

namespace DisplayFlip.UI.Services;

/// <summary>
/// Observable localization service for DisplayFlip.Wpf supporting zero-restart live switching
/// between "auto" (follow Windows UI language), "id" (Indonesian), and "en" (English).
/// </summary>
public sealed class Loc : INotifyPropertyChanged
{
    private static readonly Lazy<Loc> s_instance = new(() => new Loc());

    public static Loc Instance => s_instance.Value;

    private string _preference = LocalizationCatalog.PreferenceAuto;
    private string _effectiveLanguage = LocalizationCatalog.ResolveEffectiveLanguage(LocalizationCatalog.PreferenceAuto);

    private Loc()
    {
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? LanguageChanged;

    public string Preference => _preference;

    public string EffectiveLanguage => _effectiveLanguage;

    /// <summary>
    /// Indexer used by XAML bindings: {Binding [Key], Source={x:Static services:Loc.Instance}}.
    /// </summary>
    public string this[string key] => Get(key);

    public string Get(string key) => LocalizationCatalog.Get(key, _effectiveLanguage);

    public string Format(string key, params object?[] args) => LocalizationCatalog.Format(key, _effectiveLanguage, args);

    /// <summary>
    /// Initializes active language from ProfileStore (defaulting safely to "auto" on missing/corrupt config).
    /// </summary>
    public void Initialize(ProfileStore? store = null, CultureInfo? uiCulture = null)
    {
        string pref = LocalizationCatalog.PreferenceAuto;
        try
        {
            pref = (store ?? new ProfileStore()).GetLanguagePreference();
        }
        catch
        {
            pref = LocalizationCatalog.PreferenceAuto;
        }

        ApplyState(pref, uiCulture, raiseAlways: true);
    }

    /// <summary>
    /// Sets the active language preference ("auto", "id", or "en"), optionally persisting to ProfileStore,
    /// and notifies all XAML bindings and code-behind subscribers immediately without restarting.
    /// </summary>
    public void SetPreference(string preference, bool persist = true, ProfileStore? store = null, CultureInfo? uiCulture = null)
    {
        string normalized = LocalizationCatalog.NormalizePreference(preference);
        if (persist)
        {
            (store ?? new ProfileStore()).SaveLanguagePreference(normalized);
        }

        ApplyState(normalized, uiCulture, raiseAlways: false);
    }

    private void ApplyState(string normalizedPreference, CultureInfo? uiCulture, bool raiseAlways)
    {
        string nextEffective = LocalizationCatalog.ResolveEffectiveLanguage(normalizedPreference, uiCulture);
        bool changed = !string.Equals(_preference, normalizedPreference, StringComparison.Ordinal)
                       || !string.Equals(_effectiveLanguage, nextEffective, StringComparison.Ordinal);

        _preference = normalizedPreference;
        _effectiveLanguage = nextEffective;

        if (!changed && !raiseAlways)
            return;

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(RaiseNotifications);
        }
        else
        {
            RaiseNotifications();
        }
    }

    private void RaiseNotifications()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Preference)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EffectiveLanguage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
