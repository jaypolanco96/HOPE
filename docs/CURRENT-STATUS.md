# Current HOPE status

Updated October 6, 2026. Launcher 0.6.4 is installed with native game
2.0.0.41-dev.ge7ca86e. Game behavior source: e7ca86e. SDK behavior source:
a459a73. Launcher icon source: 7c0a937. Later documentation commits do not
change those installed program versions.

| Area | Implemented | Still needs confirmation |
| --- | --- | --- |
| Launcher | PC navigation, graphics, careers, saves, mods, help, icon | Real controller/hardware and install/recovery flows |
| Original menu | Start/Menu preserved; Escape / RB+Start opens HOPE settings | Actual Skate title return and original-menu extensions |
| Difficulty | Diagnostic/reliability work and isolated careers | Intermittent blank screen after difficulty/camera choices |
| Graphics | Existing native effects exposed in launcher/overlay | Appearance, correctness and performance on real hardware |
| Particles/clothing | Separate particle pipelines; original character shaders; corrected cloth mode, recovery age and decode consistency | New build's shirt movement retest; original effect materials |
| Cosmetic mods | No Intro, Wide Streets, three crowd styles; settings manifest importer | Crowd appearance and shader/model edge cases |
| DLC | Player-owned single-file STFS import; shared career library; native installation next launch | Real package installation and in-game availability |
| Performance | F8 bounded cadence capture and analyzer | Controlled CPU/GPU/presentation baselines, optimizations and soak |

Latest recorded checks: 139 installed launcher fixtures (30 DLC checks); 39 compiled
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
