#!/usr/bin/env python3
"""Extract HP-65 opcodeint ROM; emit .bin and .mlist (microcode listing)."""

from __future__ import annotations

import json
import re
import struct
from pathlib import Path

REPO = Path(__file__).resolve().parents[4]
SOURCE_CS = (
    REPO
    / "Catalog/Workspace/HpCalcExplorer/Reference/Decompiled/Panamatik/HP-65/HPCLASSIC/HPClassic.cs"
)
OUT_DIR = REPO / "Resource/Engine/HP-65/Firmware"
HANDLER_CATALOG = REPO / "Resource/Engine/Classic/microcode.handlers.json"


def parse_opcodeint(path: Path) -> list[int]:
    text = path.read_text(encoding="utf-8")
    m = re.search(r"opcodeint = new ushort\[(\d+)\]\s*\{([^;]+)\};", text, re.DOTALL)
    if not m:
        raise SystemExit(f"opcodeint not found in {path}")
    count = int(m.group(1))
    nums = [int(x.strip()) for x in m.group(2).replace("\n", " ").split(",") if x.strip()]
    if len(nums) != count:
        raise SystemExit(f"expected {count} words, parsed {len(nums)}")
    return nums


def build_dispatch_table() -> dict[int, str]:
    op: dict[int, str] = {}
    for i in range(0, 1024, 4):
        op[i] = "op_unknown"
        op[i + 1] = "op_jsb"
        op[i + 2] = "op_arith"
        op[i + 3] = "op_goto"
    for i in range(16):
        op[4 + (i << 6)] = "op_set_s"
        op[20 + (i << 6)] = "op_test_s_eq_0"
        op[32 + (i << 6)] = "op_set_f"
        op[36 + (i << 6)] = "op_clr_s"
    op[52] = "op_clear_s"
    for i in range(8):
        op[116 + (i << 7)] = "op_del_sel_rom"
    op[564] = "op_del_sel_grp"
    op[692] = "op_del_sel_grp"
    for i in range(16):
        op[12 + (i << 6)] = "op_set_p"
        op[44 + (i << 6)] = "op_test_p"
    op[28] = "op_dec_p"
    op[60] = "op_inc_p"
    for i in range(10):
        op[24 + (i << 6)] = "op_load_constant"
    for i in range(2):
        op[40] = "op_display_toggle"
        op[168] = "op_c_exch_m"
        op[296] = "op_c_to_stack"
        op[424] = "op_stack_to_a"
        op[552] = "op_display_off"
        op[680] = "op_m_to_c"
        op[808] = "op_down_rotate"
        op[936] = "op_clear_regs"
        for j in range(4):
            op[232 + (j << 8) + (i << 4)] = "op_data_to_c"
    for i in range(8):
        op[16 + (i << 7)] = "op_sel_rom"
        op[48] = "op_return"
        if i & 1:
            op[208] = "op_keys_to_rom_addr"
    op[624] = "op_c_to_addr"
    op[752] = "op_c_to_data"
    op[0] = "op_nop"
    op[120] = "op_memoryfull"
    op[64] = "op_buf_to_rom_addr"
    op[128] = "op_memoryinsert"
    op[256] = "op_markandsearch"
    op[384] = "op_memorydelete"
    op[512] = "op_rom_addr_to_buf"
    op[640] = "op_searchforlabel"
    op[768] = "op_pointeradvance"
    op[896] = "op_memoryinitialize"
    return op


def load_handler_catalog() -> dict[str, dict]:
    data = json.loads(HANDLER_CATALOG.read_text(encoding="utf-8"))
    by_alias: dict[str, dict] = {}
    for h in data["Handlers"]:
        by_alias[h["PanamatikAlias"]] = h
    return by_alias


def resolve_handler(catalog: dict[str, dict], panamatik: str) -> tuple[str, str, str]:
    entry = catalog.get(panamatik, catalog.get("op_unknown"))
    return (
        entry["Mnemonic"].strip().ljust(4)[:4],
        entry["HandlerId"],
        entry["Title"],
    )


def main() -> None:
    words = parse_opcodeint(SOURCE_CS)
    dispatch = build_dispatch_table()
    catalog = load_handler_catalog()
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    bin_path = OUT_DIR / "hp65.microcode.bin"
    with bin_path.open("wb") as f:
        for w in words:
            f.write(struct.pack("<H", w))

    mlist_path = OUT_DIR / "hp65.microcode.mlist"
    map_path = OUT_DIR / "hp65.microcode.map.json"

    lines: list[str] = [
        "; TeoCalc HP-65 microcode listing (.mlist)",
        "; Runtime uses hp65.microcode.bin only. This file is for viewers/debug.",
        "; Columns: Addr  Word  Mnem  HandlerId  Title",
        f"; Words={len(words)}  Bytes={len(words) * 2}",
        ";",
    ]

    entries: list[dict] = []
    for pc, word in enumerate(words):
        panamatik = dispatch.get(word, "op_unknown") if word <= 1023 else "INVALID"
        mnemonic, handler_id, title = resolve_handler(catalog, panamatik)
        lines.append(f"{pc:04X}  {word:04X}  {mnemonic}  {handler_id}  {title}")
        entries.append(
            {
                "Address": pc,
                "AddressHex": f"{pc:04X}",
                "RomWord": word,
                "RomWordHex": f"{word:04X}",
                "DispatchIndex": word,
                "Mnemonic": mnemonic.strip(),
                "HandlerId": handler_id,
                "Title": title,
                "PanamatikAlias": panamatik,
            }
        )

    mlist_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    map_path.write_text(
        json.dumps(
            {
                "Format": "TeoCalc.MicrocodeMap",
                "SchemaVersion": 1,
                "Model": "HP-65",
                "Source": "Panamatik HPClassic.opcodeint (study extract)",
                "RomBinary": "hp65.microcode.bin",
                "WordCount": len(words),
                "Entries": entries,
            },
            indent=2,
        ),
        encoding="utf-8",
    )

    print(f"Wrote {bin_path} ({bin_path.stat().st_size} bytes)")
    print(f"Wrote {mlist_path} ({len(lines)} lines)")
    print(f"Wrote {map_path}")


if __name__ == "__main__":
    main()
