# TeoCalc UI — MVP Architecture

Parallel to TeoCave. See [TeoCave UI-MVP-Architecture.md](../../TeoCave/TeoGame/UI-MVP-Architecture.md) for the full pattern.

## Paradigm

**Immediate-mode MVP (Passive View)**

```
Input → Presenter → updates ViewModel (+ reads/writes domain Model)
                 → View draws each frame from ViewModel (ImGui in TeoCalc.Rendering)
```

## Isolation

- `ImGuiNET` only in `TeoCalc.Rendering`
- `TeoCalc.Game` and `TeoCalc.Core` stay renderer-agnostic

| Role | Suffix | Assembly |
|------|--------|----------|
| Model | *(none)* | `TeoCalc.Core` |
| ViewModel | `ViewModel` | `TeoCalc.Game` |
| Navigator | `Navigator` | `TeoCalc.Game` |
| Presenter | `Presenter` | `TeoCalc.Game` |
| View | `View` | `TeoCalc.Rendering` |
| Host | `Host` | `TeoCalc.Rendering` |

## HP Calc Explorer (target)

- Model picker (20 calculators)
- Faceplate + display + key matrix per model
- Program editor for programmable models (HP-65 first)
