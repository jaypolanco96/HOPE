# Phase 1: blank difficulty screen

Status: targeted first-career renderer fallback built; live retest pending.

## Repository and baseline

Upstream: https://github.com/mchughalex/skate3recomp

Release tag: v2.0.2. Source commit: f6e0ae87fdfecbadb5c1e36c55d66a744187a3cd.
Working branch: phase1/difficulty-screen. SDK commit: 7eb0faf7787f5e01333c228b8e3f03c32f7295ea.
The tag matches the logged release version; binary equivalence to a local rebuild has not been established.

Local-only baseline in `local-phase1/baseline`: executable, runtime, settings, profile save tree, logs, and SHA-256 manifest. These files are excluded from Git. Game assets remain outside this repository.

The original game's save and logs advanced during this investigation: the later save timestamp was 09:57:55 Eastern, compared with the baseline save's 09:43:19 timestamp. No original save write was performed by the investigation tools. The historical baseline is retained for reproduction, and a separate later save/log snapshot is preserved in `local-phase1/later-snapshot`. No game process was detected when the later snapshot was taken. Do not treat a snapshot taken during live play as a proven consistent backup.

Player reports controller input. Audio/background behavior and exact selections are not yet known.

## October 6 recurrence and targeted fallback

The player supplied a screenshot of a fresh-career difficulty screen: the title and prompt render, but the option panel is empty. The boot frontend is configured to use the native scene/2D replay, while the failing session ends at `[0,10]` and the later create-a-skater flow uses `[0,67,15]`. `YieldForMenus` now forces both setup states to the game's full emulated frontend, regardless of the boot-native setting. The override ends as soon as both setup states leave the frontend stack and does not change ordinary pause menus or gameplay. A transition log records when the fallback is active.

The complete optimized Windows `skate3` target compiled and linked in `out/build/phase1`. With no game process running, the matching executable/runtime were installed into the HOPE bundle template and the affected isolated career so both existing and new careers use the fallback. The four installed hashes match the build; the previous root/career binaries are preserved in `HOPE/updates/hope-difficulty-screen-20261006-174050/rollback-manifest.json`. The screenshot and recorded frontend stack motivated this focused workaround, but the controller has not yet retested it. `out/build/phase1/portable.txt` keeps any optional test saves/settings separate from HOPE careers. Do not mark the bug verified until difficulty selection, camera selection and transition to gameplay succeed in a live run.

## Initial findings

- Original logs have no enabled frontend state probe, so they cannot identify the failing transition.
- All three completed sessions contain a BaseHeap::Release error after window shutdown and execution completion. This is a cleanup-defect lead and is deprioritized as the immediate blank-screen cause.
- Native renderer code explicitly handles multiple camera-selection preview videos. This makes video/UI resource handling worth checking, but it is not evidence of a defect.
- An existing F5 toggle switches Native/Emulated rendering. Use it on the failing screen to distinguish missing drawing from missing menu state.
- Existing `skate3_demo_path_probe` logs frontend state requests without automating input. Keep `skate3_demo_path = false`.

## First source change

The frontend state probe now logs paired begin/returned events with a sequence number, previous requested state, and elapsed time. This can distinguish repeated/reentrant requests from a transition call that never returns. A returned event means only that the function returned, not that menu initialization succeeded. The patch is opt-in and does not alter guest input or menu decisions. The full diagnostic executable has compiled and linked successfully; runtime validation is pending.

## Isolated reproduction

Run `phase1/Prepare-TestSession.ps1` from PowerShell. Parameters: `-Profile Saved|Fresh`, `-Renderer Native|Emulated`, `-Build Installed|Diagnostic`, and `-InputMethod Controller|Keyboard`. Defaults use the installed baseline and controller. Diagnostic sessions require a completed build in `out/build/phase1`. Each invocation creates a separate portable session using copied binaries/settings and, for Saved, a copy of the saved profile. Binary hashes and input method are recorded in the session manifest. Shared installed game data is referenced without copying it. Do not run two sessions simultaneously. The helper prepares a session; it does not launch the game.

1. Launch the session executable and verify its log reports the session directory as User data and the installed game directory as the game path. Verify frontend probe hooks are installed.
2. With the controller, reproduce setup using deliberate single presses. Record selected difficulty/camera, screen sequence, time, whether sound/background continues, and whether Back or navigation responds.
3. If blank, press F5 once and record whether options become visible and whether progression works. Keep the original log and label this as a mixed-renderer run.
4. Repeat in a separate Emulated session and then with a Fresh profile. Fresh means an isolated new save, not deletion of the existing save.
5. Only after slow input is covered, test rapid confirm, Back/re-entry, and focus loss. Expand to all difficulty/camera combinations after identifying the trigger.

Record every attempt in `REPRODUCTION.csv`; use `not_run` until actually tested. No live reproduction has been claimed.

## Build readiness

CMake and Ninja are available. LLVM 23.1.2 is now installed and the existing Visual Studio x64 tools/Windows SDK have been configured. The TU3 package has been downloaded and its extracted payload hashes verified against upstream's required constants. Code generation, compilation, and linking have completed successfully in the isolated build directory.

The SDK's ImGui pin is unpublished. A documented diagnostic-build workaround uses the SDK's earlier published ImGui pin and omits its two custom font-gamma assignments. Other dependencies are initialized. See `BUILD-NOTES.md` and `sdk-build-compatibility.patch` for the exact deviation; this diagnostic build is not claimed to reproduce the release byte-for-byte.

Validation completed: code-generation targets and the diagnostic executable build passed; the rounding compatibility test passed 199,260 exact-result checks plus NaN checks; the executable imports the expected diagnostic runtime DLL; its embedded manifest preserves supported Windows versions, common controls, and PerMonitorV2 DPI awareness. Four fresh diagnostic sessions (Native/Emulated, Controller/Keyboard) have been prepared with matching binary hashes. Source whitespace checks passed. Runtime reproduction and menu-fix verification remain outstanding.

Next: collect paired Native/Emulated reproductions, identify the failing state/resource, and make a focused fix. Automatic review rejected access to the isolated test window again after the user authorized continuing. No game was launched or controlled by that attempt. `CONTROLLER-TEST.md` provides a manual controller reproduction path using the prepared sessions. All reproduction cases remain `not_run`. Phase 1 remains open until the roadmap completion gate passes.

### Follow-up after camera selection

The player retested and the blank difficulty panel remained. The affected career's latest log ends at frontend stack `[0,10]`, with native loading rendering active; it never enters state 67. The earlier state-67-only fallback therefore missed this screen. It also failed to exclude setup from the separate `loading_native` decision, which could override the pause fallback. The corrected build recognizes states 10 and 67 and excludes setup from both native pause and native loading rendering. Build/install validation does not establish gameplay success: the camera-to-difficulty transition still requires a live controller retest. Saves and settings are preserved.
The corrected optimized build compiled and linked successfully and was installed into both the HOPE template and existing career with all four binary hashes verified. Its immediate rollback copies are in `HOPE/updates/hope-difficulty-camera-20261006-175728`. The previous state-67-only build was retested by the player and failed; the corrected state-10/state-67 build awaits live verification.

### Welcome wizard bypass (requested after the second failed retest)

The player's retest still failed with the emulated fallback active (`skate3_005.log`: `[0,10]`). Renderer switching is not a verified remedy. At the player's request, the new default-enabled `skate3_skip_welcome_setup` hook bypasses the entire initial difficulty/camera welcome panel. Guest boot handler `0x82704D30` normally opens FE state 10 on event 0, waits for it to disappear on event 1, then advances through `sub_826DFB30` to `0x82704DD8`. The hook sends entry through the original event-1 completion branch instead of opening the panel. It preserves initialized game settings and does not edit saves, profile flags or frontend stack memory. The next boot stage and normal cleanup remain game-owned. The hook is registered independently of diagnostic probe/automation settings.

The optimized Windows target compiled and linked. An offline C++ harness executed the actual extracted production hook and guest handler with mocked external frontend calls. It verified bypass-enabled entry advances to the exact normal next stage without opening FE10; bypass-disabled entry opens it; an active panel prevents premature advancement; exit cleanup and stack/nonvolatile/LR preservation work; no writes occur in simulated profile/settings memory. These checks do not establish live gameplay success. The game and launcher were running when the build finished; staged binaries await normal closure before installation.
After the game and launcher exited normally, the staged executable/runtime were installed into the HOPE template and existing career. All four installed hashes match the staged build; rollback copies and manifest are under `HOPE/updates/hope-welcome-bypass-20261006-181056/rollback`. No save/configuration files were modified. Live verification remains pending.
