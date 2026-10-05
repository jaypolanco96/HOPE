# HOPE PC gameplay menu

HOPE (Hills, Ollies, Pavement, Expression) updates Skate3Recomp by mchughalex: https://github.com/mchughalex/skate3recomp.

## Controls and home

Press the controller Menu/Start button during gameplay to open SKATE 3 / PC HOME. Escape, Home, and RB + Start remain alternate controls. Start continues normally during the game's frontend so initial setup can proceed. A held button cannot repeatedly toggle the PC menu; reconnecting with Menu held requires release and a fresh press.

Return to Home Screen asks for confirmation, closes the current game session, and returns to the HOPE launcher. Finish saving first. This is the PC home, not a forced transition to the original game's title screen. Existing careers resume normally; choose New career in HOPE for separate fresh saves.

The PC menu provides graphics, saves, offline player preferences, and Quit to Desktop. Challenges, Map & Replay explicitly opens the original game activities menu. Those game-authored screens may still show console wording/art. The PC menu does not promise to pause the underlying simulation.

## Local player

HOPE makes the selected offline player available automatically, preserves its existing save identity, and removes the PC menu's sign-in selector. System sign-in requests complete locally without a console account prompt or online sign-in. Existing profile files are not rewritten merely to change runtime sign-in status. Game-authored text and Xbox artwork have not been comprehensively replaced.

Provide your own legally obtained Skate 3 Xbox 360 ISO. No game download links are included.

## Verification

The diagnostic game/runtime build compiles. Disposable checks cover 11 controller routing cases, 20 profile/save-identity cases, 33 save-management cases, independent fog/haze/shafts persistence, and 75 launcher cases. The symbolic-link save fixture could not run because Windows did not grant that privilege. Original installed binaries and saves are preserved in place.

Live controller menu behavior, sign-in completion, challenges/map/replay, home return, and the original intermittent difficulty-screen blocker remain unverified. Earlier automatic approval review rejected native game UI access; this update does not bypass that restriction. This is a review build, with the existing SDK compatibility dependency deviation documented in phase1.
