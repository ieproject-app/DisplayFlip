# Data Model: Bilingual Indonesian & English Localization

## Entities

### 1. Language Preference (`AppLanguagePreference`)
- **Location**: `DisplayFlip.Core` (`LocalizationCatalog`)
- **Valid Persisted Values**:
  - `"auto"` (default when unset, null, whitespace, or unrecognized): resolves dynamically via OS UI culture (`CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("id", StringComparison.OrdinalIgnoreCase)` -> `"id"`, otherwise `"en"`).
  - `"id"`: Forces Bahasa Indonesia across UI, Tray, Canvas, and CLI.
  - `"en"`: Forces English across UI, Tray, Canvas, and CLI.
- **Validation Rules**:
  - `NormalizePreference(string? raw)` trims and lowercases input; only `"id"` and `"en"` return explicit overrides, while `"auto"` or any invalid/unknown string safely normalizes to `"auto"` (never throws).

### 2. Extended `ProfileFile` (`DisplayFlip.Core.ProfileStore`)
- **New Property**:
  - `public string Language { get; init; } = "auto";`
- **Relationships & Compatibility**:
  - Persisted via existing `ProfileStore.SaveRaw(ProfileFile)` in `%LocalAppData%\DisplayFlip\profiles.json`.
  - Compatible with `LayoutProfile.SupportedSchemaVersion = 1`; existing files without `"language"` deserialize cleanly with `"auto"`.
  - Modifying `Language` preserves all existing `Profiles`, shortcuts, `AutoApplyDrag`, and `IconStyle` values.

### 3. Localization Catalog (`DisplayFlip.Core.LocalizationCatalog`)
- **Structure**:
  - Read-only dictionary `Indonesian` (`IReadOnlyDictionary<string, string>`)
  - Read-only dictionary `English` (`IReadOnlyDictionary<string, string>`)
- **Key Naming Convention**: `<Area>.<Component>.<Element>` (e.g., `Nav.Displays`, `Displays.BtnApply`, `Canvas.PrimaryBadge`, `Tray.QuickFlip`, `Cli.Help.Usage`, `General.Lang.Title`).
- **Invariants**:
  - `Indonesian.Keys` set MUST equal `English.Keys` set (100% parity).
  - Every value in both dictionaries MUST be non-null and non-whitespace.
  - Format placeholders (`{0}`, `{1}`, etc.) in a given key MUST match in arity between `"id"` and `"en"`.

### 4. Runtime Observable Localization Service (`DisplayFlip.UI.Services.Loc`)
- **State**:
  - `Preference` (`"auto"` | `"id"` | `"en"`)
  - `EffectiveLanguage` (`"id"` | `"en"`)
- **Events / Notifications**:
  - `INotifyPropertyChanged` (`PropertyChanged("Item[]")`, `PropertyChanged(nameof(EffectiveLanguage))`, `PropertyChanged(nameof(Preference))`)
  - `event EventHandler? LanguageChanged` for imperative refresh hooks (`TrayIconManager`, `MonitorCanvasControl`, `DisplaysPage`, `GeneralPage`, `ShortcutsPage`, `AboutPage`).
