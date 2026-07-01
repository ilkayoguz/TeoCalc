"""HP-65 key slot geometry from A0.png (hp65.png template).

Four physical sizes (wxh):
  - 47x39  standard / top-grid
  - 118x39 enter-wide (row 4, cols 1-2 merged)
  - 35x38  narrow (operator column, rows 5-8)
  - 54x38  wide (rows 5-8)
"""
from __future__ import annotations

from dataclasses import dataclass
from enum import Enum

import cv2
import numpy as np
from PIL import Image

REF_W, REF_H = 470, 870


class KeySlotKind(Enum):
    StandardTop = "48x38"
    EnterWide = "118x38"
    Narrow = "36x38"
    Wide = "54x38"


@dataclass(frozen=True)
class KeySlot:
    x: int
    y: int
    w: int
    h: int
    kind: KeySlotKind
    row: int
    col: int


def _white_mask(a0: np.ndarray) -> np.ndarray:
    rgb = a0[:, :, :3].astype(np.int16)
    alpha = a0[:, :, 3]
    return (
        (rgb[:, :, 0] > 250)
        & (rgb[:, :, 1] > 250)
        & (rgb[:, :, 2] > 250)
        & (alpha > 200)
    )


def _blob_rows(
    a0: np.ndarray,
) -> list[list[tuple[int, int, int, int]]]:
    white = _white_mask(a0)
    mask = white.astype(np.uint8) * 255
    n, _labels, stats, _centroids = cv2.connectedComponentsWithStats(mask, connectivity=8)

    blobs: list[tuple[int, int, int, int]] = []
    for i in range(1, n):
        x, y, w, h, area = stats[i]
        if area > 500 and w >= 30 and h >= 30:
            blobs.append((x, y, w, h))

    rows: dict[int, list[tuple[int, int, int, int]]] = {}
    for x, y, w, h in blobs:
        key = round(y / 8) * 8
        rows.setdefault(key, []).append((x, y, w, h))

    return [sorted(rows[k], key=lambda b: b[0]) for k in sorted(rows.keys())]


def key_slots_from_a0(a0: np.ndarray) -> list[KeySlot]:
    """35 slots with ENTER merge on row 4 (1-based)."""
    row_groups = _blob_rows(a0)
    if len(row_groups) < 8:
        raise ValueError(f"Expected 8 key rows in A0, found {len(row_groups)}")

    slots: list[KeySlot] = []

    for row_idx, row in enumerate(row_groups[:4]):
        if row_idx == 3:
            # Row 4: merge left two wells into ENTER.
            x0, y0, w0, h0 = row[0]
            x1, _y1, w1, h1 = row[1]
            slots.append(
                KeySlot(
                    x=x0,
                    y=min(y0, _y1),
                    w=(x1 + w1) - x0,
                    h=max(h0, h1),
                    kind=KeySlotKind.EnterWide,
                    row=row_idx,
                    col=0,
                )
            )
            for col_idx, (x, y, w, h) in enumerate(row[2:], start=2):
                slots.append(
                    KeySlot(x, y, w, h, KeySlotKind.StandardTop, row_idx, col_idx)
                )
            continue

        for col_idx, (x, y, w, h) in enumerate(row):
            kind = KeySlotKind.StandardTop
            slots.append(KeySlot(x, y, w, h, kind, row_idx, col_idx))

    for row_idx, row in enumerate(row_groups[4:], start=4):
        for col_idx, (x, y, w, h) in enumerate(row):
            if w <= 40:
                kind = KeySlotKind.Narrow
            else:
                kind = KeySlotKind.Wide
            slots.append(KeySlot(x, y, w, h, kind, row_idx, col_idx))

    return slots


def load_a0(path) -> np.ndarray:
    img = np.array(Image.open(path).convert("RGBA"))
    if img.shape[0] != REF_H or img.shape[1] != REF_W:
        raise ValueError(f"A0 must be {REF_W}x{REF_H}, got {img.shape[1]}x{img.shape[0]}")
    return img
