"""Build 002.png — hp65.png layout template (pre-photo hp65_470 era).

This is the version before:
  - build-hp65-470.py photo chrome rebuild (hp65_470.png)
  - 000.png Panamatik restart

Source: Desktop HP-65/hp65.png — chrome frame + dark faceplate + white/transparent key wells.
"""
from __future__ import annotations

import shutil
import sys
from pathlib import Path

TEMPLATE = Path(r"C:\Users\ilkay\Desktop\HP-65\hp65.png")
ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
DEFAULT_OUT = ASSETS / "002.png"


def build(out_path: Path = DEFAULT_OUT) -> None:
    if not TEMPLATE.exists():
        raise FileNotFoundError(TEMPLATE)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(TEMPLATE, out_path)
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes) from {TEMPLATE.name}")


if __name__ == "__main__":
    name = sys.argv[1] if len(sys.argv) > 1 else "002.png"
    build(ASSETS / name)
