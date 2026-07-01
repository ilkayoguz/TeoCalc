"""Curved HP-65 faceplate panel mask from Panamatik trim (001.png)."""
from __future__ import annotations

import numpy as np
from PIL import Image

REF_W, REF_H = 470, 870

# First / last key rows in A0 (hp65 template).
KEY_ROW_TOP = 242
KEY_ROW_BOTTOM = 761
KEY_ROW_HEIGHT = 38

PANEL_TOP = 238
PANEL_BOTTOM = KEY_ROW_BOTTOM + KEY_ROW_HEIGHT + 9


def _smooth(values: np.ndarray, kernel: int = 11) -> np.ndarray:
    ker = np.ones(kernel, dtype=np.float64) / kernel
    return np.convolve(values, ker, mode="same")


def panel_edges_from_reference(ref_rgb: np.ndarray) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    """Per-scanline inner faceplate edges (curved barrel), y -> x."""
    lum = ref_rgb.mean(axis=2)
    y0, y1 = PANEL_TOP, PANEL_BOTTOM
    ys = np.arange(y0, y1 + 1)
    lefts: list[float] = []
    rights: list[float] = []

    for y in ys:
        row = lum[y, 25:445]
        dark = row < 88
        cols = np.where(dark)[0]
        if len(cols) < 120:
            lefts.append(lefts[-1] if lefts else 38.0)
            rights.append(rights[-1] if rights else 432.0)
            continue
        lefts.append(float(cols[0] + 25 + 2))
        rights.append(float(cols[-1] + 25 - 2))

    left = _smooth(np.array(lefts, dtype=np.float64))
    right = _smooth(np.array(rights, dtype=np.float64))

    # Stabilise curved caps where the dark mask meets display/footer bands.
    anchor = min(4, len(left) - 1)
    left[:anchor] = left[anchor]
    right[:anchor] = right[anchor]
    left[-anchor:] = left[-anchor - 1]
    right[-anchor:] = right[-anchor - 1]

    return ys, left.astype(int), right.astype(int)


def panel_mask_from_reference(ref_rgb: np.ndarray) -> np.ndarray:
    mask = np.zeros((REF_H, REF_W), dtype=bool)
    ys, left, right = panel_edges_from_reference(ref_rgb)
    for i, y in enumerate(ys):
        x0 = max(0, int(left[i]))
        x1 = min(REF_W - 1, int(right[i]))
        if x1 > x0:
            mask[y, x0 : x1 + 1] = True
    return mask


def load_reference_rgb(path) -> np.ndarray:
    img = np.array(Image.open(path).convert("RGB"))
    if img.shape[0] != REF_H or img.shape[1] != REF_W:
        raise ValueError(f"Reference must be {REF_W}x{REF_H}")
    return img
