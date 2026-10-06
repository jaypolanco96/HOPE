# Contributing to HOPE

See docs/BUILDING.md for the Windows source setup. Keep original creator and
dependency credits/notices intact. Retail game files, title-update packages,
saves, extracted gameplay backgrounds, generated dumps and built programs
stay outside Git. HOPE does not provide ISO links.

## Report a bug

Use the GitHub bug-report form. Include the HOPE/game version, renderer and
graphics backend, controller or keyboard, reproducible steps, expected and
actual behavior, and whether it happens with a fresh career and existing save.
For clothing problems, include the shirt, movement and particle setting.
For the blank difficulty screen, include camera/difficulty choices and whether
background/audio continue. A relevant log excerpt or screenshot can help;
avoid sharing personal paths, account details or career-save contents.

## Changes and checks

Explain the concrete behavior changed and the checks performed. Separate
source/fixture results from actual game/controller observations. Preserve
Start/Menu for the original Skate menu and Escape/RB+Start for HOPE settings.
Launcher return must not be called Skate title-screen return.

Use temporary portable roots for save/settings fixtures. Create portable.txt
and assert the resolved settings path before any write. Preserve existing
saves/preferences when testing an updater; use disposable installer targets.
Run relevant existing checks rather than broad tests unrelated to a small edit.

Particle topology does not identify a material. Keep sprite and cloth shaders,
flags, resource identities and pipelines separate. The shader compiler script
checks the pre-particle scene baseline and Vulkan varying/resource bindings.
Unknown effects should be skipped rather than recoloring clothing.

Do not claim performance improvements from guest-swap cadence alone. Use
controlled repeatable scenes and appropriate presentation/CPU/GPU measurements.
Document skipped tests and unresolved live validation honestly.
