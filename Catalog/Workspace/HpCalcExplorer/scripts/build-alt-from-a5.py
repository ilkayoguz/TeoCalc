"""Build ALT.png from A5.png: alternative HP-65 layout (numeric block on the right)."""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

SCRIPTS = Path(__file__).resolve().parent
ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
A5 = ASSETS / "A5.png"
OUT = ASSETS / "ALT.png"

KEY_RED = (220, 32, 32, 255)
KEY_ORANGE = (255, 140, 0, 255)
LABEL_WHITE = (255, 255, 255, 255)
LABEL_DARK = (24, 24, 24, 255)


def _load_alt_layout():
    name = "hp65_key_layout_alt"
    spec = importlib.util.spec_from_file_location(name, SCRIPTS / "hp65_key_layout_alt.py")
    mod = importlib.util.module_from_spec(spec)
    sys.modules[name] = mod
    spec.loader.exec_module(mod)
    return mod


def _keypad_mask(rgb: np.ndarray, alpha: np.ndarray) -> np.ndarray:
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    return (
        (b > 180) & (g > 140) & (r < 80)
        | (r > 200) & (g > 170) & (b < 100)
    ) & (alpha > 128)


def _font(size: int) -> ImageFont.ImageFont:
    for name in ("arialbd.ttf", "Arial Bold.ttf", "segoeui.ttf", "calibri.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def _draw_label(
    draw: ImageDraw.ImageDraw,
    box: tuple[int, int, int, int],
    text: str,
    font: ImageFont.ImageFont,
    fill: tuple[int, int, int, int],
) -> None:
    x0, y0, x1, y1 = box
    bbox = draw.textbbox((0, 0), text, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    x = x0 + (x1 - x0 - tw) // 2
    y = y0 + (y1 - y0 - th) // 2 - 1
    draw.text((x, y), text, fill=fill, font=font)


def build(a5_path: Path = A5, out_path: Path = OUT) -> None:
    if not a5_path.exists():
        raise FileNotFoundError(a5_path)

    alt = _load_alt_layout()
    a5 = np.array(Image.open(a5_path).convert("RGBA"))
    rgb = a5[:, :, :3].astype(np.int16)
    alpha = a5[:, :, 3]
    keypad = _keypad_mask(rgb, alpha)

    out = a5.copy()
    out[keypad, :3] = (0, 188, 212)
    out[keypad, 3] = 255

    img = Image.fromarray(out, "RGBA")
    draw = ImageDraw.Draw(img)
    rects = alt.key_rects()

    font_main = _font(13)
    font_small = _font(11)
    font_op = _font(15)

    for key in rects:
        is_op = key.label in {"\u00f7", "\u00d7", "\u2212", "+"}
        fill = KEY_ORANGE if is_op else KEY_RED
        radius = 5 if key.w >= 52 else 4
        box = (key.x, key.y, key.x + key.w, key.y + key.h)
        draw.rounded_rectangle(box, radius=radius, fill=fill)

        label_fill = LABEL_WHITE if is_op else LABEL_WHITE
        font = font_op if is_op else (font_small if len(key.label) > 4 else font_main)
        _draw_label(draw, box, key.label, font, label_fill)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    img.save(out_path, optimize=True)
    summary = alt.layout_summary(rects)
    try:
        print(summary)
    except UnicodeEncodeError:
        print(summary.encode("ascii", "replace").decode("ascii"))
    print(f"\nWrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "ALT.png"
    build(out_path=ASSETS / out_name)
