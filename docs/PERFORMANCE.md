# Performance Notes

This file records the current performance baseline and the performance-related code changes that are now present in the uncommitted work.

## Lighthouse Baseline

These numbers come from the current Lighthouse JSON reports for `https://memocardgame.vercel.app/`.

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
- The Release build is configured to ship a smaller WebAssembly payload by removing globalization and time zone features that are not currently needed.
