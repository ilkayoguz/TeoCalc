"""Build hp-logo.svg — badge variants (hp-logo, E1, E2)."""
from __future__ import annotations

import sys
from pathlib import Path

from svgpathtools import parse_path

ASSETS = Path(r"D:\$Board\Works\Side.Codes\TeoCalc\Resource\Engine\HP-65\Assets")
OUT = ASSETS / "hp-logo.svg"
E1_OUT = ASSETS / "E1.svg"
E2_OUT = ASSETS / "E2.svg"
E3_OUT = ASSETS / "E3.svg"
E4_OUT = ASSETS / "E4.svg"
HP_LOGO_OUT = ASSETS / "HpLogo.svg"

# Color groups — change here; each SVG element references its group explicitly.
COLORS = {
    "Frame": "#FFFFFF",
    "Circle": "#FFFFFF",
    "Text": "#4B5053",  # D03 keypad-panel (matches LeftPanel)
    "LeftPanel": "#4B5053",
    "RightPanel": "#005699",
}

# Legacy aliases for hp-logo / E1 builders.
FRAME = "#FFD700"
FILL = COLORS["Circle"]
HP_MARK = "#1C2025"
HP_BLUE = COLORS["RightPanel"]
KEYPAD_PANEL = COLORS["LeftPanel"]
# Badge art size inside the frame (matches legacy HP-65 brand plate proportions).
OUT_W = 856
OUT_H = int(round(4962 * OUT_W / 8020))  # 529
FRAME_PAD = int(round(150 * OUT_W / 8020))  # 16
FRAME_RX = int(round(570 * OUT_W / 8020))  # 61
FRAME_INNER_RX = max(1, FRAME_RX - FRAME_PAD)  # 45

HP_MAIN_D = (
    "M25.205078,2.0078125L20.150391,16L23.394531,16C25.514531,16,26.657641,17.631953,"
    "25.931641,19.626953L20.492188,34.001953L16.574219,34L22.150391,19L18.964844,19L13.388672,"
    "34L9.1503906,34L20.798828,2.3945312C20.327828,2.4815313,19.860391,2.5793125,19.400391,"
    "2.6953125C9.4163906,5.2023125,2.0019531,14.250953,2.0019531,25.001953C2.0019531,35.382953,"
    "8.9149062,44.174391,18.378906,47.025391C18.699906,47.122391,19.024562,47.215828,19.351562,"
    "47.298828L20.042969,45.386719L24.068359,34.257812L24.070312,34.257812L30.589844,16L38.392578,"
    "16C40.514578,16,41.656641,17.631953,40.931641,19.626953L36.183594,32.314453C35.845594,33.241453,"
    "34.762391,34,33.775391,34L28.150391,34L23.826172,45.941406L23.111328,47.917969C23.403328,"
    "47.941969,23.695234,47.964562,23.990234,47.976562C24.326234,47.991563,24.662953,48.001953,"
    "25.001953,48.001953C37.683953,48.001953,48.001953,37.684953,48.001953,25.001953C48.000953,"
    "12.609953,38.148187,2.4814375,25.867188,2.0234375C25.647188,2.0154375,25.426078,2.0098125,"
    "25.205078,2.0078125z"
)
HP_P_ACCENT_D = "M33.964844,19L29.455078,31L32.640625,31L37.150391,19L33.964844,19z"
HP_MARK_D = HP_MAIN_D + HP_P_ACCENT_D
HP_MARK_SRC = 50  # source coords for HP path / letter mask
WING_HOLE_INSET = 2.5  # keep panel fill outside the white disc edge


def _letter_mask_def() -> str:
    return (
        f'<mask id="letter-mask">\n'
        f'<rect width="{HP_MARK_SRC}" height="{HP_MARK_SRC}" fill="white"/>\n'
        f'<path fill="black" fill-rule="evenodd" d="{HP_MARK_D}"/>\n'
        f"</mask>\n"
    )

def _ring_d(x0: int, y0: int, x1: int, y1: int, ix0: int, iy0: int, ix1: int, iy1: int, r: int) -> str:
    ri = FRAME_INNER_RX
    return (
        f"M{x0 + r},{y0}H{x1 - r}A{r},{r} 0 0 1 {x1},{y0 + r}V{y1 - r}"
        f"A{r},{r} 0 0 1 {x1 - r},{y1}H{x0 + r}A{r},{r} 0 0 1 {x0},{y1 - r}"
        f"V{y0 + r}A{r},{r} 0 0 1 {x0 + r},{y0}Z"
        f"M{ix0 + ri},{iy0}H{ix1 - ri}A{ri},{ri} 0 0 1 {ix1},{iy0 + ri}V{iy1 - ri}"
        f"A{ri},{ri} 0 0 1 {ix1 - ri},{iy1}H{ix0 + ri}A{ri},{ri} 0 0 1 {ix0},{iy1 - ri}"
        f"V{iy0 + ri}A{ri},{ri} 0 0 1 {ix0 + ri},{iy0}Z"
    )


def _inner_art_d(x0: int, y0: int, x1: int, y1: int) -> str:
    """Inner rounded rect in logo-art coordinates."""
    ri = FRAME_INNER_RX
    return (
        f"M{x0 + ri},{y0}H{x1 - ri}A{ri},{ri} 0 0 1 {x1},{y0 + ri}V{y1 - ri}"
        f"A{ri},{ri} 0 0 1 {x1 - ri},{y1}H{x0 + ri}A{ri},{ri} 0 0 1 {x0},{y1 - ri}"
        f"V{y0 + ri}A{ri},{ri} 0 0 1 {x0 + ri},{y0}Z"
    )


def _inner_fill_d(ix0: int, iy0: int, ix1: int, iy1: int) -> str:
    return _inner_art_d(ix0, iy0, ix1, iy1)


def _circle_d(cx: float, cy: float, r: float) -> str:
    return (
        f"M{cx:.2f},{cy - r:.2f}A{r:.2f},{r:.2f} 0 1 0 {cx:.2f},{cy + r:.2f}"
        f"A{r:.2f},{r:.2f} 0 1 0 {cx:.2f},{cy - r:.2f}Z"
    )


def _hp_mark_placement(inner_w: int, inner_h: int) -> tuple[str, float, float, float]:
    """Return transform string and circle center/radius in logo-art space."""
    xmin, xmax, ymin, ymax = parse_path(HP_MARK_D).bbox()
    mark_w = xmax - xmin
    mark_h = ymax - ymin
    fit_h = inner_h - 1  # fully inside y = 0 .. inner_h-1 (no 1px overflow)
    scale = fit_h / mark_h
    tx = (inner_w - mark_w * scale) / 2 - xmin * scale
    ty = -ymin * scale
    cx = tx + (xmin + xmax) * scale / 2
    cy = ty + (ymin + ymax) * scale / 2
    r = min(mark_w, mark_h) * scale / 2
    xform = f"translate({tx:.4f},{ty:.4f}) scale({scale:.6f})"
    return xform, cx, cy, r


def _hp_mark_transform(inner_w: int, inner_h: int) -> str:
    return _hp_mark_placement(inner_w, inner_h)[0]


def _wing_ring_d(
    cx: float,
    cy: float,
    r: float,
    x0: int,
    y0: int,
    x1: int,
    y1: int,
    hole_inset: float = 0,
) -> str:
    """Frame inner area minus HP circle (evenodd)."""
    frame = _inner_art_d(x0, y0, x1, y1)
    hole_r = max(1.0, r - hole_inset)
    circle = _circle_d(cx, cy, hole_r)
    return frame + circle


def build(out: Path = OUT) -> tuple[int, int]:
    vw = OUT_W + 2 * FRAME_PAD
    vh = OUT_H + 2 * FRAME_PAD
    ox, oy = FRAME_PAD, FRAME_PAD
    ix1, iy1 = ox + OUT_W - 1, oy + OUT_H - 1
    frame = _ring_d(0, 0, vw - 1, vh - 1, ox, oy, ix1, iy1, FRAME_RX)
    inner = _inner_fill_d(ox, oy, ix1, iy1)
    mark_xform = _hp_mark_transform(OUT_W, OUT_H)

    svg = (
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {vw} {vh}" '
        f'width="{vw}" height="{vh}" id="hp-logo-badge">\n'
        f'<g id="hp-logo">\n'
        f'<path id="logo-frame" fill="{FRAME}" fill-rule="evenodd" d="{frame}"/>\n'
        f'<path id="logo-fill" fill="{FILL}" d="{inner}"/>\n'
        f'<g id="logo-art" transform="translate({ox},{oy})">\n'
        f'<g id="hp-mark" fill="{HP_MARK}" transform="{mark_xform}">\n'
        f'<path d="{HP_MARK_D}"/>\n'
        f"</g>\n</g>\n</g>\n</svg>\n"
    )

    out.write_text(svg, encoding="utf-8")
    print(f"viewBox {vw}x{vh}  inner {OUT_W}x{OUT_H}  frame rx {FRAME_RX}")
    print(f"hp-mark transform: {mark_xform}")
    print(f"Wrote {out} ({out.stat().st_size} bytes)")
    return vw, vh


def _badge_geometry() -> tuple[int, int, int, int, str, str, float, float, float, str, str]:
    vw = OUT_W + 2 * FRAME_PAD
    vh = OUT_H + 2 * FRAME_PAD
    ox, oy = FRAME_PAD, FRAME_PAD
    ix1, iy1 = ox + OUT_W - 1, oy + OUT_H - 1
    frame = _ring_d(0, 0, vw - 1, vh - 1, ox, oy, ix1, iy1, FRAME_RX)
    mark_xform, cx, cy, r = _hp_mark_placement(OUT_W, OUT_H)
    wing = _wing_ring_d(
        cx, cy, r, 0, 0, OUT_W - 1, OUT_H - 1, hole_inset=WING_HOLE_INSET
    )
    disc = _circle_d(cx, cy, r)
    return vw, vh, ox, oy, frame, mark_xform, cx, cy, r, wing, disc


def build_e2(out: Path = E2_OUT) -> tuple[int, int]:
    """Panels + single disc knockout; Text color matches LeftPanel."""
    c = COLORS
    vw, vh, ox, oy, frame, mark_xform, cx, cy, r, wing, disc = _badge_geometry()
    inner = _inner_fill_d(ox, oy, ox + OUT_W - 1, oy + OUT_H - 1)

    svg = (
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {vw} {vh}" '
        f'width="{vw}" height="{vh}" id="hp-logo-e2">\n'
        f'<g id="hp-logo">\n'
        f"<defs>\n"
        f'<clipPath id="clip-left-panel"><rect x="0" y="0" width="{cx:.2f}" '
        f'height="{OUT_H}"/></clipPath>\n'
        f'<clipPath id="clip-right-panel"><rect x="{cx:.2f}" y="0" '
        f'width="{OUT_W - cx:.2f}" height="{OUT_H}"/></clipPath>\n'
        f'<clipPath id="clip-disc"><path d="{disc}"/></clipPath>\n'
        f"</defs>\n"
        f'<path id="frame" fill="{c["Frame"]}" fill-rule="evenodd" d="{frame}"/>\n'
        f'<path id="background" fill="{c["Circle"]}" d="{inner}"/>\n'
        f'<g id="logo-art" transform="translate({ox},{oy})">\n'
        f'<path id="left-panel" fill="{c["LeftPanel"]}" fill-rule="evenodd" '
        f'clip-path="url(#clip-left-panel)" d="{wing}"/>\n'
        f'<path id="right-panel" fill="{c["RightPanel"]}" fill-rule="evenodd" '
        f'clip-path="url(#clip-right-panel)" d="{wing}"/>\n'
        f'<g id="hp-disc" clip-path="url(#clip-disc)">\n'
        f'<rect id="letter-fill" fill="{c["Text"]}" x="0" y="0" width="{OUT_W}" '
        f'height="{OUT_H}"/>\n'
        f'<g id="hp-mark" transform="{mark_xform}">\n'
        f'<path id="circle-mask" fill="{c["Circle"]}" fill-rule="evenodd" '
        f'd="{HP_MARK_D}"/>\n'
        f"</g></g>\n"
        f"</g>\n</g>\n</svg>\n"
    )

    out.write_text(svg, encoding="utf-8")
    print("E2 colors:", ", ".join(f"{k}={v}" for k, v in c.items()))
    print(f"Wrote {out} ({out.stat().st_size} bytes)")
    return vw, vh


def build_e3(out: Path = E3_OUT) -> tuple[int, int]:
    """Minimal E2 — same pixels, fewer elements (wing <use>, no background layer)."""
    c = COLORS
    vw, vh, ox, oy, frame, mark_xform, cx, cy, _r, wing, disc = _badge_geometry()

    svg = (
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {vw} {vh}">\n'
        f"<defs>"
        f'<path id="wing" fill-rule="evenodd" d="{wing}"/>'
        f'<clipPath id="cl"><rect width="{cx:.2f}" height="{OUT_H}"/></clipPath>'
        f'<clipPath id="cr"><rect x="{cx:.2f}" width="{OUT_W - cx:.2f}" height="{OUT_H}"/>'
        f"</clipPath>"
        f'<clipPath id="cd"><path d="{disc}"/></clipPath>'
        f"</defs>"
        f'<path fill="{c["Frame"]}" fill-rule="evenodd" d="{frame}"/>'
        f'<g transform="translate({ox},{oy})">'
        f'<use href="#wing" fill="{c["LeftPanel"]}" clip-path="url(#cl)"/>'
        f'<use href="#wing" fill="{c["RightPanel"]}" clip-path="url(#cr)"/>'
        f'<g clip-path="url(#cd)">'
        f'<rect fill="{c["Text"]}" width="{OUT_W}" height="{OUT_H}"/>'
        f'<path fill="{c["Circle"]}" fill-rule="evenodd" transform="{mark_xform}" '
        f'd="{HP_MARK_D}"/>'
        f"</g></g></svg>\n"
    )

    out.write_text(svg, encoding="utf-8")
    e2_bytes = E2_OUT.stat().st_size if E2_OUT.exists() else 0
    print(f"E3 {out.stat().st_size} bytes", end="")
    if e2_bytes:
        print(f"  (E2 {e2_bytes} bytes, -{e2_bytes - out.stat().st_size})")
    else:
        print()
    return vw, vh


def build_e4(out: Path = E4_OUT) -> tuple[int, int]:
    """E3 layout; circle under panels; masked letters + white path cap on logo solids."""
    c = COLORS
    vw, vh, ox, oy, frame, mark_xform, cx, cy, _r, wing, disc = _badge_geometry()

    svg = (
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {vw} {vh}" '
        f'width="{vw}" height="{vh}" id="hp-logo-e4" shape-rendering="geometricPrecision">\n'
        f"<defs>\n"
        f'<path id="wing" fill-rule="evenodd" d="{wing}"/>\n'
        f"{_letter_mask_def()}"
        f'<clipPath id="clip-left-panel"><rect width="{cx:.2f}" height="{OUT_H}"/>'
        f"</clipPath>\n"
        f'<clipPath id="clip-right-panel"><rect x="{cx:.2f}" '
        f'width="{OUT_W - cx:.2f}" height="{OUT_H}"/></clipPath>\n'
        f'<clipPath id="clip-disc"><path d="{disc}"/></clipPath>\n'
        f"</defs>\n"
        f'<path id="frame" fill="{c["Frame"]}" fill-rule="evenodd" d="{frame}"/>\n'
        f'<g id="logo-art" transform="translate({ox},{oy})">\n'
        f'<path id="circle" fill="{c["Circle"]}" d="{disc}"/>\n'
        f'<use id="left-panel" href="#wing" fill="{c["LeftPanel"]}" '
        f'clip-path="url(#clip-left-panel)"/>\n'
        f'<use id="right-panel" href="#wing" fill="{c["RightPanel"]}" '
        f'clip-path="url(#clip-right-panel)"/>\n'
        f'<g id="disc" clip-path="url(#clip-disc)">\n'
        f'<g id="letters" transform="{mark_xform}">\n'
        f'<rect fill="{c["Text"]}" width="{HP_MARK_SRC}" height="{HP_MARK_SRC}" '
        f'mask="url(#letter-mask)"/>\n'
        f"</g>\n"
        f'<g id="logo-cap" transform="{mark_xform}">\n'
        f'<path fill="{c["Circle"]}" fill-rule="evenodd" d="{HP_MARK_D}"/>\n'
        f"</g></g>\n"
        f"</g>\n</svg>\n"
    )

    out.write_text(svg, encoding="utf-8")
    print("E4: circle-first panels, inset wing hole, letter mask + white logo cap")
    print(f"Wrote {out} ({out.stat().st_size} bytes)")
    return vw, vh


def build_hp_logo(out: Path = HP_LOGO_OUT) -> tuple[int, int]:
    """Publish approved E4 artwork as HpLogo.svg for runtime overlay."""
    vw, vh = build_e4(out=E4_OUT)
    svg = E4_OUT.read_text(encoding="utf-8").replace('id="hp-logo-e4"', 'id="hp-logo"')
    out.write_text(svg, encoding="utf-8")
    print(f"Wrote {out} ({out.stat().st_size} bytes)")
    return vw, vh


def build_e1(out: Path = E1_OUT) -> tuple[int, int]:
    """Yellow frame, white interior, HP-blue right wing between frame and circle."""
    vw = OUT_W + 2 * FRAME_PAD
    vh = OUT_H + 2 * FRAME_PAD
    ox, oy = FRAME_PAD, FRAME_PAD
    ix1, iy1 = ox + OUT_W - 1, oy + OUT_H - 1
    frame = _ring_d(0, 0, vw - 1, vh - 1, ox, oy, ix1, iy1, FRAME_RX)
    inner = _inner_fill_d(ox, oy, ix1, iy1)
    mark_xform, cx, cy, r = _hp_mark_placement(OUT_W, OUT_H)
    wing = _wing_ring_d(cx, cy, r, 0, 0, OUT_W - 1, OUT_H - 1)
    clip_x = cx

    svg = (
        '<?xml version="1.0" encoding="UTF-8"?>\n'
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {vw} {vh}" '
        f'width="{vw}" height="{vh}" id="hp-logo-e1">\n'
        f'<g id="hp-logo">\n'
        f'<defs><clipPath id="e1-right-wing"><rect x="{clip_x:.2f}" y="0" '
        f'width="{OUT_W - clip_x:.2f}" height="{OUT_H}"/></clipPath></defs>\n'
        f'<path id="logo-frame" fill="{FRAME}" fill-rule="evenodd" d="{frame}"/>\n'
        f'<path id="logo-fill" fill="{FILL}" d="{inner}"/>\n'
        f'<g id="logo-art" transform="translate({ox},{oy})">\n'
        f'<path id="right-wing" fill="{HP_BLUE}" fill-rule="evenodd" clip-path="url(#e1-right-wing)" '
        f'd="{wing}"/>\n'
        f'<g id="hp-mark" fill="{HP_MARK}" transform="{mark_xform}">\n'
        f'<path d="{HP_MARK_D}"/>\n'
        f"</g>\n</g>\n</g>\n</svg>\n"
    )

    out.write_text(svg, encoding="utf-8")
    print(f"E1 viewBox {vw}x{vh}  circle center ({cx:.1f},{cy:.1f}) r={r:.1f}")
    print(f"Wrote {out} ({out.stat().st_size} bytes)")
    return vw, vh


if __name__ == "__main__":
    arg = sys.argv[1].lower() if len(sys.argv) > 1 else ""
    if arg == "e1":
        build_e1()
    elif arg == "e2":
        build_e2()
    elif arg == "e3":
        build_e3()
    elif arg == "e4":
        build_e4()
    elif arg in ("hp", "hplogo", "HpLogo".lower()):
        build_hp_logo()
    elif len(sys.argv) > 1:
        build(out=Path(sys.argv[1]))
    else:
        build()
