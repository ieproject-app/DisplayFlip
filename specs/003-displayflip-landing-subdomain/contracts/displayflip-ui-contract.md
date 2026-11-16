# Contract: UI Architecture, Option 3 Visual System & Anchor Clearance

**Feature**: `003-displayflip-landing-subdomain` | **Prototype Source**: [option-3-win11-dark.html](file:///D:/GitHub/multi-monitor/docs/landing-previews/option-3-win11-dark.html)

## 1. Visual Design System (`html[data-subdomain="displayflip"]`)

- **Palette & Grid (Light & `.dark` Token Parity)**:
  - Light (`html[data-subdomain="displayflip"]`): `--df-bg: #f3f5f4`, `--df-core: #ffffff`, `--df-core-alt: #f7f9f8`, `--df-code-bg: #0c100e`, `--df-code-ink: #34d399`, `--df-ink: #0f172a`, `--df-ink-sec: #475569`, `--df-accent: #059669`, `--df-accent-hover: #047857`, `--df-accent-ink: #ffffff`, `--df-grid-line: rgba(15, 23, 42, 0.04)`.
  - Dark (`html[data-subdomain="displayflip"].dark`): `--df-bg: #090b0a`, `--df-core: #111413`, `--df-core-alt: #161a18`, `--df-code-bg: #060807`, `--df-code-ink: #34d399`, `--df-ink: #f3f5f4`, `--df-ink-sec: #9ca3af`, `--df-accent: #10b981`, `--df-accent-hover: #34d399`, `--df-accent-ink: #032218`, `--df-grid-line: rgba(255, 255, 255, 0.032)`.
  - Root `body` grid pattern (`48px x 48px` architectural grid) scoped to `html[data-subdomain="displayflip"] body`.
- **Typography**:
  - Primary sans: `var(--gf-displayflip), "Segoe UI Variable Text", system-ui, sans-serif` (`IBM Plex Sans`).
  - Technical labels, terminal logs & CLI builder: `var(--gf-mono), Consolas, monospace` (`JetBrains Mono`).
- **Signatures Preserved from Option 3**:
  - Floating Fluid Island Header (`max-width: 820px`, `height: 54px`, detached `top: 16px` pill with backdrop blur).
  - Double-Bezel (`Doppelrand`) hardware shells (`outer` shell border + `6px` padding + `inner` inset rim card).
  - Button-in-Button CTA pills (`Download v1.1.0`, `Open CLI Lab`, `Copy`, `Get ZIP on GitHub`, `Get Standalone EXE`) with circular icon well translating diagonally `(2px, -1px) scale(1.06)` on hover and `:active scale(0.98)`.
  - Zero raw unicode `→` in JSX (use `->` or Lucide icons), zero em/en-dashes (`—`/`–`) in UI copy, zero `window.addEventListener('scroll')`, zero `h-screen` / `100vh`.

## 2. Floating Header Anchor Clearance (Mandatory Fix)

- Problem observed in HTML prototype: Clicking `#cli-lab`, `#architecture`, `#downloads`, or `#faq` aligned section top right behind the sticky `top-4` (`16px`) + `54px` (`70px` bottom edge) Floating Island navbar, partially overlapping `<h2>` headings.
- Contract Fix:
  - Every anchor-targeted `<section id="...">` MUST apply `scroll-mt-24 md:scroll-mt-28` (`96px` mobile / `112px` desktop CSS `scroll-margin-top`).
  - Mobile drawer link clicks MUST close the drawer before scrolling cleanly to the target anchor so no overlay remains open over the target heading.

## 3. Section Sequence & Interactive Parity

1. **Floating Island Header (`DisplayFlipHeader`)**:
   - Brand (`>_ DisplayFlip`), links (`CLI Lab` -> `#cli-lab`, `Screens` -> `#architecture`, `Packages` -> `#downloads`, `FAQ` -> `#faq`), Light/Dark theme toggle button, desktop `Download v1.1.0` pill CTA, mobile 2-line morphing hamburger -> full-screen glass drawer.
   - Touch target sizes `>= 44px` on mobile interactive controls.
2. **Hero Section**:
   - Left column: eyebrow (`Win32 CCD + CLI`), `<h1>Scriptable Display Control On Windows 11</h1>`, concise subtitle, dual pill CTAs (`Download v1.1.0` + `Open CLI Lab`).
   - Right column: Double-Bezel Hero Frame holding `<Image>` of active capture (`displays` / `shortcuts` / `about`, `-dark.webp` or `-light.webp`), live phosphor terminal status bar (`#heroLog` updating per selected hotkey key), and 3-column Hotkey Deck (`Ctrl + Alt + S`, `Win + Shift + 1`, `DisplayFlip --apply`).
3. **AdSense Slot 1 (`belowHero`)**: Rendered cleanly below hero when configured (or dev-only skeleton box in dev mode).
4. **Section 2 (`#cli-lab`) — PowerShell, Task Scheduler & AutoHotkey Builder + Sticky Rail**:
   - Left column: Double-Bezel builder with 6 flag pills, Profile Name input (auto-disabled with visual opacity reduction when flag != `--apply`), Output Language `<select>`, Output Format `<select>`, and syntax output code block + Clipboard Copy button (`Copy` -> `Copied`).
   - Right column (`316px` desktop rail, sticky `top-24`): **AdSense Slot 2 (`guideRail`, `300x250`)**.
5. **Section 3 (`#architecture`) — Inside the Fluent 2 Engine**:
   - Staggered 2-row technical capture walkthrough (`Keyboard Shortcuts` + `General & Icons`, switching `-dark.webp` / `-light.webp`).
6. **AdSense Slot 3 (`inGuide`)**: Mid-page break above Download Packages.
7. **Section 4 (`#downloads`) — Download DisplayFlip v1.1.0**:
   - 2-column Double-Bezel card grid for `DisplayFlip-v1.1.0-win-x64.zip` and `DisplayFlip-v1.1.0-Standalone-win-x64.exe`.
8. **Section 5 (`#faq`) — Technical FAQ**:
   - Accordion `<details>` grid matching `DISPLAYFLIP_FAQS`.
9. **AdSense Slot 4 (`preFooter`)** & **Footer (`DisplayFlipFooter`)**:
   - Minimal 1-line link footer (`GitHub`, `SnipGeek`, `Privacy`, `Terms`).
