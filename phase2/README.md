# Phase 2 — reliability in progress

Started at the player's request after the HOPE launcher update. Phase 1's difficulty-screen defect remains open. This update does not claim either phase's gameplay completion gate.

## First reliability changes

Profile metadata is staged to a unique sibling, checked through flush/close, and replaced only after a successful write. Windows also flushes staged file buffers. Failed replacement preserves the existing file and removes the staged copy. Malformed existing TOML is preserved instead of silently replaced with a fallback. Failed saves now return visible PC-menu feedback and do not apply the attempted profile changes to the runtime. TOML serialization escapes control characters; invalid selected IDs are normalized without changing existing player XUIDs.

Optional corrupted backgrounds no longer prevent the launcher from opening. They are skipped in memory, retained on disk, and produce recovery advice. Image decoding owns/disposes its stream even on failure, so failed decoding does not leave the image locked.

Heap rejection messages now include address, heap base, region base/count and state. Allocation/ownership behavior is unchanged. `Analyze-Sessions.ps1` produces a read-only local report with log hashes, shutdown timing, and unclassified missing requests. All four supplied logs have a rejected release after the closing marker, 196–288 ms later. This timing does not connect those errors to the difficulty screen. Required-resource status remains unproven; no game assets were edited.

## Validation

- 16 profile fixture checks: blocked replacement preserves bytes; staging cleanup; damaged TOML preservation; multi-profile round trip; original Player XUID; selected-ID correction; control-character names; invalid parent failure.
- 34 launcher fixture checks, including damaged-art startup/recovery advice, settings access without artwork, and recovery file preservation.
- 33 core save-manager checks and graphics persistence/fog independence checks pass again. Windows symbolic-link fixture unavailable due account privilege; previously documented junction tests and launcher junction checks cover linked-path rejection.
- Windows game/runtime build passes. This remains the diagnostic build with the documented ImGui compatibility deviation, not a validated release.

All checks use disposable files. Career bytes are not rewritten by these fixes. The original installed game and career are preserved; use the separate HOPE-Phase2-Preview folder. Source checkpoints and bundle hashes are recorded in its session.json.

## Runtime audit still required

Use a disposable/copy career for new career/continue, multiple save/reload cycles, pause/resume, challenge restart, camera changes, replay, display changes, controller disconnect/reconnect and quit/relaunch. Record visible failures and timestamps against the session log. Validate blocked profile writes show feedback without a runtime profile switch. Complete the planned 60-minute mixed session and clean restart with no progress loss before marking Phase 2 complete. The difficulty screen also requires its original matrix and 30 consecutive setup-to-gameplay runs.

Earlier automatic approval review rejected native game access. No game/UI input was sent for this update; gameplay, controller and visible PC-menu feedback remain unverified. See BUG-TRACKER.csv for open issues. Run the log analyzer manually with your log folder and a local output path; it never starts the game or supplies ISO links.
