"""Build C6.png from C5.png: footer brand pocket aligned with display (FACE_X × FACE_W)."""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image

SCRIPTS = Path(__file__).resolve().parent
ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
C5 = ASSETS / "C5.png"
B01 = ASSETS / "B01.png"
OUT = ASSETS / "C6.png"


def _layout():
    spec = importlib.util.spec_from_file_location(
        "hp65_faceplate_layout", SCRIPTS / "hp65_faceplate_layout.py"
    )
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def _footer_panel_color_from_b01(b01: np.ndarray, x: int, w: int, y: int, h: int) -> tuple[int, int, int]:
    patch = b01[y + 1 : y + h - 1, x : x + w, :3].astype(int)
    lum = patch.mean(axis=2)
    mask = (lum >= 45) & (lum <= 120)
    pixels = patch[mask] if mask.any() else patch.reshape(-1, 3)
    med = np.median(pixels, axis=0).astype(int)
    return int(med[0]), int(med[1]), int(med[2])


def _fill_rect(
    arr: np.ndarray,
    x0: int,
    y0: int,
    w: int,
    h: int,
    rgb: tuple[int, int, int],
) -> None:
    x1 = x0 + w - 1
    y1 = y0 + h - 1
    arr[y0 : y1 + 1, x0 : x1 + 1, :3] = rgb
    arr[y0 : y1 + 1, x0 : x1 + 1, 3] = 255


def build(
    c5_path: Path = C5,
    b01_path: Path = B01,
    out_path: Path = OUT,
) -> None:
    layout = _layout()
    if not c5_path.exists():
        raise FileNotFoundError(c5_path)
    if not b01_path.exists():
        raise FileNotFoundError(b01_path)

    b01 = np.array(Image.open(b01_path).convert("RGBA"))
    arr = np.array(Image.open(c5_path).convert("RGBA"))

    _fill_rect(
        arr,
        layout.OLD_FOOTER_X,
        layout.FOOTER_Y,
        layout.OLD_FOOTER_W,
        layout.FOOTER_H,
        layout.CHASSIS_RGB,
    )

    panel_rgb = _footer_panel_color_from_b01(
        b01, layout.FACE_X, layout.FACE_W, layout.FOOTER_Y, layout.FOOTER_H
    )
    _fill_rect(
        arr,
        layout.FACE_X,
        layout.FOOTER_Y,
        layout.FACE_W,
        layout.FOOTER_H,
        panel_rgb,
    )

    out = Image.fromarray(arr, "RGBA")
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out.save(out_path, optimize=True)

    x1 = layout.FACE_X + layout.FACE_W - 1
    y1 = layout.FOOTER_Y + layout.FOOTER_H - 1
    print(
        f"Footer (display-aligned): x={layout.FACE_X} y={layout.FOOTER_Y} "
        f"{layout.FACE_W}x{layout.FOOTER_H} (x1={x1} y1={y1})"
    )
    print(f"Footer panel RGB{panel_rgb}")
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "C6.png"
    build(out_path=ASSETS / out_name)
