# HOPE: four-phase update roadmap

Updated October 5, 2026. Phase 4 measurement work and subsequent graphics/mod/particle/branding updates are implemented. All earlier live completion gates remain open. Launcher 0.6.3 and game 2.0.0.37-dev.g66ac4f3 are installed; see [current status](../docs/CURRENT-STATUS.md). Difficulty/camera and true Skate title return remain unfinished; the shirt hotfix awaits movement retesting.

## Starting evidence

- Reported blocker: after difficulty and camera selection, the difficulty screen sometimes returns blank and prevents progression.
- Installation contains skate3.exe, rexruntime.dll, extracted game data, and three session logs. No source checkout or build instructions were found in this folder.
- Logs identify v2.0.2-Release, Direct3D 12, an NVIDIA GeForce RTX 3050 Laptop GPU, and a 2x2 draw-resolution scale. Title Update 3 patches applied successfully.
- logs/skate3_001.log:577 and logs/skate3_002.log:268 report `BaseHeap::Release failed because address is not a region start`. Their connection to the menu failure is unproven.
- Logs include missing-file warnings and frequent `2d-thumb` diagnostics. Neither proves the cause of the blank screen or a performance bottleneck.

## Phase 1 — Find and fix the difficulty-screen blocker

Goal: reliably reach gameplay after selecting difficulty and camera.

1. Identify the exact source repository and commit corresponding to the installed build, including runtime dependencies and local modifications. Establish a reproducible build before patching code.
2. Preserve the original executable/runtime, logs, settings, and saves. Identify actual save locations before copying; use disposable profiles for fresh-start tests.
3. Reproduce the failure with a matrix covering fresh/existing profiles, every difficulty/camera combination, slow/rapid confirmation, back/re-entry, controller/keyboard, and focus loss. Record steps, frequency, timestamps, and whether input, audio, or rendering continues.
4. Add targeted diagnostic events for menu entry/exit, option population, focused selection, input consumption, profile/save completion, and relevant UI resource lifecycle. Compare a successful run with a failed run.
5. Distinguish an empty option list from a rendering failure or stalled transition. Investigate stale menu state, repeated confirm events, asynchronous initialization/save races, and UI texture/resource reuse as hypotheses. Trace the memory-release errors separately and correlate them with the failure before assigning causality.
6. Fix the demonstrated cause. Where appropriate, gate confirmation until options are ready, make transitions safe against repeated input, and provide a recoverable back/retry path with visible feedback.

Completion gate: exercise the reproduction matrix and complete at least 30 consecutive setup-to-gameplay runs with zero blank-screen blocks, including the previously failing sequence. Verify settings persist after restart, existing saves load, and diagnostics show the expected transition sequence. This is a regression threshold, not proof that every intermittent failure is eliminated.

Deliverables: root-cause report, focused patch, regression coverage for the cause, and reversible test build.

## Phase 2 — Find broader bugs and improve reliability

Prerequisites requested by the player: complete the PC-friendly menu, save-management, graphics-settings update, and HOPE launcher before starting Phase 2. HOPE means Hills, Ollies, Pavement, Expression. See `PC-FRIENDLY-UPDATE.md` and `../launcher/README.md`. The launcher must credit original Skate3Recomp creator mchughalex, use the requested Bay Area styling and authentic HD gameplay backgrounds, and remind players to supply their own Xbox 360 ISO without download links. This brings part of Phase 3 forward; the Phase 1 menu blocker still needs runtime reproduction and verification.

Goal: protect progress and remove other game-stopping failures.

1. Audit startup, new career, continue, save/load, pause/resume, challenge restart, camera changes, replay, display changes, controller reconnect, and quit/relaunch.
2. Prioritize reproducible freezes, crashes, lost progress, and broken input. Record each issue with evidence, severity, reproduction steps, and affected build.
3. Investigate heap-release errors with allocation/release traces. Determine whether they occur during ordinary operation or shutdown and fix invalid ownership/lifetime handling if confirmed.
4. Classify missing-file warnings as expected optional requests or required-resource failures before making asset or filesystem changes.
5. Review save completion and failure handling; add clear failure feedback and atomic replacement where supported. Preserve compatibility with existing saves.
6. Add concise crash/session diagnostics and focused regression checks for confirmed defects.

Completion gate: complete a 60-minute session covering the audited flows without a reproducible blocker or progress loss; verify multiple save/reload cycles and clean restart. Account for known errors explicitly, with remaining issues recorded.

Deliverables: prioritized bug tracker, reliability fixes, and save-compatibility results.

## Phase 3 — Quality-of-life improvements

Goal: make everyday play and recovery easier.

1. Persist difficulty, camera, display, and input preferences correctly; expose ways to change them after initial setup.
2. Make controller/keyboard prompts reflect the active input method. Test menu focus, repeat behavior, confirm/back consistency, and reconnect recovery.
3. Improve camera-setting descriptions and previews where feasible, plus readable menu scaling across supported display sizes.
4. Show loading/saving feedback and actionable errors instead of silent stalls. Offer retry/back paths that retain valid selections.
5. Provide simple performance presets based on measurements, plus an optional frame-time/FPS display and easy access to diagnostics.
6. Add a settings reset that preserves career saves and a documented recovery procedure.

Completion gate: complete setup, adjust settings, reconnect input, restart, and recover from a simulated settings failure using the visible controls. Verify preferences survive restart and career progress remains intact.

Deliverables: QoL update, tested defaults, and short player-facing instructions.

## Phase 4 — Performance optimization and release validation

Goal: improve smoothness on this laptop without breaking menus, physics, audio, or saves.

1. Establish repeatable baseline routes in gameplay, busy scenes, menus, and replays. Separate cold-cache startup from warm-cache play; keep power mode, resolution, frame cap, and test conditions fixed.
2. Measure median and 95th/99th-percentile frame time, 1% low FPS, CPU/GPU time, memory/VRAM usage, load times, and shader compilation stalls. Repeat runs to establish normal variability.
3. Compare supported native and 2x2 resolution settings to determine GPU pressure. Profile CPU, rendering, asset streaming, UI texture handling, and diagnostic logging before selecting optimizations.
4. Optimize the measured bottlenecks: unnecessary UI uploads, synchronization, allocations, repeated work, shader/cache handling, or excessive logging as evidence warrants. Keep debug detail available on demand.
5. Validate frame pacing and supported frame caps; verify higher frame rates do not alter simulation speed, trick timing, camera behavior, or audio synchronization.
6. Repeat Phase 1 regression checks after renderer/runtime changes. Test existing saves and both cold/warm caches. Package versioned binaries, dependencies, changelog, and rollback instructions together.

Completion gate: show repeatable improvement beyond baseline variability in the targeted bottleneck, with no meaningful regression in the other measured scenes. Pass setup-screen checks, save compatibility, and a two-hour mixed-play soak test. Choose numerical performance targets after collecting the baseline.

Deliverables: before/after measurements, optimized build, release notes, and rollback package.

## Working order

The player requested Phase 2 after the PC-friendly and HOPE launcher updates. Work on reliability now while retaining the Phase 1 blocker as an open high-priority issue. Each update should state what changed, its evidence, how it was checked, and any remaining limitations. Advance based on completion gates rather than calendar deadlines.

Current next step: retest the player shirt with the particle/clothing isolation hotfix, validate the difficulty/camera blocker and original-menu flows, and collect repeatable Phase 4 measurements. Use the isolated Native and Emulated diagnostic sessions described in `CONTROLLER-TEST.md` to compare frontend transition logs. See `INVESTIGATION.md` and `BUILD-NOTES.md` for source, build, and validation details.

Player correction: keep the original Skate 3 menu on Start, with HOPE settings separate on Escape/RB+Start. Installed the corrected controls; true in-game return to Skate 3's title screen and native menu additions remain unfinished. The static investigation is recorded in ../pc-gameplay-menu/ACTUAL-MENU-INVESTIGATION.md. Preserve Restart, Trick Book, map and replay while extending the original menu; launcher return does not satisfy title-screen return.

Phase 4 started: bounded optional F8 guest-swap cadence captures, a standard-library timing analyzer, reproducible baseline routes and explicit measurement limits are implemented in ../phase4. No FPS improvement or measured preset is claimed. Optimization choices await comparable live data; earlier difficulty, actual Skate title-screen/menu rebuilding, and reliability/soak gates remain open.
