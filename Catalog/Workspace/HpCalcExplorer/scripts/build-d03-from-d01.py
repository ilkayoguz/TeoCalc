"""Build D03.png from D01.png: sharp 90° frames, no corner radius."""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
D01 = ASSETS / "D01.png"
OUT = ASSETS / "D03.png"

W, H = 409, 861
FACE_X, FACE_W = 23, 362
FACE_X1 = FACE_X + FACE_W - 1
CONTENT_Y0, CONTENT_Y1 = 24, 836

FRAME_W = 3

CHASSIS = (97, 100, 90, 255)
WHITE = (255, 255, 255, 255)

OUTER = (0, 0, W - 1, H - 1)
GUTTER_OUTER = (FRAME_W, FRAME_W, W - 1 - FRAME_W, H - 1 - FRAME_W)
INNER_WHITE = (20, 20, 388, 840)
CONTENT = (FACE_X, CONTENT_Y0, FACE_X1, CONTENT_Y1)


def _heal_rounded_inner_corner_bleed(arr: np.ndarray) -> None:
    """Drop D01 rounded-frame white that leaked inside the sharp content window."""
    rgb = arr[:, :, :3]
    white = np.all(rgb == 255, axis=2)
    xs = np.arange(W)[None, :]
    ys = np.arange(H)[:, None]
    inside = (
        (xs >= FACE_X)
        & (xs <= FACE_X1)
        & (ys >= CONTENT_Y0)
        & (ys <= CONTENT_Y1)
    )
    stray = inside & white
    ys_idx, xs_idx = np.where(stray)
    for y, x in zip(ys_idx, xs_idx, strict=False):
        if x + 3 <= FACE_X1:
            arr[y, x, :3] = arr[y, x + 3, :3]
        elif x - 3 >= FACE_X:
            arr[y, x, :3] = arr[y, x - 3, :3]
        elif y + 3 <= CONTENT_Y1:
            arr[y, x, :3] = arr[y + 3, x, :3]
        else:
            arr[y, x, :3] = arr[y - 3, x, :3]


def _ring_mask(
    size: tuple[int, int],
    outer: tuple[int, int, int, int],
    inner: tuple[int, int, int, int],
) -> Image.Image:
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)
    draw.rectangle(outer, fill=255)
    draw.rectangle(inner, fill=0)
    return mask


def _paste_color(base: Image.Image, mask: Image.Image, rgba: tuple[int, int, int, int]) -> None:
    layer = Image.new("RGBA", base.size, rgba)
    base.paste(layer, mask=mask)


def _key_well_mask(arr: np.ndarray) -> np.ndarray:
    alpha = arr[:, :, 3] == 0
    ys = np.arange(arr.shape[0])[:, None]
    xs = np.arange(arr.shape[1])[None, :]
    in_keypad = (ys >= 222) & (xs >= FACE_X) & (xs <= FACE_X1)
    return alpha & in_keypad


def build(d01_path: Path = D01, out_path: Path = OUT) -> None:
    if not d01_path.exists():
        raise FileNotFoundError(d01_path)

    src = np.array(Image.open(d01_path).convert("RGBA"))
    keys = _key_well_mask(src)
    size = (W, H)

    interior = Image.fromarray(src.copy(), "RGBA")
    canvas = Image.new("RGBA", size, CHASSIS)

    _paste_color(canvas, _ring_mask(size, GUTTER_OUTER, INNER_WHITE), CHASSIS)
    _paste_color(canvas, _ring_mask(size, OUTER, GUTTER_OUTER), WHITE)
    _paste_color(canvas, _ring_mask(size, INNER_WHITE, CONTENT), WHITE)

    interior_mask = Image.new("L", size, 0)
    ImageDraw.Draw(interior_mask).rectangle(CONTENT, fill=255)
    canvas.paste(interior, mask=interior_mask)

    out_arr = np.array(canvas)
    _heal_rounded_inner_corner_bleed(out_arr)
    out_img = Image.fromarray(out_arr, "RGBA")
    _paste_color(out_img, _ring_mask(size, INNER_WHITE, CONTENT), WHITE)
    out_arr = np.array(out_img)
    out_arr[keys] = (0, 0, 0, 0)
    Image.fromarray(out_arr, "RGBA").save(out_path, optimize=True)

    partial = int(((out_arr[:, :, 3] > 0) & (out_arr[:, :, 3] < 255)).sum())
    print(f"Sharp frames (radius=0), key well pixels: {int(keys.sum())}, blend pixels: {partial}")
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else OUT
    build(out_path=out)
