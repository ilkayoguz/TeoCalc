"""Mask helpers derived from A0.png (hp65 layout template)."""
from __future__ import annotations

import cv2
import numpy as np

PANEL_TOP = 238
PANEL_BOTTOM = 808


def chroma_green(arr: np.ndarray) -> np.ndarray:
    rgb = arr[:, :, :3].astype(np.int16)
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (g > 120) & (g > r + 40) & (g > b + 40)


def key_well_mask(a0: np.ndarray) -> np.ndarray:
    rgb = a0[:, :, :3].astype(np.int16)
    alpha = a0[:, :, 3]
    return (
        (rgb[:, :, 0] > 250)
        & (rgb[:, :, 1] > 250)
        & (rgb[:, :, 2] > 250)
        & (alpha > 200)
    )


def outer_background_mask(a0: np.ndarray) -> np.ndarray:
    rgb = a0[:, :, :3].astype(np.int16)
    alpha = a0[:, :, 3]
    keys = key_well_mask(a0)
    return (
        (np.abs(rgb[:, :, 0] - 240) < 20)
        & (np.abs(rgb[:, :, 1] - 240) < 20)
        & (np.abs(rgb[:, :, 2] - 240) < 20)
        & (alpha > 200)
        & ~keys
    )


def faceplate_panel_mask(a0: np.ndarray) -> np.ndarray:
    """Transparent faceplate interior in A0 (curved panel), keypad band only."""
    alpha = a0[:, :, 3]
    h = alpha.shape[0]
    y0 = max(0, PANEL_TOP)
    y1 = min(h - 1, PANEL_BOTTOM)
    panel = np.zeros(alpha.shape, dtype=bool)
    panel[y0 : y1 + 1, :] = alpha[y0 : y1 + 1, :] < 16
    return panel & ~key_well_mask(a0)


def enter_merge_rect_from_a0(a0: np.ndarray) -> tuple[int, int, int, int] | None:
    white = key_well_mask(a0)
    mask = white.astype(np.uint8) * 255
    n, _labels, stats, _centroids = cv2.connectedComponentsWithStats(mask, connectivity=8)

    blobs: list[tuple[int, int, int, int, int]] = []
    for i in range(1, n):
        x, y, w, h, area = stats[i]
        if area > 500 and w >= 30 and h >= 30:
            blobs.append((y, x, w, h, area))

    if not blobs:
        return None

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
    return int(x0), int(min(y0, _y1)), int((x1 + w1) - x0), int(max(h0, h1))
