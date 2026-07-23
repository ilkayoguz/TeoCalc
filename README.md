# TeoCalc

Vintage calculator emulator and Studio — sibling to [TeoCave](../TeoCave), same layering and theme stack.

**Priority model:** T-65 (launcher catalog id `HP-65` → engine folder `T-65`)

## Solution layout

| Project | Role |
|---------|------|
| `TeoCalc` | Host executable (Silk / ImGui shell) |
| `TeoCalc.Core` | Domain: CPU, stack, registers, program step, model catalog |
| `TeoCalc.Formats` | Import/export: program cards, state snapshots |
| `TeoCalc.Game` | Presentation: ViewModels, Presenters, Navigators (renderer-agnostic) |
| `TeoCalc.Rendering` | ImGui views, Silk input, platform glue |
| `TeoCalc.Panamatik` | Reference-emulator adapter (`TeoCalc.ReferenceEmulator`; upstream `Sources/**` opaque) |
| `TeoCalc.Tools` | CLI utilities (decompile helpers, batch jobs) |

## Workspace

Reference emulator binaries / decompile workspace live under `Catalog/Workspace/HpCalcExplorer/` (local tooling; gitignored binaries).

```powershell
dotnet tool install -g ilspycmd --version 10.1.0.8386
.\Catalog\Workspace\HpCalcExplorer\scripts\decompile-all.ps1 -Model HP-65
```

## Build

```powershell
dotnet build TeoCalc.slnx
dotnet run --project TeoCalc
```

## TeoTheme

`TeoCalc.Rendering` references `TeoTheme` from the sibling TeoCave checkout (`../TeoCave/TeoTheme`). App chrome uses Light / Dark / System via Settings (gear). Faceplate Retro/Modern stays a separate `CalcTheme`.

## Architecture

See solution projects above; engine resources under `Resource/Engine/T-*`.
