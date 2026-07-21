# TeoCalc TODO / backlog

Prioritized by product impact: **correctness / host wiring → debugability → editor power → polish**.

## Done / remember (CodeEncoding & cards)

- [x] Card text uses `CodeEncoding` (`mnemonic` | `machine`); legacy `Encoding` accepted on read; single `[Code]` section only (no `[Machine]` / dual program sections).
- Authoring: no faceplate glyphs in `[Code]`; use vocab (`f-1` / `9`, etc.).
- Museum W/PRGM display keycodes ≠ TeoCalc `machine` bytes (decimal internal, e.g. `43`).
- Card formats (t65/t67, CuveSoft, CodeEncoding) — mostly done; keep as reference, not open work.

---

## P0 — Correctness / host wiring

Ship a trustworthy running emulator before new UX.

1. **Local leftovers: T-01 tone sink**
   - Why: Host audio path must match firmware tone; unfinished wiring blocks “real calc” feel and correctness.
2. **Local leftovers: T-19 printer tests**
   - Why: Printer path regressions; tests lock in buffer/print behavior.
3. **Local leftovers: catalog smoke**
   - Why: Smoke that catalog/models load; catches broken bootstrap early.

---

## P1 — Debugability

Make programs inspectable and steppable — core emulator workflow.

4. **DEBUG/TRACE: VS-like step-by-step** (step into / step over)
   - Why: Primary way to verify firmware/program behavior without guessing.
5. **While calc running: watch ROM code** (under DEBUG/TRACE)
   - Why: See what the machine is executing live; pairs with step.
6. **DEBUG/TRACE DUMP**
   - Why: Snapshot state/ROM/program for bug reports and offline analysis.
7. **Registers UI: compact layout**
   - Why: More screen for debug/editor; still see working regs while stepping.

---

## P2 — Editor power features

Authoring and navigation once the calc runs and can be debugged.

8. **Editor: mnemonic or machine + copy/paste**
   - Why: Daily edit loop; dual view + clipboard are baseline for card work.
9. **Card menu: Find**
   - Why: Jump across cards/programs in a growing library.
10. **Find: partial Code search**
    - Why: Locate snippets by opcode/mnemonic fragment, not only full strings.
11. **Editor intellisense** (e.g. `34 01` → `RCL 1`; typing `R` → `RCL 1`)
    - Why: Faster, fewer encoding mistakes between mnemonic and machine.
12. **Code review: infinite-loop detection** (advisory)
    - Why: Catch hung programs before run; advisory only.
13. **Suggest missing code** when incomplete
    - Why: Nudge unfinished sequences toward runnable programs.
14. **Powerful editor: docs + embeddable links**
    - Why: In-place reference while authoring; deeper than tooltips.

---

## P3 — Polish / later

Nice-to-have after the product path above is solid.

15. **Code editor Optimize** (fewer bytes; drop redundant lines)
    - Why: Convenience, not correctness; do after edit/debug basics.
16. **Model profiles: Standard / Max**
    - Why: Standard = normal speed + defined HW; Max = top speed + HW TBD — profile work after core UX.
17. **Help: `?` or Alt+mouse on key → balloon tooltip**
    - Why: Discoverability polish on the faceplate.
18. **Teo logo (bottom-left): hover + click → mini About**
    - Why: Hardware/ROM/version/author info; branding polish, low path priority.
