"""Build KeyCap.svg — HP-65 key cap (48x38, B01 f-key reference).

Gold-first: flat face, flat dark skirt, subtle inverse-U lip inside skirt.
"""
from __future__ import annotations

import sys
from pathlib import Path

ASSETS = Path(__file__).resolve().parents[4] / "Resource/Engine/HP-65/Assets"
KEYS = ASSETS / "Keys"
OUT = KEYS / "KeyCap.svg"

W, H = 48, 38

# Uniform black frame ring (sides were 2px; top/bottom were only 1.5px before).
FRAME_W = 2
OUTER_R = 4
INNER_R = OUTER_R - FRAME_W
INNER_X = FRAME_W
INNER_Y = FRAME_W
INNER_RIGHT = W - FRAME_W
INNER_BOTTOM = H - FRAME_W

# Bleed face/skirt paint slightly under the frame ring (fixes inner-corner halos).
FACE_BLEED = 0.65

# Geometry tuned against B01.png f-key (47x39 slot → 48x38 viewBox).
FACE_BOTTOM_Y = 26
FRAME = "#1A1A1A"

# Shared hairline weight (highlight glint + inverted-U lip).
K1 = 1.1
# Equal vertical clearance: inner top → highlight, skirt top → lip upper edge.
TOP_MARGIN = 2.5
HIGHLIGHT_Y = INNER_Y + TOP_MARGIN
HIGHLIGHT_X0 = 6.0
HIGHLIGHT_X1 = 42.0

# Known face anchors — keep in sync with TeoCalc.Rendering.CalcChassisPalette.
FACE_ANCHORS = {
    "gold": "#E8B040",   # KeyCapGoldFace / KeyOrangeTop (232, 176, 64)
    "grey": "#B8B4AA",   # KeyCapGreyFace / KeyGreyFace (184, 180, 170)
    "blue": "#7EBFE7",   # KeyCapBlueFace (126, 191, 231) — B01 g-key dominant blue
}

# Per-style RGB ratios vs face (gold from B01 hand-tune; grey/blue tuned separately).
_STYLE_RATIOS: dict[str, dict[str, tuple[float, float, float] | None]] = {
    "gold": {
        "Skirt": (0.692, 0.631, 0.533),
        "Lip": (0.705, 0.682, 0.640),
        "Highlight": (1.066, 1.227, 1.840),
    },
    "grey": {
        # Lighter neutral skirt — gold ratios read too brown/dark on warm grey.
        "Skirt": (0.88, 0.88, 0.86),
        "Lip": (0.94, 0.94, 0.92),
        "Highlight": None,  # KeyCapGreyHighlight / KeyGreyTop
    },
    "blue": {
        # Skirt/lip in the blue family; top glint is KeyCapBlueHighlight / KeyBlueTop.
        "Skirt": (0.78, 0.82, 0.86),
        "Lip": (0.84, 0.88, 0.92),
        "Highlight": None,
    },
}

# Known highlights — lighter face-family tones (when Highlight ratio is None).
KNOWN_HIGHLIGHTS = {
    "grey": "#CCC8BC",  # KeyCapGreyHighlight / KeyGreyTop (204, 200, 188)
    "blue": "#94D7F8",  # KeyCapBlueHighlight (148, 215, 248) — lighter KeyCapBlueFace
}

# Black cap — face not yet palette-backed; tones stay hand-picked.
BLACK_CAP = {
    "Face": "#2C2F31",
    "Skirt": "#141618",
    "Lip": "#24282C",
    "Highlight": "#4A5056",
}


def _hex_rgb(hex_color: str) -> tuple[int, int, int]:
    h = hex_color.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def _rgb_hex(r: int, g: int, b: int) -> str:
    return f"#{r:02X}{g:02X}{b:02X}"


def _scale_rgb(rgb: tuple[int, int, int], ratios: tuple[float, float, float]) -> str:
    r, g, b = rgb
    rr, gr, br = ratios
    return _rgb_hex(
        min(255, round(r * rr)),
        min(255, round(g * gr)),
        min(255, round(b * br)),
    )


def cap_palette(style: str, face_hex: str) -> dict[str, str]:
    rgb = _hex_rgb(face_hex)
    ratios = _STYLE_RATIOS[style]
    face = face_hex.upper()
    hl = ratios["Highlight"]
    if hl is None:
        highlight = KNOWN_HIGHLIGHTS[style]
    else:
        highlight = _scale_rgb(rgb, hl)
    return {
        "Face": face,
        "Skirt": _scale_rgb(rgb, ratios["Skirt"]),
        "Lip": _scale_rgb(rgb, ratios["Lip"]),
        "Highlight": highlight,
    }


STYLES = {
    name: cap_palette(name, face) for name, face in FACE_ANCHORS.items()
}
STYLES["black"] = BLACK_CAP


def _frame_path() -> str:
    """Evenodd ring: FRAME_W thick on all four sides."""
    r, ir = OUTER_R, INNER_R
    return (
        f"M{r},0H{W - r}A{r},{r},0,0,1,{W},{r}V{H - r}"
        f"A{r},{r},0,0,1,{W - r},{H}H{r}A{r},{r},0,0,1,0,{H - r}V{r}"
        f"A{r},{r},0,0,1,{r},0Z"
        f"M{r},{INNER_Y}H{W - r}A{ir},{ir},0,0,1,{INNER_RIGHT},{OUTER_R}"
        f"V{H - OUTER_R}A{ir},{ir},0,0,1,{W - r},{INNER_BOTTOM}"
        f"H{r}A{ir},{ir},0,0,1,{INNER_X},{H - OUTER_R}V{OUTER_R}"
        f"A{ir},{ir},0,0,1,{r},{INNER_Y}Z"
    )


def _face_path() -> str:
    """Main cap — outer edge matches frame inner contour (top + stiles)."""
    y = FACE_BOTTOM_Y
    b = FACE_BLEED
    ir = INNER_R + b * 0.35
    r = OUTER_R
    return (
        f"M{r - b},{INNER_Y - b}H{W - r + b}"
        f"A{ir},{ir},0,0,1,{INNER_RIGHT + b},{r - b}"
        f"V{y}H{INNER_X - b}V{r - b}"
        f"A{ir},{ir},0,0,1,{r - b},{INNER_Y - b}Z"
    )


def _face_markup(face: str) -> str:
    """Fill + same-color stroke under the frame to kill inner-corner halos."""
    sw = FACE_BLEED * 2
    return (
        f'    <path id="key-face" fill="{face}" stroke="{face}" '
        f'stroke-width="{sw}" stroke-linejoin="round" d="{_face_path()}"/>'
    )


def _skirt_path() -> str:
    """Dark lower band — side stiles bleed under frame like the face."""
    y = FACE_BOTTOM_Y
    b = FACE_BLEED
    return (
        f"M{INNER_X - b},{y}H{INNER_RIGHT + b}"
        f"V34"
        f"Q{INNER_RIGHT + b},{INNER_BOTTOM},{W - OUTER_R},{INNER_BOTTOM}"
        f"H{OUTER_R}"
        f"Q{INNER_X - b},{INNER_BOTTOM},{INNER_X - b},34Z"
    )


def _skirt_markup(skirt: str) -> str:
    sw = FACE_BLEED * 2
    return (
        f'    <path id="key-skirt" fill="{skirt}" stroke="{skirt}" '
        f'stroke-width="{sw}" stroke-linejoin="round" d="{_skirt_path()}"/>'
    )


# Lip inverted-U — bottoms on flat frame base; top narrower than bottom (∩).
LIP_TOP_Y = FACE_BOTTOM_Y + TOP_MARGIN
LIP_BOTTOM_INSET = 2.0
LIP_TOP_INSET = 1.3
LIP_LEG_OUTWARD = 1.4
LIP_BOTTOM_LEFT = (OUTER_R + LIP_BOTTOM_INSET - LIP_LEG_OUTWARD, INNER_BOTTOM)
LIP_BOTTOM_RIGHT = (W - OUTER_R - LIP_BOTTOM_INSET + LIP_LEG_OUTWARD, INNER_BOTTOM)


def _lip_path() -> str:
    """Smooth ∩ (narrow top, wide bottom); cubics bow inward, not outward."""
    blx, bly = LIP_BOTTOM_LEFT
    brx, bry = LIP_BOTTOM_RIGHT
    ty = LIP_TOP_Y
    tlx = OUTER_R + LIP_TOP_INSET
    trx = W - OUTER_R - LIP_TOP_INSET
    span = trx - tlx
    third = span / 3
    return (
        f"M{blx},{bly}"
        f"C{blx + 0.4},{bly - 2.2},{tlx - 0.6},{ty},{tlx},{ty}"
        f"C{tlx + third},{ty},{trx - third},{ty},{trx},{ty}"
        f"C{trx + 0.6},{ty},{brx - 0.4},{bry - 2.2},{brx},{bry}"
    )


def _lip_markup(lip: str) -> str:
    return (
        f'    <path id="key-lip" fill="none" stroke="{lip}" '
        f'stroke-width="{K1}" stroke-linecap="round" stroke-linejoin="round" '
        f'd="{_lip_path()}"/>'
    )


def _highlight_path() -> str:
    """Flat K1 glint — parallel to the inverted-U top span."""
    return f"M{HIGHLIGHT_X0},{HIGHLIGHT_Y}H{HIGHLIGHT_X1}"


def build_svg(face: str, skirt: str, lip: str, highlight: str) -> str:
    return f"""<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" id="hp-key-cap">
  <g id="key-cap">
{_face_markup(face)}
{_skirt_markup(skirt)}
{_lip_markup(lip)}
    <path id="key-highlight" fill="none" stroke="{highlight}" stroke-width="{K1}" stroke-linecap="round" d="{_highlight_path()}"/>
    <path id="outer-frame" fill="{FRAME}" fill-rule="evenodd" d="{_frame_path()}"/>
  </g>
</svg>
"""


def write(path: Path, colors: dict[str, str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    svg = build_svg(colors["Face"], colors["Skirt"], colors["Lip"], colors["Highlight"])
    path.write_text(svg, encoding="utf-8")
    print(
        f"Wrote {path}  Face={colors['Face']}  Skirt={colors['Skirt']}  "
        f"Lip={colors['Lip']}  Highlight={colors['Highlight']}"
    )


def main() -> None:
    arg = sys.argv[1].lower() if len(sys.argv) > 1 else "gold"
    if arg in ("all", "*"):
        for name, colors in STYLES.items():
            out = OUT if name == "gold" else KEYS / f"KeyCap-{name}.svg"
            write(out, colors)
        return

    if arg not in STYLES:
        print(f"Unknown style {arg!r}. Choose: {', '.join(STYLES)} or all")
        sys.exit(1)

    out = OUT if arg == "gold" else KEYS / f"KeyCap-{arg}.svg"
    write(out, STYLES[arg])


if __name__ == "__main__":
    main()
