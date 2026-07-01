"""HP-65 faceplate white/silver gutter frame drawing (C7–C9)."""
from __future__ import annotations

import importlib.util
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

SCRIPTS = Path(__file__).resolve().parent


def load_layout():
    spec = importlib.util.spec_from_file_location(
        "hp65_faceplate_layout", SCRIPTS / "hp65_faceplate_layout.py"
    )
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def content_top_y(arr: np.ndarray, layout) -> int:
    chassis = layout.CHASSIS_RGB
    x0 = layout.FACE_X
    x1 = layout.FACE_X + layout.FACE_W - 1
    for y in range(layout.DISPLAY_Y - 1, layout.FRAME_SEARCH_TOP_Y - 1, -1):
        row = arr[y, x0 : x1 + 1, :3]
        non_chassis = np.any(row != chassis, axis=1)
        if int(non_chassis.sum()) < layout.FACE_W // 4:
            continue
        top = y
        while top > layout.FRAME_SEARCH_TOP_Y:
            probe = tuple(arr[top - 1, layout.FACE_X + layout.FACE_W // 2, :3])
            if probe == chassis:
                break
            top -= 1
        return top
    return layout.DISPLAY_Y - 1


def white_frame_geometry(arr: np.ndarray, layout) -> dict[str, int]:
    k = layout.K1_THICKNESS
    fx0 = layout.FACE_X
    fx1 = layout.FACE_X + layout.FACE_W - 1
    fy0 = content_top_y(arr, layout)
    fy1 = layout.FOOTER_Y + layout.FOOTER_H - 1
    gx0 = fx0 - k
    gx1 = fx1 + 1
    return {
        "fx0": fx0,
        "fx1": fx1,
        "fy0": fy0,
        "fy1": fy1,
        "gx0": gx0,
        "gx1": gx1,
        "ty0": fy0 - k,
        "ty1": fy0 - 1,
        "by0": fy1 - k + 1,
        "by1": fy1,
        "wx0": gx0,
        "wx1": gx1 + k - 1,
        "wy0": fy0 - k,
        "wy1": fy1,
        "gap_m": fy0 - (fy0 - k),
        "outer_x0": gx0,
        "outer_y0": fy0 - k,
        "outer_x1": fx1 + k,
        "outer_y1": fy1,
    }


def chassis_bounds(arr: np.ndarray, layout) -> tuple[int, int, int, int]:
    """Outer olive chassis bbox on the faceplate asset."""
    ch = np.array(layout.CHASSIS_RGB, dtype=np.uint8)
    mask = (arr[:, :, :3] == ch).all(axis=2) & (arr[:, :, 3] > 200)
    ys, xs = np.where(mask)
    if ys.size == 0:
        cx0 = layout.CHASSIS_X
        cy0 = layout.CHASSIS_Y
        cx1 = layout.CHASSIS_X + layout.CHASSIS_W - 1
        cy1 = layout.CHASSIS_Y + layout.CHASSIS_H - 1
        return cx0, cy0, cx1, cy1
    return int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())


def outer_silver_frame_geometry(arr: np.ndarray, layout) -> dict[str, int]:
    """Outermost body chrome (B01 left/right edge), K1 thick on chassis perimeter."""
    k = layout.K1_THICKNESS
    cx0, cy0, cx1, cy1 = chassis_bounds(arr, layout)
    return {
        "hx0": cx0,
        "hx1": cx1,
        "ty0": cy0,
        "ty1": cy0 + k - 1,
        "by0": cy1 - k + 1,
        "by1": cy1,
        "vx0": cx0,
        "vx1": cx0 + k - 1,
        "rx0": cx1 - k + 1,
        "rx1": cx1,
        "vx_y0": cy0 + k,
        "vx_y1": cy1 - k,
        "cx0": cx0,
        "cy0": cy0,
        "cx1": cx1,
        "cy1": cy1,
    }


def fill_rect_rgb(
    arr: np.ndarray,
    x0: int,
    y0: int,
    x1: int,
    y1: int,
    rgb: tuple[int, int, int],
) -> None:
    arr[y0 : y1 + 1, x0 : x1 + 1, :3] = rgb
    arr[y0 : y1 + 1, x0 : x1 + 1, 3] = 255


def draw_white_frame_sharp(arr: np.ndarray, geom: dict[str, int], layout) -> None:
    k = layout.K1_THICKNESS
    white = layout.WHITE_RGB
    gx0, gx1 = geom["gx0"], geom["gx1"]
    fill_rect_rgb(arr, geom["fx0"], geom["ty0"], geom["fx1"], geom["ty1"], white)
    fill_rect_rgb(arr, geom["fx0"], geom["by0"], geom["fx1"], geom["by1"], white)
    fill_rect_rgb(arr, gx0, geom["fy0"], gx0 + k - 1, geom["fy1"], white)
    fill_rect_rgb(arr, gx1, geom["fy0"], gx1 + k - 1, geom["fy1"], white)
    fill_rect_rgb(arr, gx0, geom["ty0"], gx0 + k - 1, geom["ty1"], white)
    fill_rect_rgb(arr, gx1, geom["ty0"], gx1 + k - 1, geom["ty1"], white)


def draw_white_frame_rounded(img: Image.Image, geom: dict[str, int], layout) -> None:
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle(
        (geom["outer_x0"], geom["outer_y0"], geom["outer_x1"], geom["outer_y1"]),
        radius=layout.FRAME_CORNER_RADIUS,
        outline=layout.WHITE_RGB + (255,),
        width=layout.K1_THICKNESS,
    )


def draw_silver_frame_sharp(arr: np.ndarray, silver: dict[str, int], rgb: tuple[int, int, int]) -> None:
    hx0, hx1 = silver["hx0"], silver["hx1"]
    fill_rect_rgb(arr, hx0, silver["ty0"], hx1, silver["ty1"], rgb)
    fill_rect_rgb(arr, hx0, silver["by0"], hx1, silver["by1"], rgb)
    fill_rect_rgb(arr, silver["vx0"], silver["vx_y0"], silver["vx1"], silver["vx_y1"], rgb)
    fill_rect_rgb(arr, silver["rx0"], silver["vx_y0"], silver["rx1"], silver["vx_y1"], rgb)
    fill_rect_rgb(arr, silver["vx0"], silver["ty0"], silver["vx1"], silver["ty1"], rgb)
    fill_rect_rgb(arr, silver["rx0"], silver["ty0"], silver["rx1"], silver["ty1"], rgb)


def outer_silver_rgb_from_b01(b01: np.ndarray) -> tuple[int, int, int]:
    """Outermost left chrome silver on B01 (exclude black bezel)."""
    rgb = b01[:, :, :3].astype(int)
    samples: list[np.ndarray] = []
    for y in range(80, 820, 16):
        for x in range(5, 13):
            p = rgb[y, x]
            if p.mean() > 120 and p[0] > 100:
                samples.append(p)
    if not samples:
        return 176, 188, 194
    med = np.median(samples, axis=0).astype(int)
    return int(med[0]), int(med[1]), int(med[2])
