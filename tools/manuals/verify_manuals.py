#!/usr/bin/env python3
"""Verify the qualified manual inputs and deterministic cleaned outputs."""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path

import pymupdf


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("input_directory", type=Path)
    parser.add_argument("output_directory", type=Path)
    parser.add_argument("lock_file", type=Path)
    parser.add_argument("--cover-directory", type=Path, required=True)
    args = parser.parse_args()
    lock = json.loads(args.lock_file.read_text(encoding="utf-8"))
    if lock.get("schemaVersion") != 2:
        raise ValueError("Unsupported manual lock schema")

    for expected in lock["manuals"]:
        input_path = args.input_directory / expected["name"]
        output_path = args.output_directory / expected["name"]
        if sha256(input_path) != expected["inputSha256"]:
            raise ValueError(f"Unqualified manual input: {expected['name']}")
        if sha256(output_path) != expected["outputSha256"]:
            raise ValueError(f"Manual output hash mismatch: {expected['name']}")
        cover_path = args.cover_directory / expected["coverName"]
        if sha256(cover_path) != expected["coverSha256"]:
            raise ValueError(f"Manual cover hash mismatch: {expected['coverName']}")

        document = pymupdf.open(output_path)
        sizes = sorted({(round(page.rect.width, 2), round(page.rect.height, 2)) for page in document})
        expected_sizes = sorted(tuple(size) for size in expected["pageSizes"])
        if document.page_count != expected["pageCount"] or sizes != expected_sizes:
            raise ValueError(f"Manual geometry mismatch: {expected['name']}")
        document.close()
        print(f"Verified {expected['name']} and {expected['coverName']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
