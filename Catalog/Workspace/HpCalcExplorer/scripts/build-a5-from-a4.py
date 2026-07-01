"""Build A5.png from A4.png: repaint inner bezel (whitish trim outside keypad cyan)."""
from __future__ import annotations

import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
A4 = ASSETS / "A4.png"
OUT = ASSETS / "A5.png"

# Clean light platinum trim (flat, no grain).
TRIM_COLOR = (236, 232, 222, 255)

# Pixels just outside keypad cyan / yellow pocket (the important bezel line).
TRIM_DIST_MIN = 0.5
TRIM_DIST_MAX = 4.5
LUM_MIN = 132
LUM_MAX = 248
SAT_MAX = 52


def content_masks(rgb: np.ndarray) -> dict[str, np.ndarray]:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return {
        "keypad_cyan": (b > 180) & (g > 140) & (r < 80),
        "yellow": (r > 200) & (g > 170) & (b < 100),
        "red": (r > 180) & (g < 80) & (b < 80),
        "blue": (b > 200) & (g > 120) & (r < 80),
        "green": (g > 200) & (r < 120) & (b < 120),
    }


def trim_mask(rgba: np.ndarray) -> np.ndarray:
    rgb = rgba[:, :, :3].astype(np.int16)
    alpha = rgba[:, :, 3]
    masks = content_masks(rgb)

    pocket = masks["keypad_cyan"] | masks["yellow"]
    content = pocket | masks["red"] | masks["blue"] | masks["green"]

    pocket_u8 = np.where(pocket, 0, 255).astype(np.uint8)
    dist = cv2.distanceTransform(pocket_u8, cv2.DIST_L2, 3)

    lum = rgb.mean(axis=2)
    sat = np.maximum(np.maximum(rgb[:, :, 0], rgb[:, :, 1]), rgb[:, :, 2]) - np.minimum(
        np.minimum(rgb[:, :, 0], rgb[:, :, 1]), rgb[:, :, 2]
    )

    zone = (
        ~pocket
        & (dist >= TRIM_DIST_MIN)
        & (dist <= TRIM_DIST_MAX)
        & (lum >= LUM_MIN)
        & (lum <= LUM_MAX)
        & (sat <= SAT_MAX)
        & ~content
        & (alpha > 128)
    )

    # Close single-pixel gaps for a continuous bezel stroke.
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (3, 3))
    closed = cv2.morphologyEx(zone.astype(np.uint8) * 255, cv2.MORPH_CLOSE, kernel)
    return closed > 0


def build(a4_path: Path = A4, out_path: Path = OUT) -> None:
    if not a4_path.exists():
        raise FileNotFoundError(a4_path)

    a4 = np.array(Image.open(a4_path).convert("RGBA"))
    mask = trim_mask(a4)

    out = a4.copy()
    out[mask] = TRIM_COLOR

    n = int(mask.sum())
    print(f"Trim repaint: {n} px -> RGB{TRIM_COLOR[:3]}")

    out_path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(out, "RGBA").save(out_path, optimize=True)
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "A5.png"
    build(out_path=ASSETS / out_name)
