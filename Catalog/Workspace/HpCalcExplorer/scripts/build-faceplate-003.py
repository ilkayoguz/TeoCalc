"""Build 003.png from 002.png: draw 4 key sizes in RED at A0 slot positions."""
from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageDraw

from hp65_key_slots import KeySlotKind, key_slots_from_a0, load_a0

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
SRC = ASSETS / "002.png"
MASK = ASSETS / "A0.png"
OUT = ASSETS / "003.png"

KEY_RED = (220, 32, 32, 255)

RADIUS = {
    KeySlotKind.StandardTop: 4,
    KeySlotKind.EnterWide: 5,
    KeySlotKind.Narrow: 4,
    KeySlotKind.Wide: 5,
}


def draw_red_keys(rgba: Image.Image, slots) -> None:
    draw = ImageDraw.Draw(rgba)
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
    slots = key_slots_from_a0(a0)

    rgba = Image.open(src_path).convert("RGBA")
    draw_red_keys(rgba, slots)

    counts: dict[str, int] = {}
    for slot in slots:
        label = slot.kind.value
        counts[label] = counts.get(label, 0) + 1
        print(
            f"  [{label}] row={slot.row + 1} col={slot.col + 1} "
            f"({slot.x},{slot.y}) {slot.w}x{slot.h}"
        )

    print(f"Total keys: {len(slots)}")
    for label, n in sorted(counts.items()):
        print(f"  {label}: {n}")

    out_path.parent.mkdir(parents=True, exist_ok=True)
    rgba.save(out_path, optimize=True)
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "003.png"
    build(out_path=ASSETS / out_name)
