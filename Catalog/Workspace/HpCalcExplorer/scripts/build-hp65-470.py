"""Build faceplate overlay PNG: photo chrome + faceplate fill + transparent key wells.

Usage:
  python build-hp65-470.py           -> 002.png (default)
  python build-hp65-470.py 002.png
"""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

REF_W, REF_H = 470, 870
PHOTO = Path(r"C:\Users\ilkay\Desktop\HP-65\hp65.jpg")
ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
KEY_W, KEY_H = 47, 57
KEY_GAP_H, KEY_GAP_V = 10, 8
KEY_H_SMALL = 40
KEYPAD_X, KEYPAD_Y = 91, 258
KEYPAD_INSET = 5
CARD_SLOT_BAND = 12
GOLD_BAND = 14
ROW_PITCH = KEY_H + KEY_GAP_V

DISPLAY = (68, 54, 342, 42)
SLIDER_BAND = (58, 100, 354, 142)

CELLS = [
    *[(i, i // 5, i % 5, 1, 1) for i in range(15)],
    (15, 3, 0, 1, 2),
    (17, 3, 2, 1, 1),
    (18, 3, 3, 1, 1),
    (19, 3, 4, 1, 1),
    *[
        (idx, idx // 5, idx % 5, 1, 1)
        for idx in range(20, 39)
        if idx not in (24, 29, 34, 39)
    ],
]


def load_photo_portrait() -> Image.Image:
    img = Image.open(PHOTO).convert("RGB")
    scale = REF_H / img.height
    nw = int(img.width * scale)
    img = img.resize((nw, REF_H), Image.Resampling.LANCZOS)
    x0 = max(0, (nw - REF_W) // 2)
    return img.crop((x0, 0, x0 + REF_W, REF_H))


def sample_faceplate_rgb(base: Image.Image) -> tuple[int, int, int]:
    arr = np.array(base)
    patch = arr[320:750, 120:350]
    lum = patch.mean(axis=2)
    dark = patch[lum < 80]
    if len(dark):
        return tuple(int(v) for v in dark.mean(axis=0))
    return 42, 44, 48


def cell_rect(row: int, col: int, row_span: int, col_span: int) -> tuple[int, int, int, int]:
    small_rows = row_span == 1 and row < 3
    x = KEYPAD_X + KEYPAD_INSET + col * (KEY_W + KEY_GAP_H)
    y = KEYPAD_Y + CARD_SLOT_BAND + GOLD_BAND + row * ROW_PITCH
    if small_rows:
        y += KEY_H - KEY_H_SMALL
    w = col_span * KEY_W + (col_span - 1) * KEY_GAP_H
    h = KEY_H_SMALL if small_rows else row_span * KEY_H + (row_span - 1) * KEY_GAP_V
    return int(x), int(y), int(w), int(h)


def punch_rounded(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], radius: float) -> None:
    x, y, w, h = box
    draw.rounded_rectangle((x, y, x + w, y + h), radius=radius, fill=(0, 0, 0, 0))


def build(out_path: Path) -> None:
    base = load_photo_portrait()
    face_rgb = sample_faceplate_rgb(base)
    rgba = base.convert("RGBA")
    px = rgba.load()

    x0, y0, x1, y1 = 14, 48, 456, 862
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            px[x, y] = (*face_rgb, 255)

    draw = ImageDraw.Draw(rgba)
    punch_rounded(draw, DISPLAY, 4)
    punch_rounded(draw, SLIDER_BAND, 4)

    for _idx, row, col, rs, cs in CELLS:
        box = cell_rect(row, col, rs, cs)
        radius = 5 if cs >= 2 else (4 if row < 3 else 5)
        punch_rounded(draw, box, radius)

    punch_rounded(draw, (48, 836, 374, 22), 3)

    grain = Image.effect_noise((REF_W, REF_H), 12).convert("L")
    grain = grain.filter(ImageFilter.GaussianBlur(0.6))
    garr = np.array(grain, dtype=np.float32) / 255.0
    data = np.array(rgba)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if data[y, x, 3] == 0:
                continue
            g = (garr[y, x] - 0.5) * 10
            for c in range(3):
                data[y, x, c] = int(np.clip(data[y, x, c] + g, 0, 255))
    rgba = Image.fromarray(data, "RGBA")

    out_path.parent.mkdir(parents=True, exist_ok=True)
    rgba.save(out_path, optimize=True)
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")
    print(f"Faceplate RGB {face_rgb}, wells {len(CELLS)}")


if __name__ == "__main__":
    name = sys.argv[1] if len(sys.argv) > 1 else "002.png"
    build(ASSETS / name)
