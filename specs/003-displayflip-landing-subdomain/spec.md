# Feature Specification: DisplayFlip Product Website & Subdomain (`displayflip.snipgeek.com`)

**Feature Branch**: `003-displayflip-landing-subdomain`
**Created**: 2026-11-16
**Status**: Approved Design Direction (Option 3: Command Terminal)
**Input**: User selected Option 3 (`option-3-win11-dark.html` Command Terminal prototype) as the canonical product landing experience for `displayflip.snipgeek.com`, requested anchoring offset adjustment so sticky header clicks never obscure section titles, and authorized proceeding directly into SpecKit specification, planning, and task breakdown.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Discover and Download DisplayFlip on Dedicated Domain (Priority: P1)

A Windows 10/11 multi-monitor user visits `https://displayflip.snipgeek.com/` (or discovers it from search results or the main site directory) and immediately understands what DisplayFlip does, sees real Windows 11 application captures, and downloads either the compact ~3 MB ZIP package or the ~74 MB standalone single-file executable.

**Why this priority**: Delivering the isolated product showcase and verified download entry points is the core business and product goal of launching `displayflip.snipgeek.com`.

**Independent Test**: Open `https://displayflip.snipgeek.com/` in desktop (`1440x900`) and mobile (`390x844`) viewports, confirm zero SnipGeek main-blog header/footer leakage, inspect English SEO metadata and structured product schemas, and verify both GitHub v1.1.0 release download links resolve to the published `v1.1.0` assets.

**Acceptance Scenarios**:

1. **Given** a visitor opens `https://displayflip.snipgeek.com/`, **When** the page loads, **Then** they see the isolated Command Terminal experience with Carbon Obsidian grid backdrop, Emerald Phosphor accents, Floating Fluid Island navigation, zero horizontal overflow, and crisp above-the-fold hero copy and capture preview.
2. **Given** a visitor clicks a top navigation link (`CLI Lab`, `Screens`, `Packages`, `FAQ`), **When** the page scrolls smoothly to the target section, **Then** the target section heading clears below the floating sticky header without being overlapped or partially covered.
3. **Given** a visitor clicks either `Get ZIP on GitHub` (`DisplayFlip-v1.1.0-win-x64.zip`) or `Get Standalone EXE` (`DisplayFlip-v1.1.0-Standalone-win-x64.exe`), **When** following the link, **Then** it opens the official `v1.1.0` release distribution on GitHub (`https://github.com/ieproject-app/DisplayFlip/releases/tag/v1.1.0`).

---

### User Story 2 - Interactive Hotkey Deck & Multi-Target CLI Automation Lab (Priority: P2)

A power user or Windows developer explores how DisplayFlip works before downloading by testing global shortcut actions in the Hero Hotkey Deck (`Ctrl + Alt + S`, `Win + Shift + 1`, `DisplayFlip --apply`) and generating copy-ready automation commands for CMD/PowerShell, AutoHotkey v2, or Windows Task Scheduler in the interactive CLI Lab.

**Why this priority**: The interactive Hotkey Deck and CLI Builder transform passive software marketing into an immediate utility that showcases DisplayFlip's zero-bloat CLI and hotkey architecture while increasing engaged time on page.

**Independent Test**: Click each key in the Hero Hotkey Deck and verify both the live status execution log and screenshot preview update instantaneously; switch CLI flags, profile names, language override, and output format (`cli`, `ahk`, `task`) in the CLI Lab and verify accurate command generation plus one-click clipboard copy feedback.

**Acceptance Scenarios**:

1. **Given** a visitor clicks `Win + Shift + 1` or `DisplayFlip --apply` on the Hero Hotkey Deck, **When** activated, **Then** the live execution readout line and displayed Windows 11 application capture switch to the corresponding view (`shortcuts` or `about`).
2. **Given** a visitor selects `--solo 2` and switches Output Format to `AutoHotkey v2 Bind (F13)`, **When** the selection updates, **Then** the generated output reads `F13::Run('DisplayFlip.exe --solo 2')` and the Profile Name input disables cleanly when not needed by the selected flag.
3. **Given** a visitor clicks `Copy`, **When** the command is copied to clipboard, **Then** the button provides visible confirmation (`Copied`) before returning to `Copy`.

---

### User Story 3 - Theme Parity, Visual Walkthrough & AdSense Monetization Readiness (Priority: P2)

A visitor switches between Dark and Light display themes matching Windows 11 preferences, reviews real Light/Dark screenshots of Keyboard Shortcuts and General & Icons settings, reads the Technical FAQ, and views policy-compliant AdSense placements that avoid accidental clicks near download CTAs and prevent layout shift.

**Why this priority**: AdSense is the primary monetization channel for SnipGeek subdomains, while full Light/Dark theme parity proves DisplayFlip's real Windows 11 Fluent polish and satisfies user visual trust.

**Independent Test**: Toggle Dark/Light theme and verify all theme-aware images swap cleanly between `-dark.webp` and `-light.webp`; verify all four AdSense slot boundaries are positioned cleanly (`belowHero`, `guideRail`, `inGuide`, `preFooter`) with reserved dimensions and no overlap against download action buttons.

**Acceptance Scenarios**:

1. **Given** the page renders in its default Dark theme or switched Light theme, **When** a user toggles Light/Dark mode, **Then** colors, code terminal contrast, and every Windows 11 capture (`displays`, `shortcuts`, `general`, `about`) update synchronously.
2. **Given** the four AdSense placement zones are mounted alongside page content, **When** slots are unconfigured (empty slot IDs during initial DNS propagation and setup), **Then** placeholders do not trigger malformed live ad requests in production and preserve clean visual spacing until real slot IDs are supplied.
3. **Given** real AdSense slot IDs are configured in the central AdSense registry later, **When** served in production on `displayflip.snipgeek.com`, **Then** all four slots load lazily without blocking page render or causing Cumulative Layout Shift.

---

### User Story 4 - Clean Subdomain Routing, Isolation & Search Engine Indexing (Priority: P1)

Traffic arriving at `displayflip.snipgeek.com` is routed deterministically without CDN cache poisoning or main-site navigation leakage, legacy or subpath URLs normalize cleanly, and search engines discover a dedicated English sitemap and canonical metadata exclusive to `displayflip.snipgeek.com`.

**Why this priority**: Broken subdomain isolation, stale CDN redirects, or mixed Indonesian/English hreflangs previously caused severe search indexing regressions on SnipGeek and must be prevented by design.

**Independent Test**: Run proxy routing tests, domain sitemap generation checks, layout isolation checks, and pre-deploy guards to verify deterministic host handling, zero main-site header/footer leakage, valid English canonical metadata (`https://displayflip.snipgeek.com`), and valid dedicated sitemap output.

**Acceptance Scenarios**:

1. **Given** a request hits `displayflip.snipgeek.com` (or `www.displayflip.snipgeek.com` or local `/displayflip` preview path), **When** routed by edge proxy, **Then** `www.` normalizes permanently to `https://displayflip.snipgeek.com`, internal routing rewrites cleanly without reading user cookies, and dedicated subdomain chrome replaces the main SnipGeek header/footer.
2. **Given** a request hits `snipgeek.com/displayflip` in production, **When** processed, **Then** it permanently redirects to `https://displayflip.snipgeek.com/`.
3. **Given** a search crawler requests `/sitemap.xml` on `displayflip.snipgeek.com`, **When** served, **Then** it returns only `https://displayflip.snipgeek.com` with English single-locale canonicals and zero `/id` alternates.

### Edge Cases

- Clicking top anchor links (`#cli-lab`, `#architecture`, `#downloads`, `#faq`) from scrolled positions or directly via URL hash (`/#downloads`) MUST position section headings cleanly below the sticky Floating Fluid Island header (`scroll-margin-top` offset) on both desktop and mobile viewports.
- On narrow mobile viewports (`320px` to `390px`), the Floating Fluid Island collapses desktop links into a morphing glass drawer menu that opens and closes cleanly, traps/dismisses focus gracefully on link activation, and prevents horizontal page overflow.
- Users with OS `prefers-reduced-motion: reduce` enabled MUST see all sections immediately at full opacity without blur/slide delays.
- Unconfigured AdSense slot IDs MUST NOT generate console errors or blank broken boxes in production before slot creation is completed by the owner.
- Visitors using localhost preview (`localhost:PORT/displayflip`) on Windows (where `*.localhost` may not resolve automatically) MUST be able to preview the isolated subdomain experience cleanly without editing system hosts files.
- Non-empty/special characters typed in the CLI Profile Name field MUST be sanitized cleanly so quotes cannot break the generated command output preview.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST serve an isolated, English-only product landing site at `https://displayflip.snipgeek.com/` completely free of main-site blog navigation, footer links, and Indonesian (`id`) language alternates.
- **FR-002**: The subdomain edge router MUST rewrite incoming requests for `displayflip.snipgeek.com` (including `displayflip.*` host headers and local `/displayflip` preview paths) without inspecting cookies or varying CDN cache keys by user headers, and redirect `www.displayflip.snipgeek.com` and production `snipgeek.com/displayflip` traffic permanently to `https://displayflip.snipgeek.com`.
- **FR-003**: The landing page visual identity MUST implement the approved **Option 3 (Command Terminal)** design: Carbon Obsidian / Daylight Terminal surface tokens scoped strictly to the `displayflip` subdomain, `48px` architectural grid backdrop, Emerald Phosphor accent tokens, Floating Fluid Island navigation, Double-Bezel hardware enclosures, Button-in-Button CTAs, and `IBM Plex Sans` + `JetBrains Mono` typography.
- **FR-004**: Anchor-targeted content sections MUST reserve sufficient top scroll offset (`scroll-margin-top` >= header height + top gap) so clicking header or mobile drawer navigation links never obscures section headings beneath the floating navigation bar.
- **FR-005**: The Hero module MUST provide an interactive Hotkey Deck (`Ctrl + Alt + S`, `Win + Shift + 1`, `DisplayFlip --apply`) that synchronously updates both the live execution log readout and the displayed Windows 11 application screenshot.
- **FR-006**: The CLI & Task Automation Builder MUST support interactive selection of DisplayFlip v1.1.0 flags (`--apply`, `--swap`, `--solo 1`, `--solo 2`, `--extend`, `--list --json`), custom Profile Name input (enabled only for `--apply`), optional Language override (`--lang en`, `--lang id`), and multi-target formatting (`Direct Command`, `AutoHotkey v2`, `PowerShell Scheduled Action`) with clipboard copy feedback.
- **FR-007**: All four Windows 11 captures (`displays`, `shortcuts`, `general`, `about`) MUST optimize within SnipGeek's image size budget (`<= 130 KB` per asset) and switch cleanly between `-dark.webp` and `-light.webp` variants alongside the page Dark/Light theme state.
- **FR-008**: The page MUST reserve four structured AdSense containers (`belowHero` 96px min-height, sticky desktop `guideRail` 300x250 min-height, `inGuide` 250px min-height, and `preFooter` 110px min-height) registered centrally under `AD_SLOTS.displayflip`, with empty placeholder IDs initially until live slot IDs are assigned, avoiding proximity to download CTA buttons.
- **FR-009**: The page MUST emit authoritative English metadata (`metadataBase: https://displayflip.snipgeek.com`, canonical root URL, OpenGraph/Twitter tags, favicon/manifest branding) and structured JSON-LD (`SoftwareApplication` for DisplayFlip v1.1.0 + `FAQPage`), plus a dedicated subdomain XML sitemap entry.
- **FR-010**: Automated pre-deploy guards and proxy unit tests MUST cover `displayflip.snipgeek.com` isolation so future edits cannot leak main-site chrome or break CDN cache safety.

### Key Entities

- **Subdomain Descriptor (`displayflip`)**: Routing host (`displayflip.snipgeek.com`), isolation request headers (`x-is-displayflip-subdomain`, `x-displayflip-hostname`), dedicated chrome (`DisplayFlipHeader`, `DisplayFlipFooter`), and scoped styling (`html[data-subdomain="displayflip"]`).
- **DisplayFlip Release Metadata (v1.1.0)**: Canonical links and binary specs grounded in `DisplayFlip` v1.1.0 release (`DisplayFlip-v1.1.0-win-x64.zip` ~3 MB / 6.87 MB executable requiring .NET 10 Desktop Runtime; `DisplayFlip-v1.1.0-Standalone-win-x64.exe` ~74 MB zero-prerequisite build; GitHub release tag `v1.1.0`).
- **AdSense Slot Registry (`AD_SLOTS.displayflip`)**: Named slot placeholders (`belowHero`, `guideRail`, `inGuide`, `preFooter`) mapped to Google AdSense publisher `ca-pub-6235611333449307`.
- **Interactive Hotkey & CLI State**: Active capture view key (`displays` | `shortcuts` | `about`), active CLI flag, sanitized profile name string, language override flag, and target script emitter (`cli` | `ahk` | `task`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `https://displayflip.snipgeek.com/` renders cleanly with zero console errors, zero broken images, and zero horizontal overflow across `320px`, `375px`, `390px`, `768px`, `1024px`, and `1440px` viewports.
- **SC-002**: Clicking every header navigation link scrolls smoothly so the target section heading `top` coordinate sits below the floating header bottom (`>= 84px` from viewport top).
- **SC-003**: Automated repository checks (`pre-deploy-check.mjs`, TypeScript typecheck, ESLint, proxy unit tests, and production Next.js build) complete with 0 errors and every `public/images/displayflip/*.webp` asset stays strictly `<= 130 KB`.
- **SC-004**: 100% of interactive Hero Hotkey Deck presets and CLI Lab combinations (`6 flags x 3 output targets`) produce valid, syntax-accurate DisplayFlip v1.1.0 commands and live UI transitions without page reloads.
- **SC-005**: Subdomain isolation tests confirm zero leakage of main SnipGeek chrome, zero `hreflang="id"` tags on the subdomain, and deterministic cookie-free routing across proxy tests.

## Assumptions

- DNS & Firebase App Hosting custom domain propagation for `displayflip.snipgeek.com` is already being configured by the owner externally; application routing supports both live subdomain traffic and local `localhost:PORT/displayflip` preview immediately.
- Google AdSense slot IDs will initially be registered as empty strings (`''`) in `AD_SLOTS.displayflip` ("buat tempat aja dulu") so the owner can paste real slot IDs from the AdSense dashboard without changing page layout code.
- Binary installers and portable archives continue to be hosted on GitHub Releases (`ieproject-app/DisplayFlip`), keeping SnipGeek bandwidth minimal while serving high-intent Windows documentation, interactive automation builder, and ad impressions cleanly on `displayflip.snipgeek.com`.
- User confirmed removing/omitting the prototype comparison dock (`All 3`, `Opt 1/2/3`, `Ads On/Off`) in production; production keeps a real Light/Dark theme toggle matching SnipGeek `next-themes` conventions.
