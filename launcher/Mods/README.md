# HOPE settings mods

Open Mods, tick a mod and choose Apply selected mods. Close the game first. Mods apply on the next launch to the selected career. Built-ins start disabled. Disable all restores previous settings, preserving later manual changes; each apply also keeps recovery copies in the career's mods/backups folder.

Five built-ins: No Intro Videos, Wide Streets (75-degree view), Neon Crowd (bright lime pedestrians), Pocket Crowd (half-size pedestrians), Giant Crowd (double-size pedestrians). Crowd changes require the Native renderer and the existing LivingWorld identity mapping. They affect mapped pedestrian body/hair draws only, after pose interpolation, including their native shadows. Physics, behavior, player models and vehicle draws are untouched. Scaling skips malformed/unmapped palettes. They are cosmetic experiments and still need actual play testing. Choose one crowd style at a time; the others conflict. No Intro Videos skips frontend movies, including attract videos; it does not bypass difficulty/camera setup.

Clear Air, Clean Lens and Light Ride have been removed from built-ins; those effects are ordinary Graphics options. Existing ownership records remain readable. If a removed mod was enabled, Apply your new selection or Disable all to restore its values. No automatic settings migration runs during installation.

Import accepts .hope-mod.json settings manifests, not DLLs, archives or replacement game assets. Imported mods start disabled. Different values for the same setting conflict and the entire apply is refused. Libraries are shared between careers; selections and restoration records are separate. A new career inherits current settings and their mod selection, then manages them independently. Invalid imports are refused without replacing existing mods.

Example (save as custom-crisp-view.hope-mod.json):
```json
{
  "formatVersion": 1,
  "id": "custom-crisp-view",
  "name": "Custom Crisp View",
  "author": "Your name",
  "description": "An 80-degree camera with bloom disabled.",
  "settings": {
    "skate3_field_of_view": 80,
    "skate3_native_render_scene_bloom": false
  }
}
```

IDs: lowercase letters/digits/hyphens, start with a letter, 3–64 characters. Files: maximum 64 KB. Supported settings: hope_pedestrian_style (0 original, 1 neon, 2 pocket, 3 giant); skate3_frontend_movies_auto_skip (boolean), skate3_field_of_view (40–120), skate3_native_render_scene_msaa (1, 2, 4, 8); boolean native effects skate3_native_render_scene_fog, skate3_native_render_scene_haze, skate3_native_render_scene_shafts, skate3_native_render_scene_bloom, skate3_native_render_scene_ssao. Unknown fields/settings, duplicate fields and linked paths are rejected. No executable code is loaded.

Based on Skate3Recomp by mchughalex. HOPE requires the player's own Skate 3 Xbox 360 ISO; no game files or download links are provided.
