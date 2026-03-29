# Lighthouse Performance Notes

This file records page-load metrics from Lighthouse and the load-focused changes that have already been implemented.

## What This File Covers

- Synthetic page-load audits from Lighthouse.
- Initial-load optimizations such as preload, preconnect, and smaller release payload settings.

## Lighthouse Baseline

These numbers come from the Lighthouse JSON reports for `https://memocardgame.vercel.app/`.

| Metric | Desktop | Mobile |
|--------|---------|--------|
| Performance score | 75 / 100 | 50 / 100 |
| First Contentful Paint (FCP) | ~0.5 s | ~1.6 s |
| Largest Contentful Paint (LCP) | ~2.7 s | ~15.0 s |
| Speed Index | ~0.5 s | ~1.6 s |
| Total Blocking Time (TBT) | ~280 ms | ~1.2 s |
| Time to Interactive (TTI) | ~2.7 s | ~15.0 s |
| Cumulative Layout Shift (CLS) | ~0 | ~0 |
| Initial server response time (TTFB) | ~18–20 ms | ~19–20 ms |

- Lighthouse SEO score in the initial reports was `82 / 100` on both desktop and mobile.

## Implemented Changes

### `src/MemoCardGame.Client/wwwroot/index.html`

- Added `preconnect` for the production API host.
- Added `dns-prefetch` for `cdn.jsdelivr.net`.
- Added `preload` for `_framework/blazor.webassembly.js`.

### `src/MemoCardGame.Client/MemoCardGame.Client.csproj`

- Added `InvariantGlobalization=true` for `Release`.
- Added `BlazorEnableTimeZoneSupport=false` for `Release`.

## What This Means

- The app now gives the browser an earlier hint about the API connection.
- The app now gives the browser an earlier hint about the Blazor startup script.
- The release build is configured to ship a smaller WebAssembly payload by removing globalization and time zone features that are not currently needed.

## Results After 1st Improvement

These numbers are from the next Lighthouse run after deploying the first improvement. The change is small, but they are kept here for comparison.

| Metric | Desktop | Mobile |
|--------|---------|--------|
| Performance score | 77 / 100 | 50 / 100 |
| First Contentful Paint (FCP) | ~0.5 s | ~1.6 s |
| Largest Contentful Paint (LCP) | ~2.5 s | ~14.0 s |
| Speed Index | ~0.5 s | ~1.6 s |
| Total Blocking Time (TBT) | ~260 ms | ~1.27 s |
| Time to Interactive (TTI) | ~2.5 s | ~14.0 s |
| Cumulative Layout Shift (CLS) | ~0.001 | ~0 |
| Initial server response time (TTFB) | ~17–20 ms | ~15–20 ms |

- Desktop improved slightly.
- Mobile stayed almost the same overall.
- Lighthouse SEO score in these reports is `92 / 100` on both desktop and mobile.
- The SEO improvement appears to come mainly from the page now having a valid `meta description` and a valid `canonical` URL.
