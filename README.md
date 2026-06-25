# TeoCalc

HP vintage calculator explorer — a sibling project to [TeoCave](../TeoCave), same layering and MVP conventions.

**Priority model:** HP-65

## Solution layout

| Project | Role |
|---------|------|
| `TeoCalc` | Host executable (Silk / ImGui shell, future) |
| `TeoCalc.Core` | Domain: CPU, stack, registers, program step, model catalog |
| `TeoCalc.Formats` | Import/export: program cards, state snapshots |
| `TeoCalc.Storage` | Persistence, localization seeds |
| `TeoCalc.Game` | Presentation: ViewModels, Presenters, Navigators (renderer-agnostic) |
| `TeoCalc.Rendering` | ImGui views, Silk input, platform glue |
| `TeoCalc.Tools` | CLI utilities (decompile helpers, batch jobs) |

## Workspace

Reference emulator binaries live under `Catalog/Workspace/HpCalcExplorer/Reference/Panamatik/` (one folder per model, local only, gitignored).

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

`TeoCalc.Rendering` references `TeoTheme` from the sibling TeoCave checkout (`../TeoCave/TeoTheme`). Both repos are expected side-by-side under `Side.Codes/`.

## Architecture

See [TeoCalc.Game/UI-MVP-Architecture.md](TeoCalc.Game/UI-MVP-Architecture.md).
