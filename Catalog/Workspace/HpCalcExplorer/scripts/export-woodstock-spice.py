#!/usr/bin/env python3
"""Export Woodstock/Spice firmware ROM from Panamatik decompile (study artifacts)."""

from __future__ import annotations

import argparse
import json
import re
import struct
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[4]
DECOMPILED = REPO / "Catalog/Workspace/HpCalcExplorer/Reference/Decompiled/Panamatik"

WOODSTOCK_MODELS = ("HP-21", "HP-22", "HP-25", "HP-27", "HP-29")
SPICE_MODELS = ("HP-31", "HP-32", "HP-33", "HP-34", "HP-37", "HP-38")


def find_opcode_source(model: str) -> Path:
    model_dir = DECOMPILED / model
    for path in sorted(model_dir.rglob("*.cs")):
        text = path.read_text(encoding="utf-8", errors="ignore")
        if "opcodeint = new ushort" in text:
            return path
    raise SystemExit(f"opcodeint not found for {model}")


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


def export_firmware(model: str, family: str) -> None:
    words = parse_opcodeint(find_opcode_source(model))
    slug = model.lower().replace("-", "")
    out_dir = REPO / "Resource/Engine" / model / "Firmware"
    out_dir.mkdir(parents=True, exist_ok=True)

    bin_path = out_dir / f"{slug}.microcode.bin"
    with bin_path.open("wb") as handle:
        for word in words:
            handle.write(struct.pack("<H", word))

    lines = [
        f"; TeoCalc {model} microcode listing (.mlist)",
        f"; Family: {family} — dispatch decode pending (ROM study export)",
        "; Columns: Addr  Word",
        f"; Words={len(words)}  Bytes={len(words) * 2}",
        ";",
    ]
    entries = []
    for address, word in enumerate(words):
        lines.append(f"{address:04X}  {word:04X}")
        entries.append(
            {
                "Address": address,
                "AddressHex": f"{address:04X}",
                "RomWord": word,
                "RomWordHex": f"{word:04X}",
                "Mnemonic": "RAW ",
                "HandlerId": "FirmwareStudy.PendingDispatch",
                "Title": "ROM word (handler decode pending)",
                "PanamatikAlias": "study_raw",
            }
        )

    (out_dir / f"{slug}.microcode.mlist").write_text("\n".join(lines) + "\n", encoding="utf-8")
    (out_dir / f"{slug}.microcode.map.json").write_text(
        json.dumps(
            {
                "Format": "TeoCalc.MicrocodeMap",
                "SchemaVersion": 1,
                "Model": model,
                "Family": family,
                "Source": "Panamatik opcodeint (study extract)",
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


def write_model_json(model: str, family: str, rom_words: int, program_ram_base: int = 0) -> None:
    slug = model.lower().replace("-", "")
    model_dir = REPO / "Resource/Engine" / model
    model_dir.mkdir(parents=True, exist_ok=True)
    doc = {
        "Format": "TeoCalc.Model",
        "SchemaVersion": 1,
        "Model": model,
        "DisplayName": model,
        "Inherits": family,
        "Family": family,
        "FamilyConfig": f"../{family}/Family.json",
        "Hardware": {
            "ButtonCount": 40,
            "RamBytes": 448,
            "RegisterDigits": 14,
            "ProgramRamBase": program_ram_base,
            "RomWordCount": rom_words,
        },
        "Firmware": {
            "RomBinary": f"Firmware/{slug}.microcode.bin",
            "RomListing": f"Firmware/{slug}.microcode.mlist",
            "RomMap": f"Firmware/{slug}.microcode.map.json",
            "HandlerCatalog": f"../{family}/microcode.handlers.study.json",
        },
    }
    (model_dir / "Model.json").write_text(json.dumps(doc, indent=2) + "\n", encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--family", choices=("woodstock", "spice", "all"), default="all")
    args = parser.parse_args()
    targets: list[tuple[str, str]] = []
    if args.family in ("woodstock", "all"):
        targets.extend((model, "Woodstock") for model in WOODSTOCK_MODELS)
    if args.family in ("spice", "all"):
        targets.extend((model, "Spice") for model in SPICE_MODELS)

    failures = 0
    for model, family in targets:
        try:
            words = parse_opcodeint(find_opcode_source(model))
            export_firmware(model, family)
            write_model_json(model, family, len(words))
        except SystemExit as error:
            print(f"{model}: FAILED - {error}", file=sys.stderr)
            failures += 1
    if failures:
        raise SystemExit(failures)


if __name__ == "__main__":
    main()
