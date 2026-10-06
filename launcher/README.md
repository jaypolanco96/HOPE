# HOPE

**Hills, Ollies, Pavement, Expression.** A street-skate name inspired by Bay Area hills, spots, and individual style.

HOPE is a community update built on **Skate3Recomp by mchughalex**: https://github.com/mchughalex/skate3recomp. Original project credit is visible on every launcher page and in Credits. ReXGlue, Xenia contributors, and EA Black Box / Electronic Arts are credited separately. HOPE is unofficial.

## Start

Open `C:\GOG Games\Skate3Recomp-Windows\Play HOPE.lnk` or `HOPE\HOPE.exe` in File Explorer. Keep the launcher and its support files beside `skate3.exe` and the matching runtime DLL. The local HOPE bundle uses portable settings and a copied career, separate from the original installation.

Launcher 0.6.3 embeds the original HOPE H/skateboard icon in the executable and WPF window. Editable vector art, PNG, multi-resolution ICO and a reproducible raster generator are in Branding. Older local builds are preserved under Archive/Update packages and Archive/Preview builds; launcher guides/build records are in HOPE/docs. The folder-moves.json archive ledger records the relocation. Active saves, settings, game data and rollback copies were preserved.

Home offers Play, Graphics, My saves, Game setup, and Credits. Use a mouse, Tab/arrow keys and Enter, or an XInput controller. Controller D-pad/stick up/down moves focus, left/right changes selections, A activates, and B cancels save removal or returns Home. Escape also returns Home or cancels confirmation. Native controller navigation needs an actual controller test; PlayStation/generic controller support beyond XInput is not implemented in this launcher.

## Your game, your ISO

**You must provide your own Skate 3 Xbox 360 ISO. No game files or ISO download links are provided.** This reminder remains visible on every page. Game setup can choose your ISO or an existing extracted game folder containing `default.xex`.

Play uses the original game's installer when game files are missing, passing the selected ISO through its supported `SKATE3_INSTALL_ISO` environment setting. Existing installations skip ISO reimport. The launcher does not download an ISO. The existing runtime's title-update setup remains part of the game installer. Keep its window open until setup completes.

## Graphics and saves

Graphics persists renderer, resolution, MSAA, AO, AO quality, distance fog, haze, shafts, and bloom. Native effects are disabled with Emulated selected. Launcher settings apply to the next session; use the in-game Graphics page for live changes. Editing/removal is blocked while any `skate3` process is running to protect shared save locations.

My saves lists career/save packages across profiles, supports folder access, and confirms removal. The package and optional metadata are moved to `.pc-save-backups` outside active package enumeration, with `RESTORE.txt`. Other saves and profiles are retained. Locked metadata triggers package rollback; linked/junction paths are rejected. Close HOPE/the game before restoring a recovery copy and preserve any new save first.

## Artwork

The local bundle has three authentic 1280×720 gameplay backgrounds extracted from the player's supplied `Attract_english_ntsc.vp6`. They fill the launcher window; they are native HD frames, not AI images or fabricated captures. Artwork is excluded from Git. Prepare it from your own extracted game files with:

```powershell
./launcher/Prepare-Artwork.ps1 -GameRoot '<your extracted game folder>' -OutputDirectory '<HOPE folder>/assets/gameplay'
```

The preparation script requires a local FFmpeg installation and makes no downloads. Game setup also accepts your own HD JPG/PNG screenshots. Background selection is manual, so the launcher does not animate or rotate images while idle.

## Build and check

Build with .NET 8 SDK on Windows:

```powershell
dotnet build launcher/HOPE.Launcher.csproj -c Release -o out/hope-launcher
```

The runnable launcher requires the .NET 8 Windows Desktop runtime, already available on this development machine. Copy all output files into the HOPE game bundle. Run fixture checks using `HOPE.exe --self-test --test-report <report path>`. Offline layout export uses `--root <bundle path> --render <PNG path> --page Home|Graphics|Saves|Setup|Credits --size 1360x850`; it shows no native window and starts no game. Fixture checks use disposable data, never actual player saves.

Phase 3 adds Help & recovery, reversible selected-career settings reset/restore, fullscreen/VSync/FPS preferences, active-input prompts and controller reconnect protection. See ../phase3/README.md. Live game launch/ISO installation, hardware controls, difficulty/camera persistence and the original intermittent difficulty-screen defect still require runtime validation. Measured presets await Phase 4.

PC controls correction: Start opens the original Skate 3 menu. Escape or RB + Start opens HOPE settings. Return to HOPE Launcher closes the session; actual Skate 3 title-screen return remains unimplemented. See ../pc-gameplay-menu/README.md and ACTUAL-MENU-INVESTIGATION.md.

Phase 4 starts with optional F8 timing captures and Open performance captures in Help. Captures are bounded and do not run in normal play until enabled. Python 3 is needed only for the separate analysis script, not for playing/capturing. See ../phase4/README.md for instructions and the guest-cadence measurement limits.
