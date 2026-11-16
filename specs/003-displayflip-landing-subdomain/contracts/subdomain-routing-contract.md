# Contract: Subdomain Routing, Isolation & SEO (`displayflip.snipgeek.com`)

**Feature**: `003-displayflip-landing-subdomain` | **Repository**: `D:\GitHub\SnipGeek`

## 1. Edge Proxy Contract (`src/proxy.ts`)

1. **Host Matcher**:
   - `displayflipHost = "displayflip.snipgeek.com"`
   - `isDisplayFlipHost(hostname)`: matches `hostname === displayflipHost || hostname === "www." + displayflipHost || hostname.startsWith("displayflip.")`
   - `shouldRedirectToCanonicalHost(hostname)`: MUST return `false` when `isDisplayFlipHost(hostname)` is `true`.

2. **Subdomain Rewrite Behavior (`isDisplayFlipHost`)**:
   - If `hostname !== displayflipHost && !isLocalHost(hostname)`: return `308` redirect to `https://displayflip.snipgeek.com${pathname}${search}`.
   - Set request headers:
     - `x-is-displayflip-subdomain: "true"`
     - `x-displayflip-hostname: hostname`
   - Bypass rewrite for `/api`, `/_next`, `/admin`, `/__`, or asset paths containing `.` (forwarding request headers cleanly).
   - Rewrite root `/` (and optional subpaths stripped of leading `/displayflip`) to `/${i18n.defaultLocale}/displayflip${targetSuffix}${targetQuery}` (`/en/displayflip`).
   - **Cookie Rule**: Proxy MUST NEVER read `NEXT_LOCALE` or any request cookie.

3. **Localhost Preview Branch**:
   - When `isLocalHost(hostname)` and `(pathname === "/displayflip" || pathname.startsWith("/displayflip/"))`, set `x-is-displayflip-subdomain: "true"` + `x-displayflip-hostname: hostname` and rewrite to `/en/displayflip${targetSuffix}${targetQuery}`.

4. **Main Domain Redirect (Production)**:
   - When `!isLocalHost(hostname)` and `(pathname === "/displayflip" || pathname.startsWith("/displayflip/"))`, return `301` redirect to `https://displayflip.snipgeek.com${targetPath}`.

## 2. Layout Isolation Contract (`src/app/[locale]/layout.tsx`)

- Read `x-is-displayflip-subdomain === "true"` or `hostHeader.startsWith("displayflip.")` as `isDisplayFlipSubdomain`.
- `currentSubdomain` includes `"displayflip"` -> sets `<html data-subdomain="displayflip">`.
- Load `IBM_Plex_Sans` via `next/font/google` (`variable: "--gf-displayflip"`) and pass its variable class cleanly on `<html>`.
- `generateMetadata` sets:
  - `metadataBase: new URL("https://displayflip.snipgeek.com")` when `isDisplayFlipSubdomain`
  - `icons` / `manifest` tailored for DisplayFlip (`displayflip-favicon.svg`)
- Conditional Chrome Render:
  - `isDisplayFlipSubdomain` MUST bypass `LayoutHeader`, `LayoutDeferredUi`, and `LayoutFooter`, rendering ONLY:
    - `<DisplayFlipHeader />`
    - `<main id="main-content">{children}</main>`
    - `<DisplayFlipFooter />`
- Keep `<AdSenseScript />`, `<GoogleAnalyticsTag />`, `<WebVitalsReporter />`, and `<ThemeProvider>` active.

## 3. Page Metadata & JSON-LD Contract (`src/app/[locale]/displayflip/page.tsx`)

- Single-locale English metadata (`en` only, zero `hreflang="id"`):
  - `alternates.canonical`: `"https://displayflip.snipgeek.com"`
  - `openGraph.url`: `"https://displayflip.snipgeek.com"`
  - `openGraph.siteName`: `"DisplayFlip"`
- Inline `<script type="application/ld+json">` renders `@graph` with:
  1. `SoftwareApplication` (`name: "DisplayFlip"`, `softwareVersion: "1.1.0"`, `operatingSystem: "Windows 10, Windows 11"`, `applicationCategory: "UtilitiesApplication"`, `url: "https://displayflip.snipgeek.com"`, `downloadUrl: "https://github.com/ieproject-app/DisplayFlip/releases/tag/v1.1.0"`, `offers: { "@type": "Offer", price: "0", priceCurrency: "USD" }`).
  2. `FAQPage` built directly from `DISPLAYFLIP_FAQS`.

## 4. Sitemap & Pre-Deploy Guard Contracts

- `src/app/sitemap.ts`: Detect `isDisplayFlipSubdomain` from request headers (`x-is-displayflip-subdomain` / `x-displayflip-hostname` / `host`) and return a dedicated single-entry array for `https://displayflip.snipgeek.com`.
- `scripts/pre-deploy-check.mjs`: Assert `isDisplayFlipSubdomain`, `DisplayFlipHeader`, `DisplayFlipFooter` exist in `layout.tsx` AND `isDisplayFlipHost`, `x-is-displayflip-subdomain` exist in `proxy.ts`.
- `src/proxy.test.ts`: Add unit test suite covering `displayflip.snipgeek.com`, `www.displayflip.snipgeek.com`, `/displayflip` localhost preview, production `snipgeek.com/displayflip` 301 redirect, and cookie-free CDN caching behavior.
