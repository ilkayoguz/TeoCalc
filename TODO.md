# TeoCalc TODO / backlog

## CodeEncoding (done / remember)

- [x] Card text uses `CodeEncoding` (`mnemonic` | `machine`); legacy `Encoding` accepted on read; single `[Code]` section only (no `[Machine]` / dual program sections).
- Authoring: no faceplate glyphs in `[Code]`; use vocab (`f-1` / `9`, etc.).
- Museum W/PRGM display keycodes ≠ TeoCalc `machine` bytes (decimal internal, e.g. `43`).
- Local leftovers: T-01 tone sink, T-19 printer tests, catalog smoke.

## Editor / DEBUG

1. **DEBUG/TRACE:** VS-like step-by-step program execution; step into / step over.
2. **Editor:** code in mnemonic or machine; paste/copy support.
3. **Registers:** UI can be compacted somewhere.
4. **While calc running:** watch ROM code (likely under DEBUG/TRACE).
5. **Code editor Optimize button:** fewer bytes; remove redundant lines.
6. **DEBUG/TRACE DUMP** option.
7. **Model profiles:** Standard (normal speed + defined hardware), Max (highest speed + hardware TBD).
8. **Card menu:** Find.
9. **Find:** search partial Code.
10. **Editor intellisense:** e.g. `34 01` → show `RCL 1` beside; typing `R` suggests `RCL 1`.
11. **Code review:** infinite-loop detection (advisory).
12. **Suggest missing code** when incomplete.
13. **Powerful editor:** documentation + links embeddable.
14. **Help:** `?` or Alt+mouse on key → balloon tooltip.
15. **Teo logo** bottom-left: hover + click → mini About (hardware, ROM version/size KB, year, author, version, etc.).
