"""Build C4.png from C2.png (C3 layout + card-slot fixes).

Card-slot: flat fill (no faint mid-band line from B01), bottom K1 flush with region end.
"""
from __future__ import annotations

import sys
from collections import Counter
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
C2 = ASSETS / "C2.png"
B01 = ASSETS / "B01.png"
OUT = ASSETS / "C4.png"

FACE_X = 55
FACE_W = 362

K1_THICKNESS = 4
WHITE_K1_Y = 133
SWITCH_TOP_Y = WHITE_K1_Y + K1_THICKNESS  # 137

SWITCH_CENTERS_X = (155, 320)

CARD_SLOT_TOP_K1_Y = 166
CARD_SLOT_TOP_Y = CARD_SLOT_TOP_K1_Y + K1_THICKNESS  # 170
CARD_SLOT_REGION_BOTTOM = 210
CARD_SLOT_BOTTOM_K1_Y = CARD_SLOT_REGION_BOTTOM - K1_THICKNESS + 1  # 207

GOLD_LABEL_TOP_Y = CARD_SLOT_REGION_BOTTOM + 1  # 211
GOLD_LABEL_BOTTOM_Y = 233
BOTTOM_K1_Y = 237


def _band_color(b01: np.ndarray, y0: int, y1: int) -> tuple[int, int, int]:
    patch = b01[y0 : y1 + 1, FACE_X : FACE_X + FACE_W, :3].astype(np.float32)
    lum = patch.mean(axis=2)
    mask = lum > 45
    pixels = patch[mask] if mask.any() else patch.reshape(-1, 3)
    rgb = np.median(pixels, axis=0).astype(int)
    return int(rgb[0]), int(rgb[1]), int(rgb[2])


def _card_slot_color(b01: np.ndarray) -> tuple[int, int, int]:
    """Flat card-slot tone; sample upper/lower bands only (skip B01 mid emboss)."""
    y0, y1 = CARD_SLOT_TOP_Y, CARD_SLOT_REGION_BOTTOM
    h = y1 - y0 + 1
    quarter = max(4, h // 4)
    top = b01[y0 : y0 + quarter, FACE_X : FACE_X + FACE_W, :3]
    bottom = b01[y1 - quarter + 1 : y1 + 1, FACE_X : FACE_X + FACE_W, :3]
    pixels = np.concatenate([top.reshape(-1, 3), bottom.reshape(-1, 3)], axis=0)
    lum = pixels.mean(axis=1)
    mask = lum > 45
    sample = pixels[mask] if mask.any() else pixels
    rgb = np.median(sample, axis=0).astype(int)
    return int(rgb[0]), int(rgb[1]), int(rgb[2])


def _white_k1_color(b01: np.ndarray) -> tuple[int, int, int, int]:
    rows = b01[WHITE_K1_Y : WHITE_K1_Y + K1_THICKNESS, FACE_X : FACE_X + FACE_W, :3]
    rgb = np.median(rows.reshape(-1, 3), axis=0).astype(int)
    return int(rgb[0]), int(rgb[1]), int(rgb[2]), 255


def _dark_k1_color(b01: np.ndarray) -> tuple[int, int, int, int]:
    rows = b01[CARD_SLOT_TOP_K1_Y : CARD_SLOT_TOP_K1_Y + K1_THICKNESS, FACE_X : FACE_X + FACE_W, :3]
    rgb = np.median(rows.reshape(-1, 3), axis=0).astype(int)
    return int(rgb[0]), int(rgb[1]), int(rgb[2]), 255


def _switch_hole_size_from_b01(b01: np.ndarray) -> list[tuple[int, int, int]]:
    gray = b01[:, :, :3].astype(np.float32).mean(axis=2)
    specs: list[tuple[int, int, int]] = []

    for cx in SWITCH_CENTERS_X:
        xs: list[int] = []
        ys: list[int] = []
        for y in range(CARD_SLOT_TOP_K1_Y - 2, CARD_SLOT_TOP_K1_Y + 8):
            for x in range(cx - 28, cx + 29):
                if gray[y, x] < 32:
                    xs.append(x)
                    ys.append(y)

        if not xs:
            specs.append((cx - 28, 57, 6))
            continue

        row_counts = Counter(ys)
        track_rows = [y for y, count in row_counts.items() if count >= 15]
        if not track_rows:
            track_rows = [min(ys), max(ys)]

        track_xs = [x for x, y in zip(xs, ys) if y in track_rows]
        specs.append(
            (
                min(track_xs),
                max(track_xs) - min(track_xs) + 1,
                max(track_rows) - min(track_rows) + 1,
            )
        )

    return specs


def _switch_holes_in_region(
    specs: list[tuple[int, int, int]],
    switch_top: int,
    switch_bottom: int,
) -> list[tuple[int, int, int, int]]:
    center_y = (switch_top + switch_bottom) / 2.0
    holes: list[tuple[int, int, int, int]] = []

    for x0, width, height in specs:
        y0 = int(round(center_y - (height - 1) / 2.0))
        y1 = y0 + height - 1
        holes.append((x0, y0, x0 + width - 1, y1))

    return holes


def build(c2_path: Path = C2, b01_path: Path = B01, out_path: Path = OUT) -> None:
    if not c2_path.exists():
        raise FileNotFoundError(c2_path)
    if not b01_path.exists():
        raise FileNotFoundError(b01_path)

    b01 = np.array(Image.open(b01_path).convert("RGBA"))
    switch_bottom_y = CARD_SLOT_TOP_K1_Y - 1
    hole_specs = _switch_hole_size_from_b01(b01)
    holes = _switch_holes_in_region(hole_specs, SWITCH_TOP_Y, switch_bottom_y)

    panel_color = _band_color(b01, SWITCH_TOP_Y, switch_bottom_y)
    card_slot_color = _card_slot_color(b01)
    white_k1 = _white_k1_color(b01)
    dark_k1 = _dark_k1_color(b01)

    img = Image.open(c2_path).convert("RGBA")
    draw = ImageDraw.Draw(img)

    draw.rectangle(
        (FACE_X, SWITCH_TOP_Y, FACE_X + FACE_W - 1, switch_bottom_y),
        fill=(*panel_color, 255),
    )
    draw.rectangle(
        (FACE_X, CARD_SLOT_TOP_Y, FACE_X + FACE_W - 1, CARD_SLOT_REGION_BOTTOM),
        fill=(*card_slot_color, 255),
    )
    draw.rectangle(
        (FACE_X, GOLD_LABEL_TOP_Y, FACE_X + FACE_W - 1, GOLD_LABEL_BOTTOM_Y),
        fill=(*panel_color, 255),
    )

    draw.rectangle(
        (FACE_X, WHITE_K1_Y, FACE_X + FACE_W - 1, WHITE_K1_Y + K1_THICKNESS - 1),
        fill=white_k1,
    )
    for rule_y in (CARD_SLOT_TOP_K1_Y, CARD_SLOT_BOTTOM_K1_Y, BOTTOM_K1_Y):
        draw.rectangle(
            (FACE_X, rule_y, FACE_X + FACE_W - 1, rule_y + K1_THICKNESS - 1),
            fill=dark_k1,
        )

    arr = np.array(img)
    for x0, y0, x1, y1 in holes:
        arr[y0 : y1 + 1, x0 : x1 + 1, 3] = 0

    out = Image.fromarray(arr, "RGBA")
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out.save(out_path, optimize=True)

    print(f"K1 thickness={K1_THICKNESS}px  dark RGB{dark_k1[:3]}")
    print(f"  white K1: y={WHITE_K1_Y}-{WHITE_K1_Y + K1_THICKNESS - 1}")
    print(f"  dark K1 (card-slot top): y={CARD_SLOT_TOP_K1_Y}-{CARD_SLOT_TOP_K1_Y + K1_THICKNESS - 1}")
    print(f"  dark K1 (card-slot bottom): y={CARD_SLOT_BOTTOM_K1_Y}-{CARD_SLOT_REGION_BOTTOM}")
    print(f"  dark K1 (above keys): y={BOTTOM_K1_Y}-{BOTTOM_K1_Y + K1_THICKNESS - 1}")
    print(f"  switch: y={SWITCH_TOP_Y}-{switch_bottom_y}")
    print(f"  card-slot fill: y={CARD_SLOT_TOP_Y}-{CARD_SLOT_REGION_BOTTOM}")
    print(f"  gold-label: y={GOLD_LABEL_TOP_Y}-{GOLD_LABEL_BOTTOM_Y}")
    print(f"Panel RGB{panel_color}  card-slot RGB{card_slot_color}")
    for i, (x0, y0, x1, y1) in enumerate(holes, 1):
        print(f"  switch hole {i}: {x0},{y0} {x1 - x0 + 1}x{y1 - y0 + 1}")
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "C4.png"
    build(out_path=ASSETS / out_name)
