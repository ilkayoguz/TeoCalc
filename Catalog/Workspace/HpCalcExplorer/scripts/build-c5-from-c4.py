"""Build C5.png from C4.png: B01-aligned switch holes + transparent key wells."""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
SCRIPTS = Path(__file__).resolve().parent
C4 = ASSETS / "C4.png"
B01 = ASSETS / "B01.png"
A0 = ASSETS / "A" / "A0.png"
OUT = ASSETS / "C5.png"

WHITE_K1_Y = 133
K1_THICKNESS = 4
SWITCH_TOP_Y = WHITE_K1_Y + K1_THICKNESS  # 137
SWITCH_BOTTOM_Y = 165
CARD_SLOT_TOP_K1_Y = 166

# Previous C4 build placed switch holes here (restore before re-punching).
OLD_SWITCH_HOLES = (
    (127, 148, 183, 153),
    (292, 149, 348, 153),
)

SWITCH_TRACK_W = 58
SWITCH_TRACK_H = 6


def _load_key_layout():
    spec = importlib.util.spec_from_file_location(
        "hp65_key_layout_v2", SCRIPTS / "hp65_key_layout_v2.py"
    )
    mod = importlib.util.module_from_spec(spec)
    sys.modules["hp65_key_layout_v2"] = mod
    spec.loader.exec_module(mod)
    return mod


def _switch_track_x_from_b01(b01: np.ndarray) -> tuple[int, int]:
    """58px-wide darkest track window on B01 for each switch center."""
    gray = b01[:, :, :3].astype(np.float32).mean(axis=2)
    xs: list[int] = []

    for cx in (155, 320):
        best: tuple[float, int] | None = None
        for x in range(cx - SWITCH_TRACK_W // 2 - 20, cx - SWITCH_TRACK_W // 2 + 21):
            patch = gray[167:173, x : x + SWITCH_TRACK_W]
            if patch.shape != (6, SWITCH_TRACK_W):
                continue
            score = float(patch.mean()) + abs((x + SWITCH_TRACK_W * 0.5) - cx) * 0.1
            if best is None or score < best[0]:
                best = (score, x)
        xs.append(best[1] if best else cx - SWITCH_TRACK_W // 2)

    return xs[0], xs[1]


def _switch_holes(
    left_x: int,
    right_x: int,
    switch_top: int,
    switch_bottom: int,
) -> list[tuple[int, int, int, int]]:
    center_y = (switch_top + switch_bottom) / 2.0
    y0 = int(round(center_y - (SWITCH_TRACK_H - 1) / 2.0))
    y1 = y0 + SWITCH_TRACK_H - 1
    return [
        (left_x, y0, left_x + SWITCH_TRACK_W - 1, y1),
        (right_x, y0, right_x + SWITCH_TRACK_W - 1, y1),
    ]


def _switch_fill_color(arr: np.ndarray) -> tuple[int, int, int]:
    band = arr[SWITCH_TOP_Y : SWITCH_BOTTOM_Y + 1, 55:416]
    opaque = band[:, :, 3] > 200
    if opaque.any():
        rgb = band[:, :, :3][opaque]
        med = np.median(rgb, axis=0).astype(int)
        return int(med[0]), int(med[1]), int(med[2])
    return 75, 80, 83


def _fill_rect(
    arr: np.ndarray,
    x0: int,
    y0: int,
    x1: int,
    y1: int,
    rgb: tuple[int, int, int],
) -> None:
    arr[y0 : y1 + 1, x0 : x1 + 1, :3] = rgb
    arr[y0 : y1 + 1, x0 : x1 + 1, 3] = 255


def _punch_rect(arr: np.ndarray, x0: int, y0: int, x1: int, y1: int) -> None:
    arr[y0 : y1 + 1, x0 : x1 + 1, 3] = 0


def build(
    c4_path: Path = C4,
    b01_path: Path = B01,
    a0_path: Path = A0,
    out_path: Path = OUT,
) -> None:
    if not c4_path.exists():
        raise FileNotFoundError(c4_path)
    if not b01_path.exists():
        raise FileNotFoundError(b01_path)
    if not a0_path.exists():
        raise FileNotFoundError(a0_path)

    b01 = np.array(Image.open(b01_path).convert("RGBA"))
    left_x, right_x = _switch_track_x_from_b01(b01)
    switch_holes = _switch_holes(left_x, right_x, SWITCH_TOP_Y, SWITCH_BOTTOM_Y)

    layout = _load_key_layout()
    a0 = np.array(Image.open(a0_path).convert("RGBA"))
    keys = layout.key_rects(a0)

    arr = np.array(Image.open(c4_path).convert("RGBA"))
    switch_rgb = _switch_fill_color(arr)

    for x0, y0, x1, y1 in OLD_SWITCH_HOLES:
        _fill_rect(arr, x0, y0, x1, y1, switch_rgb)

    for x0, y0, x1, y1 in switch_holes:
        _punch_rect(arr, x0, y0, x1, y1)

    for key in keys:
        _punch_rect(arr, key.x, key.y, key.x + key.w - 1, key.y + key.h - 1)

    out = Image.fromarray(arr, "RGBA")
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out.save(out_path, optimize=True)

    print(f"Switch holes (B01 x, centered in y={SWITCH_TOP_Y}-{SWITCH_BOTTOM_Y}):")
    for i, (x0, y0, x1, y1) in enumerate(switch_holes, 1):
        print(f"  {i}: {x0},{y0} {x1 - x0 + 1}x{y1 - y0 + 1}")
    print(f"Key holes: {len(keys)}")
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "C5.png"
    build(out_path=ASSETS / out_name)
