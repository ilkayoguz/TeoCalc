# TeoCalc TODO / backlog

Prioritized by product impact: **correctness / host wiring → debug foundation → Composite Dev Studio (staged) → polish**.

## Done / remember (CodeEncoding & cards)

- [x] Card text uses `CodeEncoding` (`mnemonic` | `machine`); legacy `Encoding` accepted on read; single `[Code]` section only (no `[Machine]` / dual program sections).
- Authoring: no faceplate glyphs in `[Code]`; use vocab (`f-1` / `9`, etc.).
- Museum W/PRGM display keycodes ≠ TeoCalc `machine` bytes (decimal internal, e.g. `43`).
- Card formats (t65/t67, CuveSoft, CodeEncoding) — mostly done; keep as reference, not open work.

---

## P0 — Correctness / host wiring

Ship a trustworthy running emulator before new UX.

1. **Local leftovers: T-01 tone sink** — done
   - Host wires `HostTeo01ToneSink` via `CalcFirmwareBootstrap.Teo01ToneSink`; Core default remains no-op.
2. **Local leftovers: T-19 printer tests** — done
   - Buffer / `op_pik_print`+motor flush covered; `ActCpuBase.InvokeOpcodeAliasForTests` test hook.
3. **Local leftovers: catalog smoke** — done
   - Cross-catalog boot / faceplate / idle / digit smoke; T-01 key settle uses 10ms cadence.

---

## P1 — Debug foundation (shipped)

Make programs inspectable and steppable — core emulator workflow. **Foundation done;** remaining polish folds into Composite Dev Studio below.

4. **DEBUG/TRACE: VS-like step-by-step** — foundation done
   - [x] Break / Continue / Step Into / Step Over on native gateways; faceplate Debug side panel + F5/F9/F10/F11.
   - Deferred polish → Studio epic (call-stack, Step Over edge cases).
5. **While calc running: watch ROM code** — foundation done
   - [x] ROM watch list with Follow ROM (highlights live fetch address while running/stepping).
   - Deferred polish → Studio epic (dedicated live ROM pane, scroll-to-PC).
6. **DEBUG/TRACE DUMP** — foundation done
   - [x] Copy dump + Save to Documents/TeoCalc/Dumps (PC/status/handler/registers).
   - Deferred polish → Studio epic (ROM window / listing in dump; chrome placement).
7. **Registers UI: compact layout** — foundation done
   - [x] Compact A–M (N) hex digests in Debug panel.
   - Deferred polish → Studio epic (editable regs / X-display; composite chrome).

---

## P2 — Composite Dev Studio (epic)

**Vision:** One Visual Studio–like composite surface — not scattered micro-panels. Bidirectional **Code ↔ Flowchart (FC)** (edit either, stay in sync). Prefer **side-by-side** panes (code | FC); stacked (code top / FC bottom) as fallback. Same chrome hosts debug transport, DUMP, regs, and optional **realtime ROM step** watch. **Global execution speed** (up/down) lives high in UI (title bar / transport), not buried in debug-only.

This is large — ship in **staged MVPs**. Do not jump to editable FC or global speed before sync + layout exist.

### Stage A — Listing sync (MVP0)

Shared program model so editor listing and any future FC share one source of truth.

8. **Unified listing model**
   - Why: Sync requires one canonical step list (addr / mnemonic / machine / labels) before dual panes.
   - Deliver: in-memory listing from card/`[Code]`; PC highlight hook reusable by editor + ROM watch.
9. **Editor: mnemonic or machine + copy/paste**
   - Why: Daily edit loop; dual encoding + clipboard are baseline for card work (feeds listing sync).

### Stage B — Side-by-side read-only FC (MVP1)

Layout shell + flowchart as **visualization** of the listing (not yet editable).

10. **Composite chrome shell**
    - Why: One place for editor + FC + debug buttons, DUMP, compact regs — VS-like, not floating scraps.
    - Layout: prefer code | FC side-by-side; stacked alternative; title-bar / global strip reserved for transport + speed (later).
11. **Flowchart pane (read-only)**
    - Why: See control flow next to code without leaving the composite screen.
    - Sync: selecting a listing line highlights FC node (and reverse selection → listing); PC highlight while stepping.
12. **Optional ROM live pane**
    - Why: When opened, follow overall ROM fetch live (extends P1 Follow ROM); not a permanent micro-panel — dockable in composite.

### Stage C — Editable FC ↔ Code (MVP2)

True bidirectional authoring.

13. **Editable flowchart ↔ code sync**
    - Why: Edit structure in FC or text; both stay consistent (insert/delete/reorder steps, branches).
    - Guardrails: conflict policy when both dirty; invalid FC edits surface as editor diagnostics.
14. **Editor intellisense** (e.g. `34 01` → `RCL 1`; typing `R` → `RCL 1`)
    - Why: Faster encoding between mnemonic and machine; pairs with FC node labels.
15. **Card menu: Find** + **partial Code search**
    - Why: Jump across cards; locate opcode/mnemonic fragments in a growing library.
16. **Powerful editor: docs + embeddable links**
    - Why: In-place reference while authoring; deeper than tooltips.

### Stage D — Global speed + debug polish (MVP3)

Transport and remaining P1 polish on the composite chrome.

17. **Global execution speed control**
    - Why: Speed up/down device execution rate; place in title bar / global transport (not debug-only bury).
    - Note: may later tie to Model profiles (Standard / Max) — profile enum stays P3 until speed UX exists.
18. **Debug polish on composite chrome**
    - Call-stack view; richer Step Over edge cases across ISA families.
    - ROM watch: scroll-to-PC smooth sync, larger window, cross-ref tooltips; DUMP includes ROM window / user listing.
    - Registers: editable regs / X-display formatted view in composite strip.

### Stage E — Algorithm assists (after Studio core)

Advisory helpers once Code↔FC loop is real.

19. **Code review: infinite-loop detection** (advisory)
20. **Suggest missing code** when incomplete
21. **Code editor Optimize** (fewer bytes; drop redundant lines)

---

## P3 — Polish / later

Nice-to-have after Composite Dev Studio stages A–D are solid.

22. **Model profiles: Standard / Max**
    - Why: Standard = normal speed + defined HW; Max = top speed + HW TBD — after global speed control lands.
23. **Help: `?` or Alt+mouse on key → balloon tooltip**
    - Why: Discoverability polish on the faceplate.
24. **Teo logo (bottom-left): hover + click → mini About**
    - Why: Hardware/ROM/version/author info; branding polish, low path priority.
