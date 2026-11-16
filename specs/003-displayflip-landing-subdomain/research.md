# Research & Technical Decisions: DisplayFlip Product Website (`displayflip.snipgeek.com`)

**Feature**: `003-displayflip-landing-subdomain` | **Date**: 2026-11-16

## Decision 1: Subdomain Routing & Local Preview Pattern
- **Decision**: Follow the exact `geocam` / `gear` subdomain routing pattern in `D:\GitHub\SnipGeek\src\proxy.ts`.
- **Rationale**:
  - `isDisplayFlipHost(hostname)` matches `displayflip.snipgeek.com`, `www.displayflip.snipgeek.com`, and `displayflip.*` (`displayflip.localhost`), redirecting `www.` -> `https://displayflip.snipgeek.com` (`308`) and rewriting `/` -> `/en/displayflip` with request headers `x-is-displayflip-subdomain: true` and `x-displayflip-hostname`.
  - Windows `hosts` often fails to resolve `*.localhost` out of the box, so adding an `isLocalHost(hostname) && (pathname === "/displayflip" || pathname.startsWith("/displayflip/"))` dev branch lets developers preview `http://localhost:9003/displayflip` immediately with full isolated chrome.
  - Production requests to `snipgeek.com/displayflip` redirect permanently (`301`) to `https://displayflip.snipgeek.com/`, preventing duplicate-host SEO indexing noise.
  - Excludes `!isDisplayFlipHost(hostname)` inside `shouldRedirectToCanonicalHost(hostname)` so Firebase default host fallback rules never bounce `displayflip.snipgeek.com` back to `snipgeek.com`.
- **Alternatives Considered**: Hosting a separate static GitHub Pages repo (`ieproject-app.github.io`) was rejected because AdSense monetization (`ca-pub-6235611333449307`) and analytics are consolidated in SnipGeek (`snipgeek.com`).

## Decision 2: Scoped Styling & Typography Parity (`Option 3: Command Terminal`)
- **Decision**: Scope all Option 3 visual tokens (Carbon Obsidian `#090b0a`, Daylight Terminal `#f3f5f4`, Emerald Phosphor `#10b981` / `#059669`, `48px` architectural gridlines, Double-Bezel radius tokens) under `html[data-subdomain="displayflip"]` and `html[data-subdomain="displayflip"].dark` in `src/app/globals.css`, while loading `IBM_Plex_Sans` via `next/font/google` (`variable: "--gf-displayflip"`) in `layout.tsx` alongside existing `fontMono` (`JetBrains_Mono`).
- **Rationale**: SnipGeek's global `snipgeek-rules` forbid modifying root `Bricolage_Grotesque`, `Plus_Jakarta_Sans`, or root `:root` / `.dark` HSL tokens because shared blog/labs routes depend on them. Scoped `data-subdomain` CSS rules (matching `html[data-subdomain="geocam"]`) isolate DisplayFlip visually while leaving the rest of SnipGeek untouched. Notice `next-themes` applies `.dark` class on `<html>`, so scoping both `html[data-subdomain="displayflip"]` (light) and `html[data-subdomain="displayflip"].dark` (dark) ensures instant sync with `ThemeProvider`.
- **Alternatives Considered**: Mutating `:root` CSS vars or using inline `<style>` tags was rejected for violating SnipGeek design system rules and risking style leaks during client navigation.

## Decision 3: Anchor Navigation Offset Fix (User Feedback)
- **Decision**: Add `scroll-margin-top: 96px` (`scroll-mt-24` on `#cli-lab`, `#architecture`, `#downloads`, `#faq`) plus programmatic smooth scroll handling when closing the mobile drawer so anchor jumps clear the Floating Fluid Island navbar (`top: 16px`, `height: 54px` -> bottom at `70px`, plus `26px` breathing clearance = `96px`).
- **Rationale**: Directly resolves the user's feedback on Option 3 (*"pas klik menu di headernya dia judulnya seperti rada tertutup itu nanti bantu fix aja sekalian pas implementasi"*). In the plain HTML preview, sections lacked `scroll-margin-top`, causing native `#anchor` scroll to align section top edge at `y = 0` directly underneath the sticky `top: 16px` island header.
- **Alternatives Considered**: Switching from a sticky floating island to a static top header would sacrifice the approved Option 3 Floating Fluid Island signature. `scroll-margin-top: 96px` preserves the island nav while giving every section heading crisp clearance.

## Decision 4: AdSense Architecture & Unconfigured Placeholder Behavior
- **Decision**: Register `AD_SLOTS.displayflip` in `D:\GitHub\SnipGeek\src\lib\adsense.ts` with 4 named keys (`belowHero: ""`, `guideRail: ""`, `inGuide: ""`, `preFooter: ""`), wrapped in a lightweight `DisplayFlipAdSlot` component that delegates to `@/components/analytics/adsense-unit` whenever `isAdSlotConfigured(slot)` is true, and renders clean dev-only Double-Bezel sizing previews when empty in development (`process.env.NODE_ENV === "development"`).
- **Rationale**: Matches user instruction ("Buat tempat aja dulu gak papa, nanti slot nya kita buat belakangan") and mirrors `AD_SLOTS.randomNamePicker.belowPicker` convention in `src/lib/adsense.ts`: in production, unconfigured slots return `null` so Google AdSense never throws invalid-slot errors before the owner generates live slot IDs; adding slot IDs later takes a 4-line update in `src/lib/adsense.ts`. Notice `AdSenseUnit` already wraps `leaderboard` (`min-h-[90px]`), `mediumRectangle` (`min-h-[250px] max-w-[300px]`), `inArticle` (`min-h-[280px]`), and `horizontal` (`min-h-[90px]`) anti-CLS classes and `ResizeObserver` width guards.
- **Alternatives Considered**: Hardcoding dummy slot numbers like `'0000000000'` would trigger live AdSense 400 errors in production.

## Decision 5: Prototype Dock Removal & Theme Synchronization
- **Decision**: Exclude the prototype comparison dock (`All 3 | Opt 1 | Opt 2 | Opt 3 | Ads: On/Off`) from production SnipGeek deployment; instead surface a dedicated Light/Dark theme toggle button cleanly inside the Floating Fluid Island right cluster (`DisplayFlipHeader`) and keep `Option 3`'s Dark-first (`defaultTheme="dark"` fallback feel or clean adaptation for both `light` & `.dark` `next-themes` states) so every Windows 11 screenshot (`-dark.webp` / `-light.webp`) matches the active theme.
- **Rationale**: `ThemeProvider` in `src/app/[locale]/layout.tsx` uses `defaultTheme="system"` globally; using `useTheme()` (`resolvedTheme`) inside `DisplayFlipLanding` automatically displays `-dark.webp` captures in dark mode and `-light.webp` captures in light mode without fighting global `ThemeProvider`.
- **Alternatives Considered**: Keeping `Ads: On/Off` toggle in production violates AdSense policy and exposes internal prototype controls to end users.

## Decision 6: Grounded Copy, Download Links & Pre-Deploy Guards
- **Decision**: Pull all package links, file sizes, test counts, and CLI flags strictly from verified `D:\GitHub\multi-monitor\README.md` (`DisplayFlip-v1.1.0-win-x64.zip` ~3 MB / 6.87 MB executable requiring .NET 10 Desktop Runtime; `DisplayFlip-v1.1.0-Standalone-win-x64.exe` ~74 MB portable self-contained; tag `v1.1.0`; `316` automated tests), use `"&rarr;"` or ASCII `"->"` instead of raw Unicode arrows in JSX text (per `snipgeek-rules` Cloud Build safeguard), and add `isDisplayFlipSubdomain` / `DisplayFlipHeader` / `DisplayFlipFooter` assertions to `D:\GitHub\SnipGeek\scripts\pre-deploy-check.mjs`.
- **Rationale**: Prevents stale metrics, Cloud Build minification panics, or accidental removal of isolation checks in future SnipGeek commits.
