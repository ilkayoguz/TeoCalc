#!/usr/bin/env python3
"""Export HP-65 program vocabulary from Panamatik decompile."""

from __future__ import annotations

import json
import re
from pathlib import Path

REPO = Path(__file__).resolve().parents[4]
SOURCE_CS = (
    REPO
    / "Catalog/Workspace/HpCalcExplorer/Reference/Decompiled/Panamatik/HP-65/HPCLASSIC/HPClassic.cs"
)
OUT = REPO / "Resource/Engine/HP-65/Program/program.vocabulary.json"


def parse_string_array(name: str, text: str) -> list[str]:
    m = re.search(rf"{name} = new string\[(\d+)\]\s*\{{([^;]+)\}};", text, re.DOTALL)
    if not m:
        raise SystemExit(f"{name} not found")
    block = m.group(2)
    values = re.findall(r'"([^"]*)"', block)
    expected = int(m.group(1))
    if len(values) != expected:
        raise SystemExit(f"{name}: expected {expected} items, parsed {len(values)}")
    return values


def parse_byte_array(name: str, text: str) -> list[int]:
    m = re.search(rf"{name} = new byte\[(\d+)\]\s*\{{([^;]+)\}};", text, re.DOTALL)
    if not m:
        raise SystemExit(f"{name} not found")
    return [int(x.strip()) for x in m.group(2).replace("\n", " ").split(",") if x.strip()]


def step_id(mnemonic: str, code: int) -> str:
    token = re.sub(r"[^A-Za-z0-9]+", "", mnemonic).upper() or f"CODE{code:02X}"
    return f"ProgramStep.{token}"


def main() -> None:
    text = SOURCE_CS.read_text(encoding="utf-8")
    mnemonics = parse_string_array("HPClassicMnemonics", text)
    key_chars_raw = re.search(r"HPClassicKeyChartable = new char\[(\d+)\]\s*\{([^;]+)\};", text, re.DOTALL)
    if not key_chars_raw:
        raise SystemExit("HPClassicKeyChartable not found")
    key_char_block = key_chars_raw.group(2)
    key_chars = []
    for token in re.findall(r"'((?:\\.|[^'])*)'", key_char_block):
        if token == "\\r":
            key_chars.append("\r")
        elif token == "\\b":
            key_chars.append("\b")
        elif token == "\\0":
            key_chars.append("\0")
        elif token == "\\\\":
            key_chars.append("\\")
        else:
            key_chars.append(token)
    if len(key_chars) != int(key_chars_raw.group(1)):
        raise SystemExit(f"HPClassicKeyChartable: expected {key_chars_raw.group(1)} items, parsed {len(key_chars)}")
    key_codes = parse_byte_array("HPClassicKeytable", text)

    steps = []
    for code, mnemonic in enumerate(mnemonics):
        entry = {
            "Code": code,
            "Mnemonic": mnemonic if mnemonic else f"<{code}>",
            "StepId": step_id(mnemonic, code),
            "Title": mnemonic if mnemonic else "Reserved keystroke code",
        }
        steps.append(entry)

    keys = []
    for i, ch in enumerate(key_chars):
        keys.append(
            {
                "Index": i,
                "Char": ch,
                "KeyCode": key_codes[i] if i < len(key_codes) else 0,
            }
        )

    doc = {
        "Format": "TeoCalc.ProgramVocabulary",
        "SchemaVersion": 1,
        "Model": "HP-65",
        "Source": "Panamatik HPClassic (study extract)",
        "CodeBits": 6,
        "Steps": steps,
        "KeyChart": keys,
    }

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(doc, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Wrote {OUT} ({len(steps)} steps, {len(keys)} keys)")


if __name__ == "__main__":
    main()
