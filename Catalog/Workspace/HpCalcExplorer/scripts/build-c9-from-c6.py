"""Build C9.png from C6.png: rounded white inner frame + outer B01 silver frame."""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image

SCRIPTS = Path(__file__).resolve().parent
ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
C6 = ASSETS / "C6.png"
B01 = ASSETS / "B01.png"
OUT = ASSETS / "C9.png"


def _frames():
    spec = importlib.util.spec_from_file_location(
        "hp65_faceplate_frames", SCRIPTS / "hp65_faceplate_frames.py"
    )
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def build(
    c6_path: Path = C6,
    b01_path: Path = B01,
    out_path: Path = OUT,
) -> None:
    if not c6_path.exists():
        raise FileNotFoundError(c6_path)
    if not b01_path.exists():
        raise FileNotFoundError(b01_path)

    fr = _frames()
    layout = fr.load_layout()
    b01 = np.array(Image.open(b01_path).convert("RGBA"))
    arr = np.array(Image.open(c6_path).convert("RGBA"))

    white_geom = fr.white_frame_geometry(arr, layout)
    silver_geom = fr.outer_silver_frame_geometry(arr, layout)
    silver_rgb = fr.outer_silver_rgb_from_b01(b01)

    img = Image.fromarray(arr.copy(), "RGBA")
    fr.draw_white_frame_rounded(img, white_geom, layout)
    out_arr = np.array(img)
    fr.draw_silver_frame_sharp(out_arr, silver_geom, silver_rgb)

    out = Image.fromarray(out_arr, "RGBA")
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out.save(out_path, optimize=True)

    print(f"White outer: x={white_geom['wx0']}-{white_geom['wx1']} y={white_geom['wy0']}-{white_geom['wy1']}")
    print(f"Silver outer chassis: x={silver_geom['cx0']}-{silver_geom['cx1']} y={silver_geom['cy0']}-{silver_geom['cy1']}")
    print(f"Silver RGB{silver_rgb} K1={layout.K1_THICKNESS}px")
    print(
        f"Silver bars: top y={silver_geom['ty0']}-{silver_geom['ty1']} "
        f"bottom y={silver_geom['by0']}-{silver_geom['by1']} "
        f"sides x={silver_geom['vx0']}-{silver_geom['vx1']}, "
        f"x={silver_geom['rx0']}-{silver_geom['rx1']}"
    )
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "C9.png"
    build(out_path=ASSETS / out_name)
