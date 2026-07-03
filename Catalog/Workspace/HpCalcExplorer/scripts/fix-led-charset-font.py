#!/usr/bin/env python3
"""Add Windows Unicode (3,1) cmap to LEDcharset_class.TTF for ImGui/stb_truetype."""

from __future__ import annotations

import sys
from pathlib import Path

from fontTools.ttLib import TTFont
from fontTools.ttLib.tables._c_m_a_p import cmap_format_4


def fix_font(source: Path, destination: Path) -> None:
    font = TTFont(source)
    mac_cmap: dict[int, str] | None = None
    for table in font["cmap"].tables:
        if table.platformID == 1 and table.platEncID == 0:
            mac_cmap = table.cmap
            break

    if mac_cmap is None:
        raise RuntimeError("Mac Roman cmap not found.")

    has_unicode = any(t.platformID == 3 and t.platEncID == 1 for t in font["cmap"].tables)
    if not has_unicode:
        unicode_map = {
            code: glyph_name
            for code, glyph_name in mac_cmap.items()
            if isinstance(glyph_name, str)
        }
        subtable = cmap_format_4(4)
        subtable.platformID = 3
        subtable.platEncID = 1
        subtable.language = 0
        subtable.cmap = unicode_map
        font["cmap"].tables.append(subtable)

    destination.parent.mkdir(parents=True, exist_ok=True)
    font.save(destination)


def main() -> int:
    repo = Path(__file__).resolve().parents[4]
    source = repo / "Catalog/Workspace/HpCalcExplorer/Reference/Panamatik/HP-34C/LEDcharset_class.TTF"
    if len(sys.argv) > 1:
        destination = Path(sys.argv[1])
    else:
        destination = repo / "Resource/Font/LEDcharset_class.TTF"

    fix_font(source, destination)
    print(f"Wrote {destination}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
