---

description: "Implementation task list for DisplayFlip Product Website & Subdomain (displayflip.snipgeek.com)"
---

# Tasks: DisplayFlip Product Website & Subdomain (`displayflip.snipgeek.com`)

**Input**: Design documents from `/specs/003-displayflip-landing-subdomain/`
**Prerequisites**: [plan.md](file:///D:/GitHub/multi-monitor/specs/003-displayflip-landing-subdomain/plan.md) (required), [spec.md](file:///D:/GitHub/multi-monitor/specs/003-displayflip-landing-subdomain/spec.md) (required), [research.md](file:///D:/GitHub/multi-monitor/specs/003-displayflip-landing-subdomain/research.md), [data-model.md](file:///D:/GitHub/multi-monitor/specs/003-displayflip-landing-subdomain/data-model.md), [contracts/](file:///D:/GitHub/multi-monitor/specs/003-displayflip-landing-subdomain/contracts/)

**Note on Cross-Repository Target**: Specification artifacts live in `D:\GitHub\multi-monitor\specs\003-displayflip-landing-subdomain\`. All application source paths (`public/...`, `src/...`, `scripts/...`) below refer to the Next.js codebase at `D:\GitHub\SnipGeek\`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story traceability (`[US1]`, `[US2]`, `[US3]`, `[US4]`)
- Exact file paths included in every task description

---

## Phase 1: Setup (Shared Assets & Brand Scoped Tokens)

**Purpose**: Copy verified `< 52 KB` WebP screenshots, create DisplayFlip icon branding, and define typed data models & scoped Option 3 tokens inside `D:\GitHub\SnipGeek`.

- [x] T001 Copy verified WebP captures (`displays-dark.webp`, `displays-light.webp`, `shortcuts-dark.webp`, `shortcuts-light.webp`, `general-dark.webp`, `general-light.webp`, `about-dark.webp`, `about-light.webp`, each strictly <= 52 KB) from `D:\GitHub\multi-monitor\docs\landing-previews\assets\` to `D:\GitHub\SnipGeek\public\images\displayflip\`
- [x] T002 [P] Create dual-monitor Command Terminal favicon SVG in `D:\GitHub\SnipGeek\public\displayflip-favicon.svg`
- [x] T003 [P] Create typed DisplayFlip release metadata, Hotkey Deck presets (`displays` | `shortcuts` | `about` with ASCII `->` terminal logs), CLI flags (`--apply`, `--swap`, `--solo 1`, `--solo 2`, `--extend`, `--list --json`), and FAQ entities in `D:\GitHub\SnipGeek\src\data\displayflip.ts`
- [x] T004 [P] Register `AD_SLOTS.displayflip` (`belowHero: ""`, `guideRail: ""`, `inGuide: ""`, `preFooter: ""` mapped to `ca-pub-6235611333449307`) in `D:\GitHub\SnipGeek\src\lib\adsense.ts`

---

## Phase 2: Foundational (Subdomain Routing, Isolation Chrome & Scoped CSS)

**Purpose**: Establish fail-safe subdomain routing, CSS isolation tokens, anchor clearance rules, and Chrome isolation before user story modules render.

**CRITICAL**: No user story work should go live before subdomain isolation, anchor offset styling, and CDN cache guards are in place.

- [x] T005 [US4] Write unit tests covering `displayflip.snipgeek.com`, `www.displayflip.snipgeek.com` 308 canonicalization, `/displayflip` localhost preview rewrite, production `snipgeek.com/displayflip` 301 redirect, and cookie-free CDN caching in `D:\GitHub\SnipGeek\src\proxy.test.ts`
- [x] T006 Implement `displayflipHost`, `isDisplayFlipHost`, `getDisplayFlipRedirectUrl`, `shouldRedirectToCanonicalHost` exclusion, localhost `/displayflip` preview, production `/displayflip` 301 redirect, and `x-is-displayflip-subdomain` / `x-displayflip-hostname` headers in `D:\GitHub\SnipGeek\src\proxy.ts` (depends on T005)
- [x] T007 [P] Scope Option 3 Command Terminal Carbon Obsidian / Daylight Terminal HSL tokens, `48px` architectural gridlines, Double-Bezel utilities, Button-in-Button hover transitions, and `scroll-mt-24` / `scroll-margin-top: 96px` rules strictly under `html[data-subdomain="displayflip"]` and `html[data-subdomain="displayflip"].dark` in `D:\GitHub\SnipGeek\src\app\globals.css` (without mutating global `:root` or `.dark` variables)
- [x] T008 [P] Load `IBM_Plex_Sans` via `next/font/google` (`variable: "--gf-displayflip"`) alongside existing global font loaders without replacing `Bricolage_Grotesque` or `Plus_Jakarta_Sans` in `D:\GitHub\SnipGeek\src\app\[locale]\layout.tsx`

**Checkpoint**: `npx vitest run src/proxy.test.ts` passes and scoped subdomain tokens compile cleanly without affecting main SnipGeek styles.

---

## Phase 3: User Story 4 - Clean Subdomain Chrome, Metadata & Sitemap Isolation (Priority: P1)

**Goal**: Serve `displayflip.snipgeek.com` under its own Floating Fluid Island header, minimal footer, isolated English metadata, dedicated XML sitemap, and pre-deploy guard enforcement.

**Independent Test**: Request `https://displayflip.snipgeek.com/` (or `http://localhost:9003/displayflip`) and confirm main-site navigation chrome is absent, `<html lang="en" data-subdomain="displayflip">` is rendered, and `/sitemap.xml` on `displayflip` host emits only `https://displayflip.snipgeek.com`.

- [x] T009 [P] [US4] Build Floating Fluid Island header (`>_ DisplayFlip` brand mark, nav links `#cli-lab`, `#architecture`, `#downloads`, `#faq`, Light/Dark theme toggle via `next-themes`, desktop `Download v1.1.0` CTA, and 2-line morphing mobile glass drawer that closes before anchor scroll) in `D:\GitHub\SnipGeek\src\components\layout\displayflip-header.tsx`
- [x] T010 [P] [US4] Build minimal isolated footer (`DisplayFlip Command Workbench Edition on SnipGeek` + `GitHub`, `SnipGeek`, `Privacy`, `Terms` links) in `D:\GitHub\SnipGeek\src\components\layout\displayflip-footer.tsx`
- [x] T011 [US4] Wire `isDisplayFlipSubdomain` (`x-is-displayflip-subdomain` header / `displayflip.` host), `metadataBase: https://displayflip.snipgeek.com`, `data-subdomain="displayflip"`, `fontDisplayFlip.variable`, and `DisplayFlipHeader`/`DisplayFlipFooter` branch in `D:\GitHub\SnipGeek\src\app\[locale]\layout.tsx` (depends on T008, T009, T010)
- [x] T012 [P] [US4] Add `isDisplayFlipSubdomain` header/host detection and dedicated single-URL XML sitemap output (`https://displayflip.snipgeek.com`) in `D:\GitHub\SnipGeek\src\app\sitemap.ts`
- [x] T013 [P] [US4] Add Section 3.5 isolation assertions for `isDisplayFlipSubdomain`, `DisplayFlipHeader`, `DisplayFlipFooter`, `isDisplayFlipHost`, and `x-is-displayflip-subdomain` in `D:\GitHub\SnipGeek\scripts\pre-deploy-check.mjs`

**Checkpoint**: User Story 4 isolation verified via `node scripts/pre-deploy-check.mjs`.

---

## Phase 4: User Story 1 - Option 3 Core Landing Showcase, Packages & Anchor Clearance (Priority: P1) MVP

**Goal**: Deliver the English-only DisplayFlip v1.1.0 Command Terminal landing page featuring Hero copy + CTAs, 2-row Fluent 2 engine capture showcase (`Keyboard Shortcuts` & `General & Icons`), verified v1.1.0 GitHub download packages, FAQ accordion, JSON-LD schemas, and fixed `scroll-margin-top` anchor navigation clearance.

**Independent Test**: Open `/displayflip`, click every nav link (`CLI Lab`, `Screens`, `Packages`, `FAQ`), verify every target section `<h2>` clears well below the sticky Floating Fluid Island (`>= 84px` from viewport top), verify v1.1.0 ZIP & Standalone EXE download links, and confirm zero raw Unicode arrows (`->` or Lucide icons only).

- [x] T014 [P] [US1] Implement hero, staggered 2-row Fluent 2 showcase (`#architecture` with `scroll-mt-24 md:scroll-mt-28`), verified v1.1.0 package download grid (`#downloads`), technical FAQ accordion (`#faq`), and zero-em-dash English copy in `D:\GitHub\SnipGeek\src\components\displayflip\displayflip-landing.tsx`
- [x] T015 [US1] Create `src/app/[locale]/displayflip/page.tsx` exporting English single-locale `generateMetadata` (`canonical: https://displayflip.snipgeek.com`, OpenGraph/Twitter summary card, zero `hreflang="id"`), JSON-LD (`SoftwareApplication` v1.1.0 + `FAQPage`), and `<DisplayFlipLanding />` in `D:\GitHub\SnipGeek\src\app\[locale]\displayflip\page.tsx` (depends on T003, T014)

**Checkpoint**: User Story 1 MVP loads cleanly with zero layout overflow, working anchor scroll clearance (`top >= 84px`), valid JSON-LD, and verified GitHub download CTAs.

---

## Phase 5: User Story 2 - Interactive Hotkey Deck & Multi-Target CLI Automation Lab (Priority: P2)

**Goal**: Activate interactive Option 3 tooling inside `DisplayFlipLanding`: the Hero Hotkey Deck switching live terminal execution readouts + screenshot captures, and the PowerShell / Task Scheduler / AutoHotkey v2 Builder (`#cli-lab`) with clipboard copy state.

**Independent Test**: Click each preset in Hero Hotkey Deck (`Ctrl + Alt + S`, `Win + Shift + 1`, `DisplayFlip --apply`) and verify synchronous `#heroLog` + image update; toggle `--solo 2` and `AutoHotkey v2` in `#cli-lab` and confirm exact output `F13::Run('DisplayFlip.exe --solo 2')` + profile field auto-disable + clipboard copy confirmation.

- [x] T016 [US2] Implement Hero Hotkey Deck interactive state switching (`displays` | `shortcuts` | `about` image + `$ DisplayFlip.exe ... -> [OK] ...` live log readout) inside `D:\GitHub\SnipGeek\src\components\displayflip\displayflip-landing.tsx`
- [x] T017 [US2] Implement Multi-Target CLI Automation Lab (`#cli-lab` with `scroll-mt-24 md:scroll-mt-28`, flag selector pills, sanitized `Profile Name` input disabled unless `--apply`, `--lang` selector, `cli`/`ahk`/`task` formatter, and `Copy` / `Copied` clipboard button) inside `D:\GitHub\SnipGeek\src\components\displayflip\displayflip-landing.tsx` (depends on T016)

**Checkpoint**: Hero Hotkey Deck and all `6 x 3` CLI Builder flag/target combinations work interactively without console errors or React hydration mismatches.

---

## Phase 6: User Story 3 - Theme Parity & 4-Slot AdSense Containers (Priority: P2)

**Goal**: Synchronize all DisplayFlip Windows 11 screenshots (`-dark.webp` / `-light.webp`) with `next-themes` Light/Dark mode and mount all 4 policy-compliant AdSense placement enclosures (`belowHero`, sticky `guideRail`, `inGuide`, `preFooter`).

**Independent Test**: Toggle theme button in `DisplayFlipHeader` and verify all 4 screenshots switch `-dark.webp` <-> `-light.webp`; verify dev-mode Double-Bezel AdSense placeholders reserve exact heights (`96px`, `250px`, `250px`, `110px`) and gracefully omit unfilled requests in production while `AD_SLOTS.displayflip` strings remain `''`.

- [x] T018 [P] [US3] Build Double-Bezel `DisplayFlipAdSlot` wrapper delegating configured slots to `@/components/analytics/adsense-unit` and showing sized dev-only slot labels (`belowHero`, `guideRail`, `inGuide`, `preFooter`) when `isAdSlotConfigured(slot)` is false in `D:\GitHub\SnipGeek\src\components\displayflip\displayflip-ad-slot.tsx`
- [x] T019 [US3] Integrate `useTheme()` (`resolvedTheme`) capture switching (`-dark.webp` / `-light.webp` with safe hydration fallback) and mount all 4 `DisplayFlipAdSlot` placements (`belowHero`, sticky `lg:sticky lg:top-24` `guideRail`, `inGuide`, `preFooter`) inside `D:\GitHub\SnipGeek\src\components\displayflip\displayflip-landing.tsx` (depends on T014, T017, T018)

**Checkpoint**: Light/Dark theme switching updates palette + all WebP captures cleanly; AdSense slots reserve anti-CLS boxes in dev and skip invalid calls in production until configured.

---

## Phase 7: Polish, Quality Gates & Visual Verification

**Purpose**: Verify full cross-repo correctness, anchor clearance, mobile drawer physics, TypeScript/ESLint/build cleanliness, and SnipGeek pre-deploy compliance.

- [x] T020 [P] Remove unused early-stage `concept-a/b/c.html` preview drafts from `D:\GitHub\multi-monitor\docs\landing-previews\` if obsolete, or update `D:\GitHub\multi-monitor\docs\landing-previews\index.html` status note indicating Option 3 selected for production
- [x] T021 Run `npx vitest run src/proxy.test.ts`, `npm run typecheck`, `npm run lint`, `node scripts/pre-deploy-check.mjs`, and `npm run build` inside `D:\GitHub\SnipGeek\`
- [x] T022 Run Playwright Edge visual & anchor-clearance verification against live dev server (`http://localhost:9003/displayflip`) across `320px`, `390px`, `768px`, and `1440px` viewports, asserting anchor headings sit below the Floating Island (`top >= 84px`), zero horizontal overflow, working Light/Dark theme screenshot swaps, and working Hero Hotkey Deck & CLI Builder interactions
- [x] T023 Verify `quickstart.md` validation steps pass completely and prepare clean commit-ready diff (no auto-push or deploy)

---

## Dependencies & Execution Order

### Phase Dependencies
- **Phase 1 (Setup)**: No dependencies -> T001-T004 can run in parallel immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1 -> T005-T008 establish routing tests, `proxy.ts`, scoped `globals.css`, and font loader.
- **Phase 3 (US4 Subdomain Isolation)**: Depends on Phase 2 -> T009-T013 wire layout chrome, sitemap, and `pre-deploy-check.mjs`.
- **Phase 4 (US1 Core Landing MVP)**: Depends on Phase 1 & Phase 3 -> T014-T015 deliver the complete Option 3 landing page and SEO schemas.
- **Phase 5 (US2 Interactive Deck & CLI Lab)**: Depends on T014 -> T016-T017 layer interactive Hotkey Deck & CLI Generator state.
- **Phase 6 (US3 Theme & AdSense Integration)**: Depends on T004 & T014 -> T018-T019 complete theme-aware images and 4-slot AdSense mounting.
- **Phase 7 (Polish & Verification)**: Depends on all implementation tasks T001-T019.

### MVP Scope Recommendation
- **Suggested MVP**: Phases 1 through 4 (T001-T015) deliver an isolated, SEO-ready, responsive `displayflip.snipgeek.com` subdomain with fixed anchor clearance and verified v1.1.0 download links; Phases 5-7 immediately complete interactive tooling, theme capture sync, AdSense containers, and full build verification.
