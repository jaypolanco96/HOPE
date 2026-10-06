<p align="center"><img src="launcher/Branding/hope.png" width="128" alt="HOPE skateboard launcher icon"></p>

# HOPE — Hills, Ollies, Pavement, Expression

A Windows launcher and PC-focused community update for **Skate3Recomp by [mchughalex](https://github.com/mchughalex/skate3recomp)**. HOPE adds career and save management, PC settings, experimental cosmetic mods, and a bright Bay Area street-skate interface.

**Bring your own Skate 3 Xbox 360 ISO. This repository provides no retail game files or ISO download links.** Gameplay backgrounds must come from your own game files or screenshots.

## Current build

Launcher **0.6.5** accompanies game **2.0.0.41-dev.ge7ca86e**, including player-owned DLC import, career-page refresh fixes and safer configuration recovery. The native game includes the clothing capture/recovery correction. These are locally built development versions. No downloadable HOPE release has been published to GitHub yet.

HOPE's WPF launcher targets Windows. The underlying upstream project supports other platforms; HOPE's additions have not been validated on Linux or macOS. See [current status](docs/CURRENT-STATUS.md), [changelog](CHANGELOG.md), and [four-phase roadmap](phase1/UPDATE-ROADMAP.md).

## What HOPE adds

- **Launcher:** mouse, keyboard and XInput navigation, rounded panels, HD backgrounds, a custom executable/window icon, and visible original-creator credits.
- **Careers and saves:** separate portable careers, save browsing, confirmed removal with recovery copies, and settings reset/restore. Editing is blocked while Skate 3 is running.
- **DLC:** Game setup imports player-owned Skate 3 Xbox 360 single-file STFS packages into a shared library. Imports are queued for native installation next launch, with header checks and duplicate protection. See [DLC setup and limits](docs/DLC.md).
- **PC settings:** fullscreen, VSync, frame caps, renderer selection, resolution scale, MSAA, ambient occlusion, distance fog/haze, bloom and light shafts. Advanced controls expose reflections, shadow quality/softness, AO radius/strength, mipmaps, decals, FOV and world/LOD distances. Availability depends on the renderer.
- **Mods:** No Intro Videos, Wide Streets, Neon Crowd, Pocket Crowd and Giant Crowd; imported allowlisted settings manifests. Imported mods start disabled. Crowd styles conflict and remain cosmetic experiments. See [mod format and limits](launcher/Mods/README.md).
- **Particles:** an original soft dust sprite with transparent blending. The hotfix separates particles from clothing, restores the original character shaders, and rejects garment-shaped draws. Effects remain experimental.
- **Diagnostics:** optional bounded F8 timing captures and an analyzer. These measure guest-swap cadence, not displayed FPS or GPU time. No measured performance gain or recommended performance preset is claimed.

## Start playing on Windows

For an existing HOPE installation:

1. Open `Play HOPE.lnk`, or `HOPE/HOPE.exe`.
2. Use **Game setup** to choose your own ISO or an extracted game folder containing `default.xex`.
3. Let game installation/title-update setup finish, then choose **Play**.
4. Set preferences in **Graphics** before launching. Use the in-game PC settings for supported live changes.

The launcher requires the **.NET 8 Windows Desktop runtime**. Source builds require the .NET 8 SDK. HOPE support files must remain beside `HOPE.exe`, `skate3.exe`, and the matching runtime DLL.

| Input | Action |
| --- | --- |
| Controller Start/Menu | Original Skate 3 menu: Restart, Trick Book, map and replay |
| Escape or RB + Start | Separate HOPE PC settings |
| F1 | Alternate PC settings shortcut |
| F8 | Start/save an optional timing capture |

**Return to HOPE Launcher** closes the game session; finish saving first. It does not return to Skate 3's title screen. The PC overlay does not pause simulation.

## Known issues and unfinished work

- The original intermittent blank difficulty screen after difficulty/camera selection remains open. The report involves a controller.
- A true return to Skate 3's title screen and rebuilding its original menu remain unfinished. Some game-authored console wording remains.
- The shirt still detached with particles disabled on the previous build. Game 2.0.0.41 corrects cloth-mode classification, stale pose recovery and mode-dependent geometry reuse. The affected player's movement retest is still needed. See [clothing correction](launcher/Mods/CLOTHING-STATE-FIX.md).
- Particle effect textures/colors, larger or atlas-mapped effects, individual quad sorting and softened depth intersections remain incomplete. The dust fallback is not every original effect.
- Crowd appearance, real controller navigation/reconnect, setup/save/load flows and long-session stability still need gameplay validation.
- Path tracing is unavailable. No iMMERSE Pro source was integrated. No verified playable beta-content pack or skeleton-pedestrian replacement is offered.

## Build and develop

See [Windows source build instructions](docs/BUILDING.md). Clone this repository with its pinned HOPE SDK submodule; the SDK includes local compatibility and PC-overlay changes that are not published in the original creator's repository.

The launcher can be built separately:

```powershell
dotnet build launcher/HOPE.Launcher.csproj -c Release -o out/hope-launcher
```

Launcher fixtures and offscreen previews use disposable data and start no game. The latest installed launcher passed **109 fixture checks**; clothing state passed **39 compiled checks**, particle isolation passed **15 checks**, and the texture generator passed **10 checks**. These checks do not establish live gameplay correctness. See [contributing and bug reports](CONTRIBUTING.md).

## Files and recovery

The cleaned local installation uses `HOPE/docs` for guides/build records and `Archive/Update packages` / `Archive/Preview builds` for earlier packages. Older builds and their save copies were preserved. `Archive/folder-moves.json` records relocations. Program rollback copies stay in `HOPE/updates`.

Close both launcher and game before applying an update. The updater verifies program hashes and backs up existing program files; it does not reset career progress or settings. When running an archived installer, pass the current HOPE folder explicitly as `-Target`.

## Credits

- **[mchughalex / Skate3Recomp](https://github.com/mchughalex/skate3recomp):** original recompilation, native renderer and project foundation.
- **[ReXGlue SDK](https://github.com/rexglue/rexglue-sdk)** and the [Skate-specific SDK fork](https://github.com/mchughalex/rexglue-skate3).
- **[Xenia](https://github.com/xenia-project/xenia):** Xbox 360 research and tooling.
- **EA Black Box / Electronic Arts:** original Skate 3 game.

HOPE is an unofficial community update. Existing source notices and original creator credits remain intact. The [original upstream documentation](docs/UPSTREAM.md) is preserved separately.
