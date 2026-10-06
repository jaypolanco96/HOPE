# Add your own Skate 3 DLC

Skate 3 has Xbox 360 DLC. The upstream runtime already supports automatic
installation of player-supplied packages; HOPE exposes that workflow in its
launcher. See [upstream DLC instructions](https://github.com/mchughalex/skate3recomp#installing-dlc).

1. Finish installing your own game and its title update.
2. Close Skate 3. Open **Game setup → Import owned DLC…** in HOPE.
3. Select your own Skate 3 Xbox 360 DLC package files. Multiple selection and
   extensionless filenames are supported. No packages or download links are provided.
4. Review the results. Launch the game, let its native DLC installer finish,
   and check the appropriate game menus for the imported content.

**Imported means queued, not verified installed or playable.** The launcher checks
package headers; the runtime validates/extracts the package filesystem next launch.
If content is missing, inspect the game's latest log for `DLC` messages. Corrupt
payloads, entitlement-dependent content, or unsupported game/package combinations
may fail despite a recognizable header. Do not assume every DLC is compatible.

HOPE copies packages into `dlc` beside the main `HOPE.exe`. Source files are never
moved or edited. Stable 40-character content-hash filenames fit the runtime's
content filename field. Matching imports are skipped, and a conflicting/damaged
destination is reported without replacement. Copies stage outside the scanned
folder and become visible only after completion. Errors are reported per file.

The library is shared across careers. Each career still uses its own native
content installation/save location. Launch overrides point to the main library
and enable automatic installation without editing saved settings. Launching
an isolated career directly through its `skate3.exe` bypasses these overrides;
use HOPE to launch it. Manually supplied DLC in other runtime search folders
remains subject to the native scanner and is not represented in this import list.

Supported import headers: CON, LIVE or PIRS; metadata version 1/2; marketplace
content type `00000002`; Skate 3 title `454108E6`; single-file STFS volume.
Unknown title IDs, ISOs, archives, saves, title updates and SVOD packages requiring
companion files are rejected. The importer does not convert packages or alter
licenses. Folder access is not an uninstall action: native extracted content can
remain after removing its source package. Uninstall is not implemented.

Validation: 30 synthetic DLC fixture checks cover identification, rejection,
duplicates, source preservation, locks, game-open races, temporary-file cleanup,
linked destinations, and shared-career launch configuration. These fixtures are
not playable DLC. No owned package was available for a live in-game test.
