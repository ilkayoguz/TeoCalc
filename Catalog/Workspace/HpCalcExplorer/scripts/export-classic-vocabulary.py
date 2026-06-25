#!/usr/bin/env python3
"""Export Classic model program vocabulary from Panamatik decompile."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[4]
DECOMPILED = REPO / "Catalog/Workspace/HpCalcExplorer/Reference/Decompiled/Panamatik"
CLASSIC_MODELS = ("HP-35", "HP-45", "HP-55", "HP-65", "HP-70", "HP-80")


def find_hpclassic_cs(model: str) -> Path:
    model_dir = DECOMPILED / model
    matches = sorted(model_dir.rglob("HPClassic.cs"))
    matches = [path for path in matches if "HPCLASSIC" in path.parts]
    if not matches:
        raise SystemExit(f"HPClassic.cs not found for {model}")
    return matches[0]


def parse_string_array(name: str, text: str) -> list[str]:
    match = re.search(rf"{name} = new string\[(\d+)\]\s*\{{([^;]+)\}};", text, re.DOTALL)
    if not match:
        raise SystemExit(f"{name} not found")
    block = match.group(2)
    values = re.findall(r'"([^"]*)"', block)
    expected = int(match.group(1))
    if len(values) != expected:
        raise SystemExit(f"{name}: expected {expected} items, parsed {len(values)}")
    return values


def parse_byte_array(name: str, text: str) -> list[int]:
    match = re.search(rf"{name} = new byte\[(\d+)\]\s*\{{([^;]+)\}};", text, re.DOTALL)
    if not match:
        raise SystemExit(f"{name} not found")
    return [int(value.strip()) for value in match.group(2).replace("\n", " ").split(",") if value.strip()]


def parse_key_chars(block: str, expected: int) -> list[str]:
    key_chars: list[str] = []
    for token in re.findall(r"'((?:\\.|[^'])*)'", block):
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
    if len(key_chars) != expected:
        raise SystemExit(f"HPClassicKeyChartable: expected {expected} items, parsed {len(key_chars)}")
    return key_chars


def step_id(mnemonic: str, code: int) -> str:
    token = re.sub(r"[^A-Za-z0-9]+", "", mnemonic).upper() or f"CODE{code:02X}"
    return f"ProgramStep.{token}"


def load_mnemonics(text: str, model: str) -> list[str]:
    for name in ("HPClassicMnemonics", "HP55Mnemonics", "HP65Mnemonics"):
        match = re.search(rf"{name} = new string\[(\d+)\]\s*\{{([^;]*)\}};", text, re.DOTALL)
        if not match:
            continue
        expected = int(match.group(1))
        if expected == 0:
            continue
        values = re.findall(r'"([^"]*)"', match.group(2))
        if len(values) == expected:
            return values

    template = REPO / "Resource/Engine/HP-65/Program/program.vocabulary.json"
    if not template.is_file():
        raise SystemExit(f"No mnemonics in {model} and missing template {template}")
    data = json.loads(template.read_text(encoding="utf-8"))
    return [step["Mnemonic"] for step in data["Steps"]]


def export_vocabulary(model: str) -> None:
    source_cs = find_hpclassic_cs(model)
    text = source_cs.read_text(encoding="utf-8")
    mnemonics = load_mnemonics(text, model)
    key_chars_raw = re.search(r"HPClassicKeyChartable = new char\[(\d+)\]\s*\{([^;]+)\};", text, re.DOTALL)
    if not key_chars_raw:
        raise SystemExit("HPClassicKeyChartable not found")
    key_chars = parse_key_chars(key_chars_raw.group(2), int(key_chars_raw.group(1)))
    key_codes = parse_byte_array("HPClassicKeytable", text)

    steps = []
    for code, mnemonic in enumerate(mnemonics):
        steps.append(
            {
                "Code": code,
                "Mnemonic": mnemonic if mnemonic else f"<{code}>",
                "StepId": step_id(mnemonic, code),
                "Title": mnemonic if mnemonic else "Reserved keystroke code",
            }
        )

    keys = []
    for index, character in enumerate(key_chars):
        keys.append(
            {
                "Index": index,
                "Char": character,
                "KeyCode": key_codes[index] if index < len(key_codes) else 0,
            }
        )

    out_dir = REPO / "Resource/Engine" / model / "Program"
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / "program.vocabulary.json"
    doc = {
        "Format": "TeoCalc.ProgramVocabulary",
        "SchemaVersion": 1,
        "Model": model,
        "Source": "Panamatik HPClassic (study extract)",
        "CodeBits": 6,
        "Steps": steps,
        "KeyChart": keys,
    }
    out_path.write_text(json.dumps(doc, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"{model}: {len(steps)} steps, {len(keys)} keys -> {out_path}")


def main() -> None:
    parser = argparse.ArgumentParser(description="Export Classic program vocabulary.")
    parser.add_argument("models", nargs="*", help="Model ids (default: programmable Classic models)")
    args = parser.parse_args()
    models = args.models or [model for model in CLASSIC_MODELS if model != "HP-35"]
    failures = 0
    for model in models:
        try:
            export_vocabulary(model)
        except SystemExit as error:
            print(f"{model}: FAILED - {error}", file=sys.stderr)
            failures += 1
    if failures:
        raise SystemExit(failures)


if __name__ == "__main__":
    main()
