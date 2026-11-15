# Contract: UI & Runtime Localization (`Loc`)

## Surface (`DisplayFlip.Core.LocalizationCatalog` & `DisplayFlip.UI.Services.Loc`)

### 1. Catalog Resolution (`DisplayFlip.Core.LocalizationCatalog`)
- `string NormalizePreference(string? rawPreference)` -> returns `"auto"`, `"id"`, or `"en"`.
- `string ResolveEffectiveLanguage(string? rawPreference, CultureInfo? uiCulture = null)` -> returns `"id"` or `"en"`.
- `string Get(string key, string effectiveLanguage)` -> returns translated string for `effectiveLanguage`; falls back to `"en"`, then `"id"`, then `key` only if missing everywhere (prevented by parity test).
- `string Format(string key, string effectiveLanguage, params object?[] args)` -> formats template safely (`string.Format` with `CultureInfo.InvariantCulture` / active culture).

### 2. WPF Observable Facade (`DisplayFlip.UI.Services.Loc`)
- Singleton `Loc.Instance` accessible from XAML (`{Binding [Key], Source={x:Static services:Loc.Instance}, Mode=OneWay}`) and C# (`Loc.Get(key)`, `Loc.Format(key, args)`).
- `Initialize(ProfileStore? store = null, CultureInfo? uiCulture = null)`: loads saved `Language` preference once at startup without throwing on missing/corrupt files.
- `SetPreference(string preference, ProfileStore? store = null, bool persist = true)`:
  - Normalizes preference (`"auto"` | `"id"` | `"en"`).
  - Optionally persists via `ProfileStore` without overwriting profiles/hotkeys/other settings.
  - Updates `EffectiveLanguage`, raises `PropertyChanged` for `"Item[]"`, `EffectiveLanguage`, and `Preference`, and invokes `LanguageChanged`.

### 3. Live Component Refresh Obligations
- **`DisplaysPage`**: Subscribes to `Loc.Instance.LanguageChanged` (weak/unsubscribed on `Unloaded` or page-lifetime-safe) to re-render dynamic strings (`CanvasSubtitleText`, `SelectedMonitorTitle`, `SelectedMonitorSubtitle`, `BtnSoloDisplay.Content`) **without** calling `RefreshMonitorsFromSystem()` so unapplied canvas edits (`HasUnappliedChanges`) are preserved.
- **`MonitorCanvasControl`**: Re-renders (`InvalidateVisual()`) on `LanguageChanged` so `"★ Utama"` / `"★ Primary"`, `"Snap (...)"`, and empty state update immediately.
- **`TrayIconManager`**: Rebuilds context menu items, profile submenu, and tray tooltip on `LanguageChanged`.
- **`GeneralPage` / `ShortcutsPage` / `AboutPage`**: Refresh dynamically populated cards (`IconStyleOption`, `CliCommandItem`, shortcut cards, button states) on `LanguageChanged` and display `https://displayflip.snipgeek.com` on `AboutPage`.
