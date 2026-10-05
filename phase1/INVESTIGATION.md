# Phase 1: blank difficulty screen

Status: investigation started; root cause and fix not yet verified.

## Repository and baseline

Upstream: https://github.com/mchughalex/skate3recomp

Release tag: v2.0.2. Source commit: f6e0ae87fdfecbadb5c1e36c55d66a744187a3cd.
Working branch: phase1/difficulty-screen. SDK commit: 7eb0faf7787f5e01333c228b8e3f03c32f7295ea.
The tag matches the logged release version; binary equivalence to a local rebuild has not been established.

Local-only baseline in `local-phase1/baseline`: executable, runtime, settings, profile save tree, logs, and SHA-256 manifest. These files are excluded from Git. Game assets remain outside this repository.

The original game's save and logs advanced during this investigation: the later save timestamp was 09:57:55 Eastern, compared with the baseline save's 09:43:19 timestamp. No original save write was performed by the investigation tools. The historical baseline is retained for reproduction, and a separate later save/log snapshot is preserved in `local-phase1/later-snapshot`. No game process was detected when the later snapshot was taken. Do not treat a snapshot taken during live play as a proven consistent backup.

Player reports controller input. Audio/background behavior and exact selections are not yet known.

## Initial findings

- Original logs have no enabled frontend state probe, so they cannot identify the failing transition.
- Two sessions contain a BaseHeap::Release error; this may be shutdown-related and is not established as the cause.
- Native renderer code explicitly handles multiple camera-selection preview videos. This makes video/UI resource handling worth checking, but it is not evidence of a defect.
- An existing F5 toggle switches Native/Emulated rendering. Use it on the failing screen to distinguish missing drawing from missing menu state.
- Existing `skate3_demo_path_probe` logs frontend state requests without automating input. Keep `skate3_demo_path = false`.

## First source change

The frontend state probe now logs paired begin/returned events with a sequence number, previous requested state, and elapsed time. This can distinguish repeated/reentrant requests from a transition call that never returns. A returned event means only that the function returned, not that menu initialization succeeded. The patch is opt-in and does not alter guest input or menu decisions. It is not compiled or runtime-validated yet.

## Isolated reproduction

Run `phase1/Prepare-TestSession.ps1` from PowerShell. Parameters: `-Profile Saved|Fresh` and `-Renderer Native|Emulated`. Each invocation creates a separate portable session using baseline binaries/settings and, for Saved, a copy of the saved profile. Shared installed game data is referenced without copying it. Do not run two sessions simultaneously. The helper prepares a session; it does not launch the game.

1. Launch the session executable and verify its log reports the session directory as User data and the installed game directory as the game path. Verify frontend probe hooks are installed.
2. With the controller, reproduce setup using deliberate single presses. Record selected difficulty/camera, screen sequence, time, whether sound/background continues, and whether Back or navigation responds.
3. If blank, press F5 once and record whether options become visible and whether progression works. Keep the original log and label this as a mixed-renderer run.
4. Repeat in a separate Emulated session and then with a Fresh profile. Fresh means an isolated new save, not deletion of the existing save.
5. Only after slow input is covered, test rapid confirm, Back/re-entry, and focus loss. Expand to all difficulty/camera combinations after identifying the trigger.

Record every attempt in `REPRODUCTION.csv`; use `not_run` until actually tested. No live reproduction has been claimed.

## Build readiness

CMake and Ninja are available. Initial configure failed because no C++ compiler could be found. LLVM/Clang 18+ and the Windows SDK/build environment need to be located or provisioned before a diagnostic build can be produced. TU3 code generation also needs the title-update package; installed `.xexp` files alone do not establish that the original package is available. Follow upstream README instructions and keep build output separate from the installed binaries.

The pinned SDK checkout was retrieved. Recursive dependency initialization then failed at `thirdparty/imgui`: its configured remote could not provide commit `cdda62349d6068e906c2e0ee340d6ec31eedbb9c` (`not our ref`). Dependency initialization is incomplete. Recover that exact commit from an authoritative source or establish the corrected upstream dependency before proceeding; no replacement version has been guessed.

Validation completed: the session helper successfully prepared Saved/Native and Fresh/Emulated sessions; source whitespace checks passed. Runtime reproduction and compiled-patch validation remain outstanding.

Next: collect paired Native/Emulated reproductions, complete toolchain setup, build diagnostics, identify the failing state/resource, and make a focused fix. Phase 1 remains open until the roadmap completion gate passes.
