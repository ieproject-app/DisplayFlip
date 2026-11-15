---

description: "Actionable, dependency-ordered task list for Bilingual Indonesian & English Localization (UI, Tray & CLI)"
---

# Tasks: Bilingual Indonesian & English Localization (UI, Tray & CLI)

**Input**: Design documents from `/specs/002-bilingual-id-en-support/`  
**Prerequisites**: [plan.md](file:///D:/GitHub/multi-monitor/specs/002-bilingual-id-en-support/plan.md), [spec.md](file:///D:/GitHub/multi-monitor/specs/002-bilingual-id-en-support/spec.md), [research.md](file:///D:/GitHub/multi-monitor/specs/002-bilingual-id-en-support/research.md), [data-model.md](file:///D:/GitHub/multi-monitor/specs/002-bilingual-id-en-support/data-model.md), [contracts/](file:///D:/GitHub/multi-monitor/specs/002-bilingual-id-en-support/contracts/)

**Organization**: Tasks are grouped by user story (`US1`..`US4`) following an MVP-first incremental delivery order so each story can be implemented and verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story traceability label (`[US1]`, `[US2]`, `[US3]`, `[US4]`)
- Every task includes exact repository file paths

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish shared localization namespace and core catalog scaffolding with zero new NuGet dependencies.

- [X] T001 Create `src/DisplayFlip.Core/Localization/` directory and `LocalizationCatalog` skeleton defining `"auto"`, `"id"`, `"en"` language constants in `src/DisplayFlip.Core/Localization/LocalizationCatalog.cs`
- [X] T002 Create automated test suite file skeleton with core test helpers in `src/DisplayFlip.Tests/BilingualLocalizationTests.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core localization storage, lookup, fallback, and observable WPF binding engine required by every user story.

**⚠️ CRITICAL**: No user story work (`US1`–`US4`) can begin until this phase is complete.

- [X] T003 Implement `ProfileFile.Language` (`public string Language { get; init; } = "auto";`, backward-compatible with `SchemaVersion = 1` when JSON omits `"language"`) and unified `SaveLanguagePreference` / read helpers in `src/DisplayFlip.Core/ProfileStore.cs`
- [X] T004 Implement full `"id"` and `"en"` translation key dictionaries (`Indonesian` & `English` read-only dictionaries enforcing 100% key parity, non-whitespace values, and matching `{0}`/`{1}` format arity), `NormalizePreference`, `ResolveEffectiveLanguage`, `Get`, and `Format` in `src/DisplayFlip.Core/Localization/LocalizationCatalog.cs`
- [X] T005 Implement observable `Loc` singleton (`INotifyPropertyChanged` notifying `"Item[]"`, `EffectiveLanguage`, and `Preference`, plus `LanguageChanged` event, `Initialize`, and `SetPreference`) in `src/DisplayFlip.Wpf/Services/Loc.cs`
- [X] T006 Initialize `Loc.Instance.Initialize(...)` early during WPF app startup before window/tray creation in `src/DisplayFlip.Wpf/App.xaml.cs`
- [X] T007 [P] Add dictionary key-parity (`Indonesian.Keys` set equals `English.Keys` set, no blank entries, matching placeholder arity) and `ProfileFile.Language` backward-compatibility tests in `src/DisplayFlip.Tests/BilingualLocalizationTests.cs`

**Checkpoint**: Foundation ready — `LocalizationCatalog`, `ProfileFile.Language`, and `Loc.Instance` compile and pass foundational unit tests.

---

## Phase 3: User Story 1 - Live Language Switching in Settings (Priority: P1) 🎯 MVP

**Goal**: Users can switch between Follow System / Bahasa Indonesia / English in General Settings and immediately see all navigation tabs, pages, 2D canvas annotations, dialogs, and InfoBar messages update live without losing unapplied canvas edits.

**Independent Test**: Open `GeneralPage`, change the language selector between `"en"` and `"id"`, navigate across `DisplaysPage`, `ShortcutsPage`, and `AboutPage`, and verify all XAML/code-behind/canvas strings update immediately and unapplied 2D canvas changes stay intact.

### Implementation for User Story 1

- [X] T008 [P] [US1] Bind NavigationView headers, window title, and tray restore labels to `Loc.Instance` in `src/DisplayFlip.Wpf/Views/SettingsWindow.xaml` and `src/DisplayFlip.Wpf/Views/SettingsWindow.xaml.cs`
- [X] T009 [P] [US1] Add Language selector card (`"auto"` Follow Windows / `"id"` Bahasa Indonesia / `"en"` English) and bind all General & Icons XAML text to `Loc.Instance` in `src/DisplayFlip.Wpf/Views/Pages/GeneralPage.xaml`
- [X] T010 [US1] Wire language selector selection change to `Loc.Instance.SetPreference(...)`, localize theme names, icon style cards, startup/auto-apply status messages, and `GetCliCommands()` list in `src/DisplayFlip.Wpf/Views/Pages/GeneralPage.xaml.cs` and `src/DisplayFlip.Wpf/Helpers/GeneralHelper.cs`
- [X] T011 [P] [US1] Bind all Displays & Canvas XAML labels, headers, tooltips, and action buttons to `Loc.Instance` in `src/DisplayFlip.Wpf/Views/Pages/DisplaysPage.xaml`
- [X] T012 [US1] Localize dynamic subtitle, selected monitor status, solo button text, InfoBar banners, and Save/Delete profile dialogs, and subscribe to `Loc.Instance.LanguageChanged` updating UI text without wiping `HasUnappliedChanges` in `src/DisplayFlip.Wpf/Views/Pages/DisplaysPage.xaml.cs`
- [X] T013 [P] [US1] Subscribe `MonitorCanvasControl` to `Loc.Instance.LanguageChanged` and localize `"★ Utama"` / `"★ Primary"`, `"Snap ({0},{1})"`, and empty-state rendering in `src/DisplayFlip.Wpf/Controls/MonitorCanvasControl.cs`
- [X] T014 [P] [US1] Bind Shortcuts XAML and code-behind status/validation/capture messages to `Loc.Instance` in `src/DisplayFlip.Wpf/Views/Pages/ShortcutsPage.xaml`, `src/DisplayFlip.Wpf/Views/Pages/ShortcutsPage.xaml.cs`, and `src/DisplayFlip.Wpf/Helpers/ShortcutHelper.cs`
- [X] T015 [US1] Bind About & CLI XAML to `Loc.Instance`, update CLI documentation cards & clipboard messages on `LanguageChanged`, and expose `https://displayflip.snipgeek.com` alongside `https://github.com/ieproject-app/DisplayFlip` (depends on `T010` for `GetCliCommands()`) in `src/DisplayFlip.Wpf/Views/Pages/AboutPage.xaml` and `src/DisplayFlip.Wpf/Views/Pages/AboutPage.xaml.cs`
- [X] T016 [P] [US1] Localize `DisplayActionService` and `DisplayHelper` status/error return messages via `Loc` / `LocalizationCatalog` in `src/DisplayFlip.Wpf/Services/DisplayActionService.cs` and `src/DisplayFlip.Wpf/Helpers/DisplayHelper.cs`
- [X] T017 [US1] Add STA integration tests verifying live switching updates `Loc` bindings, preserves `DisplaysPage.HasUnappliedChanges`, updates `AboutPage` CLI cards, and persists `"language"` in `src/DisplayFlip.Tests/BilingualLocalizationTests.cs`

**Checkpoint**: User Story 1 (MVP) is complete — switching language updates the full desktop UI and 2D canvas live and persists cleanly across restarts.

---

## Phase 4: User Story 2 - System Tray Quick Menu & Overlay Localization (Priority: P2)

**Goal**: System tray tooltip, tray context menu items, profile submenus, tray error prompts, and Display Identification overlays match the active language and refresh live on switch.

**Independent Test**: Switch language via `Loc.Instance.SetPreference(...)`, inspect rebuilt `TrayIconManager` context menu item strings and `IdentifyOverlayWindow` labels without restarting the app.

### Implementation for User Story 2

- [X] T018 [P] [US2] Localize primary monitor badge and screen identification overlay labels (`IdentifyOverlayWindow`) via `Loc` in `src/DisplayFlip.Wpf/Views/IdentifyOverlayWindow.xaml` and `src/DisplayFlip.Wpf/Views/IdentifyOverlayWindow.xaml.cs`
- [X] T019 [US2] Localize tray tooltip, all context menu actions, profile submenu strings, and rebuild tray menu live on `Loc.Instance.LanguageChanged` in `src/DisplayFlip.Wpf/Services/TrayIconManager.cs`
- [X] T020 [US2] Localize startup/fatal tray error dialogs in `src/DisplayFlip.Wpf/App.xaml.cs`
- [X] T021 [US2] Add unit/STA tests verifying `TrayIconManager` and `IdentifyOverlayWindow` reflect `"id"` and `"en"` strings and update on live `LanguageChanged` in `src/DisplayFlip.Tests/BilingualLocalizationTests.cs`

**Checkpoint**: User Story 2 complete — background tray interactions and screen identification overlays are fully bilingual.

---

## Phase 5: User Story 3 - Localized Command-Line Interface (CLI) Output (Priority: P3)

**Goal**: CLI `--help`, `--list`, `--swap`, `--identify`, `--solo`, `--extend`, `--apply`, and error messages honor saved preference and support non-mutating `--lang en|id` (and `--lang=en|id`) per invocation.

**Independent Test**: Run `CliRunner` with `--help`, `--list`, and invalid commands under `--lang en` and `--lang id` and verify stdout/stderr output matches the requested language while `profiles.json` remains unmodified.

### Implementation for User Story 3

- [X] T022 [US3] Implement non-mutating `--lang <id|en>` / `--lang=<id|en>` parsing (graceful fallback on missing value or unknown values) and localized `--help`, `--list`, action feedback, `--clone`/`--duplicate`, and unknown-command error output in `src/DisplayFlip.Wpf/Helpers/CliRunner.cs`
- [X] T023 [US3] Update `GeneralHelper.GetCliCommands()` reference entry/usage to document `--lang <id|en>` in `src/DisplayFlip.Wpf/Helpers/GeneralHelper.cs`
- [X] T024 [US3] Add CLI localization unit tests verifying `--help --lang en`, `--help --lang id`, `--list --lang en|id` (preserving `--list --json` validity), error output, and non-mutation of saved `ProfileFile.Language` in `src/DisplayFlip.Tests/BilingualLocalizationTests.cs`

**Checkpoint**: User Story 3 complete — CLI workflows produce clear localized terminal output in both languages.

---

## Phase 6: User Story 4 - Sensible First-Launch Default for Global & Local Users (Priority: P4)

**Goal**: Clean installs with no saved preference auto-select Bahasa Indonesia when Windows UI culture is Indonesian (`id*`) and English for all other OS locales, falling back safely on corrupted/unknown config codes.

**Independent Test**: Invoke `LocalizationCatalog.ResolveEffectiveLanguage` and `Loc.Instance.Initialize` across missing config, `"auto"`, invalid codes (`"fr"`, `""`), `CultureInfo("id-ID")`, `CultureInfo("en-US")`, and `CultureInfo("ja-JP")`.

### Implementation for User Story 4

- [X] T025 [US4] Harden `LocalizationCatalog.ResolveEffectiveLanguage` and `ProfileStore.Load` language normalization for missing files, `"auto"`, `"id"`, `"en"`, and unknown/corrupt codes in `src/DisplayFlip.Core/Localization/LocalizationCatalog.cs` and `src/DisplayFlip.Core/ProfileStore.cs`
- [X] T026 [US4] Add OS-locale fallback matrix tests (`id-ID` -> `"id"`, `en-US` / `ja-JP` / `de-DE` -> `"en"`, unknown saved code -> `"auto"` fallback) in `src/DisplayFlip.Tests/BilingualLocalizationTests.cs`

**Checkpoint**: User Story 4 complete — clean-machine launches default appropriately for both Indonesian and international users.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final cross-cutting audit, build packaging check, and end-to-end quickstart verification.

- [X] T027 [P] Audit all `src/DisplayFlip.Wpf/` `.xaml` and `.cs` files to ensure zero leftover untranslated hardcoded user-facing Indonesian/English UI strings remain
- [X] T028 [P] Update existing legacy Indonesian-asserting tests (`src/DisplayFlip.Tests/WpfCanvasDragAndE2ETests.cs`, `WpfDisplaysTests.cs`, `WpfShortcutsTests.cs`, `WpfGeneralTests.cs`) to pin explicit language state (`"id"`/`"en"`) so tests are OS-locale independent
- [X] T029 [P] Update `README.md` feature overview / CLI reference with bilingual & `--lang` documentation in `README.md`
- [X] T030 Run full test suite (`dotnet test DisplayFlip.slnx`) and execute validation checks from `specs/002-bilingual-id-en-support/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001–T002 start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 (`T003`–`T007`) — **BLOCKS all user stories (`US1`–`US4`)**.
- **User Story 1 (Phase 3, P1 🎯 MVP)**: Starts immediately after Phase 2.
- **User Story 2 (Phase 4, P2)**: Starts after Phase 2 (`Loc.Instance` + `LocalizationCatalog`); shares no blocking write conflicts with `US1`.
- **User Story 3 (Phase 5, P3)**: Starts after Phase 2 (`LocalizationCatalog` + `Loc` + `ProfileStore.Language`). `T023` updates `GeneralHelper` after `T010`/`T022`.
- **User Story 4 (Phase 6, P4)**: Hardens and verifies Phase 2 rules; can run anytime after Phase 2.
- **Polish (Phase 7)**: Depends on Phases 3–6 completing.

### Parallel Opportunities

- Within **Phase 2**: `T007` (parity tests) can be authored alongside `T004`/`T005`.
- Within **Phase 3 (US1)**: `T008` (`SettingsWindow`), `T009` (`GeneralPage.xaml`), `T011` (`DisplaysPage.xaml`), `T013` (`MonitorCanvasControl`), `T014` (`ShortcutsPage`), `T015` (`AboutPage`), and `T016` (`DisplayActionService`/`DisplayHelper`) touch distinct files and can run in parallel once Phase 2 lands.
- Within **Phase 4 (US2)**: `T018` (`IdentifyOverlayWindow`) runs in parallel with `T019` (`TrayIconManager`).

### Parallel Example: User Story 1

```text
T008 [US1] SettingsWindow.xaml(.cs)
T009 [US1] GeneralPage.xaml
T011 [US1] DisplaysPage.xaml
T013 [US1] MonitorCanvasControl.cs
T014 [US1] ShortcutsPage.xaml(.cs) & ShortcutHelper.cs
T015 [US1] AboutPage.xaml(.cs) + https://displayflip.snipgeek.com link
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete **Phase 1 (Setup)** + **Phase 2 (Foundational)** -> shared `LocalizationCatalog`, `ProfileFile.Language`, and observable `Loc.Instance` ready.
2. Complete **Phase 3 (User Story 1)** -> full desktop UI & 2D canvas live bilingual switch works and preserves unapplied canvas edits.
3. Validate `US1` independently via `dotnet test DisplayFlip.slnx` + UI smoke test.

### Incremental Delivery

1. `Setup + Foundational` -> Core localization infrastructure + 100% key parity tests.
2. `US1 (Live UI & Canvas)` -> Complete WPF app switching + `https://displayflip.snipgeek.com` in About.
3. `US2 (Tray & Overlay)` -> Background tray menu + Identify overlay localized live.
4. `US3 (CLI)` -> Terminal `--help`, `--list`, actions, and `--lang en|id`.
5. `US4 (First-Launch Locale)` + `Polish` -> Full clean-install guarantees and cross-file string audit.
