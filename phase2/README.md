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

Build checkpoint: app `4dd3497`, SDK `8892439`. Rebuilt game/runtime and reran native checks after committing; all listed checks pass. Launcher build has zero warnings/errors. Native build reports the existing deprecated getenv warning. The packaged preview is `C:\GOG Games\Skate3Recomp-Windows\HOPE-Phase2-Preview\HOPE.exe`, with copied/hash-verified career and session metadata; original binaries and career still match baseline hashes. A normal-artwork Home layout exported successfully after the recovery change. The log analyzer processed five log files (one has no execution-complete marker), with four recorded shutdown release errors; an absent marker is not proof of a crash.

## Existing saves, Home and separate careers

The player's existing save can cause the game's startup flow to continue that career again. Restart is a relaunch, so it is not a reliable route back to first-time difficulty/camera setup.

The PC menu now offers **Return to HOPE Home**. It confirms closing the session, retains saved career files, and opens or activates the parent launcher after runtime shutdown. Press Escape, Home, or RB + Start to reach the PC menu. Wait for the save indicator to finish before confirming; unsaved progress can be lost. The launcher automatically returns to Home when a session ends. Restart now waits for runtime destruction before launching a replacement process, avoiding overlapping runtimes writing to the same career.

Home now includes a career selector and **New career**. New career prepares a separate portable folder under `careers/<id>` with empty saves, a copied game program/runtime and settings, and a reference to your supplied installed game data. It does not delete or copy your existing career saves. Select Play after preparation to enter the fresh career flow. Switch to Main career to continue prior progress. Selected-career settings and save management use that career's scope. The launcher remembers selection; missing folders recover to Main career with a warning. Only completely prepared, unlinked career folders appear. Preparation runs off the UI thread and disables input until it completes. Interrupted preparation folders without career.ready are not selectable; retain them until you verify their contents before any manual cleanup.

Validation now includes **51 launcher fixtures** covering empty fresh saves, stable main-career bytes, independent settings/save scopes, selection persistence, invalid IDs, incomplete preparation and missing-folder recovery. Offline Home layouts at 1360×850 and 1280×720 and the Graphics page were inspected after the changes. Native profile/save/graphics tests pass again. Return Home, restart shutdown ordering and fresh first-time setup are implemented but still need live runtime validation; no game was started during fixtures or layout exports. The original difficulty defect remains open.

Current review package: `C:\GOG Games\Skate3Recomp-Windows\HOPE-Phase2-Review\HOPE.exe`. Prior HOPE bundles and original installation are preserved. This package is ready for player review of the implemented reliability changes; the Phase 2 60-minute gameplay completion gate remains outstanding.

Review build checkpoint: app `ee69351`, SDK `8d6d060`. The committed source was rebuilt; native checks passed and the packaged launcher passed all 51 fixtures. Launcher Release output has zero warnings/errors. Game build retains CRT deprecation warnings for getenv/_wgetenv. Review career bytes match the prior Phase 2 preview and original executable/runtime/career hashes remain at baseline. No game was launched during this validation. Bundle hashes are recorded in HOPE-Phase2-Review/session.json. All earlier preview folders remain available.
