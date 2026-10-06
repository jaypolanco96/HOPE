"""Inspect release archives without extracting or launching programs."""
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import struct
import sys
import tarfile
import zipfile

for argument in sys.argv[1:]:
    package = Path(argument)
    expected = Path(str(package) + ".sha256").read_text().split()[0]
    if hashlib.sha256(package.read_bytes()).hexdigest() != expected:
        raise ValueError("Archive checksum mismatch")
    files = {}
    if package.suffix == ".zip":
        with zipfile.ZipFile(package) as archive:
            if archive.testzip() is not None:
                raise ValueError("Invalid ZIP data")
            for member in archive.infolist():
                if not member.is_dir():
                    files[member.filename] = archive.read(member)
    else:
        with tarfile.open(package, "r:gz") as archive:
            for member in archive.getmembers():
                if member.isdir():
                    continue
                if not member.isfile():
                    raise ValueError("Unsupported archive link/device")
                files[member.name] = archive.extractfile(member).read()
    roots = set()
    for name in files:
        path = PurePosixPath(name)
        if path.is_absolute() or ".." in path.parts or "\\" in name:
            raise ValueError("Unsafe archive path")
        roots.add(path.parts[0])
        if path.name in ("settings.toml", "profiles.toml", "hope-launcher.json", "session.json") or path.suffix.upper() in (".ISO", ".XEX", ".P", ".VP6", ".ENC"):
            raise ValueError(f"Personal/game/build-input data: {name}")
    if len(roots) != 1:
        raise ValueError("Expected one top-level folder")
    root = next(iter(roots))
    def file(name):
        return files[root + "/" + name]
    if file("portable.txt") != b"" or not file("README.txt"):
        raise ValueError("Missing fresh-install instructions/portable marker")
    if not any("/notices/" in name for name in files):
        raise ValueError("Third-party notices missing")
    metadata = json.loads(file("BUILD.json"))
    if package.suffix == ".zip":
        for name in ("skate3.exe", "HOPE.exe"):
            data = file(name)
            offset = struct.unpack_from("<I", data, 0x3C)[0]
            if data[:2] != b"MZ" or data[offset:offset + 4] != b"PE\0\0" or struct.unpack_from("<H", data, offset + 4)[0] != 0x8664:
                raise ValueError("Not Windows x64")
        for line in file("SHA256SUMS.txt").decode().splitlines():
            checksum, name = line.split("  ", 1)
            if hashlib.sha256(file(name)).hexdigest() != checksum:
                raise ValueError(f"Internal checksum mismatch: {name}")
    else:
        data = file("skate3")
        if metadata["platform"] == "linux-x64":
            if data[:5] != b"\x7fELF\x02" or struct.unpack_from("<H", data, 18)[0] != 62:
                raise ValueError("Not Linux x64")
        elif data[:4] != b"\xcf\xfa\xed\xfe" or struct.unpack_from("<I", data, 4)[0] != 0x100000C:
            raise ValueError("Not macOS arm64")
        if metadata["platform"] == "macos-arm64":
            names = {PurePosixPath(name).name for name in files if len(PurePosixPath(name).parts) == 2}
            imports = 0
            for name, binary in files.items():
                if not (name.endswith(".dylib") or name == root + "/skate3"):
                    continue
                if binary[:4] != b"\xcf\xfa\xed\xfe" or struct.unpack_from("<I", binary, 4)[0] != 0x100000C:
                    raise ValueError("Non-arm64 Mac runtime")
                commands = struct.unpack_from("<I", binary, 16)[0]
                position = 32
                for _ in range(commands):
                    command, size = struct.unpack_from("<II", binary, position)
                    if command in (12, 0x80000018, 0x8000001F, 0x80000023):
                        offset = struct.unpack_from("<I", binary, position + 8)[0]
                        dependency = binary[position + offset:position + size].split(b"\0")[0].decode()
                        leaf = PurePosixPath(dependency).name
                        bundled = dependency in ("@loader_path/" + leaf, "@rpath/" + leaf) and leaf in names
                        if not dependency.startswith(("/System/Library/", "/usr/lib/")) and not bundled:
                            raise ValueError("Unresolved Mac dependency: " + dependency)
                        imports += 1
                    position += size
            print(f"Verified {imports} Mac imports resolve to bundled/system libraries.")
        if metadata["launcher_included"] or metadata["live_gameplay_tested"]:
            raise ValueError("Native preview metadata overstates support")
    print(f"Verified {package.name}: checksums, architecture, notices and fresh-install contents ({len(files)} files).")
