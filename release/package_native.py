"""Package built native binaries only; never include game data or codegen."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile

build, platform, output = Path(sys.argv[1]), sys.argv[2], Path(sys.argv[3])
folder = output / f"HOPE-0.6.5-{platform}-experimental"
folder.mkdir(parents=True, exist_ok=False)
output.mkdir(parents=True, exist_ok=True)
shutil.copy2(build / "skate3", folder / "skate3")
pattern = "*.so*" if platform == "linux-x64" else "*.dylib"
for library in build.glob(pattern):
    shutil.copy2(library, folder / library.name, follow_symlinks=True)
if not any(folder.glob("*rexruntime*")):
    raise RuntimeError("Matching native runtime missing")
if platform == "macos-arm64":
    if "arm64" not in subprocess.check_output(["file", str(folder / "skate3")], text=True):
        raise RuntimeError("Not an Apple Silicon binary")
    pending = list(folder.iterdir())
    while pending:
        binary = pending.pop()
        dependencies = subprocess.check_output(["otool", "-L", str(binary)], text=True).splitlines()[1:]
        for line in dependencies:
            dependency = line.strip().split(" (")[0]
            if dependency.startswith(("/System/Library/", "/usr/lib/")):
                continue
            name = Path(dependency).name
            destination = folder / name
            if dependency.startswith("/") and not destination.exists():
                shutil.copy2(dependency, destination)
                pending.append(destination)
            if destination.exists() and dependency != f"@loader_path/{name}":
                subprocess.run(["install_name_tool", "-change", dependency, f"@loader_path/{name}", str(binary)], check=True)
        if binary.suffix == ".dylib":
            subprocess.run(["install_name_tool", "-id", f"@loader_path/{binary.name}", str(binary)], check=True)
    for binary in folder.iterdir():
        subprocess.run(["codesign", "--force", "--sign", "-", str(binary)], check=True)
    launch = folder / "Launch HOPE.command"
    launch.write_text('#!/bin/sh\ncd "$(dirname "$0")" || exit 1\nexport VK_ICD_FILENAMES="$PWD/MoltenVK_icd.json"\nexec ./skate3 "$@"\n')
    shutil.copy2(build / "MoltenVK_icd.json", folder / "MoltenVK_icd.json")
else:
    launch = folder / "launch-hope.sh"
    launch.write_text('#!/bin/sh\ncd "$(dirname "$0")" || exit 1\nexec ./skate3 "$@"\n')
    for binary in (folder / "skate3", *folder.glob("*.so*")):
        result = subprocess.run(["ldd", str(binary)], capture_output=True, text=True)
        if result.returncode != 0 or "not found" in result.stdout:
            raise RuntimeError(f"Unresolved native dependency: {binary.name}\n{result.stdout}")
    if "x86-64" not in subprocess.check_output(["file", str(folder / "skate3")], text=True):
        raise RuntimeError("Not an x64 Linux binary")
launch.chmod(0o755)
(folder / "portable.txt").write_text("")
(folder / "README.txt").write_text(f"""HOPE 0.6.5 — {platform} experimental native preview
Based on Skate3Recomp by mchughalex: https://github.com/mchughalex/skate3recomp
Project: https://github.com/jaypolanco96/HOPE

Requires your own Skate 3 Xbox 360 ISO. No game files, DLC or download links are included.
Launch using {launch.name}. The native installer accepts your own ISO/game folder.
These are native-game builds, NOT the Windows HOPE launcher. Career management,
launcher mod import and launcher DLC import are unavailable here. Place your owned
DLC packages in dlc beside skate3 for native installation on next launch.
Use Start for Skate 3's original menu; the native PC settings expose supported controls.
Linux target: Ubuntu 24.04 or compatible, GTK3 and Vulkan GPU drivers required.
macOS target: Apple Silicon, macOS 15+, Vulkan through bundled MoltenVK.
Mac binaries are ad-hoc signed, not Developer ID signed or notarized.
Build/isolated tests do not verify actual gameplay. Shirt/difficulty issues remain open.
Extract into a NEW writable folder. Do not overwrite an existing career.
""", encoding="utf-8")
(folder / "BUILD.json").write_text(json.dumps({"platform": platform, "source_commit": os.environ.get("GITHUB_SHA"), "live_gameplay_tested": False, "launcher_included": False}, indent=2))
sdk = Path("third_party/rexglue-sdk")
for file in sdk.rglob("*"):
    if file.is_file() and file.name.upper().startswith(("LICENSE", "COPYING", "NOTICE")):
        target = folder / "notices" / file.relative_to(sdk)
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(file, target)
archive_path = output / (folder.name + ".tar.gz")
with tarfile.open(archive_path, "w:gz") as archive:
    archive.add(folder, arcname=folder.name)
checksum = hashlib.sha256(archive_path.read_bytes()).hexdigest()
(output / (archive_path.name + ".sha256")).write_text(f"{checksum}  {archive_path.name}\n")
print(f"Packaged {archive_path.name}")
