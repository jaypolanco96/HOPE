# Phase 1 diagnostic build environment

LLVM 23.1.2 is installed alongside existing Visual Studio 2026 Build Tools and Windows SDK. CMake and Ninja were already present. Configuration uses the x64 Visual Studio developer environment and explicitly selects clang-cl. Python must be selected as `C:/Python314/python.exe` for both `PYTHON_EXECUTABLE` and `Python3_EXECUTABLE`; the WindowsApps alias failed on a subsequent configuration.

Build output is isolated in `out/build/phase1`. Local configure/build helpers and logs are in ignored `local-phase1`; no installed executable has been replaced.

The SDK's GNU-style flags needed adaptation for clang-cl: strict floating-point behavior and disabling char8_t now use `/clang:` forwarding. Its `-Wall` became `/Wall` under clang-cl, enabling every diagnostic and generating hundreds of thousands of warnings; this is corrected to `/W4`, the intended Wall/Wextra equivalent. The first bootstrap build was interrupted and restarted with these corrections. Errors and the normal warning checks remain enabled.

## ImGui dependency workaround

The SDK's release pin `cdda62349d6068e906c2e0ee340d6ec31eedbb9c` is unavailable from its configured remote. SDK history shows this and its immediately preceding pin were custom settings-overlay revisions. For diagnostic builds, the SDK now uses its earlier published pin `6d910d5487d11ca567b61c7824b0c78c569d62f0` (ImGui 1.92.5). Two unsupported `RasterizerGamma` assignments were removed from the PC settings overlay font setup. This changes overlay text coverage; it is a deliberate build workaround, not a claimed exact reconstruction of the release or a fix for the game's difficulty menu. Remaining pinned dependencies were retrieved successfully.

`sdk-build-compatibility.patch` preserves the dependency workaround relative to SDK commit `7eb0faf7787f5e01333c228b8e3f03c32f7295ea`. The SDK changes also live on local branch `phase1/build-compatibility`.

## Title Update 3 verification

Downloaded from the same URL configured by upstream: `https://xboxunity.net/Resources/Lib/TitleUpdate.php?tuid=21774`.

Container: 1,773,568 bytes; SHA-256 `a3fcff1e0b6059d307e9877063e50a99aca8ac41c93ca054de653f7bf16bca0e`.

Extraction with upstream `cmake/ExtractTitleUpdateXexp.py` yielded:

- `default.xexp`: 1,701,888 bytes; SHA-256 `eb9ef9109dfa6d940df2e156e7eaeda4603d2b2319ca6451f324b1c27f2b1f4c`.
- `data/webkit/EAWebkit.xexp`: 4,096 bytes; SHA-256 `5d4a308d2a6c768fc27c8b62ccb6661171dc504f8c0011a3e116cf0074e09438`.

Both payload hashes match the required constants in `src/skate3_title_update_installer.cpp`. The package and extracted game data are excluded from Git.

## Log findings

All three `BaseHeap::Release` errors occur after `Window closing, shutting down` and `Execution complete`, roughly 0.2–0.3 seconds after shutdown starts. They remain a cleanup defect lead, but are deprioritized as the immediate explanation for the earlier blank screen.

The first two sessions both contain frontend stack progression `[0,9]` → `[0]` → `[0,24]` → `[0]` → `[0,10]`. The screen names of 9 and 10 are not established yet. This sequence provides a concrete transition to correlate with a visible reproduction; it does not prove missing options or drawing failure.

## Runtime access

Computer-use access to the isolated `skate3` test window was rejected by automatic approval review with no further reason. Explicit user approval has been requested. No UI input was sent and no runtime reproduction is claimed.
