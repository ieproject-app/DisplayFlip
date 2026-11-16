# Implementation Plan: DisplayFlip Product Website & Subdomain (`displayflip.snipgeek.com`)

**Branch**: `003-displayflip-landing-subdomain` | **Date**: 2026-11-16 | **Spec**: [spec.md](file:///D:/GitHub/multi-monitor/specs/003-displayflip-landing-subdomain/spec.md)

**Input**: Feature specification from `/specs/003-displayflip-landing-subdomain/spec.md`

## Summary

Implement `https://displayflip.snipgeek.com/` inside the unified SnipGeek Next.js App Router codebase (`D:\GitHub\SnipGeek`) based directly on the user-approved **Option 3 (Command Terminal)** prototype ([option-3-win11-dark.html](file:///D:/GitHub/multi-monitor/docs/landing-previews/option-3-win11-dark.html)), paired with complete subdomain routing isolation, English-only canonical SEO + `SoftwareApplication`/`FAQPage` structured schemas, four AdSense placeholder slots (`ca-pub-6235611333449307` ready for slot IDs later), `IBM Plex Sans` + `JetBrains Mono` subdomain-scoped styling, and `scroll-margin-top` anchor offset clearance so sticky header link clicks never partially overlap section headings.

## Technical Context

**Language/Version**: TypeScript 6.0 (`^6.0.3`), React 19 (`^19.2.5`), Next.js 16 (`^16.2.6`, App Router)
**Primary Dependencies**: Tailwind CSS v4 (`^4.2.1`), `next-themes` (`^0.4.6`), `lucide-react` (`^1.8.0`), existing `@/components/analytics/adsense-unit` and `@/lib/adsense`
**Storage**: N/A (static/declarative product data + client-side interactive Hotkey Deck & CLI Generator state; zero backend persistence)
**Testing**: Vitest 4 (`^4.1.4`), Happy DOM (`^20.10.6`), React Testing Library, `tsc --noEmit`, ESLint 9, `node scripts/pre-deploy-check.mjs`, Playwright Edge visual & anchor-clearance audit
**Target Platform**: Firebase App Hosting / Next.js SSR+Static Edge CDN serving desktop & mobile browsers (`https://displayflip.snipgeek.com` + `localhost:9003/displayflip` local preview)
**Project Type**: Cross-repo isolated web subdomain implemented in `D:\GitHub\SnipGeek` from specifications & WebP assets prepared in `D:\GitHub\multi-monitor`
**Performance Goals**: All WebP image assets strictly `<= 130 KB` (actual assets `7.1 KB` to `52 KB`), zero CLS on AdSense slots via explicit `min-height` containers, smooth `cubic-bezier(0.32, 0.72, 0, 1)` CSS transitions respecting `prefers-reduced-motion`, non-blocking `afterInteractive` AdSense SDK
**Constraints**:
- Preserve all existing SnipGeek subdomain isolation checks (`labs`, `keytest`, `gear`, `geocam`, `stickies`, `ahc`) in `src/proxy.ts`, `src/app/[locale]/layout.tsx`, and `scripts/pre-deploy-check.mjs`.
- NEVER inspect cookies or add cookie-dependent redirects in `src/proxy.ts` (CDN cache poisoning prevention).
- NEVER overwrite root `:root` / `.dark` tokens in `src/app/globals.css` or global font loaders (`Bricolage_Grotesque`, `Plus_Jakarta_Sans`) in `layout.tsx`. Scope typography and Carbon Obsidian / Emerald Phosphor tokens strictly under `html[data-subdomain="displayflip"]`.
- Single-locale English (`en`) only with zero `hreflang="id"` emissions.
- No raw unicode arrows (`->` or `&rarr;`) inside JSX text nodes that trigger Cloud Build minification crashes; no `window.addEventListener("scroll")` listeners; `className` only.
- Anchor targets (`#cli-lab`, `#architecture`, `#downloads`, `#faq`) MUST apply `scroll-margin-top` (`scroll-mt-24` / `96px`) so Floating Fluid Island header navigation never covers section headings.
**Scale/Scope**: Single dedicated product landing route (`/en/displayflip`) + isolated header/footer chrome + subdomain sitemap + edge proxy rules + automated regression tests.

## Constitution Check

*Note*: `.specify/memory/constitution.md` in `D:\GitHub\multi-monitor` is currently unpopulated template boilerplate, while authoritative repository engineering governance lives in `D:\GitHub\SnipGeek\.agent\skills\snipgeek-rules\SKILL.md` (`subdomains.md`, `design-system.md`, `seo-i18n.md`, `engineering.md`), `D:\GitHub\SnipGeek\scripts\pre-deploy-check.mjs`, and `D:\GitHub\multi-monitor\README.md`. All architectural gates below are derived directly from those canonical rules:

1. **Subdomain Isolation Gate**: `displayflip` MUST integrate alongside `geocam`, `gear`, `keytest`, `labs`, `stickies`, and `ahc` without altering their matchers or removing existing guards in `layout.tsx`, `proxy.ts`, or `pre-deploy-check.mjs`. -> **PASS**
2. **Proxy CDN Cache Safety Gate**: Zero cookie reads (`NEXT_LOCALE`) and zero header-varied redirects in `src/proxy.ts`. -> **PASS**
3. **Root Palette & Font Preservation Gate**: Global `fontDisplay`/`fontSans` and `:root`/`.dark` tokens remain untouched; `displayflip` tokens and `IBM Plex Sans` load strictly scoped (`html[data-subdomain="displayflip"]` + component/route font variable). -> **PASS**
4. **English-Only Subdomain SEO Gate**: Self-canonical `https://displayflip.snipgeek.com`, dedicated sitemap entry in `src/app/sitemap.ts`, zero `hreflang="id"`, valid `SoftwareApplication` + `FAQPage` JSON-LD matching `DisplayFlip` v1.1.0 facts. -> **PASS**
5. **Image Size & JSX Safety Gate**: All WebP assets stored under `D:\GitHub\SnipGeek\public\images\displayflip\` are verified `<= 52 KB` (well under `130 KB` limit), and all JSX avoids raw `→` characters. -> **PASS**

## Project Structure

### Documentation (this feature in `D:\GitHub\multi-monitor`)

```text
specs/003-displayflip-landing-subdomain/
├── spec.md                              # Approved feature requirements (Option 3 locked)
├── checklists/
│   └── requirements.md                  # Spec validation checklist
├── plan.md                              # This implementation plan
├── research.md                          # Phase 0 technical & pattern decisions
├── data-model.md                        # Phase 1 content, CLI lab & AdSense state model
├── quickstart.md                        # Phase 1 verification & local preview runbook
├── contracts/
│   ├── subdomain-routing-contract.md    # Edge proxy, layout chrome, sitemap & pre-deploy contract
│   └── displayflip-ui-contract.md       # UI sections, anchor scroll-mt, theme parity & AdSense contract
└── tasks.md                             # Phase 2 actionable task breakdown (/speckit-tasks)
```

### Source Code (target implementation in `D:\GitHub\SnipGeek`)

```text
D:\GitHub\SnipGeek/
├── public/
│   ├── displayflip-favicon.svg                      # Branded dual-monitor icon SVG
│   └── images/
│       └── displayflip/                             # Copied & verified <= 52 KB WebP captures
│           ├── displays-dark.webp
│           ├── displays-light.webp
│           ├── shortcuts-dark.webp
│           ├── shortcuts-light.webp
│           ├── general-dark.webp
│           ├── general-light.webp
│           ├── about-dark.webp
│           └── about-light.webp
├── src/
│   ├── proxy.ts                                     # Add isDisplayFlipHost, /displayflip local preview, 301 prod redirect
│   ├── proxy.test.ts                                # Unit tests for displayflip host, local preview & prod 301
│   ├── lib/
│   │   └── adsense.ts                               # Add AD_SLOTS.displayflip ({ belowHero, guideRail, inGuide, preFooter })
│   ├── data/
│   │   └── displayflip.ts                           # v1.1.0 release packages, hotkey deck presets, CLI flags, FAQs
│   ├── components/
│   │   ├── layout/
│   │   │   ├── displayflip-header.tsx               # Floating Fluid Island navbar + mobile morphing drawer + theme toggle
│   │   │   └── displayflip-footer.tsx               # Minimal isolated Command Workbench footer
│   │   └── displayflip/
│   │       ├── displayflip-landing.tsx              # Client orchestrator (Hero Hotkey Deck, CLI Lab, 2-row Showcase, Packages, FAQ)
│   │       └── displayflip-ad-slot.tsx              # Double-Bezel wrapper around AdSenseUnit / unconfigured placeholder
│   ├── app/
│   │   ├── globals.css                              # Scoped html[data-subdomain="displayflip"] tokens, grid backdrop & anchor scroll-mt
│   │   ├── sitemap.ts                               # Dedicated sitemap branch for isDisplayFlipSubdomain
│   │   └── [locale]/
│   │       ├── layout.tsx                           # Load IBM Plex Sans var, detect isDisplayFlipSubdomain, render isolated chrome + metadata
│   │       └── displayflip/
│   │           └── page.tsx                         # Server route metadata, JSON-LD (SoftwareApplication + FAQPage), page render
└── scripts/
    └── pre-deploy-check.mjs                         # Add DisplayFlip subdomain check to Section 3.5 isolation guard
```

**Structure Decision**: Implement `displayflip.snipgeek.com` natively inside `D:\GitHub\SnipGeek` mirroring the verified `geocam` and `gear` App Router subdomain architecture (`proxy.ts` rewrite + `layout.tsx` header-based chrome swap + `globals.css` `data-subdomain` scoping + `AD_SLOTS` registration + `sitemap.ts` subdomain isolation), while keeping `D:\GitHub\multi-monitor\specs\003-displayflip-landing-subdomain` as the authoritative cross-repo specification and task tracker.

## Complexity Tracking

No constitution violations; design reuses existing SnipGeek Next.js subdomain routing, `AdSenseUnit`, `next-themes`, and font pipeline cleanly without introducing external libraries or backend endpoints.
