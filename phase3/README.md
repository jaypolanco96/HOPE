# Phase 3 — quality of life

The first Phase 3 review build is ready at `C:\GOG Games\Skate3Recomp-Windows\HOPE-Phase3-Review\HOPE.exe`. Prior bundles and original saves are preserved. HOPE remains Hills, Ollies, Pavement, Expression, built on Skate3Recomp by mchughalex, with the original credit and own-ISO/no-download-links reminder on every page.

## Changes

- Help & recovery groups PC-menu controls, career switching/fresh-start guidance and settings recovery. Open career folder gives access to that career's settings, logs and recovery files when present.
- Reset game settings applies only to the selected career. It moves settings.toml into `.settings-backups/<timestamp>/settings.toml` and retains restore instructions. Career packages, profiles, game path, launcher/ISO/photo preferences and other careers are untouched.
- Restore selected backup keeps current settings as another backup before staged replacement. Backups are scoped to their career. Failed replacement preserves the current file and cleans staged files; linked paths are rejected. Backups may contain the original problem, so select an earlier one if needed. Reset and restore require the game to be closed and confirmation defaults to Cancel.
- The launcher still opens when settings cannot be read, shows recovery advice and makes Help accessible. It does not automatically overwrite those settings. The lock/permission must be resolved before reset or restore can succeed.
- Fullscreen, VSync and optional in-game FPS display are available in Graphics and persist to the selected career. They apply next launch; existing in-game settings continue to support live changes where available.
- On-screen launcher prompts follow keyboard, mouse or controller input. Controller connection status appears in Help. Reconnect/handoff establishes a fresh input baseline, so held A cannot activate an unintended button. Direction repeat retains a 420 ms delay and 110 ms interval. XInput launcher controls are D-pad/stick, A and B; keyboard and mouse remain available.
- Page navigation starts at the top. Layout spacing keeps primary buttons visible and recovery pages scrollable at the checked sizes.

## Checks

Launcher Release build passes with zero warnings/errors. 75 disposable fixture checks pass, including the earlier 51 launcher checks, settings byte-for-byte reset/restore, failure rollback/staging cleanup, career/profile preservation, cross-career rejection, linked-path rejection, unreadable-settings access to Help, display/FPS preference loading, and controller edge/repeat/reconnect sequences. Offline page previews are developer layout exports; they start no game and show no native window. No real settings were reset and no real saves were modified.

Phase 3 changes are in the launcher; the game/runtime binaries and existing native reliability fixes remain the Phase 2 build. Their source/SDK and hashes are recorded separately in the local bundle session.json. Hardware reconnect and visible game behavior are not established by synthetic input checks.

## Open gates

Difficulty/camera persistence and changes remain owned by the original game, without guessed memory patches or fake launcher controls. The original blank difficulty screen remains unresolved. Measured performance presets await Phase 4 measurements; this update makes no FPS improvement claim. Live setup, preference restart, hardware reconnect, recovery through actual visible dialogs and the Phase 2 mixed-session/save-load gates remain outstanding. Earlier automatic approval review rejected native game access, so those live checks were not performed by the agent. Phase 4 has now started with optional bounded guest-frame captures and analysis; see ../phase4/README.md. Measured optimization and live release gates remain open.
