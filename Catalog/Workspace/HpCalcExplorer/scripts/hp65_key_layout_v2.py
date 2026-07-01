"""HP-65 key layout: original row Y + corner anchors, equal inter-key gaps (A4)."""
from __future__ import annotations

from dataclasses import dataclass
from enum import Enum
from pathlib import Path

import cv2
import numpy as np
from PIL import Image

PANEL_LEFT = 46
PANEL_RIGHT = 426
KEY_H = 38

STANDARD_W = 48
ENTER_W = 118
NARROW_W = 36
WIDE_W = 54

A0_PATH = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets\A0.png")


class KeyKind(Enum):
    StandardTop = "48x38"
    EnterWide = "118x38"
    Narrow = "36x38"
    Wide = "54x38"


@dataclass(frozen=True)
class KeyRect:
    x: int
    y: int
    w: int
    h: int
    kind: KeyKind
    row: int
    col: int


@dataclass(frozen=True)
class RowAnchor:
    y: int
    first_left: int
    last_right: int


def _white_blobs(a0: np.ndarray) -> list[tuple[int, int, int, int]]:
    rgb = a0[:, :, :3].astype(np.int16)
    alpha = a0[:, :, 3]
    mask = (
        (rgb[:, :, 0] > 250)
        & (rgb[:, :, 1] > 250)
        & (rgb[:, :, 2] > 250)
        & (alpha > 200)
    ).astype(np.uint8) * 255
    n, _labels, stats, _centroids = cv2.connectedComponentsWithStats(mask, connectivity=8)
    blobs: list[tuple[int, int, int, int]] = []
    for i in range(1, n):
        x, y, w, h, area = stats[i]
        if area > 500 and w >= 30 and h >= 30:
            blobs.append((int(x), int(y), int(w), int(h)))
    return blobs


def row_anchors_from_a0(a0: np.ndarray) -> list[RowAnchor]:
    """Original row tops with col-1 left and col-N right from A0 wells."""
    blobs = _white_blobs(a0)
    by_y: dict[int, list[tuple[int, int, int, int]]] = {}
    for x, y, w, h in blobs:
        by_y.setdefault(y, []).append((x, y, w, h))

    anchors: list[RowAnchor] = []
    for y in sorted(by_y):
        items = sorted(by_y[y], key=lambda b: b[0])
        first_left = items[0][0]
        last = items[-1]
        last_right = last[0] + last[2] - 1
        anchors.append(RowAnchor(y=y, first_left=first_left, last_right=last_right))
    return anchors


def distribute_row(widths: list[int], first_left: int, last_right: int) -> list[int]:
    """Fix corners; spread keys with equal gaps across the original span."""
    n = len(widths)
    if n == 0:
        return []
    if n == 1:
        return [first_left]

    span = last_right - first_left + 1
    gap_total = span - sum(widths)
    if gap_total < 0:
        raise ValueError(
            f"Keys wider than original span: widths={widths} span={span}"
        )

    base_gap = gap_total // (n - 1)
    extra = gap_total % (n - 1)

    xs: list[int] = []
    x = first_left
    for i, w in enumerate(widths):
        xs.append(x)
        if i < n - 1:
            gap = base_gap + (1 if i < extra else 0)
            x += w + gap
    return xs


def key_rects(a0: np.ndarray | None = None) -> list[KeyRect]:
    if a0 is None:
        a0 = np.array(Image.open(A0_PATH).convert("RGBA"))

    anchors = row_anchors_from_a0(a0)
    if len(anchors) != 8:
        raise ValueError(f"Expected 8 key rows in A0, found {len(anchors)}")

    row_specs: list[tuple[tuple[KeyKind, int], ...]] = [
        ((KeyKind.StandardTop, STANDARD_W),) * 5,
        ((KeyKind.StandardTop, STANDARD_W),) * 5,
        ((KeyKind.StandardTop, STANDARD_W),) * 5,
        (
            (KeyKind.EnterWide, ENTER_W),
            (KeyKind.StandardTop, STANDARD_W),
            (KeyKind.StandardTop, STANDARD_W),
            (KeyKind.StandardTop, STANDARD_W),
        ),
        (
            (KeyKind.Narrow, NARROW_W),
            (KeyKind.Wide, WIDE_W),
            (KeyKind.Wide, WIDE_W),
            (KeyKind.Wide, WIDE_W),
        ),
        (
            (KeyKind.Narrow, NARROW_W),
            (KeyKind.Wide, WIDE_W),
            (KeyKind.Wide, WIDE_W),
            (KeyKind.Wide, WIDE_W),
        ),
        (
            (KeyKind.Narrow, NARROW_W),
            (KeyKind.Wide, WIDE_W),
            (KeyKind.Wide, WIDE_W),
            (KeyKind.Wide, WIDE_W),
        ),
        (
            (KeyKind.Narrow, NARROW_W),
            (KeyKind.Wide, WIDE_W),
            (KeyKind.Wide, WIDE_W),
            (KeyKind.Wide, WIDE_W),
        ),
    ]

    rects: list[KeyRect] = []
    for row_idx, (anchor, spec) in enumerate(zip(anchors, row_specs, strict=True)):
        widths = [w for _kind, w in spec]
        xs = distribute_row(widths, anchor.first_left, anchor.last_right)
        for col_idx, ((kind, w), x) in enumerate(zip(spec, xs, strict=True)):
            rects.append(KeyRect(x, anchor.y, w, KEY_H, kind, row_idx, col_idx))

    return rects


def row_layout_report(
    rects: list[KeyRect], panel_left: int = PANEL_LEFT, panel_right: int = PANEL_RIGHT
) -> dict[int, dict[str, int | list[int]]]:
    by_row: dict[int, list[KeyRect]] = {}
    for r in rects:
        by_row.setdefault(r.row, []).append(r)

    report: dict[int, dict[str, int | list[int]]] = {}
    for row, items in sorted(by_row.items()):
        items = sorted(items, key=lambda k: k.col)
        gaps = [items[i + 1].x - (items[i].x + items[i].w) for i in range(len(items) - 1)]
        left_m = items[0].x - panel_left
        right_m = panel_right - (items[-1].x + items[-1].w - 1)
        report[row] = {
            "margin_left": left_m,
            "margin_right": right_m,
            "gaps": gaps,
            "y": items[0].y,
            "first_x": items[0].x,
            "last_right": items[-1].x + items[-1].w - 1,
        }
    return report
