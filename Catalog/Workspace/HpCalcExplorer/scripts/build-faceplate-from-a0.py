"""Apply A0.png key wells + outer background mask onto 001.png (Panamatik).

A0 (hp65.png template) uses alpha oddly: key wells are white/opaque; the dark
faceplate is transparent in the PNG (preview shows black via viewer). We punch:
  - pure white pixels in A0 (key wells)
  - transparent pixels connected to the image border (outer background)
  - green chroma corners in 001

We keep 001 pixels for the interior faceplate (gold labels, dark keypad fill).

Output: Assets/002.png
"""
from __future__ import annotations

import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
MASK = ASSETS / "A0.png"
SRC = ASSETS / "001.png"
OUT = ASSETS / "002.png"


def chroma_green(arr: np.ndarray) -> np.ndarray:
    rgb = arr[:, :, :3].astype(np.int16)
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (g > 120) & (g > r + 40) & (g > b + 40)


def mask_from_a0(a0: np.ndarray) -> np.ndarray:
    rgb = a0[:, :, :3].astype(np.int16)
    alpha = a0[:, :, 3]
    key_wells = (
        (rgb[:, :, 0] > 250)
        & (rgb[:, :, 1] > 250)
        & (rgb[:, :, 2] > 250)
        & (alpha > 200)
    )
    # Light gray canvas (~240); exclude pure-white key wells (255 falls in ±20).
    outer_bg = (
        (np.abs(rgb[:, :, 0] - 240) < 20)
        & (np.abs(rgb[:, :, 1] - 240) < 20)
        & (np.abs(rgb[:, :, 2] - 240) < 20)
        & (alpha > 200)
        & ~key_wells
    )
    return key_wells | outer_bg


def enter_merge_rect_from_a0(a0: np.ndarray) -> tuple[int, int, int, int] | None:
    """Row 4 (1-based) left two key wells in A0 -> single ENTER bbox."""
    rgb = a0[:, :, :3].astype(np.int16)
    alpha = a0[:, :, 3]
    white = (
        (rgb[:, :, 0] > 250)
        & (rgb[:, :, 1] > 250)
        & (rgb[:, :, 2] > 250)
        & (alpha > 200)
    )
    mask = white.astype(np.uint8) * 255
    n, _labels, stats, _centroids = cv2.connectedComponentsWithStats(mask, connectivity=8)

    blobs: list[tuple[int, int, int, int, int]] = []
    for i in range(1, n):
        x, y, w, h, area = stats[i]
        if area > 500 and w >= 40 and h >= 30:
            blobs.append((y, x, w, h, area))

    if not blobs:
        return None

    # Cluster by row (standard 5-column wells share similar height).
    rows: dict[int, list[tuple[int, int, int, int]]] = {}
    for y, x, w, h, _area in blobs:
        key = round(y / 8) * 8
        rows.setdefault(key, []).append((x, y, w, h))

    row_keys = sorted(rows.keys())
    if len(row_keys) < 4:
        return None

    fourth = sorted(rows[row_keys[3]], key=lambda b: b[0])
    if len(fourth) < 2:
        return None

    x0, y0, w0, h0 = fourth[0]
    x1, _y1, w1, h1 = fourth[1]
    x = x0
    y = min(y0, _y1)
    w = (x1 + w1) - x0
    h = max(h0, h1)
    return int(x), int(y), int(w), int(h)


def punch_rounded_rgba(
    rgba: Image.Image, box: tuple[int, int, int, int], radius: float = 5
) -> None:
    draw = ImageDraw.Draw(rgba)
    x, y, w, h = box
    draw.rounded_rectangle((x, y, x + w, y + h), radius=radius, fill=(0, 0, 0, 0))


def build(
    mask_path: Path = MASK,
    src_path: Path = SRC,
    out_path: Path = OUT,
) -> None:
    if not mask_path.exists():
        raise FileNotFoundError(mask_path)
    if not src_path.exists():
        raise FileNotFoundError(src_path)

    a0 = np.array(Image.open(mask_path).convert("RGBA"))
    src = np.array(Image.open(src_path).convert("RGBA"))

    if a0.shape != src.shape:
        raise ValueError(f"Size mismatch: A0 {a0.shape[:2]} vs src {src.shape[:2]}")

    hole = mask_from_a0(a0)
    out = src.copy()
    out[hole, 3] = 0
    out[chroma_green(out), 3] = 0

    rgba = Image.fromarray(out, "RGBA")
    enter = enter_merge_rect_from_a0(a0)
    if enter:
        punch_rounded_rgba(rgba, enter, radius=5)
        print(f"ENTER merged slot: x={enter[0]} y={enter[1]} w={enter[2]} h={enter[3]}")
    else:
        print("ENTER merge: could not detect row-4 wells in A0")

    out = np.array(rgba)

    rgb = a0[:, :, :3].astype(np.int16)
    alpha = a0[:, :, 3]
    key_wells = int(
        (
            (rgb[:, :, 0] > 250)
            & (rgb[:, :, 1] > 250)
            & (rgb[:, :, 2] > 250)
            & (alpha > 200)
        ).sum()
    )
    outer_bg = int(
        (
            (np.abs(rgb[:, :, 0] - 240) < 20)
            & (np.abs(rgb[:, :, 1] - 240) < 20)
            & (np.abs(rgb[:, :, 2] - 240) < 20)
            & (alpha > 200)
            & ~(
                (rgb[:, :, 0] > 250)
                & (rgb[:, :, 1] > 250)
                & (rgb[:, :, 2] > 250)
            )
        ).sum()
    )
    print(f"Key wells punched: {key_wells} px")
    print(f"Outer background punched: {outer_bg} px")
    print(f"Total punched: {int(hole.sum())} px")

    out_path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(out, "RGBA").save(out_path, optimize=True)
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "002.png"
    build(out_path=ASSETS / out_name)
