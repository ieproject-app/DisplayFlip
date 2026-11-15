# Research: Bilingual Indonesian & English Localization (UI, Tray & CLI)

## Decision 1: Shared Keyed Localization Catalog + Observable XAML Binding Facade
- **Decision**: Store the complete `"id"` and `"en"` string dictionaries and language resolution rules (`"auto"`, `"id"`, `"en"`) in `DisplayFlip.Core` (`LocalizationCatalog`), wrapped by an observable `Loc` singleton (`INotifyPropertyChanged` with `"Item[]"` indexer notification + `LanguageChanged` event) in `DisplayFlip.Wpf`.
- **Rationale**:
  1. **Works in both WPF UI and headless CLI/unit tests**: `DisplayFlip.Wpf` hosts both interactive WPF windows and synchronous `CliRunner` commands (`Application.Current` is `null` during CLI execution and inside non-app-hosted xUnit STA tests). Swapping `Application.Current.Resources` fails or is unavailable when `Application.Current` is `null`, whereas a shared `LocalizationCatalog` + `Loc` singleton works identically everywhere.
  2. **Zero-restart live switching**: Raising `PropertyChanged("Item[]")` causes every XAML `{Binding [Key], Source={x:Static loc:Loc.Instance}, Mode=OneWay}` to refresh immediately without recreating pages or losing unapplied 2D canvas edits (`_hasUnappliedChanges`, `_canvasMonitors`). Subscribing to `Loc.Instance.LanguageChanged` lets `MonitorCanvasControl`, `TrayIconManager`, `IdentifyOverlayWindow`, and dynamic code-behind items rebuild/redraw immediately.
  3. **Compile/test-time parity guarantee**: Keeping both `"id"` and `"en"` dictionaries in one typed catalog allows an xUnit test to assert exact 1:1 key set equality (`IdStrings.Keys == EnStrings.Keys`) and zero empty values.
- **Alternatives considered**:
  - *`.resx` / `Properties.Resources` with `Thread.CurrentUICulture`*: Switching `CurrentUICulture` at runtime does not auto-update already-bound WPF XAML controls without custom markup extensions or window reloads, and satellite assemblies add packaging complexity to single-file publish (`publish.ps1`).
  - *WPF XAML `ResourceDictionary` swapping*: Couples localization to `Application.Current`, cannot be shared directly with `CliRunner` or `DisplayFlip.Core` tests, and drops type-checkable key parity helpers.

## Decision 2: Backward-Compatible Language Persistence in `ProfileFile`
- **Decision**: Add `public string Language { get; init; } = "auto";` to `DisplayFlip.Core.ProfileFile`, stored in `%LocalAppData%\DisplayFlip\profiles.json` alongside `IconStyle` and `AutoApplyDrag`.
- **Rationale**:
  - Existing user `profiles.json` files lack a `"language"` property; `System.Text.Json` initializes missing properties to `"auto"` (or normalization treats `null`/empty/unknown strings as `"auto"`), requiring zero migration steps and keeping `SchemaVersion = 1` compatible.
  - Adding an explicit `"auto"` option ("Ikuti Windows" / "Follow Windows") alongside `"id"` and `"en"` guarantees FR-006 first-launch behavior (`id*` -> Indonesian, everything else -> English) without overwriting the user's ability to return to OS-detection later.
- **Alternatives considered**:
  - *Separate `settings.json` or Windows Registry key*: Fragments app state across multiple locations when theme/tray/startup/icon settings already live cleanly in `ProfileFile`.

## Decision 3: Single-Invocation CLI Language Override (`--lang`) Without Config Mutation
- **Decision**: Support `--lang en|id` (and `--lang=en|id`) inside `CliRunner` as a thread-safe / run-scoped effective language override that initializes from the persisted `ProfileStore` setting unless overridden by CLI args, without writing back to `profiles.json`.
- **Rationale**:
  - Scripts and CI checks can force English or Indonesian output cleanly without mutating the desktop user's interactive UI language preference.
  - Stripping `--lang <val>` before evaluating action switches prevents breaking existing option index checks (e.g., `--apply "Profile Name"` or `--solo 1`).
- **Alternatives considered**:
  - *Environment variable only (`DISPLAYFLIP_LANG`)*: Less discoverable in `--help` and harder to document in CLI quick reference cards on `AboutPage`.

## Decision 4: Code-Behind, Custom Canvas, Tray & Helper Localization Coverage
- **Decision**: Audit and migrate all hardcoded UI/CLI strings across:
  1. `SettingsWindow.xaml` & `.xaml.cs` (navigation items, titlebar)
  2. `DisplaysPage.xaml` & `.xaml.cs` (controls, InfoBar banners, Save/Delete dialogs, dynamic monitor subtitle)
  3. `GeneralPage.xaml` & `.xaml.cs` (new Language selector card, theme cards, icon style cards, startup/auto-apply status messages)
  4. `ShortcutsPage.xaml` & `.xaml.cs` (labels, InfoBar, shortcut labels/errors)
  5. `AboutPage.xaml` & `.xaml.cs` (CLI reference cards from `GeneralHelper.GetCliCommands()`, Copy/Copied button states, Quick Links including `https://displayflip.snipgeek.com` + GitHub repository link)
  6. `IdentifyOverlayWindow.xaml` & `.xaml.cs` (`Primary` badge, screen overlay labels)
  7. `MonitorCanvasControl.cs` (`★ Utama` / `★ Primary`, `Snap (x,y)`, empty state text)
  8. `TrayIconManager.cs` & `App.xaml.cs` (tray tooltip, menu items, profile submenu, error dialog)
  9. `DisplayActionService.cs`, `CliRunner.cs`, `DisplayHelper.cs`, `GeneralHelper.cs`, `ShortcutHelper.cs` (user-facing status/error text)
- **Rationale**: Ensures SC-001 (100% coverage with zero leaked Indonesian strings when switched to English, and vice versa) and updates the stale docs URL (`github.com/akses/multi-monitor`) on `AboutPage` to `https://displayflip.snipgeek.com` as requested by the owner.
