# HOPE launcher — before Phase 2

HOPE means **Hills, Ollies, Pavement, Expression**. This launcher adds bright lime, pink and mint text, rounded panels, shadows, a Bay Area street-skate theme and full-window gameplay backgrounds. Original Skate3Recomp creator **mchughalex** is credited on every page and prominently in Credits. The original upstream project is https://github.com/mchughalex/skate3recomp.

The local playable bundle is `C:\GOG Games\Skate3Recomp-Windows\HOPE`. Open `HOPE.exe`. The original installation remains separate. The bundle uses portable settings and a copied career.

Home, Graphics, My saves, Game setup and Credits support mouse/keyboard navigation; XInput controller navigation is implemented. Graphics writes the existing renderer settings, including AO, MSAA, fog, haze, shafts and bloom. Save removal requires confirmation and archives packages and metadata with recovery instructions. Editing is blocked while any skate3 process runs.

Every page reminds the player to supply their own Skate 3 Xbox 360 ISO. No ISO download links or game files are included in the source repository. Play invokes the original installer when needed. The local backgrounds are three authentic native 1280×720 frames extracted from the player's installed attract movie. Artwork is excluded from Git; the preparation script makes no downloads and records provenance.

Validation: launcher Release build passes with zero warnings/errors; 30 disposable fixture checks pass, including settings persistence, install arguments, save isolation, locked metadata rollback and rejection of linked paths. All five pages were exported and visually inspected offline; Home also inspected at 1280×720. Artwork preparation ran successfully against the local game movie. These checks do not establish live controller/gameplay/ISO-install behavior. The original intermittent blank difficulty screen remains unresolved, and Phase 2 has not started. Earlier automatic approval review rejected native game access, so live game verification was not performed.

See `launcher/README.md` for launch, build, controls and recovery instructions. Backgrounds, compiled binaries, copied saves and local reports are local-only. The source and SDK compatibility patch remain versioned.
