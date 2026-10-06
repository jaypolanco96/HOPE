"""Keep local generated build inputs out of source/release distributions."""
import argparse
import base64
import io
import os
from pathlib import Path
import tarfile
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

parser = argparse.ArgumentParser()
parser.add_argument("mode", choices=["seal", "open"])
parser.add_argument("archive", type=Path)
parser.add_argument("--key-file", type=Path)
args = parser.parse_args()
if args.mode == "seal":
    key = AESGCM.generate_key(bit_length=256)
    if args.key_file is None:
        parser.error("seal requires --key-file")
    args.key_file.write_text(base64.b64encode(key).decode("ascii"))
    buffer = io.BytesIO()
    with tarfile.open(fileobj=buffer, mode="w:gz") as archive:
        for file in sorted(Path("generated").rglob("*")):
            if file.is_file() and file.suffix in (".cpp", ".h", ".cmake"):
                archive.add(file, arcname=file.as_posix(), recursive=False)
    nonce = os.urandom(12)
    args.archive.write_bytes(nonce + AESGCM(key).encrypt(nonce, buffer.getvalue(), b"HOPE-codegen-v1"))
    print(f"Encrypted build inputs: {args.archive.stat().st_size} bytes")
else:
    key = base64.b64decode(os.environ["HOPE_CODEGEN_KEY"], validate=True)
    payload = args.archive.read_bytes()
    plaintext = AESGCM(key).decrypt(payload[:12], payload[12:], b"HOPE-codegen-v1")
    with tarfile.open(fileobj=io.BytesIO(plaintext), mode="r:gz") as archive:
        for member in archive.getmembers():
            path = Path(member.name)
            if not member.isfile() or path.is_absolute() or ".." in path.parts or path.parts[0] != "generated":
                raise ValueError("Unexpected build input path")
            path.parent.mkdir(parents=True, exist_ok=True)
            with archive.extractfile(member) as source:
                path.write_bytes(source.read())
    print("Generated inputs restored for compilation only.")
