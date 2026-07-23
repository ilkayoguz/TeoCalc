# TeoCalc roadmap

Updated after Studio W/PRGM / F9 / speed / Find / help ship. Older P0–P3 Studio staging is closed or absorbed below.

**Deferred separately:** GUI / DEBUG visual review pass (known rough edges; not chased here).

---

## Done (recent Studio)

- [x] Unified Classic listing from RAM; Code | Keys | FC columns; toolbar (card I/O left, transport right).
- [x] Read-only flowchart + selection/PTR sync; F10 Code-grain / F11 micro; Seek highlight fix.
- [x] W/PRGM live edit + Save / leave confirm; double-click set start.
- [x] F9 breakpoints; execution speed `− N× +` / `[` `]`; Studio Find; DUMP ROM±16 + listing.
- [x] ROM watch 64-row + Classic CrossRef hover; Alt+key help; About modal.

Reference (cards / encoding): `CodeEncoding`, t65/t67, museum keycodes ≠ machine bytes — keep as lore, not open work.

---

## P0 — App chrome theme & Settings (highest)

Reuse TeoCave’s **standalone** `TeoTheme` stack (already referenced by `TeoCalc.Rendering`). Do **not** restyle the faceplate/calc body yet — only shell chrome.

### TeoCave reuse (already exists)

| Piece | Role |
|-------|------|
| `AppThemePreference` | `Light` / `Dark` / `System` |
| `AppThemeService` | preference → `ThemeAppearance` + `ThemePalette`; `Changed` event |
| `ThemePack` Light/Dark branches | same token keywords both sides |
| `TeoTheme.Windows` | `WindowsHostThemeResolver` + `WindowsHostThemeWatcher` (registry `AppsUseLightTheme`) |
| TeoCave pattern | `SilkAppTheme.Initialize` → load pack → wire resolver/watcher → `ApplyImGuiStyle` + native title-bar chrome; settings radio + `UserSettingsStore` persist |

TeoCalc today hardcodes Dark toolbar colors in `CalcStudioChromeStyle` (copied from TeoTheme Dark tokens) — replace with live `AppThemeService.Current`.

### Deliverables

1. **Wire `AppThemeService`** in Launcher + each T-XX window (shared preference store). — **done** (`CalcAppTheme` + `CalcUserSettingsStore`)
2. **Theme surfaces (phase 1):** window title bar, left/right side panes (Studio / Debug / Explorer chrome), ImGui shell style. **Out of scope for now:** faceplate / keycaps / calc body (`CalcTheme` Retro/Modern stays independent). — **done** (title band + side strip + Studio toolbar/listing tokens + launcher accents)
3. **Title-bar Settings icon** on Launcher and every T-XX calc window → **modal** for global app settings. — **done**
4. **First setting in modal:** appearance = Dark / Light / System (same UX as TeoCave `DrawAppThemePreferenceSelector`). — **done**
5. **Persist** preference (TeoCalc user settings; mirror TeoCave `UserSettingsStore` AppTheme field). — **done** (`%LocalAppData%\TeoCalc\UserSettings.json`)

**Follow-ups:** ~~theme Studio code listing / Debug text for Light~~; ~~launcher tile accents from `AccentColor`~~ — **done**.

---

## P1 — Product identity & engine

Ship TeoCalc as its own surface; then survey native engine headroom.

### A. Zero Panamatik / HP residue (TeoCalc-owned)

Upstream `TeoCalc.Panamatik/Sources/**` stays opaque. Everything else we own should not advertise HP / Panamatik.

1. **Engine id / Resource folders** — [x] `Resource/Engine/T-*`; `CalcModelIds.ToEngineId` maps catalog `HP-*` → engine `T-*` (no T→HP re-prefix).
2. **Catalog / Model.json / Family** — `DisplayName`, catalog entries, Family `Reference`/`Dispatch` no longer ship `HP-*` / “from Panamatik” as product identity.
   - [x] All `Model.json` `DisplayName` + engine `Model` → `T-*`
   - [x] Classic/Woodstock/Spice `Family.json` DisplayName + Reference demoted (no HP/Panamatik product wording)
   - [x] `TeoCalcCatalog.json` family DisplayNames + provenance note
   - [x] About modal shows `ProductLabel` only (no HP half)
3. **Public wrappers** — [x] `IReferenceEngine` / `ReferenceEngine*` under `TeoCalc.ReferenceEmulator` (project folder `TeoCalc.Panamatik`; `Sources/**` untouched).
4. **Rendering hard-coded `"HP-xx"`** — [x] critical path uses `CalcModelIds.IsEngine` / `ToEngineId` / `ToShortId`; asset fallbacks `T-65`. Type names `Hp21`/`Hp65*` deferred.
5. **Tests / docs / workspace names** — [x] README product identity (`T-65`); `Hp*Faceplate*` test class names + `HpCalcExplorer` workspace folder kept as tooling lore (rename later if desired).

### B. Engine improvement audit

6. **Survey + short plan** — [x] see [Catalog/Documents/Engine-Improvement-Audit.md](Catalog/Documents/Engine-Improvement-Audit.md)
   - [x] Align `InferFamily` → `Teo01`/`Teo19`/`Teo67` (+ Model.json Family + bootstrap/layout aliases)
   - Next engine PR candidates: Woodstock/Spice Act policy; Teo19/Teo67 factoring.

---

## P2 — Edit-mode code editor (replaces editable FC)

**Decision:** Free-format dual editor (C#-IDE mental model) in edit mode. Editable flowchart (#13) is **not** pursued; FC stays visualization. Authoring = faceplate keys (done) **and/or** this text editor.

7. **Edit-mode dual pane**
   - Left: machine free text (`34 01` …); right: Keys/mnemonics (`LBL A` …).
   - Invalid machine tokens blocked; invalid Keys tokens blocked.
   - Completions: e.g. `LB…` → popup (select or keep typing).
   - Sync both sides from one program model; paste/import respect encoding.
8. **ROM viewer placement** (was #12)
   - Prefer **one composite with the editor**, *or* dock ROM viewer **to the right of the calc** — not a permanent orphan micro-panel.
   - Follow live fetch / scroll-to-PC when open.
9. **Docs + embeddable links** (was #16) — easiest content win once editor shell exists; in-place reference while authoring.

---

## P3 — Profiles

10. **Model / session profiles** (was #22)
    - Combobox of predefined profiles.
    - Side button: toggle profile features on/off; **Save as new profile**.
    - Speed / HW knobs fold in here once UX is clear (old Standard/Max idea).

---

## P4 — Debug / trace redesign (design-first)

11. **Debug / trace / live ROM mental model** (was #18)
    - Current stack works but is not satisfying; **no clear product picture yet**.
    - Next step: short design spike (what is “debug” vs “Studio transport” vs “ROM watch”) — then implement.
    - Likely includes: call-stack / Step Out, editable regs, smoother PC follow — only after the model sits.
    - Improve iteratively; do not pile chrome first.

---

## Parked — algorithm assists (clarify before building)

Original #19–#21 were “smart helpers after Studio core.” Meaning:

| # | Intent | Example |
|---|--------|---------|
| **19** Infinite-loop advisory | Static/heuristic warning if GTO/GSB can spin forever | “LBL A → GTO A with no exit” |
| **20** Suggest missing code | Heuristic when a routine looks incomplete | GSB target with no `LBL`, open branch |
| **21** Optimize | Suggest fewer bytes / drop redundant steps | Consecutive no-ops, rewrite to shorter encoding |

**Status:** Parked until you want them. Not on the critical path; easy to misread as “auto-fix program.” Prefer shipping P0–P1 first.

---

## Later / separate track

- **GUI + DEBUG visual review** — dedicated pass on layout, contrast, panel clutter, keyboard focus.
- Faceplate / catalog polish not listed above stays opportunistic.
