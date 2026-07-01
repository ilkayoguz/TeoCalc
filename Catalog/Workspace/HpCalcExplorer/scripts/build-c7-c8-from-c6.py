"""Build C7.png (sharp) and C8.png (rounded) from C6.png: K1 white gutter frame."""
from __future__ import annotations

import importlib.util
import sys
from pathlib import Path

import numpy as np
from PIL import Image

SCRIPTS = Path(__file__).resolve().parent
ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
C6 = ASSETS / "C6.png"
OUT_SHARP = ASSETS / "C7.png"
OUT_ROUND = ASSETS / "C8.png"


def _frames():
    spec = importlib.util.spec_from_file_location(
        "hp65_faceplate_frames", SCRIPTS / "hp65_faceplate_frames.py"
    )
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def build(
    c6_path: Path = C6,
    out_sharp: Path = OUT_SHARP,
    out_round: Path = OUT_ROUND,
) -> None:
    if not c6_path.exists():
        raise FileNotFoundError(c6_path)

    fr = _frames()
    layout = fr.load_layout()
    base = np.array(Image.open(c6_path).convert("RGBA"))
    geom = fr.white_frame_geometry(base, layout)

    sharp = base.copy()
    fr.draw_white_frame_sharp(sharp, geom, layout)
    Image.fromarray(sharp, "RGBA").save(out_sharp, optimize=True)

    round_arr = base.copy()
    round_img = Image.fromarray(round_arr, "RGBA")
    fr.draw_white_frame_rounded(round_img, geom, layout)
    round_img.save(out_round, optimize=True)

    k = layout.K1_THICKNESS
    print(
        f"Faceplate content: x={geom['fx0']}-{geom['fx1']} ({layout.FACE_W}px) "
        f"y={geom['fy0']}-{geom['fy1']}"
    )
    print(
        f"Top bar: y={geom['ty0']}-{geom['ty1']} | Bottom bar: y={geom['by0']}-{geom['by1']} | "
        f"Side gutters: x={geom['gx0']}-{geom['gx0'] + k - 1}, "
        f"x={geom['gx1']}-{geom['gx1'] + k - 1}"
    )
    print(f"Wrote {out_sharp} ({out_sharp.stat().st_size} bytes) [sharp]")
    print(f"Wrote {out_round} ({out_round.stat().st_size} bytes) [rounded]")


if __name__ == "__main__":
    build()
