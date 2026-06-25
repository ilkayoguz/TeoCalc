# HP Calc Explorer workspace

Reference **Panamatik** emulator bundles and ilspycmd output for consolidating HP vintage calculators into TeoCalc.

**Status:** Panamatik reference layout — HP-65 priority.

## Layout

| Path | Git | Purpose |
|------|-----|---------|
| `Reference/Panamatik New/HP-65/` | ignored | One folder per model: `.exe`, `.kml`, images, fonts, config |
| `Reference/Decompiled/Panamatik/` | ignored | ilspycmd output (study only) |
| `scripts/decompile-all.ps1` | tracked | Batch decompile with per-model reference path |

Each Panamatik model ships as a self-contained folder (skins, fonts, configs). Do **not** flatten exes into a single directory — ilspycmd needs `-r` pointed at the model folder.

## Decompile output layout (read this first)

ilspycmd `-p --nested-directories` creates **folders from .NET namespaces**, not from HP model names.

This is **not** cross-model contamination between `HP-65/` and `HP-67/` output directories. Each model decompiles into its own tree under `Reference/Decompiled/Panamatik/<model>/`.

What looks confusing is **inside a single `.exe`**:

| Model | Main engine class | Namespace folder(s) in output | Notes |
|-------|-------------------|----------------------------------|-------|
| HP-35, 45, 55, **65**, 70, 80 | `HPCLASSIC.HPClassic` | `HPCLASSIC/` | Shared Panamatik "Classic" codebase |
| HP-21, 25, 29C | `HP25.HP25` | `HP25/` | Woodstock line; **29C reuses HP25 class name** |
| HP-31E, 32E, 33, 34C | `HPSpice.HPSpice` | `HPSpice/` | Spice line |
| **HP-67** | `HP67.HP67` | `HP67/` **and** `HP25/` | `HP25/` is only `Properties` boilerplate left in the same assembly |
| HP-01, 19C | own `HP01`, `HP19C` | matching folder | |

So `HP-67/HP25/` does **not** mean HP-25 files were copied from another decompile folder — the **HP67 exe embeds `HP25.Properties.*` types** in the same assembly (Visual Studio template leftovers).

Run namespace inventory before studying code:

```powershell
.\Catalog\Workspace\HpCalcExplorer\scripts\inventory-models.ps1
```

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
