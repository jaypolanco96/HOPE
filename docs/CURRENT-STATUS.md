# Current HOPE status

Updated October 7, 2026. Launcher 0.6.6 is installed with native game
2.0.0.51-dev.g7a603df and the texture-descriptor correction described below.
These local builds include working-tree changes beyond their embedded base
commit identifiers. Launcher icon source: 7c0a937. The published 0.6.5
release packages do not yet contain these later local fixes.

The [0.6.5 Community Preview](https://github.com/jaypolanco96/HOPE/releases/tag/hope-v0.6.5-preview.1)
provides Windows x64 launcher/game and experimental Linux x64/macOS arm64
native-game packages. The launcher remains Windows-only. Native CI source:
a3a8d4f; each native platform passed 63 isolated checks. These are not live
gameplay tests. Mac minimum: Apple Silicon/macOS 15+, ad-hoc signed and not
notarized. See [package scope](RELEASES.md).

| Area | Implemented | Still needs confirmation |
| --- | --- | --- |
| Launcher | PC navigation, graphics, careers, saves, mods, help, icon | Real controller/hardware and install/recovery flows |
| Original menu | Start/Menu preserved; Escape / RB+Start opens HOPE settings | Actual Skate title return and original-menu extensions |
| Difficulty | Both renderer fallback retests failed. A default-enabled welcome-wizard bypass is installed in the HOPE template and existing career; it follows the original boot completion branch and retains initialized settings | Live controller test through subsequent career setup and gameplay |
| Graphics | Existing native effects exposed in launcher/overlay | Appearance, correctness and performance on real hardware |
| Particles/clothing | Separate particle pipelines; original character shaders; corrected cloth mode, recovery age and decode consistency | New build's shirt movement retest; original effect materials |
| Cosmetic mods | No Intro, Wide Streets, three crowd styles; settings manifest importer | Crowd appearance and shader/model edge cases |
| DLC | Player-owned single-file STFS import; shared career library; native installation next launch | Real package installation and in-game availability |
| Performance | F8 bounded cadence capture and analyzer | Controlled CPU/GPU/presentation baselines, optimizations and soak |

Latest recorded checks: 164 installed launcher fixtures (30 DLC and 25 career/
configuration regression checks); 24 synthetic updater checks; 39 compiled
clothing state regression checks; 15 compiled
particle/cloth isolation checks; 10 texture checks; two isolated particle
shader variants compiled for both D3D12 and Vulkan with UV/resource bindings
verified. Character scene source and original SPIR-V table match the
pre-particle build. These are not live gameplay tests.

Historical native checks cover input/menu routing, profile reliability,
save management, graphics persistence and timing capture. Their counts and
limitations are in the phase checkpoints; they are not a claim that every
check was rerun for an icon or documentation-only change.

The player confirmed the shirt still detaches with particles disabled on
game 2.0.0.37. The new correction addresses invalid mode flags observed in
that session's log plus stale recovery and decode-mode bugs. Twelve existing
save/settings/mod/original/shared files retained their hashes after install
and fixture tests. The visible outcome still requires the same player/shirt
movement retest; do not mark it verified until that succeeds. See
[clothing correction](../launcher/Mods/CLOTHING-STATE-FIX.md).

Path tracing, skeleton pedestrians and playable beta content are unavailable.
No restricted iMMERSE Pro source or retail art is bundled. The current
particle sprite is original HOPE art, not every Skate 3 effect material.

Local guides/build metadata are in HOPE/docs. Program recovery copies stay
in HOPE/updates. Older packages are under Archive/Update packages and
Archive/Preview builds; their save/settings copies were preserved. Historical
checkpoint paths refer to their original locations unless stated otherwise.

The earlier graphics-fixture shared-settings incident and baseline recovery
remain documented in launcher/Mods/ADVANCED-GRAPHICS.md. New save/settings
fixtures must create portable.txt and assert their paths before writing.

All four phases have implementation work; earlier completion gates and
Phase 4 performance/soak validation are still open. No measured FPS gain or
performance preset is advertised.

The October 6 launcher review fixed stale career save/recovery pages, unreadable
preferences, invalid game-folder recovery and skipped N-format careers in updates.
Synthetic updater tests include all-file locking and save/settings preservation.
The latest inspected game log (`skate3_022.log`) contains recurring XMA input
offset diagnostics and a heap-release diagnostic during shutdown. These are
recorded for native investigation; no audio or memory fix is claimed from this
launcher update, and no live playback regression test was performed.

Subsequent October 6 gameplay testing reproduced an audio bit-stream assertion.
The native correction validates XMA offsets before setting the cursor and
bounds packet-tail reads; 6,932 guarded reference reads passed. A separate
invalid guest-function call remains unresolved and has additional caller
diagnostics. See [audio crash investigation](AUDIO-BOUNDS-FIX.md). Live soak
testing and updated release packages remain outstanding.

The October 7 texture assertion (`!fetch.stacked`, `info.cpp:45`) is handled
by rejecting invalid texture shape combinations through the parser's existing
failure result. A compiled regression covers 64 descriptor combinations;
live visual/gameplay retesting remains required. See
[texture assertion correction](TEXTURE-FETCH-FIX.md).

Launcher 0.6.6 removes phase/development copy and capture controls from the
player interface, and adds checksum-verified public-release updates under
Help & recovery. Validation passed 178 launcher fixtures and 35 installer
checks, plus an offline layout review. See [launcher updates](LAUNCHER-UPDATES.md)
for release requirements and the remaining live self-update validation.
