#!/usr/bin/env python3
"""Reproducibly clean the three local MechWarrior 4 manuals."""

from __future__ import annotations

import argparse
import io
import math
from pathlib import Path

import numpy as np
import pymupdf
from PIL import Image


BK_NAME = "Mechwarrior 4 Black Knight Manual.pdf"
MERC_NAME = "Mechwarrior 4 Mercenaries Manual.pdf"
VENGEANCE_NAME = "Mechwarrior 4 Vengeance Manual.pdf"


def png_bytes(image: Image.Image) -> bytes:
    buffer = io.BytesIO()
    image.save(buffer, format="PNG", optimize=True)
    return buffer.getvalue()


def common_metadata(title: str) -> dict[str, str]:
    return {
        "title": title,
        "author": "Microsoft",
        "subject": "MechWarrior 4 reference manual",
        "keywords": "MechWarrior 4, manual, reference",
        "creator": "MW4 Remastered reproducible manual pipeline",
        "producer": "PyMuPDF",
    }


def clean_black_knight(source_path: Path, output_path: Path) -> None:
    source = pymupdf.open(source_path)
    if source.page_count != 35:
        raise ValueError(f"Unexpected Black Knight page count: {source.page_count}")
    cover_page = source[0]
    cover_rect = cover_page.rect
    body_rect = source[1].rect
    if cover_rect.width < body_rect.width * 1.9 or cover_rect.width > body_rect.width * 2.1:
        raise ValueError("Black Knight first page is not the expected two-cover spread")

    output = pymupdf.open()
    # The cover is a composite of three image objects on a rotated PDF page.
    # Render the composed visual page losslessly, then split its visible left
    # and right halves. This preserves the spine/paperclip overlays that cannot
    # be recovered by extracting only the background image.
    cover_pixmap = cover_page.get_pixmap(matrix=pymupdf.Matrix(3, 3), alpha=False, colorspace=pymupdf.csRGB)
    cover_image = Image.frombytes("RGB", (cover_pixmap.width, cover_pixmap.height), cover_pixmap.samples)
    if cover_image.width % 2:
        raise ValueError(f"Black Knight cover render has odd width: {cover_image.width}")
    midpoint = cover_image.width // 2
    back_image = cover_image.crop((0, 0, midpoint, cover_image.height))
    front_image = cover_image.crop((midpoint, 0, cover_image.width, cover_image.height))

    front = output.new_page(width=body_rect.width, height=body_rect.height)
    front.insert_image(front.rect, stream=png_bytes(front_image), keep_proportion=True)
    output.insert_pdf(source, from_page=1, to_page=source.page_count - 1)
    back = output.new_page(width=body_rect.width, height=body_rect.height)
    back.insert_image(back.rect, stream=png_bytes(back_image), keep_proportion=True)

    output.set_metadata(common_metadata("MechWarrior 4: Black Knight - Field Manual"))
    output.save(output_path, garbage=4, deflate=True, reproducible=True, no_new_id=True)
    output.close()
    source.close()


def last_horizontal_detail_row(page: pymupdf.Page) -> int:
    pixmap = page.get_pixmap(matrix=pymupdf.Matrix(1, 1), alpha=False, colorspace=pymupdf.csGRAY)
    pixels = np.frombuffer(pixmap.samples, dtype=np.uint8).reshape(pixmap.height, pixmap.width).astype(np.int16)
    horizontal_edges = np.abs(np.diff(pixels, axis=1)).mean(axis=1)
    rows = np.where(horizontal_edges > 1.5)[0]
    return int(rows[-1]) if len(rows) else -1


def clean_vengeance(source_path: Path, output_path: Path) -> int:
    source = pymupdf.open(source_path)
    if source.page_count != 98:
        raise ValueError(f"Unexpected Vengeance page count: {source.page_count}")
    sizes = {(round(page.rect.width, 2), round(page.rect.height, 2)) for page in source}
    if len(sizes) != 1:
        raise ValueError(f"Vengeance pages do not share one source size: {sorted(sizes)}")

    detail_rows = [last_horizontal_detail_row(page) for page in source]
    if min(detail_rows) < 300 or max(detail_rows) > 360:
        raise ValueError(f"Vengeance content boundary is outside the reviewed range: {min(detail_rows)}..{max(detail_rows)}")
    crop_height = int(math.ceil((max(detail_rows) + 4) / 2) * 2)

    output = pymupdf.open()
    output.insert_pdf(source)
    for page in output:
        page.set_cropbox(pymupdf.Rect(0, 0, page.rect.width, crop_height))
    output.set_metadata(common_metadata("MechWarrior 4: Vengeance - BattleTech Reference Manual"))
    output.save(output_path, garbage=4, deflate=True, reproducible=True, no_new_id=True)
    output.close()
    source.close()
    return crop_height


def copy_mercenaries(source_path: Path, output_path: Path) -> None:
    source = pymupdf.open(source_path)
    if source.page_count != 19:
        raise ValueError(f"Unexpected Mercenaries page count: {source.page_count}")
    output = pymupdf.open()
    output.insert_pdf(source)
    output.set_metadata(common_metadata("MechWarrior 4: Mercenaries - Manual"))
    output.save(output_path, garbage=4, deflate=True, reproducible=True, no_new_id=True)
    output.close()
    source.close()


def validate_output(path: Path, page_count: int) -> None:
    document = pymupdf.open(path)
    if document.page_count != page_count:
        raise ValueError(f"Unexpected output page count for {path.name}: {document.page_count}")
    for index, page in enumerate(document):
        if page.rect.width <= 0 or page.rect.height <= 0:
            raise ValueError(f"Invalid output geometry in {path.name} page {index + 1}")
        pixmap = page.get_pixmap(matrix=pymupdf.Matrix(0.5, 0.5), alpha=False)
        if not pixmap.samples:
            raise ValueError(f"Output page did not render in {path.name} page {index + 1}")
        pixels = np.frombuffer(pixmap.samples, dtype=np.uint8)
        if float(pixels.std()) < 0.5:
            raise ValueError(f"Output page appears blank in {path.name} page {index + 1}")
    document.close()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("input_directory", type=Path)
    parser.add_argument("output_directory", type=Path)
    args = parser.parse_args()
    args.output_directory.mkdir(parents=True, exist_ok=True)

    black_knight = args.output_directory / BK_NAME
    vengeance = args.output_directory / VENGEANCE_NAME
    mercenaries = args.output_directory / MERC_NAME
    clean_black_knight(args.input_directory / BK_NAME, black_knight)
    crop_height = clean_vengeance(args.input_directory / VENGEANCE_NAME, vengeance)
    copy_mercenaries(args.input_directory / MERC_NAME, mercenaries)

    validate_output(black_knight, 36)
    validate_output(vengeance, 98)
    validate_output(mercenaries, 19)
    print(f"Black Knight: split cover spread; 36 output pages")
    print(f"Vengeance: cropped 98 pages to {crop_height} points high")
    print("Mercenaries: preserved 19 pages with normalized metadata")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
