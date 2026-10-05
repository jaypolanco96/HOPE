# PC-friendly update before Phase 2

This preview adds a PC menu around the existing game. Phase 2 has not started, and the intermittent blank difficulty screen remains under investigation.

## Launch the preview

In File Explorer, open `C:\GOG Games\Skate3Recomp-Windows\PC-Friendly-Preview\skate3.exe`. This folder contains the new executable and its matching runtime, settings, and a hash-verified copy of the latest career save. The installed executable and original career remain separate. Do not run the installed game and preview together.

## Controls

Press **Escape**, **F1**, or **RB + Start** on the controller to open the PC menu (the controller shortcut can be changed under Controls). **Home** returns directly to its Main Menu page. Navigate using the mouse, arrow keys and Enter, or the controller's D-pad and A/B buttons.

Main Menu offers Resume Game, Graphics Settings, Manage Saves, Restart Game, and Quit to Desktop. Quit also stays visible in the left rail and opens confirmation. This is a host menu overlay; it does not jump the game's internal career state back to its Xbox title menu, and the game continues running behind it. Finish saving before quitting or restarting.

## Graphics

Display and Graphics are separate pages. Graphics includes MSAA anti-aliasing (Off/2x/4x/8x), ambient occlusion, full-resolution AO, distance fog, atmospheric haze, sun shafts, bloom, shadow options, and draw distance. Native effects are disabled in the menu when Emulated rendering is selected. Distance fog, haze, and shafts are independent switches; disabling one does not disable the others. MSAA, AO, fog, haze, and shafts apply immediately and persist in the settings file. Display/device settings that require a restart continue to use Apply & Restart.

Removing fog can expose distant scenery transitions. Higher MSAA/AO/shadow settings increase GPU or video-memory use; this update does not claim measured performance gains.

## Save management

Saves lists packages for the selected player profile, using the runtime's actual storage location, including its portable `saves` override. The career package appears as the player's name followed by Career. Refresh after saving to update the list.

Delete Selected Save opens a confirmation showing the package name. Cancel or Back keeps it. Confirm Delete & Quit closes the game first. Only after runtime teardown releases save handles does the host move the selected package and its optional metadata into a recovery directory. Other saves, profiles, settings, and game files are retained. If teardown crashes before the maintenance hook, removal does not run.

Recovery copies are outside the active package enumeration directory: the parent of the save-package root contains `.pc-save-backups/<package-root-name>/<slot-and-unique-suffix>/`. Each copy has `data`, optional `content.header`, and `RESTORE.txt` with the original paths. Close the game before restoring; preserve any new save before moving a recovery copy back. This UI removes a save from active storage rather than permanently erasing the recovery copy.

Missing/inaccessible paths, links/junctions, unsafe package names, and invalid metadata cause removal to fail without intentionally deleting data. A metadata move failure attempts to restore the package to its original path. Failures produce an error message and a log entry. Do not run multiple game instances against the same save storage while managing saves.

## Validation

The save manager has passed 37 fixture checks covering original and portable layouts, latest save bytes, metadata, unrelated saves, invalid names, locked data, metadata rollback, and junction rejection. Symbolic-link creation is unavailable on this Windows account; junction fixtures tested the Windows reparse-point rejection path instead. Real player saves have not been removed by development tools.

The final Windows executable and matching runtime built successfully from source commit `5f20469` and SDK commit `078b08a`. The actual settings writer/loader passed a round-trip test for MSAA, AO, AO quality, fog, haze, and sun shafts while preserving an unrelated setting. The packaged binary/runtime hashes match the build, the executable imports the expected diagnostic DLL, and the copied career files match the originals. Source whitespace checks passed.

Live game-window checks remain unrun in `PC-UI-CHECKS.csv`: automatic approval review rejected game-window control in the preceding investigation. Compilation and fixture tests do not establish visual quality, controller navigation, shutdown behavior in a real session, or save restore compatibility in the game. Use the preview for these checks before replacing the installed version.
