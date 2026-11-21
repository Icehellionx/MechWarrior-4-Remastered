"""Build a Steam Deck alpha ZIP using one exact, already built Windows setup."""

import argparse
import hashlib
from pathlib import Path
import re
import stat
import zipfile

SETUP_NAME = "MechWarrior-4-Remastered-Setup.exe"


def add_file(archive, name, data, mode):
    entry = zipfile.ZipInfo(name)
    entry.create_system = 3
    entry.external_attr = (stat.S_IFREG | mode) << 16
    archive.writestr(entry, data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("setup", type=Path)
    parser.add_argument("setup_sha256")
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    expected = args.setup_sha256.lower()
    if not re.fullmatch(r"[a-f0-9]{64}", expected):
        parser.error("setup_sha256 must contain 64 hexadecimal characters")
    if args.output.suffix.lower() != ".zip":
        parser.error("output must be a .zip path")

    setup = args.setup.read_bytes()
    if not setup.startswith(b"MZ") or hashlib.sha256(setup).hexdigest() != expected:
        parser.error("setup is not the expected, checksum-verified Windows installer")

    source = Path(__file__).resolve().parent
    members = (
        ("README.md", (source / "README.md").read_bytes(), 0o644),
        ("launch.sh", (source / "launch.sh").read_bytes(), 0o755),
        (SETUP_NAME, setup, 0o644),
    )
    manifest = "".join(
        f"{hashlib.sha256(data).hexdigest()}  {name}\n" for name, data, _ in members
    )
    with zipfile.ZipFile(args.output, "x") as archive:
        for name, data, mode in members:
            add_file(archive, name, data, mode)
        add_file(archive, "SHA256SUMS.txt", manifest.encode("ascii"), 0o644)

    with args.output.open("rb") as bundle:
        digest = hashlib.file_digest(bundle, "sha256").hexdigest()
    print(f"{digest}  {args.output.name}")


if __name__ == "__main__":
    main()
