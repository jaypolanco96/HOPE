# HOPE changelog

Historical entries describe local development builds. The community preview below is published as a GitHub prerelease.

## HOPE 0.6.5 Community Preview — Windows, Linux and macOS packages

- Windows x64 fresh-install archive: launcher 0.6.5, native game
  2.0.0.41-dev.ge7ca86e and bundled .NET launcher runtime.
- Experimental Linux x64 and Apple Silicon macOS native-game archives.
  These do not include the Windows launcher or its mod/save/DLC import UI.
  Mac target: macOS 15+, ad-hoc signed, not notarized. Linux target:
  Ubuntu 24.04 or compatible, GTK3, Vulkan drivers and curl.
- Preserve TU3 runtime hooks when compiling existing TU3-generated inputs;
  make the three isolated policy/texture regression targets portable.
- Windows launcher passed 164 fixture checks. Each native CI platform passed
  39 clothing, 15 particle isolation and 9 texture checks. Architecture,
  archive hashes, notices, fresh contents and native dependencies inspected.
- No retail files, DLC, gameplay backgrounds or personal saves/settings
  included. Live gameplay, shirt appearance and difficulty recovery remain
  unverified. See [release/package scope](docs/RELEASES.md).

## Launcher 0.6.5 — Career refresh and setup recovery

- Refresh all visible career pages after switching/creating a career. Save
  lists and recovery backups no longer remain scoped to the previous career;
  old removal confirmations are cleared. Existing removal scope checks remain.
- Fix the updater's career ID check: HOPE creates 32-character N-format GUID
  folders, but the installer only recognized hyphenated D-format GUIDs. Both
  are now updated, and a locked career program blocks the entire update.
- Keep Game setup accessible when launcher preferences are locked/unreadable.
  Preserve the preferences file and show recovery guidance.
- Handle invalid, empty or unreadable game-folder settings without breaking
  readiness checks. Block launch until repaired instead of silently choosing
  another game folder. Selecting the installed folder repairs the setting.
- Release build, 164 launcher fixtures and 24 synthetic installer checks passed.
  No game was launched by these checks. Native game remains 2.0.0.41.


## Launcher 0.6.4 — Player-owned DLC import

- Add multi-file DLC import, package names/sizes, refresh and folder access
  to Game setup. Original files remain untouched; duplicate copies are skipped.
- Check Xbox 360 package headers, Skate 3 title ID and marketplace content
  type; reject saves, updates, unsupported companion-file packages and linked paths.
- Share the DLC library across main and isolated careers through launch overrides.
  Copy outside the scanned DLC folder and commit only while the game is closed.
- Native game remains 2.0.0.41. Imported means queued, not installed or verified
  playable. No retail DLC is distributed. Real DLC appearance remains untested.
- Release build and 139 launcher fixture checks passed, including 30 DLC checks.


## Game 2.0.0.41 — Clothing capture and recovery correction

- The previous shirt hotfix failed, including with particles disabled.
  Fresh owner cloth state now takes precedence over foreign shader banks.
- Bound stale recovery to three missed frames without renewing recovered
  poses; verify garment owner/extent and reset mode-transition history.
- Preserve decode mode/owner across deduplication, scene/shadow rendering
  and shape blending, including equal-payload mode changes.
- Reject behind-camera/nonfinite samples, uncertain garment removal and
  invalid packed-palette translations.
- Native build succeeded; 39 clothing, 15 particle isolation and 10 texture
  checks passed. Installed launcher passed 109 fixtures. Twelve save,
  settings, mod and original/shared files retained their hashes.
- Launcher stays at 0.6.3. Live shirt appearance requires a movement retest.
  See [details](launcher/Mods/CLOTHING-STATE-FIX.md).

## 0.6.3 — Launcher icon and folder organization

- Original H/skateboard icon embedded in the executable and window; multiple
  icon resolutions, editable SVG and generator retained in source.
- Direct Play HOPE shortcut; guides/build records moved to HOPE/docs.
- Older update/preview folders archived with a move ledger; save/settings
  copies preserved. Current game remains the 0.6.2 clothing hotfix build.
- Installed launcher: 109 fixture checks; embedded EXE icon extracted/checked.

## 0.6.2 — Particle/clothing isolation hotfix

- Reject clothing shaders, continuous garment UVs, oversized/invalid panels
  and incomplete quad batches before assigning the particle material.
- Separate sprite shader/pipelines and synthetic cache keys; restore original
  scene/character source and SPIR-V from the pre-particle build.
- 15 isolation and 10 texture checks; shader varying/resource checks.
- Shirt detachment/disappearance report still needs the user's movement retest.

## 0.6.1 — Original particle texture

- Original soft dust texture, transparent rim, clamped sampling and alpha
  blending for captured quad-list draws; per-device texture upload.
- A subsequent shirt regression report prompted the 0.6.2 isolation hotfix.
- Original game effect textures/colors remain unmapped; effects experimental.

## 0.6.0 — Advanced graphics and creative cosmetic mods

- Additional controls for reflections, HDR intermediate lighting, shadow
  softness/quality, AO radius/strength, bloom/shafts, mips, decals and particles.
- Neon Crowd, Pocket Crowd and Giant Crowd replace ordinary graphics presets.
- No restricted path-tracing code copied; unavailable beta/skeleton features
  are reported honestly instead of exposed as nonfunctional toggles.

## Earlier development phases

PC launcher/overlay, isolated careers, reversible save removal, settings
recovery, original-menu control correction, mod manifests and optional timing
captures are recorded in phase1, phase2, phase3, phase4 and pc-gameplay-menu.
Difficulty-screen, true Skate title return and live validation gates remain
open. See docs/CURRENT-STATUS.md and phase1/UPDATE-ROADMAP.md.
