#!/usr/bin/env python3
"""Export Classic-family microcode ROM + vocabulary from Panamatik decompile."""

from __future__ import annotations

import argparse
import json
import re
import struct
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[4]
DECOMPILED = REPO / "Catalog/Workspace/HpCalcExplorer/Reference/Decompiled/Panamatik"
HANDLER_CATALOG = REPO / "Resource/Engine/Classic/microcode.handlers.json"

CLASSIC_MODELS = ("HP-35", "HP-45", "HP-55", "HP-65", "HP-70", "HP-80")


def find_hpclassic_cs(model: str) -> Path:
    model_dir = DECOMPILED / model
    if not model_dir.is_dir():
        raise SystemExit(f"Decompiled model folder not found: {model_dir}")
    matches = sorted(model_dir.rglob("HPClassic.cs"))
    matches = [path for path in matches if "HPCLASSIC" in path.parts]
    if not matches:
        raise SystemExit(f"HPClassic.cs not found under {model_dir}")
    return matches[0]


def parse_opcodeint(path: Path) -> list[int]:
    text = path.read_text(encoding="utf-8")
    match = re.search(r"opcodeint = new ushort\[(\d+)\]\s*\{([^;]+)\};", text, re.DOTALL)
    if not match:
        raise SystemExit(f"opcodeint not found in {path}")
    count = int(match.group(1))
    nums = [int(value.strip()) for value in match.group(2).replace("\n", " ").split(",") if value.strip()]
    if len(nums) != count:
        raise SystemExit(f"{path}: expected {count} words, parsed {len(nums)}")
    return nums


def build_dispatch_table() -> dict[int, str]:
    op: dict[int, str] = {}
    for index in range(0, 1024, 4):
        op[index] = "op_unknown"
        op[index + 1] = "op_jsb"
        op[index + 2] = "op_arith"
        op[index + 3] = "op_goto"
    for index in range(16):
        op[4 + (index << 6)] = "op_set_s"
        op[20 + (index << 6)] = "op_test_s_eq_0"
        op[32 + (index << 6)] = "op_set_f"
        op[36 + (index << 6)] = "op_clr_s"
    op[52] = "op_clear_s"
    for index in range(8):
        op[116 + (index << 7)] = "op_del_sel_rom"
    op[564] = "op_del_sel_grp"
    op[692] = "op_del_sel_grp"
    for index in range(16):
        op[12 + (index << 6)] = "op_set_p"
        op[44 + (index << 6)] = "op_test_p"
    op[28] = "op_dec_p"
    op[60] = "op_inc_p"
    for index in range(10):
        op[24 + (index << 6)] = "op_load_constant"
    for index in range(2):
        op[40] = "op_display_toggle"
        op[168] = "op_c_exch_m"
        op[296] = "op_c_to_stack"
        op[424] = "op_stack_to_a"
        op[552] = "op_display_off"
        op[680] = "op_m_to_c"
        op[808] = "op_down_rotate"
        op[936] = "op_clear_regs"
        for bank in range(4):
            op[232 + (bank << 8) + (index << 4)] = "op_data_to_c"
    for index in range(8):
        op[16 + (index << 7)] = "op_sel_rom"
        op[48] = "op_return"
        if index & 1:
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
    return {entry["PanamatikAlias"]: entry for entry in data["Handlers"]}


def resolve_handler(catalog: dict[str, dict], panamatik: str) -> tuple[str, str, str]:
    entry = catalog.get(panamatik, catalog.get("op_unknown"))
    return (
        entry["Mnemonic"].strip().ljust(4)[:4],
        entry["HandlerId"],
        entry["Title"],
    )


def export_microcode(model: str, source_cs: Path, out_dir: Path) -> None:
    words = parse_opcodeint(source_cs)
    dispatch = build_dispatch_table()
    catalog = load_handler_catalog()
    out_dir.mkdir(parents=True, exist_ok=True)

    slug = model.lower().replace("-", "")
    bin_path = out_dir / f"{slug}.microcode.bin"
    with bin_path.open("wb") as handle:
        for word in words:
            handle.write(struct.pack("<H", word))

    lines = [
        f"; TeoCalc {model} microcode listing (.mlist)",
        "; Runtime uses .bin only. This file is for viewers/debug.",
        "; Columns: Addr  Word  Mnem  HandlerId  Title",
        f"; Words={len(words)}  Bytes={len(words) * 2}",
        ";",
    ]
    entries: list[dict] = []
    for address, word in enumerate(words):
        panamatik = dispatch.get(word, "op_unknown") if word <= 1023 else "INVALID"
        mnemonic, handler_id, title = resolve_handler(catalog, panamatik)
        lines.append(f"{address:04X}  {word:04X}  {mnemonic}  {handler_id}  {title}")
        entries.append(
            {
                "Address": address,
                "AddressHex": f"{address:04X}",
                "RomWord": word,
                "RomWordHex": f"{word:04X}",
                "DispatchIndex": word,
                "Mnemonic": mnemonic.strip(),
                "HandlerId": handler_id,
                "Title": title,
                "PanamatikAlias": panamatik,
            }
        )

    (out_dir / f"{slug}.microcode.mlist").write_text("\n".join(lines) + "\n", encoding="utf-8")
    (out_dir / f"{slug}.microcode.map.json").write_text(
        json.dumps(
            {
                "Format": "TeoCalc.MicrocodeMap",
                "SchemaVersion": 1,
                "Model": model,
                "Source": "Panamatik HPClassic.opcodeint (study extract)",
                "RomBinary": bin_path.name,
                "WordCount": len(words),
                "Entries": entries,
            },
            indent=2,
        )
        + "\n",
        encoding="utf-8",
    )
    print(f"{model}: {len(words)} words -> {out_dir}")


def export_model(model: str) -> None:
    source_cs = find_hpclassic_cs(model)
    out_dir = REPO / "Resource/Engine" / model / "Firmware"
    export_microcode(model, source_cs, out_dir)


def main() -> None:
    parser = argparse.ArgumentParser(description="Export Classic model firmware from Panamatik decompile.")
    parser.add_argument("models", nargs="*", help=f"Model ids (default: all Classic: {', '.join(CLASSIC_MODELS)})")
    args = parser.parse_args()
    models = args.models or list(CLASSIC_MODELS)
    failures = 0
    for model in models:
        try:
            export_model(model)
        except SystemExit as error:
            print(f"{model}: FAILED - {error}", file=sys.stderr)
            failures += 1
    if failures:
        raise SystemExit(failures)


if __name__ == "__main__":
    main()
