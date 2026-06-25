# Classic microcode naming cross-check

TeoCalc uses three parallel naming layers for Classic-family **firmware** (not user program steps):

| Layer | Example | Role |
|-------|---------|------|
| **TeoCalc `HandlerId`** | `ClassicCpu.SubroutineJump` | C# dispatch method |
| **Grid mnemonic (4 char)** | `JSB ` | `.mlist` viewer column |
| **Panamatik alias** | `op_jsb` | Study reference from decompile |

Published references for cross-check:

1. **Nonpareil / uasm** — Eric Smith's GPL simulator; Classic models 35/45/55/80. Uses `jsb`, `go`, `rtn`, delayed select, etc. See `microcode.crossref.json` → `NonpareilMnemonic`.
2. **US patents 3,863,060 and 4,001,569** — HP's first-generation calculator microcode; complete listings for HP-45/55/80 in patent appendices.
3. **HP Journal (May 1974)** — HP-65 article describes 3072 ten-bit **microinstructions** in firmware ROM (distinct from user program keystroke codes).

## HP Museum note

The [HP Museum](https://www.hpmuseum.org/) documents calculators at the user/feature level. It does **not** publish a Classic dispatch mnemonic table equivalent to Panamatik `op_*` names. Museum cross-check for TeoCalc means:

- Confirm **ROM size** and **program step limits** per model page.
- Confirm **HP-65** program semantics (labels, GTO, card reader) vs firmware microcode.
- Use Museum as user-facing vocabulary; use patents/Nonpareil for microcode listing.

## Model ROM sizes (exported `.bin`)

| Model | Words | Notes |
|-------|------:|-------|
| HP-35 | 768 | Single ROM, no delayed group |
| HP-45 | 2048 | 8×256 ROM pages |
| HP-55 | 3072 | 3K ROM, timer mode |
| HP-65 | 3072 | Program memory + label search handlers |
| HP-70 | 2048 | Business; program RAM at offset 112 |
| HP-80 | 1792 | Business |

Regenerate firmware: `python Catalog/Workspace/HpCalcExplorer/scripts/export-classic-model.py`

Regenerate vocabulary: `python Catalog/Workspace/HpCalcExplorer/scripts/export-classic-vocabulary.py`
