#!/usr/bin/env python3
"""Render manual contact sheets and report page/content geometry."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

import pymupdf
from PIL import Image, ImageChops, ImageDraw, ImageOps


def content_bbox(image: Image.Image) -> tuple[int, int, int, int] | None:
    grayscale = image.convert("L")
    difference = ImageChops.difference(grayscale, Image.new("L", grayscale.size, 255))
    mask = difference.point(lambda value: 255 if value > 12 else 0)
    return mask.getbbox()


def make_contact_sheets(name: str, pages: list[Image.Image], output: Path) -> list[str]:
    sheet_paths: list[str] = []
    tile_width, tile_height = 220, 300
    for start in range(0, len(pages), 20):
        batch = pages[start : start + 20]
        sheet = Image.new("RGB", (tile_width * 5, tile_height * 4), "#202629")
        draw = ImageDraw.Draw(sheet)
        for offset, page in enumerate(batch):
            thumbnail = ImageOps.contain(page.convert("RGB"), (tile_width - 16, tile_height - 30))
            x = (offset % 5) * tile_width + (tile_width - thumbnail.width) // 2
            y = (offset // 5) * tile_height + 22
            sheet.paste(thumbnail, (x, y))
            draw.text((offset % 5 * tile_width + 6, offset // 5 * tile_height + 4), f"Page {start + offset + 1}", fill="white")
        path = output / f"{name}-contact-{start + 1:03d}-{start + len(batch):03d}.jpg"
        sheet.save(path, quality=88)
        sheet_paths.append(str(path))
    return sheet_paths


def inspect(path: Path, output: Path, dpi: int) -> dict:
    document = pymupdf.open(path)
    rendered: list[Image.Image] = []
    pages: list[dict] = []
    scale = dpi / 72
    for index, page in enumerate(document):
        pixmap = page.get_pixmap(matrix=pymupdf.Matrix(scale, scale), alpha=False, colorspace=pymupdf.csRGB)
        image = Image.frombytes("RGB", (pixmap.width, pixmap.height), pixmap.samples)
        rendered.append(image)
        bbox = content_bbox(image)
        pages.append(
            {
                "page": index + 1,
                "media_points": [round(page.rect.width, 2), round(page.rect.height, 2)],
                "render_pixels": [image.width, image.height],
                "content_bbox_pixels": list(bbox) if bbox else None,
                "content_fraction": round(((bbox[2] - bbox[0]) * (bbox[3] - bbox[1])) / (image.width * image.height), 4) if bbox else 0,
            }
        )

    stem = path.stem.lower().replace(" ", "-")
    contacts = make_contact_sheets(stem, rendered, output)
    return {"file": path.name, "page_count": len(document), "pages": pages, "contact_sheets": contacts}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("input_directory", type=Path)
    parser.add_argument("output_directory", type=Path)
    parser.add_argument("--dpi", type=int, default=72)
    args = parser.parse_args()
    args.output_directory.mkdir(parents=True, exist_ok=True)
    reports = [inspect(path, args.output_directory, args.dpi) for path in sorted(args.input_directory.glob("*.pdf"))]
    report_path = args.output_directory / "manual-inspection.json"
    report_path.write_text(json.dumps(reports, indent=2), encoding="utf-8")
    print(report_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
