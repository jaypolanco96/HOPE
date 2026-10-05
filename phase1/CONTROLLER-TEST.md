# Phase 1 controller test

The diagnostic build records menu transitions. It does not yet fix the blank difficulty screen. These portable sessions keep test saves separate from your normal save.

## First run: Native rendering

Open `local-phase1/sessions/20261005-105418-Fresh-Native-Diagnostic-Controller-6f00b610/skate3.exe` from this repository in File Explorer. Run only one test session at a time.

1. Use your controller to reach difficulty selection. Confirm once per screen, allowing each screen to finish appearing.
2. Record the difficulty and camera choices and whether you reach gameplay.
3. If the difficulty screen becomes blank, note whether the background moves, audio continues, and controller navigation or Back responds.
4. Press F5 on the keyboard once. Record whether the options return and whether you can continue. This changes rendering during the run; report it as a mixed-renderer run.
5. Close the test normally. Keep its `logs` folder and `session.json`.

## Second run: Emulated rendering

Open `local-phase1/sessions/20261005-105423-Fresh-Emulated-Diagnostic-Controller-b30e0dd6/skate3.exe`. Repeat the same difficulty and camera choices. Record success or failure even if neither run fails.

Tell the developer which session you ran, your choices, the outcome, and what F5 did if the screen went blank. The logs remain locally available for investigation; no upload is needed when working in this workspace.

Do not overwrite the installed executable with this diagnostic build. Fresh-profile results do not establish whether an existing career save triggers the issue. More tests will follow once the first pair is analyzed.
