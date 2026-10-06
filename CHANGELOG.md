# HOPE changelog

These entries describe local development builds, not published release assets.

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
