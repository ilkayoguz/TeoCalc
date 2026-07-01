"""Build A1.png from A0.png: red keys + cyan faceplate (no transparency in panel)."""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
SRC = ASSETS / "A0.png"
OUT = ASSETS / "A1.png"

KEY_RED = (220, 32, 32, 255)
PANEL_CYAN = (0, 188, 212, 255)

# Keypad faceplate band in A0 (transparent pocket behind keys).
PANEL_TOP = 238
PANEL_BOTTOM = 808


def key_well_mask(a0: np.ndarray) -> np.ndarray:
    rgb = a0[:, :, :3].astype(np.int16)
    alpha = a0[:, :, 3]
    return (
        (rgb[:, :, 0] > 250)
        & (rgb[:, :, 1] > 250)
        & (rgb[:, :, 2] > 250)
        & (alpha > 200)
    )


def faceplate_mask(a0: np.ndarray) -> np.ndarray:
    alpha = a0[:, :, 3]
    panel = np.zeros(alpha.shape, dtype=bool)
    panel[PANEL_TOP : PANEL_BOTTOM + 1, :] = (
        alpha[PANEL_TOP : PANEL_BOTTOM + 1, :] < 16
    )
    return panel


def build(src_path: Path = SRC, out_path: Path = OUT) -> None:
    if not src_path.exists():
        raise FileNotFoundError(src_path)

    a0 = np.array(Image.open(src_path).convert("RGBA"))
    out = a0.copy()

    keys = key_well_mask(a0)
    panel = faceplate_mask(a0)

    out[panel] = PANEL_CYAN
    out[keys] = KEY_RED

    n_keys = int(keys.sum())
    n_panel = int(panel.sum())
    print(f"Red keys: {n_keys} px")
    print(f"Cyan faceplate: {n_panel} px (y={PANEL_TOP}..{PANEL_BOTTOM})")

    out_path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(out, "RGBA").save(out_path, optimize=True)
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out_name = sys.argv[1] if len(sys.argv) > 1 else "A1.png"
    build(out_path=ASSETS / out_name)
