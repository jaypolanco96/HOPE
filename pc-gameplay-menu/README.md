# HOPE controls: original Skate 3 menu restored

HOPE = Hills, Ollies, Pavement, Expression. Based on Skate3Recomp by mchughalex: https://github.com/mchughalex/skate3recomp.

## Installed behavior

Controller Start/Menu once again reaches Skate 3's own pause menu. Its Restart, Trick Book, challenge, map and replay actions remain controlled by the game. The HOPE settings overlay opens separately with Escape or RB + Start (the pre-existing F1 alternate also remains). Home is no longer a PC menu shortcut.

RB + Start is consumed before the original game receives it, and holding/reconnecting with that chord cannot repeatedly activate settings. Start alone is preserved, including after a controller reconnect.

The overlay is labelled HOPE / PC SETTINGS. Its former Return to Home Screen option is now Return to HOPE Launcher, with an explicit description that it closes the session and does not return to Skate 3's title screen. Finish saving first. This option is not presented as the game's main menu.

Automatic offline local-player availability remains enabled with the existing save identity. No console account is required by the host implementation. Some game-authored console wording remains.

## Actual game menu work

The original menu has NOT yet been rebuilt and a true in-session return to Skate 3's title screen is NOT implemented. Read ACTUAL-MENU-INVESTIGATION.md for the local asset/code findings and the remaining transition work. The prototype which intercepted Start was removed rather than left in the way of original game actions.

## Verification and installation

The diagnostic game/runtime and launcher compile. Checks pass: 12 original-menu/chord input cases, 20 offline-player/profile cases, 33 save-management cases, graphics persistence and independent fog/haze/shafts cases, and 75 launcher cases. The unchanged installer previously passed 12 disposable checks. A save-management symbolic-link fixture could not run due to missing Windows privilege.

Game/launcher build source: d39fafe. SDK: ea30f5a. Launcher: 0.3.2. PC-MENU-BUILD.json is the installed payload record; older session.json may describe an earlier build. Program files are backed up before installation. Saves/settings and original root binaries are preserved. The standalone updater can also update completed separate career program files; it refuses installation while Skate 3 is running.

Live menu controls, original actions, offline sign-in completion and the intermittent difficulty-screen blocker remain unverified. Earlier automatic approval review rejected native game UI access; no workaround was used to launch or operate the game. Offscreen launcher export and disposable fixtures do not launch Skate 3.

You must provide your own Skate 3 Xbox 360 ISO. No game download links are supplied.

Installed checkpoint: nine existing HOPE save/settings files were hash-identical after installing this correction, as were the original root skate3.exe, rexruntime.dll and roaming career. Previous program files are backed up in HOPE\updates\pc-menu-99f3c44b-3650-4523-b027-d19adcc745a9. Offscreen Help layout inspection passed; no game was launched.
