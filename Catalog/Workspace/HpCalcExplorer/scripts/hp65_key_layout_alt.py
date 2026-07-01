"""HP-65 alternative faceplate layout: numerics + ops on the right."""
from __future__ import annotations

from dataclasses import dataclass
from enum import Enum
from pathlib import Path

import numpy as np
from PIL import Image

from hp65_key_layout_v2 import (
    KEY_H,
    PANEL_LEFT,
    PANEL_RIGHT,
    STANDARD_W,
    RowAnchor,
    distribute_row,
    row_anchors_from_a0,
)

A0_PATH = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets\A0.png")

# Right block: digit columns + operator column (phone-style, ops on far right).
NUM_W = 48
OP_W = 52
RIGHT_GAP = 14
LEFT_MARGIN = PANEL_LEFT + 24
RIGHT_MARGIN = PANEL_RIGHT - 24


class AltKeyKind(Enum):
    Standard = "48x38"
    Operator = "52x38"
    Wide = "60x38"


@dataclass(frozen=True)
class AltKeyRect:
    x: int
    y: int
    w: int
    h: int
    kind: AltKeyKind
    row: int
    col: int
    key_index: int
    label: str


# KeyChart index + primary label (program.vocabulary.json / CalcFaceplateLayout).
LEFT_ROWS: list[list[tuple[int, str]]] = [
    [(0, "A"), (1, "B"), (2, "C"), (3, "D"), (4, "E")],
    [(5, "DSP"), (6, "GTO"), (7, "LBL"), (8, "RTN"), (9, "SST")],
    [(10, "f"), (11, "f-1"), (12, "STO"), (13, "RCL"), (14, "g")],
    [(17, "CHS"), (18, "EEX"), (19, "CLx"), (38, "R/S")],
]

# row 4-7: each tuple is (key_index, label) left-to-right; rightmost is operator.
RIGHT_ROWS: list[tuple[tuple[int, str], ...]] = [
    ((21, "7"), (22, "8"), (23, "9"), (35, "\u00f7")),
    ((26, "4"), (27, "5"), (28, "6"), (30, "\u00d7")),
    ((31, "1"), (32, "2"), (33, "3"), (20, "\u2212")),
    ((36, "0"), (37, "."), (15, "ENTER"), (25, "+")),
]


def _right_column_xs() -> list[int]:
    widths = [NUM_W, NUM_W, NUM_W, OP_W]
    xs: list[int] = []
    x_right = RIGHT_MARGIN
    for w in reversed(widths):
        x = x_right - w + 1
        xs.insert(0, x)
        x_right = x - RIGHT_GAP - 1
    return xs


def key_rects(a0: np.ndarray | None = None) -> list[AltKeyRect]:
    if a0 is None:
        a0 = np.array(Image.open(A0_PATH).convert("RGBA"))

    anchors = row_anchors_from_a0(a0)
    if len(anchors) != 8:
        raise ValueError(f"Expected 8 key rows in A0, found {len(anchors)}")

    rects: list[AltKeyRect] = []

    # Left block rows 0-3.
    for row_idx in range(4):
        anchor = anchors[row_idx]
        row_keys = LEFT_ROWS[row_idx]

        if row_idx < 3:
            widths = [STANDARD_W] * 5
            xs = distribute_row(widths, LEFT_MARGIN, RIGHT_MARGIN)
            kind = AltKeyKind.Standard
        else:
            widths = [72, 72, 72, 72]
            xs = distribute_row(widths, LEFT_MARGIN, RIGHT_MARGIN)
            kind = AltKeyKind.Wide

        for col_idx, ((key_index, label), x, w) in enumerate(
            zip(row_keys, xs, widths, strict=True)
        ):
            rects.append(
                AltKeyRect(x, anchor.y, w, KEY_H, kind, row_idx, col_idx, key_index, label)
            )

    # Right numeric block rows 4-7.
    right_xs = _right_column_xs()
    for row_offset, spec in enumerate(RIGHT_ROWS):
        row_idx = 4 + row_offset
        anchor = anchors[row_idx]
        for col_idx, ((key_index, label), x) in enumerate(zip(spec, right_xs, strict=True)):
            w = OP_W if col_idx == 3 else NUM_W
            kind = AltKeyKind.Operator if col_idx == 3 else AltKeyKind.Standard
            if label == "ENTER":
                kind = AltKeyKind.Standard
            rects.append(
                AltKeyRect(
                    x, anchor.y, w, KEY_H, kind, row_idx, col_idx + 10, key_index, label
                )
            )

    return rects


def layout_summary(rects: list[AltKeyRect]) -> str:
    lines = ["HP-65 alternative layout (numeric column right)", ""]
    by_row: dict[int, list[AltKeyRect]] = {}
    for r in rects:
        by_row.setdefault(r.row, []).append(r)
    for row in sorted(by_row):
        items = sorted(by_row[row], key=lambda k: k.x)
        labels = ", ".join(f"{k.label}({k.key_index})" for k in items)
        side = "left" if row < 4 else "numeric"
        lines.append(f"  row {row} [{side}]: {labels}")
    return "\n".join(lines)
