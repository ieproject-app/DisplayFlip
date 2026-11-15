# Implementation Plan: Bilingual Indonesian & English Localization (UI, Tray & CLI)

**Branch**: `002-bilingual-id-en-support` | **Date**: 2026-11-15 | **Spec**: [spec.md](file:///D:/GitHub/multi-monitor/specs/002-bilingual-id-en-support/spec.md)

**Input**: Feature specification from `/specs/002-bilingual-id-en-support/spec.md`

## Summary

Add complete bilingual support (**Bahasa Indonesia** `"id"` and **English** `"en"`, plus `"auto"` OS-locale detection on first launch) across the entire DisplayFlip WPF desktop application, 2D canvas control, system tray quick menu, user notifications/dialogs, and headless CLI runner (`--lang en|id`). A shared keyed string catalog (`LocalizationCatalog`) and backward-compatible `ProfileFile.Language` setting live in `DisplayFlip.Core`, while an observable `Loc` facade (`INotifyPropertyChanged` + `LanguageChanged`) drives zero-restart live switching in `DisplayFlip.Wpf` without disturbing pending 2D canvas edits. In addition, the About page quick links will include the dedicated product domain `https://displayflip.snipgeek.com` alongside the public GitHub repository.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0` for Core, `net10.0-windows` for Windows/WPF/Tests)  
**Primary Dependencies**: WPF, `WPF-UI` (`4.3.0`), `System.Text.Json`, Windows Forms `NotifyIcon` (`TrayIconManager`), xUnit + FluentAssertions  
**Storage**: Local JSON configuration (`%LocalAppData%\DisplayFlip\profiles.json` via `ProfileStore`)  
**Testing**: `dotnet test DisplayFlip.slnx` (xUnit + FluentAssertions on STA threads for WPF controls/pages)  
**Target Platform**: Windows 10 / Windows 11 (x64)  
**Project Type**: Desktop utility (`DisplayFlip.Wpf`) + headless CLI runner (`CliRunner`) + shared Core (`DisplayFlip.Core`)  
**Performance Goals**: Instant (`< 1s`, in-memory dictionary lookup) live language switch across all open pages, canvas visuals, and tray menus without app restart or monitor topology re-query  
**Constraints**: Preserve existing `profiles.json` files (`SchemaVersion = 1`) cleanly; never lose unapplied 2D canvas layout changes (`HasUnappliedChanges`) during live language switches; work cleanly when `Application.Current == null` (CLI and STA unit tests); keep legacy `DisplayFlip` WinForms build intact  
**Scale/Scope**: 6 XAML views/pages + code-behind, 1 custom 2D `MonitorCanvasControl`, 1 tray host service, 5 helper/action services including `CliRunner`, and ~250-350 localized UI/CLI string keys across `"id"` and `"en"`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- `.specify/memory/constitution.md` is still the baseline template without project-specific overrides; standard engineering gates applied:
  1. **Zero breaking changes to existing core topology or profile schemas**: `ProfileFile.Language` defaults to `"auto"`, keeping legacy JSON files valid without migration.
  2. **Testability & contract parity**: Pure catalog & resolution rules live in `DisplayFlip.Core` so parity tests (`id` keys == `en` keys, non-empty, matching format placeholders) and CLI tests run deterministically without OS UI dependency.
  3. **Post-Design Check (PASSED)**: Phase 1 `data-model.md`, `contracts/localization-contract.md`, and `contracts/cli-localization-contract.md` preserve single-source-of-truth strings in `DisplayFlip.Core` and introduce zero new runtime dependencies or platform hazards.

## Project Structure

### Documentation (this feature)

```text
specs/002-bilingual-id-en-support/
├── spec.md                                 # Feature specification (validated 16/16)
├── plan.md                                 # This file (/speckit-plan output)
├── research.md                             # Phase 0 architectural & design decisions
├── data-model.md                           # Language preference & LocalizationCatalog entities
├── quickstart.md                           # End-to-end verification guide
├── checklists/
│   └── requirements.md                     # Spec quality checklist
└── contracts/
    ├── localization-contract.md            # Runtime Loc & live-refresh contract
    └── cli-localization-contract.md        # CLI --lang & localized output contract
```

### Source Code (repository root)

```text
src/
├── DisplayFlip.Core/
│   ├── Models.cs                           # Existing layout/profile models (unchanged schema v1)
│   ├── ProfileStore.cs                     # + Language ("auto"|"id"|"en") property & SetLanguage helper
│   └── Localization/
│       └── LocalizationCatalog.cs          # Complete "id" & "en" keyed catalogs + normalization/OS fallback
├── DisplayFlip.Windows/                    # Existing Win32 CCD/fallback layer (keep messages non-breaking)
├── DisplayFlip.Wpf/
│   ├── App.xaml.cs                         # Initialize Loc.Instance early from ProfileStore
│   ├── Services/
│   │   ├── Loc.cs                          # INotifyPropertyChanged indexer & LanguageChanged facade
│   │   ├── DisplayActionService.cs         # Localized action status/error messages
│   │   └── TrayIconManager.cs              # Dynamic localized tray tooltip, menu & profile submenu
│   ├── Controls/
│   │   └── MonitorCanvasControl.cs         # Localized canvas labels ("★ Utama"/"★ Primary", Snap, empty state)
│   ├── Helpers/
│   │   ├── CliRunner.cs                    # --lang <id|en> parsing + localized CLI help/list/errors
│   │   ├── DisplayHelper.cs                # Localized helper feedback messages
│   │   ├── GeneralHelper.cs                # Localized icon styles, theme names, CLI docs, Get/SetLanguage
│   │   └── ShortcutHelper.cs               # Localized shortcut helper labels/errors
│   └── Views/
│       ├── SettingsWindow.xaml(.cs)        # Localized NavigationView & titlebar
│       ├── IdentifyOverlayWindow.xaml(.cs) # Localized primary badge
│       └── Pages/
│           ├── DisplaysPage.xaml(.cs)      # Bound XAML + live InfoBar/dialog/subtitle localization
│           ├── GeneralPage.xaml(.cs)       # Language selector UI card + live refresh
│           ├── ShortcutsPage.xaml(.cs)     # Bound XAML + live messages
│           └── AboutPage.xaml(.cs)         # Bound XAML + CLI cards + https://displayflip.snipgeek.com link
└── DisplayFlip.Tests/
    └── BilingualLocalizationTests.cs       # Parity, OS fallback, persistence, live switch, canvas preserve & CLI tests
```

**Structure Decision**: Place `LocalizationCatalog` and `ProfileFile.Language` directly inside `DisplayFlip.Core` so UI (`DisplayFlip.Wpf`), headless CLI (`CliRunner`), and automated tests share a single verified source of truth without extra assemblies or `.resx` packaging overhead in single-file publish builds. Note that the About page Quick Links section will expose `https://displayflip.snipgeek.com` alongside the public GitHub repository (`https://github.com/ieproject-app/DisplayFlip`).

## Complexity Tracking

No constitution violations or unjustified abstractions. All state lives within existing `DisplayFlip.Core` and `DisplayFlip.Wpf` projects.
