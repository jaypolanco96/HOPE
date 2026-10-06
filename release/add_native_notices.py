"""Add final runtime notices without modifying signed binaries/permissions."""
import hashlib
import io
from pathlib import Path
import sys
import tarfile

source, output = map(Path, sys.argv[1:3])
with tarfile.open(source, "r:gz") as original, tarfile.open(output, "w:gz") as target:
    members = original.getmembers()
    root = members[0].name.split("/")[0]
    for member in members:
        if member.isfile() and member.name.endswith("/README.txt"):
            text = original.extractfile(member).read().replace(b"GTK3 and Vulkan GPU drivers required.", b"GTK3, Vulkan drivers and curl required.")
            member.size = len(text)
            target.addfile(member, io.BytesIO(text))
        else:
            target.addfile(member, original.extractfile(member) if member.isfile() else None)
    if "macos" in source.name:
        for notice in sorted(Path("release/notices").glob("*.txt")):
            target.add(notice, arcname=f"{root}/notices/macos-vulkan/{notice.name}")
Path(str(output) + ".sha256").write_text(f"{hashlib.sha256(output.read_bytes()).hexdigest()}  {output.name}\n")
print(f"Finalized {output.name}; binaries unchanged.")
