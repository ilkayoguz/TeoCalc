"""Build faceplate-d03.svg (+ key layout JSON) from D03.png measurements."""
from __future__ import annotations

import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
D03 = ASSETS / "D03.png"
OUT_SVG = ASSETS / "faceplate-d03.svg"
OUT_LAYOUT = ASSETS / "faceplate-d03-layout.json"
BODY_SVG = ASSETS / "Body.svg"

W, H = 409, 861
FX, FW = 23, 362

COL = {
    "chassis": "#61645A",
    "face": "#4B5053",
    "switch": "#4C4E50",
    "burgundy": "#443138",
    "footer": "#3C3E41",
    "darkK1": "#1C2025",
    "whiteK1": "#B8C0CD",
    "white": "#FFFFFF",
    "black": "#000000",
}

RGB = {
    (97, 100, 90): "chassis",
    (75, 80, 83): "face",
    (76, 78, 80): "switch",
    (68, 49, 56): "burgundy",
    (60, 62, 65): "footer",
    (28, 32, 37): "darkK1",
    (184, 192, 205): "whiteK1",
    (255, 255, 255): "white",
    (0, 0, 0): "black",
}

DISPLAY_IDS = ("burgundy-cap-top", "lcd-window", "burgundy-cap-bottom")
CONTROL_IDS = (
    "white-rule-top",
    "face-above-switch",
    "dark-rule-above-switch",
    "switch-track",
    "face-below-switch",
    "dark-rule-above-keypad",
)


def _row_hue(im: np.ndarray, y: int) -> str | None:
    row = im[y, FX : FX + FW]
    opaque = row[:, 3] > 0
    if not opaque.any():
        return None
    rgb = row[opaque, :3].astype(int)
    vals, counts = np.unique(rgb, axis=0, return_counts=True)
    tag = RGB.get(tuple(vals[counts.argmax()]))
    if tag == "black" and 28 <= y <= 92:
        return "black"
    if tag == "black":
        return None
    return tag


def _scan_bands(im: np.ndarray) -> list[tuple[str, int, int, str]]:
    spans: list[tuple[str, int, int]] = []
    y = 24
    while y <= 836:
        tag = _row_hue(im, y)
        if tag is None:
            y += 1
            continue
        y0 = y
        while y <= 836 and _row_hue(im, y) == tag:
            y += 1
        spans.append((tag, y0, y - 1))
    out: list[tuple[str, int, int, str]] = []
    cap = {"burgundy": 0, "darkK1": 0, "face": 0}
    for tag, y0, y1 in spans:
        if tag == "burgundy":
            cap["burgundy"] += 1
            bid = "burgundy-cap-top" if cap["burgundy"] == 1 else "burgundy-cap-bottom"
        elif tag == "black":
            bid = "lcd-window"
        elif tag == "whiteK1":
            bid = "white-rule-top"
        elif tag == "darkK1":
            cap["darkK1"] += 1
            bid = (
                "dark-rule-above-switch"
                if cap["darkK1"] == 1
                else "dark-rule-above-keypad"
            )
        elif tag == "face":
            cap["face"] += 1
            bid = (
                "face-above-switch"
                if cap["face"] == 1
                else "face-below-switch"
                if cap["face"] == 2
                else "keypad-panel"
            )
            color = COL["switch"] if bid == "face-below-switch" else COL[tag]
        elif tag == "switch":
            bid = "switch-track"
        elif tag == "footer":
            bid = "brand-plate"
        else:
            continue
        if bid != "face-below-switch":
            color = COL["black"] if bid == "lcd-window" else COL[tag]
        out.append((bid, y0, y1, color))
    return out


def _rect(bid: str, y0: int, y1: int, color: str) -> str:
    rid = "brand-plate-fill" if bid == "brand-plate" else bid
    return f'<rect id="{rid}" x="{FX}" y="{y0}" width="{FW}" height="{y1 - y0 + 1}" fill="{color}"/>'


def _ring_d(ox0: int, oy0: int, ox1: int, oy1: int, ix0: int, iy0: int, ix1: int, iy1: int) -> str:
    return (
        f"M{ox0},{oy0}H{ox1}V{oy1}H{ox0}Z"
        f"M{ix0},{iy0}H{ix1}V{iy1}H{ix0}Z"
    )


def _extract_key_wells(im: np.ndarray) -> list[dict[str, int]]:
    trans = im[:, :, 3] == 0
    h, w = trans.shape
    visited = np.zeros_like(trans, dtype=bool)
    keys: list[tuple[int, int, int, int]] = []
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
                keys.append((x0, y0, rw, rh))
    keys.sort(key=lambda r: (r[1], r[0]))
    return [{"id": f"key-{i + 1:02d}", "x": x, "y": y, "w": w, "h": h} for i, (x, y, w, h) in enumerate(keys)]


def build(d03_path: Path = D03, out_svg: Path = OUT_SVG, out_layout: Path = OUT_LAYOUT) -> None:
    if not d03_path.exists():
        raise FileNotFoundError(d03_path)

    im = np.array(Image.open(d03_path).convert("RGBA"))
    keys = _extract_key_wells(im)
    bands = _scan_bands(im)
    rects = {bid: _rect(bid, y0, y1, color) for bid, y0, y1, color in bands}
    brand_group = f'<g id="brand-plate">{rects["brand-plate"]}</g>'

    svg = f"""<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" id="hp65-body">
<g id="frame-assembly" fill-rule="evenodd">
<path id="chassis-gutter" fill="{COL["chassis"]}" d="{_ring_d(3,3,405,857,20,20,388,840)}"/>
<path id="outer-white-frame" fill="{COL["white"]}" d="{_ring_d(0,0,408,860,3,3,405,857)}"/>
<path id="inner-white-frame" fill="{COL["white"]}" d="{_ring_d(20,20,388,840,23,24,384,836)}"/>
</g>
<g id="faceplate-surface">
<g id="display-bezel">{"".join(rects[i] for i in DISPLAY_IDS)}</g>
<g id="control-strip">{"".join(rects[i] for i in CONTROL_IDS)}</g>
<g id="keypad-panel">{rects["keypad-panel"]}</g>
{brand_group}
</g>
</svg>
"""

    switch = next((b for b in bands if b[0] == "switch-track"), None)
    switch_track = (
        {"x": FX, "y": switch[1], "w": FW, "h": switch[2] - switch[1] + 1}
        if switch
        else {"x": FX, "y": 154, "w": FW, "h": 41}
    )

    layout = {
        "source": "D03.png",
        "viewBox": [W, H],
        "faceOrigin": [FX, 24],
        "faceSize": [FW, 813],
        "bands": [{"id": b[0], "y": b[1], "h": b[2] - b[1] + 1} for b in bands],
        "switchTrack": switch_track,
        "keys": keys,
        "notes": {
            "svg": "Solid faceplate only; keys/switches are runtime overlays.",
            "switchHoles": "Not punched in D03; use switchTrack bounds for slot art.",
        },
    }

    out_svg.write_text(svg, encoding="utf-8")
    BODY_SVG.write_text(svg, encoding="utf-8")
    out_layout.write_text(json.dumps(layout, indent=2) + "\n", encoding="utf-8")
    print("Bands:", ", ".join(f"{b[0]}@{b[1]}-{b[2]}" for b in bands))
    print(f"Keys in layout JSON: {len(keys)}  SVG bytes: {out_svg.stat().st_size}")
    print(f"Wrote {out_svg}")
    print(f"Wrote {BODY_SVG} (no logo — runtime overlay)")
    print(f"Wrote {out_layout}")


if __name__ == "__main__":
    svg_out = Path(sys.argv[1]) if len(sys.argv) > 1 else OUT_SVG
    build(out_svg=svg_out)
