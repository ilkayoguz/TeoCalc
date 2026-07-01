"""Build faceplate-d01.svg from D01.png measurements (minimal, named layers)."""
from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
D01 = ASSETS / "D01.png"
OUT = ASSETS / "faceplate-d01.svg"

# D01 canonical colors (exact RGB).
COL = {
    "chassis": "#61645A",
    "face": "#4B5053",
    "switch": "#4C4E50",
    "burgundy": "#443138",
    "footer": "#3C3E41",
    "dark_k1": "#1C2025",
    "white_k1": "#B8C0CD",
    "white": "#FFFFFF",
    "black": "#000000",
}

W, H = 409, 861
FACE_X, FACE_W = 23, 362
FACE_X1 = FACE_X + FACE_W - 1
CONTENT_Y0, CONTENT_Y1 = 24, 836

FRAME_W = 3
FRAME_R = 10  # equal on outer + inner white frames

OUTER = (0, 0, W - 1, H - 1)
GUTTER_OUTER = (FRAME_W, FRAME_W, W - 1 - FRAME_W, H - 1 - FRAME_W)
INNER_WHITE = (20, 20, 388, 840)
CONTENT = (FACE_X, CONTENT_Y0, FACE_X1, CONTENT_Y1)


def _rounded_rect_path(x0: int, y0: int, x1: int, y1: int, r: int) -> str:
    r = min(r, (x1 - x0) // 2, (y1 - y0) // 2)
    return (
        f"M{x0 + r},{y0}H{x1 - r}A{r},{r} 0 0 1 {x1},{y0 + r}V{y1 - r}"
        f"A{r},{r} 0 0 1 {x1 - r},{y1}H{x0 + r}A{r},{r} 0 0 1 {x0},{y1 - r}"
        f"V{y0 + r}A{r},{r} 0 0 1 {x0 + r},{y0}Z"
    )


def _ring_path(outer: tuple[int, int, int, int], inner: tuple[int, int, int, int], r: int) -> str:
    ox0, oy0, ox1, oy1 = outer
    ix0, iy0, ix1, iy1 = inner
    ri = max(1, r - FRAME_W)
    return _rounded_rect_path(ox0, oy0, ox1, oy1, r) + _rounded_rect_path(ix0, iy0, ix1, iy1, ri)


def _extract_key_wells(im: np.ndarray) -> list[tuple[int, int, int, int]]:
    trans = im[:, :, 3] == 0
    h, w = trans.shape
    visited = np.zeros_like(trans, dtype=bool)
    rects: list[tuple[int, int, int, int]] = []
    for y in range(222, h):
        for x in range(w):
            if not trans[y, x] or visited[y, x]:
                continue
            y0 = y1 = y
            x0 = x1 = x
            stack = [(y, x)]
            visited[y, x] = True
            while stack:
                cy, cx = stack.pop()
                y0 = min(y0, cy)
                y1 = max(y1, cy)
                x0 = min(x0, cx)
                x1 = max(x1, cx)
                for ny, nx in ((cy + 1, cx), (cy - 1, cx), (cy, cx + 1), (cy, cx - 1)):
                    if 0 <= ny < h and 0 <= nx < w and trans[ny, nx] and not visited[ny, nx]:
                        visited[ny, nx] = True
                        stack.append((ny, nx))
            rw, rh = x1 - x0 + 1, y1 - y0 + 1
            if rw >= 30 and rh >= 30:
                rects.append((x0, y0, rw, rh))
    rects.sort(key=lambda r: (r[1], r[0]))
    return rects


def build(d01_path: Path = D01, out_path: Path = OUT) -> None:
    if not d01_path.exists():
        raise FileNotFoundError(d01_path)

    im = np.array(Image.open(d01_path).convert("RGBA"))
    keys = _extract_key_wells(im)
    key_path = "".join(f"M{x},{y}h{w}v{h}H{x}Z" for x, y, w, h in keys)

    bands = [
        ("burgundy-bevel-top", 24, 27, COL["burgundy"]),
        ("display-region", 28, 92, COL["black"]),
        ("burgundy-bevel-bottom", 93, 115, COL["burgundy"]),
        ("rule-white-k1", 117, 120, COL["white_k1"]),
        ("face-above-switch", 121, 149, COL["face"]),
        ("rule-dark-k1-above-switch", 150, 153, COL["dark_k1"]),
        ("switch-region", 154, 194, COL["switch"]),
        ("face-between-switch-keys", 195, 217, COL["face"]),
        ("rule-dark-k1-above-keys", 218, 221, COL["dark_k1"]),
        ("keypad-backing", 222, 808, COL["face"]),
        ("logo-region", 809, 836, COL["footer"]),
    ]

    lines = [
        '<?xml version="1.0" encoding="UTF-8"?>',
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" '
        f'width="{W}" height="{H}" id="hp65-faceplate-d01">',
        "  <title>HP-65 faceplate (D01)</title>",
        f"  <desc>From D01.png; outer and inner white frames share rx={FRAME_R}.</desc>",
        '  <g id="faceplate-root">',
        f'    <rect id="canvas-backdrop" width="{W}" height="{H}" fill="{COL["black"]}"/>',
        '    <g id="frame-assembly">',
        f'      <path id="frame-gutter" fill="{COL["chassis"]}" fill-rule="evenodd" '
        f'd="{_ring_path(GUTTER_OUTER, INNER_WHITE, FRAME_R)}"/>',
        f'      <path id="outer-chrome-frame" fill="{COL["white"]}" fill-rule="evenodd" '
        f'd="{_ring_path(OUTER, GUTTER_OUTER, FRAME_R)}"/>',
        f'      <path id="inner-face-frame" fill="{COL["white"]}" fill-rule="evenodd" '
        f'd="{_ring_path(INNER_WHITE, CONTENT, FRAME_R)}"/>',
        "    </g>",
        '    <g id="display-stack">',
    ]
    for band_id, y0, y1, color in bands[:3]:
        lines.append(
            f'      <rect id="{band_id}" x="{FACE_X}" y="{y0}" width="{FACE_W}" '
            f'height="{y1 - y0 + 1}" fill="{color}"/>'
        )
    lines.append("    </g>")
    lines.append('    <g id="separator-rules">')
    for band_id, y0, y1, color in bands[3:9]:
        lines.append(
            f'      <rect id="{band_id}" x="{FACE_X}" y="{y0}" width="{FACE_W}" '
            f'height="{y1 - y0 + 1}" fill="{color}"/>'
        )
    lines.append("    </g>")
    lines += [
        '    <g id="keypad-region">',
        f'      <rect id="keypad-backing" x="{FACE_X}" y="222" width="{FACE_W}" height="587" fill="{COL["face"]}"/>',
        '      <path id="key-wells" fill="' + COL["black"] + '" d="' + key_path + '"/>',
        "    </g>",
        f'    <rect id="logo-region" x="{FACE_X}" y="809" width="{FACE_W}" height="28" fill="{COL["footer"]}"/>',
        "  </g>",
        "</svg>",
    ]

    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Keys: {len(keys)}  Frame rx={FRAME_R}px (equal)  Size {W}x{H}")
    print(f"Wrote {out_path} ({out_path.stat().st_size} bytes)")


if __name__ == "__main__":
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else OUT
    build(out_path=out)
