# HP Calc Explorer workspace

Reference emulator binaries and ilspycmd output for consolidating HP vintage calculators into TeoCalc.

**Status:** Workspace scaffold — HP-65 priority.

## Layout

| Path | Git | Purpose |
|------|-----|---------|
| `Reference/Exe/` | ignored | Third-party `.exe` inputs (20 models) |
| `Reference/Decompiled/` | ignored | ilspycmd output (study only) |
| `scripts/decompile-all.ps1` | tracked | Batch decompile helper |

## Decompile

1. Copy exes into `Reference/Exe/` (e.g. `HP-65.exe`).
2. From repo root:

```powershell
.\Catalog\Workspace\HpCalcExplorer\scripts\decompile-all.ps1
.\Catalog\Workspace\HpCalcExplorer\scripts\decompile-all.ps1 -Model HP-65
```

Requires: `dotnet tool install -g ilspycmd --version 10.1.0.8386`

## Legal

Reference exes are third-party emulators. Keep them local; consolidated code in `TeoCalc.Core` is clean-room Teo work.
