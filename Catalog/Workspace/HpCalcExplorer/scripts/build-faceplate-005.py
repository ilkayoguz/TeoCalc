"""Build 005.png from 001 + A0 masks: cyan curved panel, slight-round red keys.

Uses A0.png semantics (like 002):
  - outer gray canvas -> transparent
  - transparent faceplate pocket (keypad band) -> cyan
  - chrome / display / switches / footer -> keep 001 Panamatik
  - key slots -> red, slight corner radius (003 style, not pill)
"""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from hp65_a0_masks import (
    PANEL_BOTTOM,
    PANEL_TOP,
    chroma_green,
    faceplate_panel_mask,
    outer_background_mask,
)
from hp65_key_slots import KeySlotKind, key_slots_from_a0, load_a0

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
SRC = ASSETS / "001.png"
MASK = ASSETS / "A0.png"
OUT = ASSETS / "005.png"

PANEL_CYAN = (0, 188, 212, 255)
KEY_RED = (220, 32, 32, 255)

RADIUS = {
    KeySlotKind.StandardTop: 4,
    KeySlotKind.EnterWide: 5,
    KeySlotKind.Narrow: 4,
    KeySlotKind.Wide: 5,
}


def gold_label_mask(rgb: np.ndarray, panel: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    gold = (
        (r.astype(np.int16) > 125)
        & (g.astype(np.int16) > 95)
        & (b.astype(np.int16) < 130)
        & (r > g)
    )
    white_ink = (
        (r.astype(np.int16) > 175)
        & (g.astype(np.int16) > 170)
        & (b.astype(np.int16) > 165)
    )
    return panel & (gold | white_ink)


def draw_red_keys(draw: ImageDraw.ImageDraw, slots) -> None:
    for slot in slots:
        r = RADIUS[slot.kind]
        draw.rounded_rectangle(
            (slot.x, slot.y, slot.x + slot.w, slot.y + slot.h),
            radius=r,
            fill=KEY_RED,
        )


def build(
    src_path: Path = SRC,
    mask_path: Path = MASK,
    out_path: Path = OUT,
) -> None:
    if not src_path.exists():
        raise FileNotFoundError(src_path)
    if not mask_path.exists():
        raise FileNotFoundError(mask_path)

    a0 = load_a0(mask_path)
    src = np.array(Image.open(src_path).convert("RGBA"))
    slots = key_slots_from_a0(a0)

    outer = outer_background_mask(a0)
    panel = faceplate_panel_mask(a0)
    labels = gold_label_mask(src[:, :, :3], panel)

    out = src.copy()
    out[outer | chroma_green(src), 3] = 0
    out[panel] = PANEL_CYAN
    for c in range(4):
        out[:, :, c] = np.where(labels, src[:, :, c], out[:, :, c])

    rgba = Image.fromarray(out, "RGBA")
    draw = ImageDraw.Draw(rgba)
    draw_red_keys(draw, slots)

    print(f"Panel cyan: y={PANEL_TOP}..{PANEL_BOTTOM}, {int(panel.sum())} px")
    print(f"Outer punched: {int(outer.sum())} px")
    print(f"Keys: {len(slots)} (radius 4-5, not pill)")

    out_path.parent.mkdir(parents=True, exist_ok=True)
    rgba.save(out_path, optimize=True)
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "005.png"
    build(out_path=ASSETS / out_name)
