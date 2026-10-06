"""Create a fresh Windows preview from an explicit program allowlist."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys
import zipfile

publish, native, output = map(Path, sys.argv[1:4])
folder = output / "HOPE-0.6.5-windows-x64-preview"
folder.mkdir(parents=True, exist_ok=False)
for file in publish.rglob("*"):
    if file.is_file() and file.suffix.lower() != ".pdb":
        target = folder / file.relative_to(publish)
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(file, target)
for name in ("skate3.exe", "rexruntimerd.dll"):
    shutil.copy2(native / name, folder / name)
(folder / "portable.txt").write_text("")
(folder / "skate3.toml").write_text('game_data_root = "game"\n')
(folder / "README.txt").write_text("""HOPE 0.6.5 — Windows x64 community preview
Hills, Ollies, Pavement, Expression
Made with heart for the Skate 3 community.

1. Extract this entire folder into a NEW writable location.
2. Open HOPE.exe. The .NET launcher runtime is included.
3. In Game setup, choose your own Skate 3 Xbox 360 ISO or installed game folder.
4. Complete the native game/title-update setup, then choose Play.
5. Set graphics before launch, import your owned DLC, and choose your own
   gameplay screenshot as the background. No game images are bundled.

Controller Start/Menu: original Skate 3 menu (Restart, Trick Book, map, replay).
Escape or RB + Start: HOPE PC settings. F1: alternate shortcut.
Return to HOPE Launcher closes the session. Finish saving first.
The PC overlay does not pause the game or return to Skate 3's title screen.

Requires your own Skate 3 Xbox 360 ISO. No retail game files, DLC, saves or
download links are included. Experimental mods/particles remain experimental.
Shirt movement and intermittent difficulty-screen issues need gameplay retests.
This is a development preview, not a promise of complete compatibility.
Do not overwrite an existing installation or career with this fresh package.

HOPE: https://github.com/jaypolanco96/HOPE
Based on Skate3Recomp by mchughalex: https://github.com/mchughalex/skate3recomp
SDK: https://github.com/jaypolanco96/HOPE-SDK
Original ReXGlue and Xenia contributors are credited in the launcher and notices.
Skate 3: EA Black Box / Electronic Arts. Unofficial; not endorsed by EA.
""", encoding="utf-8")
for doc in ("DLC.md", "CURRENT-STATUS.md"):
    (folder / "docs").mkdir(exist_ok=True)
    shutil.copy2(Path("docs") / doc, folder / "docs" / doc)
notices = folder / "notices"
nuget = Path(os.environ.get("NUGET_PACKAGES", str(Path.home() / ".nuget/packages")))
frameworks = json.loads((publish / "HOPE.runtimeconfig.json").read_text())["runtimeOptions"]["includedFrameworks"]
for framework in frameworks:
    pack = framework["name"].lower() + ".runtime.win-x64"
    source = nuget / pack / framework["version"]
    licenses = [file for file in source.iterdir() if file.name.upper().startswith(("LICENSE", "THIRD-PARTY-NOTICES"))]
    if not licenses:
        raise RuntimeError(f"Missing runtime notices for {pack}")
    for file in licenses:
        target = notices / "dotnet" / pack / file.name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(file, target)
sdk = Path("third_party/rexglue-sdk")
for file in sdk.rglob("*"):
    if file.is_file() and (file.name.upper().startswith("LICENSE") or file.name.upper().startswith("COPYING") or file.name.upper().startswith("NOTICE")):
        target = notices / file.relative_to(sdk)
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(file, target)
(folder / "BUILD.json").write_text(json.dumps({"launcher": "0.6.5", "native_game": "2.0.0.41-dev.ge7ca86e", "self_contained_launcher": True, "native_gameplay_verified": False}, indent=2))
files = sorted(file for file in folder.rglob("*") if file.is_file())
for file in files:
    relative = file.relative_to(folder)
    if file.name in ("hope-launcher.json", "settings.toml", "profiles.toml", "session.json") or file.suffix.upper() in (".ISO", ".XEX", ".P"):
        raise RuntimeError(f"Personal/game data in fresh package: {relative}")
(folder / "SHA256SUMS.txt").write_text("".join(f"{hashlib.sha256(file.read_bytes()).hexdigest()}  {file.relative_to(folder).as_posix()}\n" for file in files))
archive = output / (folder.name + ".zip")
with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=6) as zip_file:
    for file in sorted(folder.rglob("*")):
        if file.is_file():
            zip_file.write(file, file.relative_to(output))
(output / (archive.name + ".sha256")).write_text(f"{hashlib.sha256(archive.read_bytes()).hexdigest()}  {archive.name}\n")
print(f"Packaged {archive.name}: {archive.stat().st_size} bytes; no player/game data included")
