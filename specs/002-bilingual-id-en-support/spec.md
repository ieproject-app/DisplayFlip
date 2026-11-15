# Feature Specification: Bilingual Indonesian & English Localization (UI, Tray & CLI)

**Feature Branch**: `002-bilingual-id-en-support`

**Created**: 2026-11-15

**Status**: Draft

**Input**: User description: "Penambahan fitur bahasa Indonesia + English (UI, tray, dialog, kanvas, dan output CLI) untuk persiapan rilis publik open-source DisplayFlip"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Live Language Switching in Settings (Priority: P1)

As an international or Indonesian desktop user, I want to switch DisplayFlip's display language between English and Bahasa Indonesia from General Settings and immediately see the interface update without restarting the application.

**Why this priority**: Open-source international users cannot comfortably adopt an application whose controls, warnings, and instructions are locked in Indonesian, while Indonesian users expect their native language to remain first-class.

**Independent Test**: Open General Settings, change the language selector from Indonesian to English (and back), and verify all visible navigation items, page headers, controls, 2D canvas badges/labels, tooltips, and status notifications update immediately in the running window.

**Acceptance Scenarios**:

1. **Given** the application is displaying Bahasa Indonesia, **When** the user selects "English" in General Settings, **Then** all visible labels, buttons, tabs, tooltips, canvas annotations (such as primary indicator and snap coordinates), and confirmation dialogs immediately switch to English without requiring an app restart.
2. **Given** the user switches the language to English and closes or restarts DisplayFlip, **When** the application launches again (normally or minimized to the system tray), **Then** the application starts and renders in English as previously saved.
3. **Given** the application is running in English, **When** an informational or error banner is triggered (such as applying display layout, resetting layout, saving/deleting a profile, or registering a hotkey), **Then** both the banner title and detailed message appear in English.

---

### User Story 2 - System Tray Quick Menu & Overlay Localization (Priority: P2)

As a user who primarily interacts with DisplayFlip from the background system tray or uses display identification overlays, I want tray menu items, tray notifications, and on-screen display identification overlays to match my chosen language.

**Why this priority**: DisplayFlip is designed for background tray residency; if the main window is English while tray actions and screen overlays remain Indonesian, daily workflows feel broken and unfinished.

**Independent Test**: Switch language to English, right-click the system tray icon, inspect every menu item and submenu, trigger a tray action notification, and open Display Identification overlays to confirm every visible phrase matches the active language.

**Acceptance Scenarios**:

1. **Given** the active language is English, **When** the user opens the system tray context menu, **Then** all menu actions (such as quick swap, solo monitor toggles, apply saved profile submenu, open settings, and exit) appear in English.
2. **Given** the user changes the language while the application stays resident in the tray, **When** the user opens the tray menu immediately afterward without restarting, **Then** the tray menu reflects the newly selected language.
3. **Given** the user triggers screen identification or an error occurs from a tray command, **When** the overlay or notification is shown, **Then** all overlay labels and notification messages use the active language.

---

### User Story 3 - Localized Command-Line Interface (CLI) Output (Priority: P3)

As a power user or automation script author, I want DisplayFlip's CLI help text, device listing, action feedback, and error messages to support both English and Bahasa Indonesia so automation workflows and terminal usage are clear in my preferred language.

**Why this priority**: DisplayFlip supports headless CLI switches (`--swap`, `--identify`, `--solo`, `--list`, `--help`, profile actions); public open-source users reading documentation or troubleshooting commands need readable English terminal output alongside Indonesian.

**Independent Test**: Run CLI commands under English preference and Indonesian preference (and via an explicit command-line language override if provided) and confirm help usage text, monitor summaries, and error messages match the selected language while preserving exact technical identifiers and coordinate values.

**Acceptance Scenarios**:

1. **Given** the user's saved language preference is English, **When** the user runs CLI help, status listing, or action commands, **Then** command descriptions, headings, and outcome/error messages are output in English.
2. **Given** the user passes an explicit language flag on the CLI command (e.g., requesting English or Indonesian for that invocation), **When** the CLI executes, **Then** that command's terminal output uses the requested language without corrupting or overwriting unrelated display settings.
3. **Given** no saved language preference exists on a clean machine, **When** the CLI runs, **Then** output defaults cleanly based on the operating system language rules without crashing or emitting empty placeholders.

---

### User Story 4 - Sensible First-Launch Default for Global & Local Users (Priority: P4)

As a first-time user downloading DisplayFlip from GitHub, I want the application to start in a language that makes sense for my system automatically, while letting me override it anytime.

**Why this priority**: First impressions on an open-source release depend on immediate readability on launch before the user hunts for a language dropdown.

**Independent Test**: Launch DisplayFlip with no existing user settings file under an Indonesian system locale versus a non-Indonesian system locale (e.g., English, Japanese, German) and verify the initial active language matches expected fallback behavior.

**Acceptance Scenarios**:

1. **Given** no user preference file exists and the Windows system UI language is Indonesian (`id`), **When** DisplayFlip launches for the first time, **Then** the interface defaults to Bahasa Indonesia.
2. **Given** no user preference file exists and the Windows system UI language is English or any other non-Indonesian language, **When** DisplayFlip launches for the first time, **Then** the interface defaults to English.

---

### Edge Cases

- **Missing or corrupted translation entry**: If any localized phrase is missing or empty in one language set, the application falls back to a readable non-empty string (never blank UI controls or raw internal key tokens).
- **Corrupted or unknown saved language value**: If the saved configuration file contains an unrecognized language code (e.g., `"fr"` or empty string), the application gracefully falls back to the default language selection rule without throwing errors.
- **Unapplied edits on 2D canvas during live language switch**: Switching language in General Settings and navigating back to Displays MUST preserve any unapplied monitor layout edits on the canvas rather than discarding them.
- **Dynamic user-created content**: User-defined profile names, custom hotkey labels, hardware monitor model names, and device paths (`\\.\DISPLAY1`) remain verbatim and MUST NOT be altered or translated.
- **Dynamic text length differences**: Longer English or Indonesian phrasing MUST wrap cleanly or fit inside buttons, badges, and subtitles without clipping or overlapping adjacent badges/controls.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support two complete UI and CLI language packs: Bahasa Indonesia (`id`) and English (`en`).
- **FR-002**: System MUST provide a dedicated language selector in General Settings allowing users to choose between Bahasa Indonesia and English (and optionally reset/follow system default).
- **FR-003**: System MUST apply language changes immediately across all active application views, navigation headers, 2D canvas annotations, dialogs, and status messages without requiring an application restart.
- **FR-004**: System MUST update the system tray context menu, tray tooltip, and tray action feedback whenever the active language changes.
- **FR-005**: System MUST persist the user's explicit language selection in the existing user configuration store so it is preserved across restarts and background tray sessions.
- **FR-006**: On first launch when no explicit language preference has been saved, the system MUST select Bahasa Indonesia if the operating system UI culture is Indonesian, and English for all other locales.
- **FR-007**: System MUST localize all CLI help descriptions, monitor listing labels, success outcomes, and error messages according to the saved preference, with support for an optional per-invocation CLI language flag (`--lang en` / `--lang id`); if `--lang` is omitted a value or given an unrecognized value, the system MUST fall back safely to the saved/default preference without crashing.
- **FR-008**: Every user-facing text entry defined in one supported language pack MUST have a corresponding non-empty translation in the other supported language pack (100% key parity across both languages).
- **FR-009**: User-created data (layout profile names, saved monitor identifiers, hotkey key combinations) and hardware identifiers MUST remain untouched by translation; low-level OS/driver error codes returned by the Windows display engine may remain appended verbatim inside localized user-facing wrappers.
- **FR-010**: Changing the active language MUST NOT overwrite or discard pending, unapplied monitor layout edits in the Displays view.
- **FR-011**: The About page MUST expose the official website link (`https://displayflip.snipgeek.com`) and the public GitHub repository link (`https://github.com/ieproject-app/DisplayFlip`), replacing any legacy repository URLs.

### Key Entities

- **Language Preference**: Represents the user's selected display language state (explicitly chosen `"id"` or `"en"`, or unset/system-follow on initial launch), stored alongside general preferences.
- **Localization Dictionary**: A structured pair of keyed text collections (Bahasa Indonesia and English) covering main window navigation, all settings pages, 2D canvas visual labels, modal dialogs, tray menu & notifications, and CLI messages.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of user-visible text elements across all application pages, modal dialogs, 2D monitor canvas labels, system tray menus, and CLI outputs render in the active language with zero untranslated leftover Indonesian phrases when switched to English (and vice versa).
- **SC-002**: Language switching in General Settings updates the active window and tray menu visibly in under 1 second without requiring an application restart.
- **SC-003**: 100% translation key parity is verified automatically between the English and Indonesian dictionaries (zero missing keys and zero empty values).
- **SC-004**: First-time launch with no configuration file opens in English on non-Indonesian OS locales and in Bahasa Indonesia on Indonesian OS locales in 100% of verified test cases.
- **SC-005**: Existing unapplied 2D canvas edits and existing user profile files created before this update remain intact and readable with zero data loss or migration errors.

## Assumptions

- Scope covers two languages (`id` and `en`) for the upcoming public open-source release; adding further community translations later will build on the same keyed localization structure.
- Existing stored user profiles and hotkey configurations from prior versions remain fully compatible without requiring manual user migration (missing language field in legacy config triggers the system-locale default rule until the user explicitly chooses a language).
- SnipGeek branding links or project URLs displayed on the About page remain consistent across both languages unless descriptive subtitles around them require localization.
