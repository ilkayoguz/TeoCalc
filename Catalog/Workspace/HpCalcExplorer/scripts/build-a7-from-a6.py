"""Build C2.png from C1.png: display lower band (A5 green zone) in B01 card-slot color."""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
A5 = ASSETS / "A" / "A5.png"
C1 = ASSETS / "C1.png"
B01 = ASSETS / "B01.png"
OUT = ASSETS / "C2.png"

# A5 overlay: green strip directly under the blue display window.
STRIP_X = 55
STRIP_Y = 109
STRIP_W = 362
STRIP_H = 23


def _strip_rect_from_a5(a5_path: Path) -> tuple[int, int, int, int]:
    if not a5_path.exists():
        return STRIP_X, STRIP_Y, STRIP_W, STRIP_H

    a5 = np.array(Image.open(a5_path).convert("RGBA"))
    rgb = a5[:, :, :3].astype(np.int16)
    alpha = a5[:, :, 3]
    green = (
        (rgb[:, :, 1] > 200)
        & (rgb[:, :, 0] < 120)
        & (rgb[:, :, 2] < 120)
        & (alpha > 128)
    )
    ys, xs = np.where(green)
    if ys.size == 0:
        return STRIP_X, STRIP_Y, STRIP_W, STRIP_H
    return (
        int(xs.min()),
        int(ys.min()),
        int(xs.max() - xs.min() + 1),
        int(ys.max() - ys.min() + 1),
    )


def _strip_color_from_b01(b01_path: Path, rect: tuple[int, int, int, int]) -> tuple[int, int, int, int]:
    if not b01_path.exists():
        return (68, 50, 56, 255)

    x, y, w, h = rect
    b01 = np.array(Image.open(b01_path).convert("RGBA"))
    patch = b01[y : y + h, x : x + w, :3].astype(np.float32)
    rgb = np.median(patch.reshape(-1, 3), axis=0).astype(int)
    return int(rgb[0]), int(rgb[1]), int(rgb[2]), 255


def build(
    c1_path: Path = C1,
    a5_path: Path = A5,
    b01_path: Path = B01,
    out_path: Path = OUT,
) -> None:
    if not c1_path.exists():
        raise FileNotFoundError(c1_path)

    strip = _strip_rect_from_a5(a5_path)
    color = _strip_color_from_b01(b01_path, strip)

    img = Image.open(c1_path).convert("RGBA")
    draw = ImageDraw.Draw(img)
    x, y, w, h = strip
    draw.rectangle((x, y, x + w - 1, y + h - 1), fill=color)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    img.save(out_path, optimize=True)

    print(f"Strip {x},{y} {w}x{h} RGB{color[:3]} (from B01)")
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "C2.png"
    build(out_path=ASSETS / out_name)
