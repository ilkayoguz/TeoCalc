"""Build A6.png: minimal faceplate from A5 geometry (keys, display, body only)."""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

SCRIPTS = Path(__file__).resolve().parent
ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
A0 = ASSETS / "A0.png"
A5 = ASSETS / "A5.png"
OUT = ASSETS / "A6.png"

REF_W, REF_H = 470, 870

# Measured once from A5.png (display blue window + opaque silhouette).
DISPLAY_X = 55
DISPLAY_Y = 44
DISPLAY_W = 362
DISPLAY_H = 94
CHASSIS_X = 5
CHASSIS_Y = 4
CHASSIS_W = 462
CHASSIS_H = 863
CHASSIS_RADIUS = 28

BODY_COLOR = (97, 100, 90, 255)
DISPLAY_COLOR = (0, 0, 0, 255)
KEY_RED = (220, 32, 32, 255)

KEY_RADIUS = {
    "48x38": 4,
    "118x38": 5,
    "36x38": 4,
    "54x38": 5,
}


def _load_layout():
    name = "hp65_key_layout_v2"
    spec = importlib.util.spec_from_file_location(name, SCRIPTS / "hp65_key_layout_v2.py")
    mod = importlib.util.module_from_spec(spec)
    sys.modules[name] = mod
    spec.loader.exec_module(mod)
    return mod


def _measure_from_a5(a5_path: Path) -> None:
    """Optional: refresh DISPLAY_* / CHASSIS_* / BODY_COLOR from A5 before deleting it."""
    global DISPLAY_X, DISPLAY_Y, DISPLAY_W, DISPLAY_H
    global CHASSIS_X, CHASSIS_Y, CHASSIS_W, CHASSIS_H, BODY_COLOR

    if not a5_path.exists():
        return

    a5 = np.array(Image.open(a5_path).convert("RGBA"))
    rgb = a5[:, :, :3].astype(np.int16)
    alpha = a5[:, :, 3]

    blue = (
        (rgb[:, :, 2] > 200)
        & (rgb[:, :, 1] < 170)
        & (rgb[:, :, 0] < 100)
        & (alpha > 128)
    )
    ys, xs = np.where(blue)
    if ys.size:
        DISPLAY_X = int(xs.min())
        DISPLAY_Y = int(ys.min())
        DISPLAY_W = int(xs.max() - xs.min() + 1)
        DISPLAY_H = int(ys.max() - ys.min() + 1)

    opaque = alpha > 128
    ys, xs = np.where(opaque)
    if ys.size:
        CHASSIS_X = int(xs.min())
        CHASSIS_Y = int(ys.min())
        CHASSIS_W = int(xs.max() - xs.min() + 1)
        CHASSIS_H = int(ys.max() - ys.min() + 1)


def build(out_path: Path = OUT, a5_path: Path = A5) -> None:
    if not A0.exists():
        raise FileNotFoundError(A0)

    _measure_from_a5(a5_path)

    layout = _load_layout()
    a0 = np.array(Image.open(A0).convert("RGBA"))
    keys = layout.key_rects(a0)

    img = Image.new("RGBA", (REF_W, REF_H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    chassis = (
        CHASSIS_X,
        CHASSIS_Y,
        CHASSIS_X + CHASSIS_W - 1,
        CHASSIS_Y + CHASSIS_H - 1,
    )
    draw.rounded_rectangle(chassis, radius=CHASSIS_RADIUS, fill=BODY_COLOR)

    display = (
        DISPLAY_X,
        DISPLAY_Y,
        DISPLAY_X + DISPLAY_W - 1,
        DISPLAY_Y + DISPLAY_H - 1,
    )
    draw.rectangle(display, fill=DISPLAY_COLOR)

    for key in keys:
        radius = KEY_RADIUS.get(key.kind.value, 4)
        box = (key.x, key.y, key.x + key.w - 1, key.y + key.h - 1)
        draw.rounded_rectangle(box, radius=radius, fill=KEY_RED)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    img.save(out_path, optimize=True)

    print(f"Chassis {CHASSIS_X},{CHASSIS_Y} {CHASSIS_W}x{CHASSIS_H} r={CHASSIS_RADIUS}")
    print(f"Display {DISPLAY_X},{DISPLAY_Y} {DISPLAY_W}x{DISPLAY_H}")
    print(f"Body RGB{BODY_COLOR[:3]}")
    print(f"Keys {len(keys)}")
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "A6.png"
    build(out_path=ASSETS / out_name)
