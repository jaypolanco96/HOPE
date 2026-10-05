# Actual Skate 3 menu investigation

## Confirmed by local inspection

The player's original game archives contain core_menu.apt/.const and trickguide.apt/.const in fedata.big and fedynamic.big. Their EB archive entries and RefPack decompressed lengths were checked against archive metadata. Extracted retail assets remain in ignored local scratch directories and are not distributed or committed.

The core menu APT constants call a native coremenu interface: GetNumMenus, GetNumOptions, GetOptionName, GetSubOptionName, GetOptionHelperDescription and GetCurrentMenuLevel. This means its option list and actions cannot be extended reliably by changing visible text alone. The Trick Book is its own screen and must remain attached to the game's menu/navigation lifecycle.

The locally decoded executable contains QuitSession and ID_CROSSBAR_QUIT_SESSION plus the frontend manager and quit confirmation identifiers. These establish an existing session-exit path to trace. They do not establish that the path returns a single-player career to the title screen. No guessed state ID, detached game-state call, fixed button macro or destructive sign-out is installed.

The existing frontend probe only names state 24 (press-start) and 47 (language-select). Its SetState hook at 0x82D0AFA0 logs calls; it does not provide a safe session shutdown API. Forcing state 24 from an arbitrary host thread could leave career/world/save state active and is not a supported home-return implementation.

Archive structure/compression were cross-checked against the original parser author's source:
https://github.com/GHFear/RW4ArchiveTool/blob/main/RW4ArchiveTool/Archives/Parsers/big_eb_parser.h
https://github.com/GHFear/RW4ArchiveTool/blob/main/RW4ArchiveTool/Archives/Compression/refpack/refpackd.h
These were inspected as references, not installed as a dependency or copied into the project implementation.

## Remaining work

1. Resolve the coremenu native bindings in the title-update-matched executable, including option enablement and selection dispatch; record original option IDs across career/challenge/replay contexts.
2. Trace the existing QuitSession action and save/confirmation gates to its frontend transition; determine whether it is online-session-only or a usable career-to-title path.
3. Add PC actions through the same native menu lifecycle, preserving all existing Restart/Trick Book/map/replay actions and back navigation. Graphics, quit-to-desktop and true return-to-title must have distinct action IDs and honest labels.
4. Validate with a copied career: title-to-continue, original pause actions, challenge restart, Trick Book, PC action cancellation, saving indicators, true return-to-title and re-entering the same career. Do not promote a menu rewrite without these checks.

Native game UI access was previously rejected by automatic approval review. Read-only archive/executable inspection remains possible; live transition validation is presently unavailable. The safe installed correction restores the original menu and leaves actual menu rebuilding explicitly unfinished.
