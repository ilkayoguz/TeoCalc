"""Build A2.png from A1.png: solid yellow footer brand panel (clean curved mask)."""
from __future__ import annotations

import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
A0 = ASSETS / "A0.png"
A1 = ASSETS / "A1.png"
OUT = ASSETS / "A2.png"

FOOTER_YELLOW = (255, 214, 0, 255)
PANEL_TOP = 238


def cyan_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (b > 180) & (g > 140) & (r < 100)


def red_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (r > 180) & (g < 80) & (b < 80)


def panel_side_bounds(a0: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
    """Left/right x bounds per row from A0 transparent faceplate pocket."""
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


def symmetrize_bottom_curve(
    bottom: np.ndarray, x0: int, x1: int
) -> np.ndarray:
    """Mirror footer depth so left and right curved wings match."""
    cx = (x0 + x1) // 2
    out = bottom.copy()
    for x in range(x0, cx + 1):
        mirror = x0 + x1 - x
        depth = max(int(out[x]), int(out[mirror]))
        out[x] = depth
        out[mirror] = depth
    return out


def footer_bottom_curve(
    a0: np.ndarray, x0: int, x1: int, footer_top: int, cap_y: int
) -> np.ndarray:
    """Per-column last dark-interior row (tolerates logo highlights)."""
    lum = a0[:, :, :3].mean(axis=2)
    bottom = np.full(x1 + 1, footer_top - 1, dtype=np.int32)
    y1 = min(cap_y, lum.shape[0] - 1)
    for x in range(x0, x1 + 1):
        ys = np.where(lum[footer_top : y1 + 1, x] < 120)[0]
        if ys.size:
            bottom[x] = footer_top + int(ys[-1])
    return bottom


def footer_panel_mask(a0: np.ndarray, cyan_bottom: int) -> np.ndarray:
    left, right = panel_side_bounds(a0)
    if cyan_bottom < 0:
        raise ValueError("Could not locate cyan panel bottom in A1")

    ref_y = cyan_bottom
    while ref_y >= PANEL_TOP and left[ref_y] < 0:
        ref_y -= 1
    if left[ref_y] < 0:
        raise ValueError("A0 panel side bounds not found")

    x0 = int(left[ref_y])
    x1 = int(right[ref_y])
    footer_top = cyan_bottom + 1
    lum = a0[:, :, :3].mean(axis=2)

    # Horizontal inner-trim band sits just below the footer pocket.
    cap_y = footer_top
    for y in range(footer_top, a0.shape[0]):
        band = lum[y, x0 : x1 + 1]
        if (band > 150).mean() > 0.3:
            cap_y = y - 1
            break
    else:
        cap_y = min(a0.shape[0] - 1, footer_top + 40)

    bottom = footer_bottom_curve(a0, x0, x1, footer_top, cap_y)
    bottom = symmetrize_bottom_curve(bottom, x0, x1)
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


def build(a1_path: Path = A1, a0_path: Path = A0, out_path: Path = OUT) -> None:
    if not a1_path.exists():
        raise FileNotFoundError(a1_path)
    if not a0_path.exists():
        raise FileNotFoundError(a0_path)

    a1 = np.array(Image.open(a1_path).convert("RGBA"))
    a0 = np.array(Image.open(a0_path).convert("RGBA"))
    rgb = a1[:, :, :3]

    cyan = cyan_mask(rgb)
    ys = np.where(cyan.any(axis=1))[0]
    if ys.size == 0:
        raise ValueError("No cyan panel found in A1")
    cyan_bottom = int(ys[-1])

    footer = footer_panel_mask(a0, cyan_bottom)
    footer &= ~cyan
    footer &= ~red_mask(rgb)

    out = a1.copy()
    out[footer] = FOOTER_YELLOW

    n = int(footer.sum())
    y0 = int(np.where(footer.any(axis=1))[0][0])
    y1 = int(np.where(footer.any(axis=1))[0][-1])
    print(f"Cyan bottom: y={cyan_bottom}")
    print(f"Yellow footer: {n} px (y={y0}..{y1})")

    out_path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(out, "RGBA").save(out_path, optimize=True)
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "A2.png"
    build(out_path=ASSETS / out_name)
