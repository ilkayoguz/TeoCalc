"""Build 001.png from 000.png: red keypad background + transparent key wells."""
from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
SRC = ASSETS / "000.png"
OUT = ASSETS / "001.png"

KEY_W, KEY_H = 47, 57
KEY_GAP_H, KEY_GAP_V = 10, 8
KEY_H_SMALL = 40
KEYPAD_X, KEYPAD_Y = 91, 258
KEYPAD_W, KEYPAD_H = 287, 522
KEYPAD_INSET = 5
CARD_SLOT_BAND = 12
GOLD_BAND = 14
ROW_PITCH = KEY_H + KEY_GAP_V

KEYPAD_RED = (220, 32, 32, 255)

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


def chroma_green_alpha(arr: np.ndarray) -> np.ndarray:
    rgb = arr[:, :, :3].astype(np.int16)
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    green = (g > 120) & (g > r + 40) & (g > b + 40)
    return green


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


def build() -> None:
    if not SRC.exists():
        raise FileNotFoundError(SRC)

    base = Image.open(SRC).convert("RGBA")
    data = np.array(base)
    green = chroma_green_alpha(data)
    data[green, 3] = 0

    rgba = Image.fromarray(data, "RGBA")
    draw = ImageDraw.Draw(rgba)

    kx, ky, kw, kh = KEYPAD_X, KEYPAD_Y, KEYPAD_W, KEYPAD_H
    draw.rectangle((kx, ky, kx + kw, ky + kh), fill=KEYPAD_RED)

    for _idx, row, col, rs, cs in CELLS:
        box = cell_rect(row, col, rs, cs)
        radius = 5 if cs >= 2 else (4 if row < 3 else 5)
        punch_rounded(draw, box, radius)

    rgba.save(OUT, optimize=True)
    print(f"Wrote {OUT} ({OUT.stat().st_size} bytes)")
    print(f"Key wells: {len(CELLS)}, keypad red fill {KEYPAD_RED[:3]}")


if __name__ == "__main__":
    build()
