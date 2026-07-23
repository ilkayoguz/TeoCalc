# HP-65 firmware (microcode ROM)

Internal CPU ROM extracted from Panamatik `HPClassic.opcodeint` for study — **not** user program steps (STO/LBL).

| File | Role |
|------|------|
| `hp65.microcode.bin` | Raw ROM: 3072 little-endian `uint16` words (6144 bytes) |
| `hp65.microcode.mlist` | Microcode listing (viewer/debug) |
| `hp65.microcode.map.json` | Same data structured for tools / UI |

Regenerate after re-decompile:

```bash
python Catalog/Workspace/HpCalcExplorer/scripts/export-hp65-microcode.py
```

User-facing program vocabulary (STO, LBL, GTO) is a separate layer: `../Program/program.vocabulary.json`.
