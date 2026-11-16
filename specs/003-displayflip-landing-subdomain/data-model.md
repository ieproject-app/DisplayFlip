# Data Model: DisplayFlip Product Website (`displayflip.snipgeek.com`)

**Feature**: `003-displayflip-landing-subdomain` | **Date**: 2026-11-16

All entities below are static TypeScript data structures in `D:\GitHub\SnipGeek\src\data\displayflip.ts` and client UI state; no database tables or server mutations are required.

## 1. `HotkeyDeckPreset`
Represents one interactive button in the Hero Hotkey Deck that updates both the simulated terminal log and preview capture.

| Field | Type | Validation / Constraints | Example |
|---|---|---|---|
| `id` | `'displays' \| 'shortcuts' \| 'about'` | Unique key matching WebP filename prefix | `'shortcuts'` |
| `shortcutLabel` | `string` | Non-empty monospace key combo or CLI call | `'Win + Shift + 1'` |
| `caption` | `string` | Short 2–4 word English caption | `'Conflict-Safe Hotkeys'` |
| `statusLog` | `string` | ASCII-safe terminal status readout (`->`, zero raw `→`) | `'$ DisplayFlip.exe --hotkey Win+Shift+1 -> [OK] Bound to Studio Desk'` |
| `imageAlt` | `string` | Descriptive English alt text | `'DisplayFlip Keyboard Shortcuts panel'` |

## 2. `CliFlagOption`
Represents selectable command-line flags in the PowerShell / Task Scheduler / AutoHotkey v2 Generator.

| Field | Type | Validation / Constraints | Example |
|---|---|---|---|
| `flag` | `'--apply' \| '--swap' \| '--solo 1' \| '--solo 2' \| '--extend' \| '--list --json'` | Must match actual `DisplayFlip.exe` v1.1.0 CLI flags in `README.md` | `'--apply'` |
| `requiresProfile` | `boolean` | Only `true` for `'--apply'`; disables Profile Name input when `false` | `true` |
| `summary` | `string` | Short English description for tooltip/aria | `'Apply saved monitor profile'` |

## 3. `CliGeneratorState` & Output Mapping
Client state in `DisplayFlipLanding`:

- `activeFlag`: `CliFlagOption['flag']` (default `'--apply'`)
- `profileName`: `string` (default `'Studio Desk'`; sanitized via `.replace(/["']/g, '').trim() || 'Studio Desk'`)
- `langOverride`: `'' | '--lang en' | '--lang id'` (default `''`)
- `targetFormat`: `'cli' | 'ahk' | 'task'` (default `'cli'`)

Deterministic command synthesis:
1. `argList = [activeFlag, ...(requiresProfile ? [`"${sanitizedProfile}"`] : []), ...(langOverride ? [langOverride] : [])]`
2. `argStr = argList.join(' ')`
3. Output by `targetFormat`:
   - `'cli'` -> `DisplayFlip.exe ${argStr}`
   - `'ahk'` -> `F13::Run('DisplayFlip.exe ${argStr}')`
   - `'task'` -> `New-ScheduledTaskAction -Execute "DisplayFlip.exe" -Argument '${argStr}'`

## 4. `ReleasePackage`
Represents verified DisplayFlip `v1.1.0` download cards sourced from `D:\GitHub\multi-monitor\README.md`:

| Field | Type | Value (Standard ZIP) | Value (Standalone EXE) |
|---|---|---|---|
| `id` | `'zip' \| 'standalone'` | `'zip'` | `'standalone'` |
| `badge` | `string` | `'Recommended - ~3 MB ZIP'` | `'Portable - ~74 MB Single EXE'` |
| `filename` | `string` | `'DisplayFlip-v1.1.0-win-x64.zip'` | `'DisplayFlip-v1.1.0-Standalone-win-x64.exe'` |
| `description` | `string` | Requires `.NET 10 Desktop Runtime`. Fast startup for daily tray usage. | Self-contained build with runtime bundled inside. Runs from USB drives. |
| `href` | `string` | `https://github.com/ieproject-app/DisplayFlip/releases/tag/v1.1.0` | `https://github.com/ieproject-app/DisplayFlip/releases/tag/v1.1.0` |
| `ctaLabel` | `string` | `'Get ZIP on GitHub'` | `'Get Standalone EXE'` |
| `isPrimary` | `boolean` | `true` | `false` |

## 5. `AdSlotKey` (`AD_SLOTS.displayflip`)
Defined in `D:\GitHub\SnipGeek\src\lib\adsense.ts`:

| Key | AdSense Size Prop | Placement & Policy Constraint | Initial Value |
|---|---|---|---|
| `belowHero` | `'leaderboard'` | Centered horizontal slot (`min-h-[90px]`) directly below hero, well away from primary CTA row | `''` |
| `guideRail` | `'mediumRectangle'` | Desktop sticky `300x250` slot inside `.sticky top-24` aside beside the CLI Lab | `''` |
| `inGuide` | `'inArticle'` | Mid-page horizontal break (`min-h-[250px]`) between Fluent Engine showcase & Download cards | `''` |
| `preFooter` | `'horizontal'` | Closing banner cleanly above `DisplayFlipFooter` | `''` |

## 6. `DisplayFlipFaqItem`
Pairs visible `<details>` FAQ accordion items and `FAQPage` JSON-LD `mainEntity` schema from a single array (`DISPLAYFLIP_FAQS`) so structured data never diverges from rendered page copy.
