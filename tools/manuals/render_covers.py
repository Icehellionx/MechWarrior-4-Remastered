#!/usr/bin/env python3
"""Render deterministic launcher thumbnails from the cleaned manual covers."""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path

import pymupdf
from PIL import Image


MANUALS = (
    ("Mechwarrior 4 Black Knight Manual.pdf", (327.14, 510.36), None),
    ("Mechwarrior 4 Mercenaries Manual.pdf", (652.12, 510.26), None),
    ("Mechwarrior 4 Vengeance Manual.pdf", (509.80, 323.00), None),
)


def render_cover(source_path: Path, output_path: Path, expected_size: tuple[float, float], crop: tuple[float, float, float, float] | None) -> None:
    document = pymupdf.open(source_path)
    try:
        if document.page_count < 1:
            raise ValueError(f"Manual has no pages: {source_path.name}")
        page = document[0]
        actual_size = (round(page.rect.width, 2), round(page.rect.height, 2))
        if actual_size != expected_size:
            raise ValueError(f"Unexpected first-page geometry for {source_path.name}: {actual_size}")

        clip = page.rect
        if crop is not None:
            clip = pymupdf.Rect(
                page.rect.width * crop[0],
                page.rect.height * crop[1],
                page.rect.width * crop[2],
                page.rect.height * crop[3],
            )
        pixmap = page.get_pixmap(matrix=pymupdf.Matrix(3, 3), clip=clip, alpha=False, colorspace=pymupdf.csRGB)
        image = Image.frombytes("RGB", (pixmap.width, pixmap.height), pixmap.samples)
        image.thumbnail((92, 92), Image.Resampling.LANCZOS)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        image.save(output_path, format="PNG", optimize=True, compress_level=9)
    finally:
        document.close()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("input_directory", type=Path)
    parser.add_argument("output_directory", type=Path)
    args = parser.parse_args()

    expected_outputs: set[Path] = set()
    for name, expected_size, crop in MANUALS:
        source = args.input_directory / name
        output = args.output_directory / (Path(name).stem + ".cover.png")
        render_cover(source, output, expected_size, crop)
        expected_outputs.add(output.resolve())
        digest = hashlib.sha256(output.read_bytes()).hexdigest()
        with Image.open(output) as rendered:
            size = rendered.size
        print(f"{output.name}: {digest} ({size[0]}x{size[1]})")

    actual_outputs = {path.resolve() for path in args.output_directory.glob("*.cover.png")}
    if actual_outputs != expected_outputs:
        unexpected = sorted(str(path) for path in actual_outputs - expected_outputs)
        raise ValueError(f"Unexpected manual-cover outputs: {unexpected}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
