# Quickstart & Verification Runbook: `displayflip.snipgeek.com`

**Feature**: `003-displayflip-landing-subdomain` | **Target Repo**: `D:\GitHub\SnipGeek`

## Prerequisites
- Node.js `>= 22.0.0`, npm `>= 10.0.0` in `D:\GitHub\SnipGeek`
- Python 3.12 + Playwright (`msedge` channel) for browser verification
- Source WebP assets copied from `D:\GitHub\multi-monitor\docs\landing-previews\assets\` to `D:\GitHub\SnipGeek\public\images\displayflip\`

## Verification Commands (Execute in `D:\GitHub\SnipGeek`)

1. **Automated Unit Tests (Proxy Subdomain & Component Tests)**:
   ```powershell
   npx vitest run src/proxy.test.ts
   ```
   Expected: All `proxy` tests pass including `displayflip.snipgeek.com`, `www.displayflip.snipgeek.com` 308 normalization, `/displayflip` localhost preview, and production `/displayflip` 301 redirect.

2. **Typecheck, Lint, Pre-Deploy Guard & Production Build**:
   ```powershell
   npm run typecheck
   npm run lint
   node scripts/pre-deploy-check.mjs
   npm run build
   ```
   Expected: 0 TypeScript errors, 0 ESLint errors, all `public/images/displayflip/*.webp` assets verified `< 130 KB`, all Section 3.5 subdomain checks (`labs`, `keytest`, `gear`, `geocam`, `displayflip`) green, Next.js build succeeds cleanly.

3. **Live Dev Server Visual & Functional Audit (`http://localhost:9003/displayflip`)**:
   - Start SnipGeek dev server (`npm run dev` on port `9003`).
   - Run Playwright (`msedge` channel) checks across `1440x900`, `768x1024`, and `390x844`:
     - Verify main SnipGeek blog header/footer is NOT rendered; only `DisplayFlipHeader` and `DisplayFlipFooter` render.
     - Click each header anchor (`CLI Lab`, `Screens`, `Packages`, `FAQ`) and assert target `<h2>.getBoundingClientRect().top >= 84` (never obscured by sticky header).
     - Click each Hero Hotkey Deck button and assert `#heroLog` and `<img src>` update synchronously.
     - Toggle Light/Dark theme and verify all 4 screenshots switch between `-dark.webp` and `-light.webp`.
     - Verify CLI Lab flag/language/target state machine (`--solo 2` + `ahk` -> `F13::Run('DisplayFlip.exe --solo 2')`).
     - Inspect rendered `sitemap.xml` on `displayflip` host header and confirm canonical `https://displayflip.snipgeek.com` with zero `hreflang="id"`.
