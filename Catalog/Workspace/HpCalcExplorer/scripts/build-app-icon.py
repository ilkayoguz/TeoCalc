"""Build TeoCalc App.ico from HP-65 faceplate palette (flat key faces, no gradients)."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

OUT = Path(__file__).resolve().parents[4] / "TeoCalc" / "App.ico"

# Body.svg / CalcChassisPalette face colors
BODY = (75, 80, 83)  # #4B5053
FRAME = (28, 32, 37)  # #1C2025 inner dark rule
DISPLAY_BEZEL = (68, 49, 56)  # #443138 burgundy cap
LCD = (0, 0, 0)
DIGIT = (255, 72, 32)

KEY_GREY = (184, 180, 170)
KEY_GOLD = (232, 176, 64)
KEY_BLUE = (126, 191, 231)
KEY_BLACK = (28, 26, 24)


def _draw_key(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], color: tuple[int, int, int], radius: int) -> None:
    x1, y1, x2, y2 = box
    skirt = max(1, int((y2 - y1) * 0.24))
    face_bottom = y2 - skirt
    draw.rounded_rectangle((x1, y1, x2, face_bottom), radius=radius, fill=color)
    draw.rectangle((x1, face_bottom, x2, y2), fill=color)


def render(size: int) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    margin = max(1, size // 14)
    frame = max(1, size // 28)
    outer = size - margin
    radius_outer = max(2, size // 7)

    draw.rounded_rectangle((margin, margin, outer, outer), radius=radius_outer, fill=FRAME)

    inner = margin + frame
    inner_outer = outer - frame
    body_radius = max(2, size // 9)
    draw.rounded_rectangle((inner, inner, inner_outer, inner_outer), radius=body_radius, fill=BODY)

    pad_x = max(2, size // 10)
    top = inner + max(2, size // 18)
    left = inner + pad_x
    right = inner_outer - pad_x
    disp_h = max(4, size // 5)
    cap_h = max(1, disp_h // 5)
    draw.rectangle((left, top, right, top + cap_h), fill=DISPLAY_BEZEL)
    draw.rectangle((left, top + cap_h, right, top + disp_h), fill=LCD)

    seg_w = max(2, int((right - left) * 0.18))
    seg_h = max(2, int(disp_h * 0.22))
    draw.rectangle(
        (right - seg_w - max(1, size // 24), top + cap_h + seg_h, right - max(1, size // 24), top + cap_h + seg_h * 2),
        fill=DIGIT,
    )

    gap = max(1, size // 22)
    keys_top = top + disp_h + gap
    keys_bottom = inner_outer - max(2, size // 14)
    keys_h = keys_bottom - keys_top
    key_h = (keys_h - gap) // 2
    key_w = (right - left - gap) // 2
    key_r = max(1, size // 24)
    colors = [KEY_GOLD, KEY_BLUE, KEY_GREY, KEY_BLACK]

    for index, color in enumerate(colors):
        row, col = divmod(index, 2)
        x1 = left + col * (key_w + gap)
        y1 = keys_top + row * (key_h + gap)
        _draw_key(draw, (x1, y1, x1 + key_w, y1 + key_h), color, key_r)

    return img


def main() -> None:
    sizes = [16, 24, 32, 48, 64, 128, 256]
    images = [render(size) for size in sizes]
    OUT.parent.mkdir(parents=True, exist_ok=True)
    images[-1].save(
        OUT,
        format="ICO",
        sizes=[(size, size) for size in sizes],
        append_images=images[:-1],
    )
    print(f"Wrote {OUT} ({OUT.stat().st_size} bytes, {len(sizes)} sizes)")


if __name__ == "__main__":
    main()
