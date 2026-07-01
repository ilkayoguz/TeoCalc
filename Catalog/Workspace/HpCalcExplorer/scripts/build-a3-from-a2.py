"""Build A3.png: cyan keypad (no black grooves), centered red keys, mirrored yellow footer."""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

SCRIPTS = Path(__file__).resolve().parent
ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
A0 = ASSETS / "A0.png"
A2 = ASSETS / "A2.png"
OUT = ASSETS / "A3.png"

PANEL_CYAN = (0, 188, 212, 255)
KEY_RED = (220, 32, 32, 255)
FOOTER_YELLOW = (255, 214, 0, 255)
PANEL_TOP = 238


def _load_layout():
    import sys

    name = "hp65_key_layout_v2"
    spec = importlib.util.spec_from_file_location(
        name, SCRIPTS / "hp65_key_layout_v2.py"
    )
    mod = importlib.util.module_from_spec(spec)
    sys.modules[name] = mod
    spec.loader.exec_module(mod)
    return mod


def panel_side_bounds(a0: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    alpha = a0[:, :, 3]
    h = alpha.shape[0]
    left = np.full(h, -1, dtype=np.int32)
    right = np.full(h, -1, dtype=np.int32)
    for y in range(PANEL_TOP, h):
        xs = np.where(alpha[y] < 16)[0]
        if xs.size:
            left[y] = int(xs[0])
            right[y] = int(xs[-1])
    return left, right


def keypad_pocket_mask(a0: np.ndarray, y_bottom: int) -> np.ndarray:
    """Faceplate interior across each row (includes former key/black grooves)."""
    left, right = panel_side_bounds(a0)
    alpha = a0[:, :, 3]
    h, w = alpha.shape
    mask = np.zeros((h, w), dtype=bool)

    ref_left = 46
    ref_right = 426
    for y in range(PANEL_TOP, min(y_bottom, h - 1) + 1):
        row_l = ref_left
        row_r = ref_right
        if left[y] >= 0 and right[y] > left[y]:
            row_l = int(left[y])
            row_r = int(right[y])
        mask[y, row_l : row_r + 1] = True

    return mask


def mirror_left_footer_curve(
    bottom: np.ndarray, x0: int, x1: int
) -> np.ndarray:
    """Right wing follows the left wing (left curve is master)."""
    cx = (x0 + x1) // 2
    out = bottom.copy()
    for x in range(x0, cx + 1):
        out[x0 + x1 - x] = out[x]
    return out


def footer_bottom_curve(
    a0: np.ndarray, x0: int, x1: int, footer_top: int, cap_y: int
) -> np.ndarray:
    lum = a0[:, :, :3].mean(axis=2)
    bottom = np.full(x1 + 1, footer_top - 1, dtype=np.int32)
    y1 = min(cap_y, lum.shape[0] - 1)
    for x in range(x0, x1 + 1):
        ys = np.where(lum[footer_top : y1 + 1, x] < 120)[0]
        if ys.size:
            bottom[x] = footer_top + int(ys[-1])
    return bottom


def footer_panel_mask(a0: np.ndarray, footer_top: int, x0: int, x1: int) -> np.ndarray:
    lum = a0[:, :, :3].mean(axis=2)
    cap_y = footer_top
    for y in range(footer_top, a0.shape[0]):
        band = lum[y, x0 : x1 + 1]
        if (band > 150).mean() > 0.3:
            cap_y = y - 1
            break
    else:
        cap_y = min(a0.shape[0] - 1, footer_top + 40)

    bottom = footer_bottom_curve(a0, x0, x1, footer_top, cap_y)
    bottom = mirror_left_footer_curve(bottom, x0, x1)
    footer_bottom = int(np.max(bottom[x0 : x1 + 1]))
    if footer_bottom < footer_top:
        raise ValueError(f"Invalid footer band: y={footer_top}..{footer_bottom}")

    h, w = a0.shape[:2]
    mask = np.zeros((h, w), dtype=bool)
    for y in range(footer_top, footer_bottom + 1):
        for x in range(x0, x1 + 1):
            if y <= bottom[x]:
                mask[y, x] = True

    band = mask[footer_top : footer_bottom + 1].astype(np.uint8) * 255
    kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (21, 5))
    band = cv2.morphologyEx(band, cv2.MORPH_CLOSE, kernel)
    mask[footer_top : footer_bottom + 1] = band > 0

    for y in range(footer_top, footer_bottom + 1):
        for x in range(x0, x1 + 1):
            if y > bottom[x]:
                mask[y, x] = False

    mask &= lum <= 210
    return mask


def paint_rect(out: np.ndarray, x: int, y: int, w: int, h: int, color: tuple[int, int, int, int]) -> None:
    out[y : y + h, x : x + w] = color


def build(a2_path: Path = A2, a0_path: Path = A0, out_path: Path = OUT) -> None:
    layout = _load_layout()
    if not a2_path.exists():
        raise FileNotFoundError(a2_path)
    if not a0_path.exists():
        raise FileNotFoundError(a0_path)

    a2 = np.array(Image.open(a2_path).convert("RGBA"))
    a0 = np.array(Image.open(a0_path).convert("RGBA"))
    out = a2.copy()

    keys = layout.key_rects()
    margins = layout.row_margins(keys, layout.PANEL_LEFT, layout.PANEL_RIGHT)
    # Match A2 cyan panel extent (curved wings below last key row).
    keypad_bottom = 832

    pocket = keypad_pocket_mask(a0, keypad_bottom)
    out[pocket] = PANEL_CYAN

    for key in keys:
        paint_rect(out, key.x, key.y, key.w, key.h, KEY_RED)

    footer_top = keypad_bottom + 1
    footer = footer_panel_mask(a0, footer_top, layout.PANEL_LEFT, layout.PANEL_RIGHT)
    footer &= ~pocket
    out[footer] = FOOTER_YELLOW

    print("Row margins (left, right):")
    for row, (left_m, right_m) in sorted(margins.items()):
        print(f"  row {row}: {left_m}px / {right_m}px")

    n_keys = sum(k.w * k.h for k in keys)
    print(f"Keys: {len(keys)} rects, {n_keys} px")
    print(f"Keypad cyan through y={keypad_bottom}, footer from y={footer_top}")

    out_path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(out, "RGBA").save(out_path, optimize=True)
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "A3.png"
    build(out_path=ASSETS / out_name)
