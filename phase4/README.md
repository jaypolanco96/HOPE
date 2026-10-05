# Phase 4 — performance measurement and validation

Phase 4 has started. This first checkpoint supplies bounded frame-pacing captures and reproducible analysis. It does not claim an FPS improvement or that Phase 4 is complete. HOPE is Hills, Ollies, Pavement, Expression, based on Skate3Recomp by mchughalex (https://github.com/mchughalex/skate3recomp). Supply your own Skate 3 Xbox 360 ISO; no game download links are provided.

## Capture during play

1. Launch HOPE normally. Start/Menu still opens Skate 3's original menu; Escape or RB + Start opens separate HOPE settings. Restart, Trick Book, map and replay remain original game actions. Actual Skate 3 title-screen return/menu rebuilding remains unfinished.
2. Choose a repeatable route and wait for loading to finish. Press F8 to start a capture, repeat the same route for 60 seconds, then press F8 again to save it. Collection stops automatically at 60 seconds or 20,000 intervals; saving occurs on the next F8 press or normal game shutdown. An abrupt crash cannot save the in-memory capture.
3. The game log reports the full capture path under its cache folder's `performance` subfolder. Files are named `hope_frame_capture_<timestamp>_<sequence>.csv`. Failed writes retain samples for another F8 save attempt while the process is alive. Do not repeatedly tap F8: it starts, saves, then starts the next capture.
4. Keep resolution, effects, cap, power mode, route and camera fixed within a run. Record them alongside the capture. Changing graphics mid-capture makes comparisons ambiguous. Native/Emulated and gameplay/context transitions are flagged in each row; they do not identify exact named menus, maps or challenges.

The recorder is off by default. Its inactive frame path is one atomic flag check; it takes no timestamps, allocates no storage, and writes no files until enabled. Active storage is bounded to 20,000 rows (roughly 480 KB of sample data), allocated before sampling. The guest callback only records bounded samples under a short lock; CSV serialization occurs when saving, outside that callback. These are code properties, not measured game-overhead numbers.

This capture works without enabling the SDK's heavyweight draw/fingerprint profiler. In earlier builds that profiler was compiled out, so F8 could not produce frame timing captures. The new F8 capture replaces that unavailable shortcut. It requires the default native-render hook layer to remain enabled; Emulated scene mode can still be sampled.

## Analyze a capture

The standard-library analyzer needs Python 3 on the analysis machine; Python is not required to play or record.

```powershell
python .\analyze_performance.py "C:\path\hope_frame_capture_....csv" --output "C:\path\new-report.json"
```

Reports give sampled duration, median/mean/p95/p99/worst interval, cadence, slowest 1% cadence, and counts over 33.33/50 ms. Renderer and gameplay/context groups remain separate, and transition intervals are excluded. Captures with fewer than 300 intervals or 10 seconds of usable group data are marked short. The analyzer refuses malformed samples, nonmonotonic sequences and aggregate counter CSVs; it never overwrites an existing output report.

**These are guest swap intervals, including frame pacing and waits. They are not displayed FPS, CPU/GPU execution time, VRAM/RAM, shader compilation stalls or measured loading duration.** Context 0 combines menus/loading; context 1 identifies gameplay. GPU/presentation tools and controlled live runs are still needed for the other Phase 4 metrics.

## Initial evidence

Existing player logs contain some slow inline texture-creation reports and one run with MSAA x8. Saved HOPE settings use 2x resolution scale and MSAA x8. These are observations, not evidence that those settings or texture creation cause steady gameplay slowdown. Slow-decode logs are threshold-selected events, not a full timing distribution. Graphics settings have not been changed automatically, and no measured preset is advertised.

## Baseline protocol and remaining gates

- Record laptop power mode/AC state, renderer, output size/scale, effects, cap, driver and build. Keep them fixed. Do not clear the player's real shader cache to manufacture a cold run; use isolated test caches if supported.
- Collect three equivalent warm runs each for quiet free skate, a busy scene, original pause/Trick Book and replay. Track menus/loading separately. Record a first-run startup sample separately from steady play. Repeat the same camera movement and route.
- Compare native and 2x scale, and x4/x8 MSAA, as separate visual-quality experiments. Change one setting at a time between runs. Establish normal run-to-run variance before deciding on an optimization or default preset.
- Resolve CPU/GPU time, memory, shader stalls, presentation pacing and load duration with appropriate live profiling. Optimize the measured bottleneck, then repeat equivalent runs and compare beyond baseline variance.
- Validate physics/trick timing/audio across supported caps, the difficulty/camera blocker, save/load/restart, original menu actions and the two-hour mixed-play soak test. Do not mark release validation complete while these are outstanding.

Earlier automatic approval review rejected native game UI access. This checkpoint uses source inspection, existing logs, synthetic/disposable tests and offscreen launcher rendering; no game was launched or operated to obtain fabricated performance results. True game-menu rebuilding and the intermittent blank difficulty screen remain open alongside the performance work.

## Verification

`skate3_frame_capture_test` verifies initial baselines, monotonic timestamps, context/renderer transitions, sample/time bounds, default-off behavior, write-failure retention/retry, schema preservation, concurrent stop and distinct output files using disposable directories. `python -m unittest discover -s phase4/tests -p 'test_*.py' -v` verifies percentile/slowest-1% definitions, grouping/transition exclusion and invalid/empty/aggregate input handling. Original-menu, profile, save and graphics checks remain part of the native build verification; launcher fixture checks remain required.
