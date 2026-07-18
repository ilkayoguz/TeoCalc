#!/usr/bin/env python3
"""Export program.vocabulary.json for all Panamatik models from decompiled sources."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[4]
SOURCES = REPO / "TeoCalc.Panamatik" / "Sources"

MODEL_BINDINGS: list[dict[str, str]] = [
    {"model": "HP-21", "folder": "HP21", "file": "HP25/HP25.cs", "chars": "HP2xKeyChartable", "codes": "HP2xKeytable"},
    {"model": "HP-22", "folder": "HP22", "file": "HP25/HP25.cs", "chars": "HP2xKeyChartable", "codes": "HP2xKeytable"},
    {"model": "HP-25", "folder": "HP25", "file": "HP25/HP25.cs", "chars": "HP2xKeyChartable", "codes": "HP2xKeytable"},
    {"model": "HP-27", "folder": "HP27", "file": "HP25/HP25.cs", "chars": "HP2xKeyChartable", "codes": "HP2xKeytable"},
    {"model": "HP-29", "folder": "HP29", "file": "HP25/HP25.cs", "chars": "HP2xKeyChartable", "codes": "HP2xKeytable"},
    {"model": "HP-31", "folder": "HP31", "file": "HPSpice/HPSpice.cs", "chars": "HP3xKeyChartable", "codes": "HP3xKeytable"},
    {"model": "HP-32", "folder": "HP32", "file": "HPSpice/HPSpice.cs", "chars": "HP3xKeyChartable", "codes": "HP3xKeytable"},
    {"model": "HP-33", "folder": "HP33", "file": "HPSpice/HPSpice.cs", "chars": "HP3xKeyChartable", "codes": "HP3xKeytable"},
    {"model": "HP-34", "folder": "HP34", "file": "HPSpice/HPSpice.cs", "chars": "HP3xKeyChartable", "codes": "HP3xKeytable"},
    {"model": "HP-37", "folder": "HP37", "file": "HPSpice/HPSpice.cs", "chars": "HP3xKeyChartable", "codes": "HP3xKeytable"},
    {"model": "HP-38", "folder": "HP38", "file": "HPSpice/HPSpice.cs", "chars": "HP3xKeyChartable", "codes": "HP3xKeytable"},
    {"model": "HP-35", "folder": "HP35", "file": "HPCLASSIC/HPClassic.cs", "chars": "HPClassicKeyChartable", "codes": "HPClassicKeytable"},
    {"model": "HP-45", "folder": "HP45", "file": "HPCLASSIC/HPClassic.cs", "chars": "HPClassicKeyChartable", "codes": "HPClassicKeytable"},
    {"model": "HP-55", "folder": "HP55", "file": "HPCLASSIC/HPClassic.cs", "chars": "HPClassicKeyChartable", "codes": "HPClassicKeytable"},
    {"model": "HP-65", "folder": "HP65", "file": "HPCLASSIC/HPClassic.cs", "chars": "HPClassicKeyChartable", "codes": "HPClassicKeytable"},
    {"model": "HP-70", "folder": "HP70", "file": "HPCLASSIC/HPClassic.cs", "chars": "HPClassicKeyChartable", "codes": "HPClassicKeytable"},
    {"model": "HP-80", "folder": "HP80", "file": "HPCLASSIC/HPClassic.cs", "chars": "HPClassicKeyChartable", "codes": "HPClassicKeytable"},
    {"model": "HP-67", "folder": "HP67", "file": "HP67/HP67.cs", "chars": "HP67KeyChartable", "codes": "HP67Keytable"},
    {"model": "HP-01", "folder": "HP01", "file": "HP01/HP01.cs", "chars": "HP01KeyChartable", "codes": "HP01Keytable"},
    {"model": "HP-19C", "folder": "HP19", "file": "HP19C/HP19C.cs", "chars": "HP19CKeyChartable", "codes": "HP19CKeytable"},
]


def parse_char_array(text: str, name: str) -> list[str]:
    match = re.search(rf"{re.escape(name)} = new char\[(\d+)\]\s*\{{(.*?)}};", text, re.DOTALL)
    if not match:
        raise ValueError(f"{name} not found")
    chars: list[str] = []
    for token in re.findall(r"'((?:\\.|[^'])*)'", match.group(2)):
        if token == "\\r":
            chars.append("\r")
        elif token == "\\b":
            chars.append("\b")
        elif token == "\\0":
            chars.append("\0")
        elif token == "\\\\":
            chars.append("\\")
        else:
            chars.append(token)
    expected = int(match.group(1))
    if len(chars) != expected:
        raise ValueError(f"{name}: expected {expected}, parsed {len(chars)}")
    return chars


def parse_byte_array(text: str, name: str) -> list[int]:
    match = re.search(rf"{re.escape(name)} = new byte\[(\d+)\]\s*\{{(.*?)}};", text, re.DOTALL)
    if not match:
        raise ValueError(f"{name} not found")
    return [int(value.strip()) for value in match.group(2).replace("\n", " ").split(",") if value.strip()]


def export_model(binding: dict[str, str]) -> None:
    model = binding["model"]
    source = SOURCES / binding["folder"] / binding["file"]
    if not source.is_file():
        raise FileNotFoundError(source)
    text = source.read_text(encoding="utf-8", errors="replace")
    key_chars = parse_char_array(text, binding["chars"])
    key_codes = parse_byte_array(text, binding["codes"])

    keys = []
    for index, character in enumerate(key_chars):
        keys.append({"Index": index, "Char": character, "KeyCode": key_codes[index] if index < len(key_codes) else 0})

    out_dir = REPO / "Resource" / "Engine" / model / "Program"
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / "program.vocabulary.json"
    doc = {
        "Format": "TeoCalc.ProgramVocabulary",
        "SchemaVersion": 1,
        "Model": model,
        "Source": f"Panamatik {binding['folder']} ({binding['chars']})",
        "CodeBits": 6,
        "Steps": [],
        "KeyChart": keys,
    }
    out_path.write_text(json.dumps(doc, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"{model}: {len(keys)} keys -> {out_path.relative_to(REPO)}")


def patch_model_json(model: str) -> None:
    path = REPO / "Resource" / "Engine" / model / "Model.json"
    if not path.is_file():
        write_minimal_model_json(model)
        return
    data = json.loads(path.read_text(encoding="utf-8"))
    program = data.setdefault("Program", {})
    program["MaxSteps"] = program.get("MaxSteps", 0)
    program["Vocabulary"] = "Program/program.vocabulary.json"
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def write_minimal_model_json(model: str) -> None:
    family = {
        "HP-01": "HP01",
        "HP-19C": "HP19C",
        "HP-67": "Classic",
    }.get(model, "Panamatik")
    path = REPO / "Resource" / "Engine" / model / "Model.json"
    path.parent.mkdir(parents=True, exist_ok=True)
    doc = {
        "Format": "TeoCalc.Model",
        "SchemaVersion": 1,
        "Model": model,
        "DisplayName": model,
        "Family": family,
        "Hardware": {
            "ButtonCount": 40,
            "RamBytes": 448,
            "RegisterDigits": 14,
            "ProgramRamBase": 0,
            "RomWordCount": 1024,
        },
        "Program": {
            "MaxSteps": 0,
            "Vocabulary": "Program/program.vocabulary.json",
        },
        "Firmware": {},
    }
    path.write_text(json.dumps(doc, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def main() -> None:
    failures = 0
    for binding in MODEL_BINDINGS:
        model = binding["model"]
        try:
            export_model(binding)
            patch_model_json(model)
        except (OSError, ValueError, json.JSONDecodeError) as error:
            print(f"{model}: FAILED - {error}", file=sys.stderr)
            failures += 1
    if failures:
        raise SystemExit(failures)


if __name__ == "__main__":
    main()
