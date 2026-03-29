# DevTools Interaction Performance Notes

This file records runtime interaction measurements from Chrome DevTools Performance recordings.

## What This File Covers

- Real user-style interaction traces from the Chrome Performance panel.
- Click-to-request timing for `Start game`, card flips, and turn resolution.
- Main-thread behavior during live gameplay interactions.

## How This Differs From Lighthouse

- `PERFORMANCE-LIGHTHOUSE.md` covers synthetic page-load audits.
- This file covers live interaction latency after the app is already open.

## Recordings Reviewed

- `perf_startgame_awake_Trace-20260329T200654.json.gz`
- `perf_flip_single_Trace-20260329T201710.json.gz`
- `perf_flip_turn 2cards_not match_Trace-20260329T201958.json.gz`
- `perf_flip_1st turn 2cards_not match_Trace-20260329T202059.json.gz`

## Summary

| Recording | Main-thread work near click | API timing | Main conclusion |
|----------|------------------------------|------------|-----------------|
| Start game | click dispatch ~69 ms | `POST /games` ~8.5 s, then `GET /games/{id}` ~0.89 s | Startup delay is dominated by backend latency and an extra round trip. |
| Single flip | click dispatch ~33 ms | `POST /flip` ~0.40 s | Card reveal delay is mostly waiting for the API response. |
| Turn after game is already in progress | no meaningful long task found | flip 1 ~0.15 s, flip 2 ~0.19 s, resolve ~0.49 s | Later turns are noticeably faster and mostly network-bound. |
| Very first recorded turn | no meaningful long task found | flip 1 ~0.30 s, flip 2 ~0.35 s, resolve ~0.47 s | The first turn is a bit slower than a later turn, which suggests server warm-up effects. |

## Detailed Notes

### Start game

- The request to create a game started about `64 ms` after the click.
- `POST /games` took about `8.5 s`.
- After that, the client made an additional `GET /games/{id}` request that took about `0.89 s`.
- Total time from click until the state load finished was about `9.45 s`.

### Single card flip

- The flip request started about `29 ms` after the click.
- `POST /flip` completed in about `395 ms`.

### Two-card turn recorded later in the game

- First flip completed in about `151 ms`.
- Second flip completed in about `192 ms`.
- `POST /resolve` then completed in about `495 ms`.

### Two-card turn recorded as the first turn

- First flip completed in about `304 ms`.
- Second flip completed in about `353 ms`.
- `POST /resolve` then completed in about `474 ms`.

## Main Findings

- The browser main thread does not currently look like the primary bottleneck.
- No meaningful long tasks were found during the card-flip recordings.
- The biggest delay is server and network time, especially for `Start game`.
- `Start game` is slower than gameplay because it currently includes both `POST /games` and a follow-up `GET /games/{id}`.
- The first turn is slower than a later turn, which may point to backend warm-up, database warm-up, or cache warm-up.

## Implemented Follow-Up Actions

- Added a lightweight `GET /healthz` endpoint on the API.
- Added a client warm-up call on first render so the API can start waking before the user presses `Start game`.
- Changed `POST /games` to return the full initial `GameStateDto`.
- Removed the extra `GET /games/{id}` from the normal `Start game` flow.
- Changed `/flip` and `/resolve` to return updated state without reloading the game again from the repository.
- Added lightweight timing logs on the API and in the client request methods.
- Removed repeated card sorting in `Index.razor`.
- Replaced repeated mismatch overlay list scans in `GameBoard.razor` with a dictionary lookup.

## Next Checks

1. Re-record the same 4 DevTools traces and compare them with the numbers in this file.
2. Add an external uptime ping if Koyeb sleep still hurts real-world usage.
3. Consider combining the second flip and resolve into one server call if gameplay behavior still allows a good reveal experience.
4. If card reveals still feel heavy after the network improvements, test local emoji assets or more eager image loading instead of CDN-loaded Twemoji files.
