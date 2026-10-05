# HOPE settings mods

Open Mods, tick a mod and choose Apply selected mods. Close the game first. Mods apply on the next launch to the selected career. Built-ins start disabled. Disable all restores previous settings, preserving later manual changes; each apply also keeps recovery copies in the career's mods/backups folder.

Five built-ins: No Intro Videos, Wide Streets (75-degree view), Clear Air (fog/haze/shafts off), Clean Lens (bloom off), Light Ride (2x MSAA, AO off). Graphics effects require the Native renderer. Light Ride reduces visual quality; no FPS gain has been measured. No Intro Videos skips frontend movies, including attract videos; it does not bypass difficulty/camera setup. Its actual in-game behavior still needs player validation.

Import accepts .hope-mod.json settings manifests, not DLLs, archives or replacement game assets. Imported mods start disabled. Different values for the same setting conflict and the entire apply is refused. Libraries are shared between careers; selections and restoration records are separate. Invalid imports are refused without replacing existing mods.

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

IDs: lowercase letters/digits/hyphens, start with a letter, 3–64 characters. Files: maximum 64 KB. Supported settings: skate3_frontend_movies_auto_skip (boolean), skate3_field_of_view (40–120), skate3_native_render_scene_msaa (1, 2, 4, 8); boolean native effects skate3_native_render_scene_fog, skate3_native_render_scene_haze, skate3_native_render_scene_shafts, skate3_native_render_scene_bloom, skate3_native_render_scene_ssao. Unknown fields/settings, duplicate fields and linked paths are rejected. No executable code is loaded.

Based on Skate3Recomp by mchughalex. HOPE requires the player's own Skate 3 Xbox 360 ISO; no game files or download links are provided.
