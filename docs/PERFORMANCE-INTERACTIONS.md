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

### Before improvements

- `perf_startgame_awake_Trace-20260329T200654.json.gz`
- `perf_flip_single_Trace-20260329T201710.json.gz`
- `perf_flip_turn 2cards_not match_Trace-20260329T201958.json.gz`
- `perf_flip_1st turn 2cards_not match_Trace-20260329T202059.json.gz`

### After warm-up and request-path improvements

- `1st improvement_start game_Trace-20260329T211142.json.gz`
- `1st improvement_2 turns - 4 cards revelaed_Trace-20260329T211232.json.gz`
- `1st imprevement_ startgame after short inactivity_Trace-20260329T212723.json.gz`

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

## Results After Improvements

### Start game when the API was warm

- `POST /games` started about `54 ms` after the click.
- `POST /games` completed in about `107 ms`.
- The earlier follow-up `GET /games/{id}` is no longer part of the normal `Start game` path.
- Compared with the earlier recording, this is a very large improvement from roughly `9.45 s` total down to about `0.11 s` for the main request path.

### Start game after short inactivity

- This recording was taken after roughly `5-10 minutes` of inactivity.
- `POST /games` started about `55 ms` after the click.
- `POST /games` completed in about `2.53 s`.
- This is still much faster than the earlier baseline, but it shows that short inactivity can still lead to a noticeable delay.

### Two turns after improvements

- Turn 1: first flip about `301 ms`, second flip about `240 ms`, resolve about `94 ms`.
- Turn 2: first flip about `96 ms`, second flip about `138 ms`, resolve about `101 ms`.
- The biggest visible improvement is the resolve call, which dropped from roughly `474-495 ms` before to about `94-101 ms` after the server-side request-path cleanup.

## Comparison Summary

| Interaction | Before | After | What changed |
|------------|--------|-------|--------------|
| Start game, warm | `POST /games` ~8.5 s + `GET /games/{id}` ~0.89 s | `POST /games` ~0.11 s | Warm-up helped and the extra state-loading request was removed. |
| Start game, after short inactivity | not recorded in the original set | `POST /games` ~2.53 s | Still noticeably slower than fully warm, but much better than the old start flow. |
| First recorded turn | flips ~0.30 s and ~0.35 s, resolve ~0.47 s | turn 1 flips ~0.30 s and ~0.24 s, resolve ~0.09 s | Resolve got much faster after removing redundant server reloads. |
| Later turn | flips ~0.15 s and ~0.19 s, resolve ~0.49 s | turn 2 flips ~0.10 s and ~0.14 s, resolve ~0.10 s | Later turns became more consistent and resolve latency dropped sharply. |

## Updated Conclusions

- The implemented changes clearly improved runtime interaction performance.
- The browser main thread still does not look like the primary bottleneck.
- The biggest remaining risk is backend sleep or warm-up time after inactivity.
- In a fully warm state, the app now responds much faster.
- In a semi-idle state, `Start game` can still feel too slow for a game, which points more to hosting behavior than to browser rendering.

## Next Checks

1. Re-record the same 4 DevTools traces and compare them with the numbers in this file.
2. Add an external uptime ping if Koyeb sleep still hurts real-world usage.
3. Consider combining the second flip and resolve into one server call if gameplay behavior still allows a good reveal experience.
4. If card reveals still feel heavy after the network improvements, test local emoji assets or more eager image loading instead of CDN-loaded Twemoji files.
