"""Build 004.png: curved çivit panel + full-round red key caps on 002 chrome."""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from hp65_faceplate_panel import PANEL_BOTTOM, PANEL_TOP, panel_mask_from_reference
from hp65_key_slots import key_slots_from_a0, load_a0

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
CHROME = ASSETS / "002.png"
REF = ASSETS / "001.png"
MASK = ASSETS / "A0.png"
OUT = ASSETS / "004.png"

# Çivit / navy faceplate
PANEL_INDIGO = (38, 48, 78, 255)
KEY_RED = (220, 32, 32, 255)


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
        & panel
    )
    return panel & (gold | white_ink)


def pill_radius(w: int, h: int) -> int:
    return min(w, h) // 2


def draw_pill_keys(draw: ImageDraw.ImageDraw, slots) -> None:
    for slot in slots:
        r = pill_radius(slot.w, slot.h)
        draw.rounded_rectangle(
            (slot.x, slot.y, slot.x + slot.w, slot.y + slot.h),
            radius=r,
            fill=KEY_RED,
        )


def build(
    chrome_path: Path = CHROME,
    ref_path: Path = REF,
    mask_path: Path = MASK,
    out_path: Path = OUT,
) -> None:
    for path in (chrome_path, ref_path, mask_path):
        if not path.exists():
            raise FileNotFoundError(path)

    chrome = np.array(Image.open(chrome_path).convert("RGBA"))
    ref_rgb = np.array(Image.open(ref_path).convert("RGB"))
    a0 = load_a0(mask_path)
    slots = key_slots_from_a0(a0)

    panel = panel_mask_from_reference(ref_rgb)
    labels = gold_label_mask(chrome[:, :, :3], panel)

    out = chrome.copy()
    out[panel] = PANEL_INDIGO
    for c in range(4):
        out[:, :, c] = np.where(labels, chrome[:, :, c], out[:, :, c])

    rgba = Image.fromarray(out, "RGBA")
    draw_pill_keys(ImageDraw.Draw(rgba), slots)

    ys = np.where(panel.any(axis=1))[0]
    xs_l = [np.where(panel[y])[0][0] for y in ys[::40]]
    xs_r = [np.where(panel[y])[0][-1] for y in ys[::40]]
    print(f"Panel y={PANEL_TOP}..{PANEL_BOTTOM}")
    for y, xl, xr in zip(ys[::40], xs_l, xs_r):
        print(f"  y={y:3d}  x {xl}-{xr}  width={xr - xl + 1}")

    print(f"Keys: {len(slots)} (pill radius = height/2)")
    for kind in sorted({s.kind.value for s in slots}):
        n = sum(1 for s in slots if s.kind.value == kind)
        sample = next(s for s in slots if s.kind.value == kind)
        print(
            f"  {kind}: {n}  pill_r={pill_radius(sample.w, sample.h)}"
        )

    out_path.parent.mkdir(parents=True, exist_ok=True)
    rgba.save(out_path, optimize=True)
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "004.png"
    build(out_path=ASSETS / out_name)
