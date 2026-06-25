# HP Calc Explorer workspace

Reference **Panamatik** emulator bundles and ilspycmd output for consolidating HP vintage calculators into TeoCalc.

**Status:** Panamatik reference layout — HP-65 priority.

## Layout

| Path | Git | Purpose |
|------|-----|---------|
| `Reference/Panamatik/HP-65/` | ignored | One folder per model: `.exe`, `.kml`, images, fonts, config |
| `Reference/Decompiled/Panamatik/` | ignored | ilspycmd output (study only) |
| `scripts/decompile-all.ps1` | tracked | Batch decompile with per-model reference path |

Each Panamatik model ships as a self-contained folder (skins, fonts, configs). Do **not** flatten exes into a single directory — ilspycmd needs `-r` pointed at the model folder.

## Decompile

From repo root:

```powershell
# all models that have an .exe
.\Catalog\Workspace\HpCalcExplorer\scripts\decompile-all.ps1

# HP-65 only (folder name HP-65, exe may be HP65_105.exe)
.\Catalog\Workspace\HpCalcExplorer\scripts\decompile-all.ps1 -Model HP-65

# HP-34C ships two exes — decompile both
.\Catalog\Workspace\HpCalcExplorer\scripts\decompile-all.ps1 -Model HP-34C -AllExes
```

Requires: `dotnet tool install -g ilspycmd --version 10.1.0.8386`

### Primary exe selection

When `-AllExes` is not set, the script picks one exe per folder:

1. Sole exe in folder wins.
2. Otherwise skip `*101*` / `*Stainless*` suffix variants when possible.
3. Prefer names matching the folder token (`HP-65` → `HP65…`).
4. Tie-break: largest file.

## Legal

Reference bundles are third-party emulators. Keep them local; consolidated code in `TeoCalc.Core` is clean-room Teo work.
