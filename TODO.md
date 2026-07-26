# TeoCalc roadmap

**Status: backlog clear** (2026-07-26). Open product tracks from this file are closed for the Studio / Debug / assists arc.

---

## Shipped (this arc)

### App chrome & Settings (P0)
- Light / Dark / System via `CalcAppTheme` + `%LocalAppData%\TeoCalc\UserSettings.json`
- Title-bar Settings modal; Studio/Debug shell tokens from live theme

### Product identity (P1)
- Engine folders `T-*`; catalog DisplayNames TeoCalc-owned; About shows product label only
- Engine audit notes: [Catalog/Documents/Engine-Improvement-Audit.md](Catalog/Documents/Engine-Improvement-Audit.md)

### Studio editor (P2)
- Tabs: **Code** (visual listing + FC) | **Text** (dual + completions) | **ROM** | **Docs** | **Card**
- W/PRGM edit, Find, F9 breakpoints, speed, faceplate keys still author

### Session profiles (P3)
- Slow / Standard / Fast / Max + Save as; execution-speed feature toggle

### Machine debug (P4)
- Studio transport vs microcode grain (Debug open / Ctrl+F10/F11)
- Step Out (Shift+F11); ROM follow margin scroll; editable registers; call stack
- Theme contrast polish on Debug / ROM (ASCII UI chrome for ProggyClean)

### Algorithm assists (#19-21)
| # | Hint | Where |
|---|------|--------|
| 19 | Self-GTO infinite-loop suspect | Docs → Advisories |
| 20 | Missing LBL / open GTO/GSB | Docs → Advisories |
| 21 | Consecutive NOPs; duplicate RTN/R/S | Docs → Advisories |

Hints only - never auto-rewrite the program.

---

## Outside this roadmap (not open TODOs)

These stay **opportunistic / other repos or PRs**, not tracked as unfinished work here:

- Deeper engine factoring (Woodstock/Spice Act policy; Teo19/Teo67) - see Engine-Improvement-Audit
- Peripheral HW knobs beyond session speed profiles (when a real peripheral needs them)
- Broader faceplate / catalog rename lore (`Hp*` type names, workspace folders)
- Stop Debugging as a distinct session lifecycle (today Shift+F5 = leave pause)

Encoding lore (`CodeEncoding`, museum LED ≠ RAM bytes) remains reference, not backlog.
