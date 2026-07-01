"""Build D02.png from D01.png: equal white-frame radii + opaque gutter corner fill."""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
D01 = ASSETS / "D01.png"
OUT = ASSETS / "D02.png"

W, H = 409, 861
FACE_X, FACE_W = 23, 362
FACE_X1 = FACE_X + FACE_W - 1
CONTENT_Y0, CONTENT_Y1 = 24, 836

FRAME_W = 3
FRAME_R = 6  # equal outer + inner white frame corner radius
FRAME_SCALE = 4  # supersample frame rings for smooth corner blends

CHASSIS = (97, 100, 90, 255)
WHITE = (255, 255, 255, 255)

OUTER = (0, 0, W - 1, H - 1)
GUTTER_OUTER = (FRAME_W, FRAME_W, W - 1 - FRAME_W, H - 1 - FRAME_W)
INNER_WHITE = (20, 20, 388, 840)
CONTENT = (FACE_X, CONTENT_Y0, FACE_X1, CONTENT_Y1)


def _scale_box(box: tuple[int, int, int, int], scale: int) -> tuple[int, int, int, int]:
    x0, y0, x1, y1 = box
    s = scale
    return x0 * s, y0 * s, x1 * s + (s - 1), y1 * s + (s - 1)


def _render_frame_layer(size: tuple[int, int]) -> Image.Image:
    """Anti-aliased chassis + white rings (supersampled, then downscaled)."""
    sw, sh = size[0] * FRAME_SCALE, size[1] * FRAME_SCALE
    hi_size = (sw, sh)
    r = FRAME_R * FRAME_SCALE
    w = FRAME_W * FRAME_SCALE

    def ring(outer: tuple[int, int, int, int], inner: tuple[int, int, int, int]) -> Image.Image:
        inner_r = max(1, r - w)
        mask = Image.new("L", hi_size, 0)
        draw = ImageDraw.Draw(mask)
        draw.rounded_rectangle(_scale_box(outer, FRAME_SCALE), radius=r, fill=255)
        draw.rounded_rectangle(_scale_box(inner, FRAME_SCALE), radius=inner_r, fill=0)
        return mask

    hi = Image.new("RGBA", hi_size, CHASSIS)
    _paste_color(hi, ring(GUTTER_OUTER, INNER_WHITE), CHASSIS)
    _paste_color(hi, ring(OUTER, GUTTER_OUTER), WHITE)
    _paste_color(hi, ring(INNER_WHITE, CONTENT), WHITE)
    return hi.resize(size, Image.Resampling.LANCZOS)


def _paste_color(base: Image.Image, mask: Image.Image, rgba: tuple[int, int, int, int]) -> None:
    layer = Image.new("RGBA", base.size, rgba)
    base.paste(layer, mask=mask)


def _key_well_mask(arr: np.ndarray) -> np.ndarray:
    """Transparent keypad holes to preserve through frame rebuild."""
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

    # Faceplate interior (inside inner white hole) copied verbatim from D01.
    interior = Image.fromarray(src.copy(), "RGBA")
    canvas = _render_frame_layer(size)

    interior_mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(interior_mask)
    inner_r = max(1, FRAME_R - FRAME_W)
    draw.rounded_rectangle(CONTENT, radius=inner_r, fill=255)

    canvas.paste(interior, mask=interior_mask)

    out_arr = np.array(canvas)
    out_arr[keys] = (0, 0, 0, 0)
    Image.fromarray(out_arr, "RGBA").save(out_path, optimize=True)

    key_px = int(keys.sum())
    partial = int(((out_arr[:, :, 3] > 0) & (out_arr[:, :, 3] < 255)).sum())
    print(f"Frame radius: {FRAME_R}px (equal outer + inner), AA scale {FRAME_SCALE}x")
    print(f"Key well pixels preserved: {key_px}, corner blend pixels: {partial}")
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else OUT
    build(out_path=out)
