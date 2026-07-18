"""Build calc-body-shell.png from prototype; layout width narrowed for 5x7 keypad."""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[4]
DRAFT = ROOT / "Catalog/Documents/calc-body-layout-draft.png"
OUT_PNG = ROOT / "Resource/Engine/Shared/Assets/calc-body-shell.png"
OUT_JSON = ROOT / "Resource/Engine/Shared/Assets/calc-body-shell-layout.json"

KEY_COLS = 5
KEY_ROWS = 7
KEY_CELL = 62
KEY_GAP = 10
KEY_PAD = 22
SIDE_MARGIN = 16


def crop_calculator(rgb: np.ndarray) -> np.ndarray:
    col_dark = (rgb.max(axis=2) < 140).sum(axis=0)
    cols = np.where(col_dark > 200)[0]
    x0, x1 = int(cols[0]), int(cols[-1])
    sub = rgb[:, x0 : x1 + 1]
    mask = sub.max(axis=2) < 140
    ys, _ = np.where(mask)
    y0, y1 = int(ys.min()), int(ys.max())
    return rgb[y0 : y1 + 1, x0 : x1 + 1].copy()


def target_layout_width() -> int:
    return KEY_PAD * 2 + KEY_COLS * KEY_CELL + (KEY_COLS - 1) * KEY_GAP + SIDE_MARGIN * 2


def sample_faceplate(crop: np.ndarray) -> tuple[int, int, int]:
    h, w = crop.shape[:2]
    y = min(int(h * 0.42), h - 1)
    row = crop[y, int(w * 0.15) : int(w * 0.85)]
    if row.ndim == 1:
        return tuple(int(x) for x in row)
    dark = row[row.max(axis=1) < 125]
    if len(dark):
        return tuple(int(x) for x in dark.mean(axis=0))
    return tuple(int(x) for x in crop[y, w // 2])


def sample_display_glass(crop: np.ndarray, display: dict) -> tuple[int, int, int]:
    x0, y0 = display["X"], display["Y"]
    glass = crop[y0 : y0 + display["Height"], x0 : x0 + display["Width"]]
    inner = glass[6:-6, 6:-6]
    dark = inner[inner.max(axis=2) < 110]
    if len(dark):
        return tuple(int(x) for x in dark.mean(axis=0))
    return 42, 12, 16


def sample_brushed_metal(crop: np.ndarray, logo: dict) -> tuple[int, int, int]:
    x0, y0 = logo["X"], logo["Y"]
    strip = crop[y0 : y0 + logo["Height"], x0 : x0 + int(logo["Width"] * 0.26)]
    return tuple(int(x) for x in strip.mean(axis=(0, 1)))


def flatten_keypad(crop: np.ndarray, layout: dict, panel: tuple[int, int, int]) -> None:
    kp = layout["KeypadSlot"]
    crop[kp["Y"] : kp["Y"] + kp["Height"], kp["X"] : kp["X"] + kp["Width"]] = panel


def clean_logo_strip(crop: np.ndarray, layout: dict, metal: tuple[int, int, int]) -> None:
    logo = layout["LogoSlot"]
    ly0, lx0, lw, lh = logo["Y"], logo["X"], logo["Width"], logo["Height"]
    wipe_x = lx0 + int(lw * 0.28)
    crop[ly0 : ly0 + lh, wipe_x : lx0 + lw] = metal


def measure_slots(crop: np.ndarray) -> dict:
    h, w = crop.shape[:2]

    def row_mean(y: int) -> float:
        return float(crop[y, 16 : w - 16].mean())

    display_y0, display_y1 = int(h * 0.055), int(h * 0.13)
    row_scores = [row_mean(y) for y in range(display_y0, display_y1)]
    glass_y = display_y0 + int(np.argmin(row_scores))
    glass_rows = crop[glass_y : glass_y + 40, 16 : w - 16]
    dark = glass_rows.max(axis=2) < 95
    ys, xs = np.where(dark)
    if len(xs) == 0:
        display = {"X": 16, "Y": 58, "Width": w - 32, "Height": 68}
        glass_inset = {"Left": 7, "Top": 10, "Right": 7, "Bottom": 8}
    else:
        gx0, gx1 = int(xs.min()) + 16, int(xs.max()) + 16
        gy0, gy1 = int(ys.min()) + glass_y, int(ys.max()) + glass_y
        pad = 5
        display = {
            "X": max(16, gx0 - pad),
            "Y": max(16, gy0 - pad),
            "Width": min(w - 32, gx1 - gx0 + pad * 2),
            "Height": min(int(h * 0.12), gy1 - gy0 + pad * 2),
        }
        glass_inset = {
            "Left": gx0 - display["X"],
            "Top": gy0 - display["Y"],
            "Right": display["X"] + display["Width"] - gx1,
            "Bottom": display["Y"] + display["Height"] - gy1,
        }

    switch_y0, switch_y1 = int(h * 0.13), int(h * 0.20)
    switch_row = switch_y0 + (switch_y1 - switch_y0) // 2
    switches = {"X": 16, "Y": switch_y0, "Width": w - 32, "Height": switch_y1 - switch_y0}

    keypad_y0 = switch_y1 + 4
    logo_y0 = int(h * 0.84)
    keypad = {"X": 16, "Y": keypad_y0, "Width": w - 32, "Height": logo_y0 - keypad_y0 - 4}
    logo = {"X": 16, "Y": logo_y0, "Width": w - 32, "Height": h - logo_y0 - 8}

    return {
        "Format": "CalcBody.ShellLayout",
        "SchemaVersion": 5,
        "ShellPixelWidth": w,
        "ShellPixelHeight": h,
        "ReferenceWidth": w,
        "ReferenceHeight": h,
        "ReferenceKeypadRows": KEY_ROWS,
        "ReferenceKeypadCols": KEY_COLS,
        "DisplaySlot": display,
        "DisplayGlassInset": glass_inset,
        "SwitchSlot": switches,
        "SwitchRowY": switch_row,
        "SwitchLeftNorm": 0.248,
        "SwitchRightNorm": 0.752,
        "KeypadSlot": keypad,
        "LogoSlot": logo,
        "BrandTextLeftNorm": 0.34,
        "KeyGrid": {
            "PadX": KEY_PAD,
            "PadY": KEY_PAD,
            "GapX": KEY_GAP,
            "GapY": KEY_GAP,
            "CapInset": 0.22,
            "RowCounts": {
                "Classic": 7,
                "Woodstock": 7,
                "HP01": 4,
                "HP19C": 6,
            },
        },
    }


def scale_slot(slot: dict, scale: float) -> dict:
    return {
        "X": round(slot["X"] * scale, 1),
        "Y": slot["Y"],
        "Width": round(slot["Width"] * scale, 1),
        "Height": slot["Height"],
    }


def narrow_layout(layout: dict, target_w: int) -> dict:
    src_w = layout["ShellPixelWidth"]
    if src_w <= target_w:
        layout["ReferenceWidth"] = target_w
        return layout
    s = target_w / src_w
    layout["ReferenceWidth"] = target_w
    for name in ("DisplaySlot", "SwitchSlot", "KeypadSlot", "LogoSlot"):
        layout[name] = scale_slot(layout[name], s)
    layout["BrandTextLeftNorm"] = round(layout["BrandTextLeftNorm"] * s * (src_w / target_w), 3)
    return layout


def main() -> None:
    rgb = np.array(Image.open(DRAFT).convert("RGB"))
    crop = crop_calculator(rgb)
    layout = measure_slots(crop)
    panel = sample_faceplate(crop)
    metal = sample_brushed_metal(crop, layout["LogoSlot"])

    flatten_keypad(crop, layout, panel)
    clean_logo_strip(crop, layout, metal)

    glass = sample_display_glass(crop, layout["DisplaySlot"])
    layout["DisplayGlassRgb"] = list(glass)

    layout = narrow_layout(layout, target_layout_width())

    OUT_PNG.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(crop).save(OUT_PNG)
    OUT_JSON.write_text(json.dumps(layout, indent=2) + "\n", encoding="utf-8")
    print(f"Shell -> {OUT_PNG} ({layout['ShellPixelWidth']}x{layout['ShellPixelHeight']})")
    print(f"Layout ref {layout['ReferenceWidth']}x{layout['ReferenceHeight']}")
    print(f"DisplayGlass #{glass[0]:02X}{glass[1]:02X}{glass[2]:02X}")


if __name__ == "__main__":
    main()
